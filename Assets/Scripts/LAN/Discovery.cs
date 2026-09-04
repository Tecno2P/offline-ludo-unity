using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace LudoGame.LAN
{
    [Serializable]
    public class RoomAdvertisement
    {
        public string RoomCode;
        public string HostName;
        public int PlayerCount;
        public int MaxPlayers;
        public string HostIp;
    }

    // Host side: periodically broadcasts "here I am" on the LAN so clients don't need to type an IP.
    public class DiscoveryBroadcaster
    {
        public const int DiscoveryPort = 24828;
        private UdpClient _udp;
        private Thread _thread;
        private volatile bool _running;

        public void Start(Func<RoomAdvertisement> getAdvertisement, int intervalMs = 1000)
        {
            _udp = new UdpClient();
            _udp.EnableBroadcast = true;
            _running = true;
            _thread = new Thread(() =>
            {
                var endpoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
                while (_running)
                {
                    var ad = getAdvertisement();
                    var json = JsonConvert.SerializeObject(ad);
                    var bytes = Encoding.UTF8.GetBytes(json);
                    try { _udp.Send(bytes, bytes.Length, endpoint); } catch (Exception) { /* network hiccup, keep trying */ }
                    Thread.Sleep(intervalMs);
                }
            }) { IsBackground = true };
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            _udp?.Close();
        }
    }

    // Client side: listens for host broadcasts and reports discovered rooms.
    public class DiscoveryListener
    {
        private UdpClient _udp;
        private Thread _thread;
        private volatile bool _running;

        public event Action<RoomAdvertisement> OnRoomFound;

        public void Start()
        {
            _udp = new UdpClient(DiscoveryBroadcaster.DiscoveryPort);
            _running = true;
            _thread = new Thread(() =>
            {
                var remote = new IPEndPoint(IPAddress.Any, 0);
                while (_running)
                {
                    try
                    {
                        var data = _udp.Receive(ref remote);
                        var json = Encoding.UTF8.GetString(data);
                        var ad = JsonConvert.DeserializeObject<RoomAdvertisement>(json);
                        ad.HostIp = remote.Address.ToString();
                        OnRoomFound?.Invoke(ad);
                    }
                    catch (Exception)
                    {
                        // socket closed on Stop(), or a malformed packet - just keep listening
                    }
                }
            }) { IsBackground = true };
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            _udp?.Close();
        }
    }
}
