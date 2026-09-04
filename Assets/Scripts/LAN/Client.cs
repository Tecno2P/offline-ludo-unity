using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace LudoGame.LAN
{
    // Runs on every non-host phone. Never decides game outcomes itself - only sends
    // requests (roll, move) and renders whatever the host broadcasts back.
    public class Client
    {
        private TcpClient _tcp;
        private NetworkStream _stream;
        private Thread _readThread;
        private volatile bool _running;

        public string SessionToken { get; private set; }
        public int PlayerId { get; private set; }
        public int AssignedColor { get; private set; }

        public event Action<NetMessage> OnMessageReceived;
        public event Action OnDisconnected;

        public bool Connect(string hostIp, string playerName, int timeoutMs = 5000)
            => ConnectInternal(hostIp, playerName, existingPlayerId: -1, timeoutMs);

        // Call after a dropped connection to rejoin the same match as the same color -
        // pass the PlayerId this Client had before the disconnect.
        public bool Reconnect(string hostIp, int existingPlayerId, string playerName, int timeoutMs = 5000)
            => ConnectInternal(hostIp, playerName, existingPlayerId, timeoutMs);

        private bool ConnectInternal(string hostIp, string playerName, int existingPlayerId, int timeoutMs)
        {
            try
            {
                _tcp = new TcpClient();
                var result = _tcp.BeginConnect(hostIp, HostServer.Port, null, null);
                if (!result.AsyncWaitHandle.WaitOne(timeoutMs)) return false;
                _tcp.EndConnect(result);

                _stream = _tcp.GetStream();
                _running = true;
                _readThread = new Thread(ReadLoop) { IsBackground = true };
                _readThread.Start();

                var payload = new PlayerJoinPayload { PlayerName = playerName, ExistingPlayerId = existingPlayerId };
                Send(NetMessage.Create(MessageType.ROOM_JOIN, "", 0, JsonConvert.SerializeObject(payload)));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void SendReady() => Send(NetMessage.Create(MessageType.PLAYER_READY, SessionToken, PlayerId, "{}"));

        public void RequestRoll() => Send(NetMessage.Create(MessageType.ROLL_REQUEST, SessionToken, PlayerId, "{}"));

        public void RequestMove(int tokenId, int diceValue)
        {
            var payload = new MoveRequestPayload { TokenId = tokenId, DiceValue = diceValue };
            Send(NetMessage.Create(MessageType.MOVE_REQUEST, SessionToken, PlayerId, JsonConvert.SerializeObject(payload)));
        }

        private void ReadLoop()
        {
            try
            {
                while (_running)
                {
                    var msg = ReadMessage();
                    if (msg == null) break;

                    if (msg.Type == MessageType.ROOM_ACCEPT)
                    {
                        var payload = JsonConvert.DeserializeObject<PlayerJoinPayload>(msg.PayloadJson);
                        PlayerId = payload.PlayerId;
                        AssignedColor = payload.AssignedColor;
                        SessionToken = msg.SessionToken;
                    }

                    OnMessageReceived?.Invoke(msg);
                }
            }
            catch (Exception)
            {
                // dropped connection
            }
            finally
            {
                _running = false;
                OnDisconnected?.Invoke();
            }
        }

        public void Disconnect()
        {
            _running = false;
            _tcp?.Close();
        }

        private void Send(NetMessage msg)
        {
            try
            {
                var json = JsonConvert.SerializeObject(msg);
                var bytes = Encoding.UTF8.GetBytes(json);
                var lengthPrefix = BitConverter.GetBytes(bytes.Length);
                _stream.Write(lengthPrefix, 0, 4);
                _stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception)
            {
                // write failed - read loop will surface the disconnect
            }
        }

        private NetMessage ReadMessage()
        {
            var lengthBuf = ReadExact(4);
            if (lengthBuf == null) return null;
            int length = BitConverter.ToInt32(lengthBuf, 0);
            var payload = ReadExact(length);
            if (payload == null) return null;
            return JsonConvert.DeserializeObject<NetMessage>(Encoding.UTF8.GetString(payload));
        }

        private byte[] ReadExact(int count)
        {
            var buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = _stream.Read(buffer, offset, count - offset);
                if (read <= 0) return null;
                offset += read;
            }
            return buffer;
        }
    }
}
