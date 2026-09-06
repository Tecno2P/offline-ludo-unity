using System;
using System.Collections.Generic;
using System.Linq;
using LudoGame.Core;

namespace LudoGame.LAN
{
    public class ConnectedPlayer
    {
        public int PlayerId;
        public string Name;
        public PlayerColor Color;
        public bool Ready;
        public bool Connected = true;
        public System.Net.Sockets.TcpClient Socket; // null for whichever player is the local host
    }

    public class RoomManager
    {
        public string RoomCode { get; private set; }
        public string SessionToken { get; private set; }
        public string HostName { get; private set; }
        public int MaxPlayers { get; private set; }

        // The PlayerId that runs on THIS device with no socket (normally 0 for a freshly
        // created room, but a promoted host during migration keeps whatever id it already had).
        public int LocalPlayerId { get; private set; }

        private readonly List<ConnectedPlayer> _players = new List<ConnectedPlayer>();
        private static readonly PlayerColor[] ColorPool = { PlayerColor.Red, PlayerColor.Green, PlayerColor.Yellow, PlayerColor.Blue };
        private int _nextPlayerId = 1;

        public IReadOnlyList<ConnectedPlayer> Players => _players;

        public RoomManager(string hostName, int maxPlayers = 4)
        {
            HostName = hostName;
            MaxPlayers = Math.Clamp(maxPlayers, 2, 4);
            RoomCode = GenerateRoomCode();
            SessionToken = Guid.NewGuid().ToString("N");
            LocalPlayerId = 0;

            // The host is a player too (playerId 0, no socket - it never talks to itself over TCP).
            _players.Add(new ConnectedPlayer
            {
                PlayerId = 0,
                Name = hostName,
                Color = ColorPool[0],
                Ready = true,
                Socket = null,
            });
        }

        // Private - used only by ResumeForMigration, which fully populates every field itself.
        private RoomManager() { }

        // Rebuilds a RoomManager for a client that's being promoted to host mid-match after
        // the original host disconnected. Keeps the same room code/session token/roster so
        // reconnecting clients recognize this as "the same room", just now hosted elsewhere.
        public static RoomManager ResumeForMigration(string roomCode, string sessionToken, int maxPlayers,
            IEnumerable<ConnectedPlayer> existingRoster, int promotedLocalPlayerId)
        {
            var room = new RoomManager
            {
                RoomCode = roomCode,
                SessionToken = sessionToken,
                MaxPlayers = maxPlayers,
                LocalPlayerId = promotedLocalPlayerId,
            };

            foreach (var p in existingRoster)
            {
                var copy = new ConnectedPlayer
                {
                    PlayerId = p.PlayerId,
                    Name = p.Name,
                    Color = p.Color,
                    Ready = true,
                    // Every socket is stale after a host change - the local player gets none
                    // (it's the host now), everyone else must TCP-reconnect to pick theirs back up.
                    Socket = null,
                    Connected = p.PlayerId == promotedLocalPlayerId,
                };
                room._players.Add(copy);
                if (copy.PlayerId == promotedLocalPlayerId) room.HostName = copy.Name;
            }

            // Everyone who isn't the promoted host starts marked disconnected until they
            // actually reconnect over TCP to the new host.
            foreach (var p in room._players)
                if (p.PlayerId != promotedLocalPlayerId) p.Connected = false;

            room._nextPlayerId = room._players.Count == 0 ? 1 : room._players.Max(p => p.PlayerId) + 1;
            return room;
        }

        private static string GenerateRoomCode()
        {
            var rng = new Random();
            return rng.Next(1000, 9999).ToString();
        }

        public bool IsFull => _players.Count >= MaxPlayers;

        public ConnectedPlayer AddPlayer(string name, System.Net.Sockets.TcpClient socket)
        {
            if (IsFull) return null;
            if (_players.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && p.Connected))
                return null; // duplicate-name prevention while that player is still connected

            var color = ColorPool[_players.Count];
            var player = new ConnectedPlayer
            {
                PlayerId = _nextPlayerId++,
                Name = name,
                Color = color,
                Socket = socket,
            };
            _players.Add(player);
            return player;
        }

        public void MarkDisconnected(int playerId)
        {
            var p = _players.FirstOrDefault(x => x.PlayerId == playerId);
            if (p != null) p.Connected = false;
        }

        public bool TryReconnect(int playerId, System.Net.Sockets.TcpClient socket)
        {
            var p = _players.FirstOrDefault(x => x.PlayerId == playerId);
            if (p == null) return false;
            p.Socket = socket;
            p.Connected = true;
            return true;
        }

        public RosterPayload BuildRoster()
        {
            var roster = new RosterPayload();
            foreach (var p in _players)
            {
                roster.Players.Add(new RosterEntry
                {
                    PlayerId = p.PlayerId,
                    PlayerName = p.Name,
                    Color = (int)p.Color,
                    Connected = p.Connected,
                });
            }
            return roster;
        }

        public bool AllReady => _players.Count >= 2 && _players.All(p => p.Ready);
    }
}
