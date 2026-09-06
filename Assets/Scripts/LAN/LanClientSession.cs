using System;
using System.Collections.Generic;
using LudoGame.Core;
using LudoGame.Gameplay;
using Newtonsoft.Json;

namespace LudoGame.LAN
{
    // Wraps Client for a joining device. Never decides outcomes itself - every roll/move is a
    // request to the host, and every event fired here is a translation of what the host sent back.
    public class LanClientSession : ILudoGameSession
    {
        private readonly Client _client;
        public GameState State { get; private set; }
        public PlayerColor CurrentTurn { get; private set; }
        public PlayerColor LocalColor => (PlayerColor)_client.AssignedColor;
        public int LocalPlayerId => _client.PlayerId;
        public bool IsMyTurn => CurrentTurn == LocalColor;

        // Kept in sync via ROSTER_UPDATE so this client always knows the full player list
        // (id, name, color, connected) - this is what makes host migration possible: every
        // client can independently work out who should become the new host.
        public List<RosterEntry> Roster { get; private set; } = new List<RosterEntry>();
        public string RoomCode { get; set; } // set by LanJoinFlow when known (discovered join); may stay null for manual-IP joins
        public string SessionToken { get; private set; }

        public event Action<PlayerColor> OnTurnStarted;
        public event Action<DiceRolledArgs> OnDiceRolled;
        public event Action<MoveAppliedArgs> OnMoveApplied;
        public event Action<PlayerColor> OnGameWon;
        public event Action<PlayerColor> OnPlayerDisconnected;
        public event Action<PlayerColor> OnTurnTimedOut;
        public event Action OnConnectionLost; // no ILudoGameSession equivalent - LAN-specific, UI can subscribe if it cares
        public event Action OnRosterUpdated;

        public LanClientSession(Client client)
        {
            _client = client;
            _client.OnMessageReceived += HandleMessage;
            _client.OnDisconnected += () => OnConnectionLost?.Invoke();
        }

        public void RequestRoll() => _client.RequestRoll();
        public void RequestMove(int tokenId) => _client.RequestMove(tokenId, State.LastDiceValue);

        // No local timer to run - the host enforces timeouts and simply advances the turn,
        // which arrives here as an ordinary TURN_START.
        public void Tick(float deltaTime) { }

        // Whoever holds the lowest PlayerId among players the roster still shows as
        // "supposed to be here" (i.e. everyone except the host, playerId 0, who we already
        // know just dropped) is the agreed-upon next host - every surviving client computes
        // this the same way independently, so no extra negotiation round-trip is needed.
        public bool AmINextHost()
        {
            int? lowestOtherId = null;
            foreach (var entry in Roster)
            {
                if (entry.PlayerId == 0) continue; // the old host - never a migration candidate
                if (lowestOtherId == null || entry.PlayerId < lowestOtherId) lowestOtherId = entry.PlayerId;
            }
            return lowestOtherId.HasValue && lowestOtherId.Value == LocalPlayerId;
        }

        private void HandleMessage(NetMessage msg)
        {
            switch (msg.Type)
            {
                case MessageType.GAME_START:
                    State = JsonConvert.DeserializeObject<GameState>(msg.PayloadJson);
                    CurrentTurn = State.CurrentTurn;
                    SessionToken = _client.SessionToken;
                    break;

                case MessageType.TURN_START:
                    CurrentTurn = JsonConvert.DeserializeObject<PlayerColor>(msg.PayloadJson);
                    if (State != null) State.CurrentTurn = CurrentTurn;
                    OnTurnStarted?.Invoke(CurrentTurn);
                    break;

                case MessageType.ROLL_RESULT:
                    var rollPayload = JsonConvert.DeserializeObject<RollResultPayload>(msg.PayloadJson);
                    if (State != null) State.LastDiceValue = rollPayload.DiceValue;
                    OnDiceRolled?.Invoke(new DiceRolledArgs { Color = CurrentTurn, Value = rollPayload.DiceValue, ForfeitTurn = rollPayload.ForfeitTurn });
                    break;

                case MessageType.MOVE_RESULT:
                    var movePayload = JsonConvert.DeserializeObject<MoveResultPayload>(msg.PayloadJson);
                    ApplyRemoteMoveToLocalState(movePayload);
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
                    if (State != null) State.Winner = winner;
                    OnGameWon?.Invoke(winner);
                    break;

                case MessageType.PLAYER_DISCONNECT:
                    OnPlayerDisconnected?.Invoke(FindColorFromRoster(msg.SenderPlayerId));
                    break;

                case MessageType.PLAYER_RECONNECT:
                    // Host resends full state on reconnect - resync entirely rather than
                    // trying to patch in whatever we missed while disconnected.
                    State = JsonConvert.DeserializeObject<GameState>(msg.PayloadJson);
                    CurrentTurn = State.CurrentTurn;
                    break;

                case MessageType.TURN_TIMEOUT:
                    var timedOutColor = JsonConvert.DeserializeObject<PlayerColor>(msg.PayloadJson);
                    OnTurnTimedOut?.Invoke(timedOutColor);
                    break;

                case MessageType.ROSTER_UPDATE:
                    var roster = JsonConvert.DeserializeObject<RosterPayload>(msg.PayloadJson);
                    Roster = roster.Players;
                    OnRosterUpdated?.Invoke();
                    break;
            }
        }

        // Client mirrors the host's authoritative result into its own local copy of GameState
        // purely for rendering - it never uses this to independently decide anything.
        private void ApplyRemoteMoveToLocalState(MoveResultPayload payload)
        {
            if (State == null) return;
            var color = (PlayerColor)payload.PlayerColor;
            var token = State.GetTokens(color).Find(t => t.TokenId == payload.TokenId);
            if (token != null) token.RelativePosition = payload.NewRelativePosition;

            if (payload.CapturedOpponent && payload.CapturedOpponentColor >= 0)
            {
                var oppColor = (PlayerColor)payload.CapturedOpponentColor;
                var oppToken = State.GetTokens(oppColor).Find(t => t.TokenId == payload.CapturedOpponentTokenId);
                if (oppToken != null) oppToken.SendHome();
            }
        }

        private PlayerColor FindColorFromRoster(int playerId)
        {
            foreach (var entry in Roster)
                if (entry.PlayerId == playerId) return (PlayerColor)entry.Color;
            return CurrentTurn; // roster hasn't arrived yet (shouldn't normally happen) - fall back rather than throw
        }
    }
}
