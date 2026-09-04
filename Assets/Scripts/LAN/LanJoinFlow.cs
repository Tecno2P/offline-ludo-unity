using System;
using System.Collections.Generic;
using LudoGame.LAN;

namespace LudoGame.LAN
{
    // The single entry point a "Join Room" screen should call. Handles both flows from the
    // spec: browsing discovered LAN rooms, and typing a room code/IP directly as a fallback.
    public class LanJoinFlow
    {
        private readonly DiscoveryListener _listener = new DiscoveryListener();
        public Client Client { get; private set; }
        public LanClientSession Session { get; private set; }

        public event Action<RoomAdvertisement> OnRoomDiscovered;

        // Call when the "Join Room" screen opens, to populate a list of nearby rooms.
        public void StartScanning()
        {
            _listener.OnRoomFound += ad => OnRoomDiscovered?.Invoke(ad);
            _listener.Start();
        }

        public void StopScanning() => _listener.Stop();

        // Call when the user taps a discovered room, or after they type a host IP manually
        // (room code alone isn't routable on its own without the IP/broadcast match - the
        // discovered RoomAdvertisement carries HostIp already; a manual-entry UI should ask
        // for the host's LAN IP alongside the code as the spec's documented fallback).
        public bool JoinByIp(string hostIp, string playerName)
        {
            StopScanning();
            Client = new Client();
            bool connected = Client.Connect(hostIp, playerName);
            if (!connected) return false;

            Session = new LanClientSession(Client);
            return true;
        }

        public bool JoinDiscoveredRoom(RoomAdvertisement room, string playerName) => JoinByIp(room.HostIp, playerName);

        // Call after Client.OnDisconnected fires mid-match, to attempt getting the same
        // color/tokens back rather than being treated as a new player.
        public bool Reconnect(string hostIp, int previousPlayerId, string playerName)
        {
            Client = new Client();
            bool connected = Client.Reconnect(hostIp, previousPlayerId, playerName);
            if (!connected) return false;
            Session = new LanClientSession(Client);
            return true;
        }

        public void SendReady() => Client?.SendReady();

        public void Leave()
        {
            StopScanning();
            Client?.Disconnect();
        }
    }
}
