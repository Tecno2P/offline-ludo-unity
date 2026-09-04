using System.Collections.Generic;
using System.Linq;

namespace LudoGame.Core
{
    public class MoveResult
    {
        public bool Success;
        public bool CapturedOpponent;
        public PlayerColor CapturedColor;
        public int CapturedTokenId = -1;
        public bool TokenFinished;
        public bool ExtraTurn;
        public string Reason; // set when Success == false, for debugging/UI
    }

    // This is the single source of truth for what counts as a legal move. It must run
    // ONLY on the host in LAN play; clients just render whatever the host broadcasts.
    public class RulesSystem
    {
        private readonly GameState _state;

        public RulesSystem(GameState state)
        {
            _state = state;
        }

        public List<Token> GetLegalMoves(PlayerColor color, int diceValue)
        {
            return _state.GetTokens(color).Where(t => t.CanMove(diceValue)).ToList();
        }

        public MoveResult ApplyMove(PlayerColor color, int tokenId, int diceValue)
        {
            var token = _state.GetTokens(color).FirstOrDefault(t => t.TokenId == tokenId);
            var result = new MoveResult();

            if (token == null) { result.Reason = "Unknown token"; return result; }
            if (!token.CanMove(diceValue)) { result.Reason = "Illegal move for this dice value"; return result; }

            token.Move(diceValue);
            result.Success = true;

            if (token.IsFinished)
            {
                result.TokenFinished = true;
                result.ExtraTurn = true;
            }
            else if (!BoardSystem.IsInHomeStretch(token.RelativePosition))
            {
                // Check capture: any opponent token sharing this global cell, unless it's a safe cell.
                if (!BoardSystem.IsSafeCell(color, token.RelativePosition))
                {
                    int myGlobal = token.GlobalCell;
                    foreach (var opponent in _state.AllTokens.Where(t => t.Owner != color && !t.InYard && !t.IsFinished))
                    {
                        if (opponent.GlobalCell == myGlobal)
                        {
                            result.CapturedOpponent = true;
                            result.CapturedColor = opponent.Owner;
                            result.CapturedTokenId = opponent.TokenId;
                            opponent.SendHome();
                            result.ExtraTurn = true;
                        }
                    }
                }
            }

            if (diceValue == 6) result.ExtraTurn = true;

            return result;
        }

        public bool HasPlayerWon(PlayerColor color)
        {
            return _state.GetTokens(color).All(t => t.IsFinished);
        }

        public PlayerColor? CheckWinner()
        {
            foreach (var color in _state.ActiveColors)
            {
                if (HasPlayerWon(color)) return color;
            }
            return null;
        }
    }
}
