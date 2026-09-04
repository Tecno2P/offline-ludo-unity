using Newtonsoft.Json;

namespace LudoGame.LAN
{
    // The single entry point a "Create Room" button should call. Wires together HostServer
    // (TCP, authoritative state) and DiscoveryBroadcaster (UDP, so joiners don't need to type
    // an IP) so the UI layer only has to deal with one object.
    public class LanHostFlow
    {
        public HostServer Server { get; }
        public LanHostSession Session { get; }
        private readonly DiscoveryBroadcaster _broadcaster = new DiscoveryBroadcaster();

        public string RoomCode => Server.Room.RoomCode;

        public LanHostFlow(string hostName, int maxPlayers)
        {
            Server = new HostServer(hostName, maxPlayers);
            Session = new LanHostSession(Server);
        }

        // Call when the user taps "Create Room". Starts listening for clients AND starts
        // advertising the room over LAN broadcast so joiners see it without typing an IP.
        public void OpenRoom()
        {
            Server.Start();
            _broadcaster.Start(() => new RoomAdvertisement
            {
                RoomCode = Server.Room.RoomCode,
                HostName = Server.Room.HostName,
                PlayerCount = Server.Room.Players.Count,
                MaxPlayers = Server.Room.MaxPlayers,
            });
        }

        // Call once all slots show ready, or let PLAYER_READY auto-start via HostServer's
        // own AllReady check - either is fine, this is just the manual "Start Game" button path.
        public void StartMatch()
        {
            _broadcaster.Stop(); // stop advertising once the room is playing - it's no longer joinable
            Session.StartMatch();
        }

        public void CloseRoom()
        {
            _broadcaster.Stop();
            Server.Stop();
        }
    }
}
