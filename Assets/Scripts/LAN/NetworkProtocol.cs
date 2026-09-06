using System;
using System.Collections.Generic;

namespace LudoGame.LAN
{
    // Versioned so a future client build can detect and refuse to talk to an incompatible host.
    public static class ProtocolVersion
    {
        public const int Current = 1;
    }

    public enum MessageType
    {
        ROOM_CREATE,
        ROOM_DISCOVER,
        ROOM_JOIN,
        ROOM_ACCEPT,
        ROOM_REJECT,
        PLAYER_READY,
        GAME_START,
        TURN_START,
        ROLL_REQUEST,
        ROLL_RESULT,
        MOVE_REQUEST,
        MOVE_RESULT,
        CAPTURE_EVENT,
        TOKEN_FINISH,
        PLAYER_DISCONNECT,
        PLAYER_RECONNECT,
        GAME_END,
        HEARTBEAT,
        TURN_TIMEOUT,
        ROSTER_UPDATE,
    }

    [Serializable]
    public class NetMessage
    {
        public int ProtocolVersion = LudoGame.LAN.ProtocolVersion.Current;
        public MessageType Type;
        public string SessionToken;   // per-room auth token, prevents cross-room bleed
        public int SenderPlayerId;
        public string PayloadJson;    // JSON-serialized payload specific to Type
        public long Timestamp;

        public static NetMessage Create(MessageType type, string sessionToken, int senderId, string payloadJson)
        {
            return new NetMessage
            {
                Type = type,
                SessionToken = sessionToken,
                SenderPlayerId = senderId,
                PayloadJson = payloadJson,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };
        }
    }

    [Serializable]
    public class RollResultPayload
    {
        public int DiceValue;
        public bool ForfeitTurn;
    }

    [Serializable]
    public class MoveRequestPayload
    {
        public int TokenId;
        public int DiceValue;
    }

    [Serializable]
    public class MoveResultPayload
    {
        public int PlayerColor;
        public int TokenId;
        public int NewRelativePosition;
        public bool CapturedOpponent;
        public int CapturedOpponentColor = -1;
        public int CapturedOpponentTokenId = -1;
        public bool TokenFinished;
        public bool ExtraTurn;
    }

    [Serializable]
    public class PlayerJoinPayload
    {
        public int PlayerId;
        public string PlayerName;
        public int AssignedColor;
        // Set by a client that previously had this PlayerId, so the host treats this as a
        // reconnect (keep their color/tokens) rather than a brand-new join.
        public int ExistingPlayerId = -1;
    }

    [Serializable]
    public class RosterEntry
    {
        public int PlayerId;
        public string PlayerName;
        public int Color;
        public bool Connected;
    }

    [Serializable]
    public class RosterPayload
    {
        public List<RosterEntry> Players = new List<RosterEntry>();
    }
}
