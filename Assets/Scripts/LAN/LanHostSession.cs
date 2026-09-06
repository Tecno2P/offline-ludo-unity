using System;
using LudoGame.Core;
using LudoGame.Gameplay;
using Newtonsoft.Json;

namespace LudoGame.LAN
{
    // Wraps HostServer for the host device's own UI (whether it's the original host, or a
    // client that got promoted to host via migration - Room.LocalPlayerId tells us which
    // roster entry is "us" either way). Lets the host's screen use the exact same
    // ILudoGameSession contract as GameManager (local play) and LanClientSession (joiners).
    public class LanHostSession : ILudoGameSession
    {
        private readonly HostServer _server;

        public GameState State => _server.State;
        public PlayerColor CurrentTurn => _server.State.CurrentTurn;
        public bool IsMyTurn => _server.State != null && CurrentTurn == HostColor;
        public PlayerColor HostColor { get; private set; }

        public event Action<PlayerColor> OnTurnStarted;
        public event Action<DiceRolledArgs> OnDiceRolled;
        public event Action<MoveAppliedArgs> OnMoveApplied;
        public event Action<PlayerColor> OnGameWon;
        public event Action<PlayerColor> OnPlayerDisconnected;
        public event Action<PlayerColor> OnTurnTimedOut;

        private PlayerColor _lastBroadcastTurn;

        public LanHostSession(HostServer server)
        {
            _server = server;
            HostColor = _server.Room.Players.Find(p => p.PlayerId == _server.Room.LocalPlayerId).Color;

            _server.OnBroadcastSent += HandleBroadcast;
            _server.OnPlayerDisconnected += id =>
            {
                foreach (var p in _server.Room.Players)
                    if (p.PlayerId == id) { OnPlayerDisconnected?.Invoke(p.Color); return; }
            };
        }

        public void StartMatch() => _server.StartMatch();

        public void RequestRoll() => _server.RequestRollFromPlayer(_server.Room.LocalPlayerId);
        public void RequestMove(int tokenId) => _server.RequestMoveFromPlayer(_server.Room.LocalPlayerId, tokenId);

        // The host enforces timeouts itself via a background timer in HostServer - nothing
        // for this device's Update() loop to drive.
        public void Tick(float deltaTime) { }

        // Every state change flows through here, whether it originated from the host's own
        // moves or a client's - so this is the single place turning host broadcasts into events.
        private void HandleBroadcast(NetMessage msg)
        {
            switch (msg.Type)
            {
                case MessageType.TURN_START:
                    var turn = JsonConvert.DeserializeObject<PlayerColor>(msg.PayloadJson);
                    if (turn != _lastBroadcastTurn || _lastBroadcastTurn == default)
                    {
                        _lastBroadcastTurn = turn;
                        OnTurnStarted?.Invoke(turn);
                    }
                    break;

                case MessageType.ROLL_RESULT:
                    var rollPayload = JsonConvert.DeserializeObject<RollResultPayload>(msg.PayloadJson);
                    var rollerColor = FindColorByPlayerId(msg.SenderPlayerId);
                    OnDiceRolled?.Invoke(new DiceRolledArgs { Color = rollerColor, Value = rollPayload.DiceValue, ForfeitTurn = rollPayload.ForfeitTurn });
                    break;

                case MessageType.MOVE_RESULT:
                    var movePayload = JsonConvert.DeserializeObject<MoveResultPayload>(msg.PayloadJson);
                    OnMoveApplied?.Invoke(new MoveAppliedArgs
                    {
                        Color = (PlayerColor)movePayload.PlayerColor,
                        TokenId = movePayload.TokenId,
                        NewRelativePosition = movePayload.NewRelativePosition,
                        CapturedOpponent = movePayload.CapturedOpponent,
                        CapturedColor = movePayload.CapturedOpponentColor >= 0 ? (PlayerColor)movePayload.CapturedOpponentColor : default,
                        CapturedTokenId = movePayload.CapturedOpponentTokenId,
                        TokenFinished = movePayload.TokenFinished,
                        ExtraTurn = movePayload.ExtraTurn,
                    });
                    break;

                case MessageType.GAME_END:
                    var winner = JsonConvert.DeserializeObject<PlayerColor>(msg.PayloadJson);
                    OnGameWon?.Invoke(winner);
                    break;

                case MessageType.TURN_TIMEOUT:
                    var timedOutColor = JsonConvert.DeserializeObject<PlayerColor>(msg.PayloadJson);
                    OnTurnTimedOut?.Invoke(timedOutColor);
                    break;
            }
        }

        private PlayerColor FindColorByPlayerId(int playerId)
        {
            foreach (var p in _server.Room.Players)
                if (p.PlayerId == playerId) return p.Color;
            return default;
        }
    }
}
