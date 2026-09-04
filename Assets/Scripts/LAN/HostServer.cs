using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using LudoGame.Core;
using Newtonsoft.Json; // Unity package: com.unity.nuget.newtonsoft-json

namespace LudoGame.LAN
{
    // Runs only on the host device. Owns the authoritative GameState, RulesSystem and turn
    // order. Every gameplay message - whether it arrived over TCP from a client, or was called
    // directly by the host's own local UI (LanHostSession) - passes through the same
    // Request*FromPlayer methods, so there is exactly one code path that can mutate state.
    public class HostServer
    {
        public const int Port = 24827; // arbitrary fixed port for LAN play
        private TcpListener _listener;
        private Thread _acceptThread;
        private volatile bool _running;

        public RoomManager Room { get; }
        public GameState State { get; private set; }
        private RulesSystem _rules;
        private TurnSystem _turns;
        private readonly DiceSystem _dice = new DiceSystem();
        private readonly object _stateLock = new object();

        public event Action<ConnectedPlayer> OnPlayerJoined;
        public event Action<int> OnPlayerDisconnected;
        public event Action<NetMessage> OnBroadcastSent; // lets the host's own UI mirror every state change

        public HostServer(string hostName, int maxPlayers)
        {
            Room = new RoomManager(hostName, maxPlayers);
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            _running = true;
            _acceptThread = new Thread(AcceptLoop) { IsBackground = true };
            _acceptThread.Start();
        }

        public void Stop()
        {
            _running = false;
            _listener?.Stop();
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                TcpClient client;
                try { client = _listener.AcceptTcpClient(); }
                catch (SocketException) { break; } // listener stopped

                var thread = new Thread(() => HandleClient(client)) { IsBackground = true };
                thread.Start();
            }
        }

        private void HandleClient(TcpClient client)
        {
            var stream = client.GetStream();
            try
            {
                while (_running && client.Connected)
                {
                    var msg = ReadMessage(stream);
                    if (msg == null) break; // disconnected
                    HandleMessage(msg, client);
                }
            }
            catch (Exception)
            {
                // connection dropped - fall through to disconnect handling
            }
            finally
            {
                HandleDisconnect(client);
            }
        }

        private void HandleMessage(NetMessage msg, TcpClient client)
        {
            // Reject anything not meant for this room, or from a protocol version we don't understand.
            if (msg.ProtocolVersion != ProtocolVersion.Current) return;
            if (msg.Type != MessageType.ROOM_JOIN && msg.SessionToken != Room.SessionToken) return;

            switch (msg.Type)
            {
                case MessageType.ROOM_JOIN:
                    HandleJoin(msg, client);
                    break;

                case MessageType.PLAYER_READY:
                    HandleReady(msg);
                    break;

                case MessageType.ROLL_REQUEST:
                    RequestRollFromPlayer(msg.SenderPlayerId);
                    break;

                case MessageType.MOVE_REQUEST:
                    var req = JsonConvert.DeserializeObject<MoveRequestPayload>(msg.PayloadJson ?? "{}");
                    RequestMoveFromPlayer(msg.SenderPlayerId, req.TokenId);
                    break;

                case MessageType.HEARTBEAT:
                    break; // keep-alive, no action needed

                default:
                    // Unknown/out-of-order message for host to receive - ignore rather than crash.
                    break;
            }
        }

        private void HandleJoin(NetMessage msg, TcpClient client)
        {
            var payload = JsonConvert.DeserializeObject<PlayerJoinPayload>(msg.PayloadJson ?? "{}");

            if (State != null)
            {
                // Match already in progress - only a returning player (known playerId, previously
                // marked disconnected) may rejoin. A genuinely new join is rejected outright.
                if (payload != null && payload.ExistingPlayerId >= 0 && HandleReconnect(payload.ExistingPlayerId, client))
                {
                    var rejoined = Room.Players.Find(p => p.PlayerId == payload.ExistingPlayerId);
                    var acceptRejoin = new PlayerJoinPayload
                    {
                        PlayerId = rejoined.PlayerId,
                        PlayerName = rejoined.Name,
                        AssignedColor = (int)rejoined.Color,
                    };
                    Send(client, NetMessage.Create(MessageType.ROOM_ACCEPT, Room.SessionToken, 0, JsonConvert.SerializeObject(acceptRejoin)));
                }
                else
                {
                    Send(client, NetMessage.Create(MessageType.ROOM_REJECT, "", 0, "{}"));
                }
                return;
            }

            var player = Room.AddPlayer(payload?.PlayerName ?? "Player", client);

            if (player == null)
            {
                Send(client, NetMessage.Create(MessageType.ROOM_REJECT, "", 0, "{}"));
                return;
            }

            var accept = new PlayerJoinPayload
            {
                PlayerId = player.PlayerId,
                PlayerName = player.Name,
                AssignedColor = (int)player.Color,
            };
            Send(client, NetMessage.Create(MessageType.ROOM_ACCEPT, Room.SessionToken, 0, JsonConvert.SerializeObject(accept)));
            OnPlayerJoined?.Invoke(player);
        }

        private void HandleReady(NetMessage msg)
        {
            var player = FindPlayer(msg.SenderPlayerId);
            if (player != null) player.Ready = true;

            if (Room.AllReady && State == null)
                StartMatch();
        }

        public void StartMatch()
        {
            var order = Room.Players.Select(p => p.Color).ToList();

            lock (_stateLock)
            {
                State = GameState.CreateNew(Guid.NewGuid().ToString("N"), order);
                _rules = new RulesSystem(State);
                _turns = new TurnSystem(order);
                State.CurrentTurn = _turns.CurrentPlayer;
            }

            Broadcast(NetMessage.Create(MessageType.GAME_START, Room.SessionToken, 0, JsonConvert.SerializeObject(State)));
            BroadcastTurnStart();
        }

        private void BroadcastTurnStart()
        {
            Broadcast(NetMessage.Create(MessageType.TURN_START, Room.SessionToken, 0, JsonConvert.SerializeObject(State.CurrentTurn)));
        }

        // Called for both socket-originated rolls (via HandleMessage) and the host's own
        // local player rolling (via LanHostSession) - the one and only path into dice logic.
        public void RequestRollFromPlayer(int playerId)
        {
            var player = FindPlayer(playerId);
            if (player == null || State == null) return;
            if (player.Color != State.CurrentTurn) return; // not their turn - ignore silently

            (int value, bool forfeit) roll;
            lock (_stateLock)
            {
                roll = _dice.Roll();
                State.LastDiceValue = roll.value;
            }

            var payload = new RollResultPayload { DiceValue = roll.value, ForfeitTurn = roll.forfeit };
            Broadcast(NetMessage.Create(MessageType.ROLL_RESULT, Room.SessionToken, playerId, JsonConvert.SerializeObject(payload)));

            if (roll.forfeit)
            {
                AdvanceTurnAndBroadcast(extraTurn: false);
                return;
            }

            var legal = _rules.GetLegalMoves(player.Color, roll.value);
            if (legal.Count == 0)
            {
                // No legal move available on this roll - pass the turn.
                AdvanceTurnAndBroadcast(extraTurn: false);
            }
        }

        // Same one-path principle as RequestRollFromPlayer.
        public void RequestMoveFromPlayer(int playerId, int tokenId)
        {
            var player = FindPlayer(playerId);
            if (player == null || State == null) return;
            if (player.Color != State.CurrentTurn) return;

            MoveResult result;
            lock (_stateLock)
            {
                result = _rules.ApplyMove(player.Color, tokenId, State.LastDiceValue);
            }

            if (!result.Success) return; // illegal move request - drop it, don't trust the sender

            var payload = new MoveResultPayload
            {
                PlayerColor = (int)player.Color,
                TokenId = tokenId,
                NewRelativePosition = State.GetTokens(player.Color).Find(t => t.TokenId == tokenId).RelativePosition,
                CapturedOpponent = result.CapturedOpponent,
                CapturedOpponentColor = result.CapturedOpponent ? (int)result.CapturedColor : -1,
                CapturedOpponentTokenId = result.CapturedTokenId,
                TokenFinished = result.TokenFinished,
                ExtraTurn = result.ExtraTurn,
            };
            Broadcast(NetMessage.Create(MessageType.MOVE_RESULT, Room.SessionToken, playerId, JsonConvert.SerializeObject(payload)));

            var winner = _rules.CheckWinner();
            if (winner.HasValue)
            {
                State.Winner = winner;
                Broadcast(NetMessage.Create(MessageType.GAME_END, Room.SessionToken, 0, JsonConvert.SerializeObject(winner.Value)));
                return;
            }

            AdvanceTurnAndBroadcast(result.ExtraTurn);
        }

        private void AdvanceTurnAndBroadcast(bool extraTurn)
        {
            if (!extraTurn) _turns.AdvanceTurn(extraTurn: false);
            State.CurrentTurn = _turns.CurrentPlayer;
            BroadcastTurnStart();
        }

        private void HandleDisconnect(TcpClient client)
        {
            foreach (var p in Room.Players)
            {
                if (p.Socket == client)
                {
                    Room.MarkDisconnected(p.PlayerId);
                    Broadcast(NetMessage.Create(MessageType.PLAYER_DISCONNECT, Room.SessionToken, p.PlayerId, "{}"));
                    OnPlayerDisconnected?.Invoke(p.PlayerId);
                    break;
                }
            }
        }

        // Called when a previously-dropped client reconnects with the same playerId - keeps
        // their color/tokens intact rather than treating them as a brand-new player.
        public bool HandleReconnect(int playerId, TcpClient newSocket)
        {
            bool ok = Room.TryReconnect(playerId, newSocket);
            if (ok)
            {
                Broadcast(NetMessage.Create(MessageType.PLAYER_RECONNECT, Room.SessionToken, playerId,
                    JsonConvert.SerializeObject(State)));
            }
            return ok;
        }

        private ConnectedPlayer FindPlayer(int playerId)
        {
            foreach (var p in Room.Players) if (p.PlayerId == playerId) return p;
            return null;
        }

        private void Broadcast(NetMessage msg)
        {
            foreach (var p in Room.Players)
            {
                if (p.Connected && p.Socket != null) Send(p.Socket, msg);
            }
            OnBroadcastSent?.Invoke(msg); // host's own UI (LanHostSession) listens here
        }

        private static void Send(TcpClient client, NetMessage msg)
        {
            try
            {
                var json = JsonConvert.SerializeObject(msg);
                var bytes = Encoding.UTF8.GetBytes(json);
                var lengthPrefix = BitConverter.GetBytes(bytes.Length);
                var stream = client.GetStream();
                stream.Write(lengthPrefix, 0, 4);
                stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception)
            {
                // socket write failed - the accept/handle loop's disconnect handling will catch it
            }
        }

        private static NetMessage ReadMessage(NetworkStream stream)
        {
            var lengthBuf = ReadExact(stream, 4);
            if (lengthBuf == null) return null;
            int length = BitConverter.ToInt32(lengthBuf, 0);
            var payload = ReadExact(stream, length);
            if (payload == null) return null;
            var json = Encoding.UTF8.GetString(payload);
            return JsonConvert.DeserializeObject<NetMessage>(json);
        }

        private static byte[] ReadExact(NetworkStream stream, int count)
        {
            var buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read <= 0) return null;
                offset += read;
            }
            return buffer;
        }
    }
}
