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
        public System.Net.Sockets.TcpClient Socket; // null for the host's own local "player"
    }

    public class RoomManager
    {
        public string RoomCode { get; }
        public string SessionToken { get; }
        public string HostName { get; }
        public int MaxPlayers { get; }

        private readonly List<ConnectedPlayer> _players = new List<ConnectedPlayer>();
        private readonly PlayerColor[] _colorPool = { PlayerColor.Red, PlayerColor.Green, PlayerColor.Yellow, PlayerColor.Blue };
        private int _nextPlayerId = 1;

        public IReadOnlyList<ConnectedPlayer> Players => _players;

        public RoomManager(string hostName, int maxPlayers = 4)
        {
            HostName = hostName;
            MaxPlayers = Math.Clamp(maxPlayers, 2, 4);
            RoomCode = GenerateRoomCode();
            SessionToken = Guid.NewGuid().ToString("N");

            // The host is a player too (playerId 0, no socket - it never talks to itself over TCP).
            _players.Add(new ConnectedPlayer
            {
                PlayerId = 0,
                Name = hostName,
                Color = _colorPool[0],
                Ready = true,
                Socket = null,
            });
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

            var color = _colorPool[_players.Count];
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

        public bool AllReady => _players.Count >= 2 && _players.All(p => p.Ready);
    }
}
