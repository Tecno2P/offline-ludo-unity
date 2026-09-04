using System;
using System.Collections.Generic;
using System.Linq;
using LudoGame.Core;
using LudoGame.Core.AI;

namespace LudoGame.Gameplay
{
    public enum MatchMode { VsAI, LocalMultiplayer }

    public class PlayerSlot
    {
        public PlayerColor Color;
        public bool IsAI;
        public AIDifficulty Difficulty;
        public string DisplayName;
    }

    // Drives ONE device's local game loop (VS AI or pass-and-play). Implements the same
    // ILudoGameSession contract as the LAN sessions so UI code never branches on game mode.
    public class GameManager : ILudoGameSession
    {
        public GameState State { get; private set; }
        public MatchMode Mode { get; }
        private readonly List<PlayerSlot> _slots;
        private readonly RulesSystem _rules;
        private readonly TurnSystem _turns;
        private readonly DiceSystem _dice;
        private readonly Dictionary<PlayerColor, AIController> _aiControllers = new Dictionary<PlayerColor, AIController>();

        public PlayerColor CurrentTurn => State.CurrentTurn;
        public bool IsMyTurn => !IsAI(CurrentTurn); // pass-and-play: true whenever a human owns this turn

        // Turn timeout: if a human doesn't act within this window, auto-skip so the match
        // never hard-stalls on an away player (spec item 11: "turn timeout").
        public float TurnTimeoutSeconds = 30f;
        private float _turnTimer;
        private List<Token> _pendingLegalMoves;

        public event Action<PlayerColor> OnTurnStarted;
        public event Action<DiceRolledArgs> OnDiceRolled;
        public event Action<MoveAppliedArgs> OnMoveApplied;
        public event Action<PlayerColor> OnGameWon;
        public event Action<PlayerColor> OnPlayerDisconnected; // never fires locally - present to satisfy the interface
        public event Action<PlayerColor> OnTurnTimedOut;

        public GameManager(MatchMode mode, List<PlayerSlot> slots, int? diceSeed = null)
            : this(mode, slots, GameState.CreateNew(Guid.NewGuid().ToString("N"), slots.Select(s => s.Color).ToList()), diceSeed)
        {
        }

        // Used by MatchSaveSystem.Resume() to rebuild a GameManager from a saved in-progress
        // match, resuming from whichever player's turn it was rather than restarting at slot 0.
        public GameManager(MatchMode mode, List<PlayerSlot> slots, GameState resumedState, int? diceSeed = null)
        {
            Mode = mode;
            _slots = slots;
            State = resumedState;
            _rules = new RulesSystem(State);

            var order = slots.Select(s => s.Color).ToList();
            _turns = new TurnSystem(order);
            // Rotate the fresh TurnSystem forward until it lines up with the resumed CurrentTurn,
            // since TurnSystem itself doesn't serialize (it's rebuilt each session).
            int safety = order.Count;
            while (_turns.CurrentPlayer != State.CurrentTurn && safety-- > 0)
                _turns.AdvanceTurn(extraTurn: false);

            _dice = new DiceSystem(diceSeed);

            foreach (var slot in slots.Where(s => s.IsAI))
                _aiControllers[slot.Color] = new AIController(State, slot.Difficulty);
        }

        // Call after construction to persist current state - e.g. on app pause/quit.
        public void SaveProgress()
        {
            LudoGame.Offline.MatchSaveSystem.Save(Mode, _slots, State);
        }

        public void StartMatch()
        {
            State.CurrentTurn = _turns.CurrentPlayer;
            BeginTurn();
        }

        private void BeginTurn()
        {
            _turnTimer = 0f;
            _pendingLegalMoves = null;
            State.CurrentTurn = _turns.CurrentPlayer;
            OnTurnStarted?.Invoke(State.CurrentTurn);

            if (IsAI(State.CurrentTurn))
                RequestRoll(); // AI acts immediately, no timer needed
        }

        private bool IsAI(PlayerColor color) => _slots.First(s => s.Color == color).IsAI;

        // Call every frame from your MonoBehaviour's Update() for the timeout to function.
        public void Tick(float deltaTime)
        {
            if (IsAI(State.CurrentTurn)) return; // AI never times out

            _turnTimer += deltaTime;
            if (_turnTimer >= TurnTimeoutSeconds)
            {
                OnTurnTimedOut?.Invoke(State.CurrentTurn);
                AdvanceTurn(extraTurn: false);
            }
        }

        // UI calls this when the human player taps the dice, or GameManager calls it itself for AI.
        public void RequestRoll()
        {
            var color = State.CurrentTurn;
            var (value, forfeit) = _dice.Roll();
            State.LastDiceValue = value;
            OnDiceRolled?.Invoke(new DiceRolledArgs { Color = color, Value = value, ForfeitTurn = forfeit });

            if (forfeit)
            {
                AdvanceTurn(extraTurn: false);
                return;
            }

            var legal = _rules.GetLegalMoves(color, value);
            if (legal.Count == 0)
            {
                AdvanceTurn(extraTurn: false);
                return;
            }

            if (IsAI(color))
            {
                var aiChoice = _aiControllers[color].ChooseMove(color, legal, value);
                RequestMove(aiChoice.TokenId);
            }
            else
            {
                // Wait for UI to call RequestMove() with the token the player tapped.
                _pendingLegalMoves = legal;
                _turnTimer = 0f; // give them the full window to pick a token, not just to roll
            }
        }

        public List<Token> GetPendingLegalMoves() => _pendingLegalMoves;

        public void RequestMove(int tokenId)
        {
            var color = State.CurrentTurn;
            int diceValue = State.LastDiceValue;

            if (!IsAI(color) && _pendingLegalMoves != null && !_pendingLegalMoves.Any(t => t.TokenId == tokenId))
                return; // UI tried to move a token that wasn't actually legal this roll

            var result = _rules.ApplyMove(color, tokenId, diceValue);
            _pendingLegalMoves = null;

            if (!result.Success) { AdvanceTurn(extraTurn: false); return; }

            var token = State.GetTokens(color).First(t => t.TokenId == tokenId);
            OnMoveApplied?.Invoke(new MoveAppliedArgs
            {
                Color = color,
                TokenId = tokenId,
                NewRelativePosition = token.RelativePosition,
                CapturedOpponent = result.CapturedOpponent,
                CapturedColor = result.CapturedColor,
                CapturedTokenId = result.CapturedTokenId,
                TokenFinished = result.TokenFinished,
                ExtraTurn = result.ExtraTurn,
            });

            var winner = _rules.CheckWinner();
            if (winner.HasValue)
            {
                State.Winner = winner;
                OnGameWon?.Invoke(winner.Value);
                return;
            }

            AdvanceTurn(result.ExtraTurn);
        }

        private void AdvanceTurn(bool extraTurn)
        {
            if (!extraTurn) _turns.AdvanceTurn(extraTurn: false);
            BeginTurn();
        }
    }
}
