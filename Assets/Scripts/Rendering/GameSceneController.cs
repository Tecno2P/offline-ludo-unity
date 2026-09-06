using System.Collections.Generic;
using System.Linq;
using LudoGame.Audio;
using LudoGame.Core;
using LudoGame.Gameplay;
using LudoGame.Offline;
using LudoGame.Systems;
using UnityEngine;

namespace LudoGame.Rendering
{
    // Drop this on an empty GameObject in your gameplay scene, assign Session at runtime
    // (a GameManager, LanHostSession, or LanClientSession - anything implementing
    // ILudoGameSession), and call Initialize(). Everything else runs off session events.
    public class GameSceneController : MonoBehaviour
    {
        public BoardBuilder BoardPrefabHolder; // add a BoardBuilder component to this same object, or assign one
        public float CellSize = 1f;

        private ILudoGameSession _session;
        private BoardBuilder _board;
        private DiceView _dice;
        private GameSettings _settings;
        private readonly Dictionary<(PlayerColor color, int id), TokenView> _tokens = new Dictionary<(PlayerColor, int), TokenView>();

        public void Initialize(ILudoGameSession session)
        {
            _session = session;
            _settings = SettingsSystem.Load();

            _board = BoardPrefabHolder != null ? BoardPrefabHolder : gameObject.AddComponent<BoardBuilder>();
            _board.CellSize = CellSize;
            _board.Build();

            SpawnTokens();
            _dice = DiceView.Create(transform, _board.CellToWorld(BoardLayout.Center.row, BoardLayout.Center.col) + new Vector3(2f, 0, 0));

            SubscribeSession();

            AudioManager.Instance.PlayGameStart();
        }

        private void OnDestroy()
        {
            UnsubscribeSession();
        }

        // Called after host migration (or a client reconnect) hands back a new session
        // object for what is logically the same match. Re-points every event subscription
        // and snaps tokens to their authoritative positions - never rebuilds the board.
        public void Rebind(ILudoGameSession newSession)
        {
            UnsubscribeSession();
            _session = newSession;
            SubscribeSession();
            ResyncTokenPositions();
            HandleTurnStarted(_session.CurrentTurn); // re-apply highlighting immediately, don't wait for the next natural turn event
        }

        private void SubscribeSession()
        {
            _session.OnTurnStarted += HandleTurnStarted;
            _session.OnDiceRolled += HandleDiceRolled;
            _session.OnMoveApplied += HandleMoveApplied;
            _session.OnGameWon += HandleGameWon;
            _session.OnPlayerDisconnected += HandlePlayerDisconnected;
            _session.OnTurnTimedOut += HandleTurnTimedOut;
        }

        private void UnsubscribeSession()
        {
            if (_session == null) return;
            _session.OnTurnStarted -= HandleTurnStarted;
            _session.OnDiceRolled -= HandleDiceRolled;
            _session.OnMoveApplied -= HandleMoveApplied;
            _session.OnGameWon -= HandleGameWon;
            _session.OnPlayerDisconnected -= HandlePlayerDisconnected;
            _session.OnTurnTimedOut -= HandleTurnTimedOut;
        }

        // Snaps every token to wherever the (possibly newly-resynced) authoritative State
        // says it should be - no animation, since this is a reconnect/migration catch-up,
        // not a normal move.
        private void ResyncTokenPositions()
        {
            foreach (var color in _session.State.ActiveColors)
            {
                foreach (var token in _session.State.GetTokens(color))
                {
                    if (_tokens.TryGetValue((color, token.TokenId), out var view))
                        view.transform.position = WorldPositionFor(color, token.RelativePosition, token.TokenId);
                }
            }
        }

        private void Update()
        {
            _session?.Tick(Time.deltaTime);
        }

        private void SpawnTokens()
        {
            foreach (var color in _session.State.ActiveColors)
            {
                foreach (var token in _session.State.GetTokens(color))
                {
                    var (row, col) = BoardLayout.GetYardSlot(color, token.TokenId);
                    var worldPos = _board.CellToWorld(row, col);
                    var view = TokenView.Create(transform, color, token.TokenId, worldPos);
                    _tokens[(color, token.TokenId)] = view;

                    if (!token.InYard)
                        view.transform.position = WorldPositionFor(color, token.RelativePosition, token.TokenId);
                }
            }
        }

        // Player taps a token on screen - call this from your input/raycast code with the
        // tapped TokenView. Illegal or out-of-turn taps are simply ignored by the session.
        public void OnTokenTapped(TokenView view)
        {
            if (_session == null || !_session.IsMyTurn) return;
            AudioManager.Instance.PlayButtonClick();
            VibrationSystem.Light();
            _session.RequestMove(view.TokenId);
        }

        // Call from your dice-tap UI button.
        public void OnDiceTapped()
        {
            if (_session == null || !_session.IsMyTurn) return;
            _session.RequestRoll();
        }

        private void HandleTurnStarted(PlayerColor color)
        {
            foreach (var kvp in _tokens)
                kvp.Value.SetInteractable(kvp.Key.color == color);

            if (_session.IsMyTurn && _settings.Notifications)
                AudioManager.Instance.PlayTurnNotification();
        }

        private void HandleDiceRolled(DiceRolledArgs args)
        {
            _dice.PlayRoll(args.Value);
            AudioManager.Instance.PlayDiceRoll();
            VibrationSystem.Light();
        }

        private void HandleMoveApplied(MoveAppliedArgs args)
        {
            if (!_tokens.TryGetValue((args.Color, args.TokenId), out var view)) return;

            // Figure out where this token was before the move so we can walk every
            // intermediate cell (no teleporting - spec item 14).
            var token = _session.State.GetTokens(args.Color).First(t => t.TokenId == args.TokenId);
            int fromRelative = args.NewRelativePosition - _session.State.LastDiceValue;
            if (fromRelative < -1) fromRelative = -1;

            var waypoints = new List<Vector3>();
            if (fromRelative < 0)
            {
                // Leaving the yard on a 6 - single hop straight to the start cell.
                waypoints.Add(WorldPositionFor(args.Color, 0, args.TokenId));
            }
            else
            {
                var cellSequence = BoardLayout.GetCellSequence(args.Color, fromRelative, args.NewRelativePosition);
                foreach (var (row, col) in cellSequence)
                    waypoints.Add(_board.CellToWorld(row, col));
            }

            view.AnimateMove(waypoints, perStepSeconds: 0.14f, onComplete: () =>
            {
                AudioManager.Instance.PlayTokenMove();

                if (args.CapturedOpponent && _tokens.TryGetValue((args.CapturedColor, args.CapturedTokenId), out var capturedView))
                {
                    AudioManager.Instance.PlayCapture();
                    VibrationSystem.DoublePulse();
                    capturedView.PlayCapturedReaction(() =>
                    {
                        var (yardRow, yardCol) = BoardLayout.GetYardSlot(args.CapturedColor, args.CapturedTokenId);
                        capturedView.transform.position = _board.CellToWorld(yardRow, yardCol);
                    });
                }
            });
        }

        private void HandleGameWon(PlayerColor winner)
        {
            AudioManager.Instance.PlayVictory();
            VibrationSystem.VictoryPattern();
            // Hook your victory banner/confetti UI here - this is the single event it needs.
        }

        private void HandlePlayerDisconnected(PlayerColor color)
        {
            AudioManager.Instance.PlayPlayerLeave();
            // Hook a "player disconnected" toast/UI here.
        }

        private void HandleTurnTimedOut(PlayerColor color)
        {
            // The host already advanced the turn by the time this fires - this is purely
            // feedback so the player understands why the turn moved on without them acting.
            AudioManager.Instance.PlayPlayerLeave();
        }

        private Vector3 WorldPositionFor(PlayerColor color, int relativePos, int tokenId)
        {
            if (relativePos < 0)
            {
                var (row, col) = BoardLayout.GetYardSlot(color, tokenId);
                return _board.CellToWorld(row, col);
            }
            var (r, c) = BoardLayout.GetCellForRelativePosition(color, relativePos);
            return _board.CellToWorld(r, c);
        }
    }
}
