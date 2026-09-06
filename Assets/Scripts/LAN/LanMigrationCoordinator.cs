using System;
using System.Collections.Generic;
using System.Linq;
using LudoGame.Core;

namespace LudoGame.LAN
{
    // Attach this to a client session once a match is underway. If the host disconnects
    // mid-match, every surviving client independently computes the same next-host candidate
    // (lowest remaining PlayerId) via LanClientSession.AmINextHost() - no extra negotiation
    // round-trip needed. The chosen client spins up its own HostServer pre-loaded with the
    // last-known GameState and re-advertises the SAME room code; everyone else scans for that
    // code and reconnects with their existing PlayerId to resume with color/tokens intact.
    public class LanMigrationCoordinator
    {
        private readonly LanClientSession _oldSession;
        private readonly string _playerName;
        private DiscoveryListener _rescueScanner;

        public event Action<LanHostSession> PromotedToHost;
        public event Action<LanClientSession> ReconnectedAsClient;
        public event Action MigrationFailed;

        public LanMigrationCoordinator(LanClientSession session, string playerName)
        {
            _oldSession = session;
            _playerName = playerName;
            _oldSession.OnConnectionLost += HandleHostLost;
        }

        private void HandleHostLost()
        {
            // Only a mid-match disconnect is a migration event - a lost connection during the
            // lobby (before GAME_START) is just an ordinary failed/aborted join.
            if (_oldSession.State == null) return;

            if (_oldSession.AmINextHost())
                BecomeHost();
            else
                WaitAndReconnectToNewHost();
        }

        private void BecomeHost()
        {
            var roster = _oldSession.Roster
                .Where(r => r.PlayerId != 0) // the old host isn't part of the resumed roster
                .Select(r => new ConnectedPlayer
                {
                    PlayerId = r.PlayerId,
                    Name = r.PlayerName,
                    Color = (PlayerColor)r.Color,
                })
                .ToList();

            string roomCode = _oldSession.RoomCode ?? new Random().Next(1000, 9999).ToString();
            string sessionToken = _oldSession.SessionToken ?? Guid.NewGuid().ToString("N");

            var resumedRoom = RoomManager.ResumeForMigration(roomCode, sessionToken, maxPlayers: 4,
                roster, promotedLocalPlayerId: _oldSession.LocalPlayerId);

            var newServer = new HostServer(resumedRoom, _oldSession.State);
            newServer.Start();

            var broadcaster = new DiscoveryBroadcaster();
            broadcaster.Start(() => new RoomAdvertisement
            {
                RoomCode = resumedRoom.RoomCode,
                HostName = resumedRoom.HostName,
                PlayerCount = resumedRoom.Players.Count(p => p.Connected),
                MaxPlayers = resumedRoom.MaxPlayers,
            });
            // Stop re-advertising once the rescue window has passed - everyone who was going
            // to reconnect should have found this room well within 20 seconds.
            new System.Threading.Timer(_ => broadcaster.Stop(), null, 20000, System.Threading.Timeout.Infinite);

            var newHostSession = new LanHostSession(newServer);
            PromotedToHost?.Invoke(newHostSession);
        }

        private void WaitAndReconnectToNewHost()
        {
            _rescueScanner = new DiscoveryListener();
            var targetRoomCode = _oldSession.RoomCode;
            int myPlayerId = _oldSession.LocalPlayerId;

            _rescueScanner.OnRoomFound += ad =>
            {
                // If we never learned the room code (manual-IP join), accept the first room
                // that appears during a rescue scan - there should only be one mid-migration.
                if (targetRoomCode != null && ad.RoomCode != targetRoomCode) return;

                _rescueScanner.Stop();

                var newClient = new Client();
                bool ok = newClient.Reconnect(ad.HostIp, myPlayerId, _playerName);
                if (!ok) { MigrationFailed?.Invoke(); return; }

                var newSession = new LanClientSession(newClient) { RoomCode = ad.RoomCode };
                ReconnectedAsClient?.Invoke(newSession);
            };
            _rescueScanner.Start();
        }
    }
}
