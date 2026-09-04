using System;
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
        public bool IsMyTurn => CurrentTurn == LocalColor;

        public event Action<PlayerColor> OnTurnStarted;
        public event Action<DiceRolledArgs> OnDiceRolled;
        public event Action<MoveAppliedArgs> OnMoveApplied;
        public event Action<PlayerColor> OnGameWon;
        public event Action<PlayerColor> OnPlayerDisconnected;
        public event Action OnConnectionLost; // no ILudoGameSession equivalent - LAN-specific, UI can subscribe if it cares

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

        private void HandleMessage(NetMessage msg)
        {
            switch (msg.Type)
            {
                case MessageType.GAME_START:
                    State = JsonConvert.DeserializeObject<GameState>(msg.PayloadJson);
                    CurrentTurn = State.CurrentTurn;
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
                    OnPlayerDisconnected?.Invoke(FindColorFromLocalState(msg.SenderPlayerId));
                    break;

                case MessageType.PLAYER_RECONNECT:
                    // Host resends full state on reconnect - resync entirely rather than
                    // trying to patch in whatever we missed while disconnected.
                    State = JsonConvert.DeserializeObject<GameState>(msg.PayloadJson);
                    CurrentTurn = State.CurrentTurn;
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

        private PlayerColor FindColorFromLocalState(int playerId)
        {
            // Client doesn't keep a player-id-to-color roster (only the host does) - this is a
            // best-effort fallback for UI notifications, not used for any rules decision.
            return CurrentTurn;
        }
    }
}
