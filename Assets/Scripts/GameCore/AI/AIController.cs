using System.Collections.Generic;
using System.Linq;
using LudoGame.Core;

namespace LudoGame.Core.AI
{
    public enum AIDifficulty { Easy, Normal, Hard, Expert }

    // Picks which legal token to move. Higher difficulty weighs captures/safety more heavily.
    public class AIController
    {
        private readonly AIDifficulty _difficulty;
        private readonly GameState _state;

        public AIController(GameState state, AIDifficulty difficulty)
        {
            _state = state;
            _difficulty = difficulty;
        }

        public Token ChooseMove(PlayerColor color, List<Token> legalTokens, int diceValue)
        {
            if (legalTokens.Count == 0) return null;
            if (legalTokens.Count == 1) return legalTokens[0];

            switch (_difficulty)
            {
                case AIDifficulty.Easy:
                    return legalTokens[new System.Random().Next(legalTokens.Count)];

                case AIDifficulty.Normal:
                    // Prefer leaving the yard, then furthest-advanced token.
                    return legalTokens.OrderByDescending(t => t.InYard ? 1 : 0)
                                       .ThenByDescending(t => t.RelativePosition)
                                       .First();

                case AIDifficulty.Hard:
                case AIDifficulty.Expert:
                default:
                    return ChooseBestScored(color, legalTokens, diceValue);
            }
        }

        private Token ChooseBestScored(PlayerColor color, List<Token> legalTokens, int diceValue)
        {
            Token best = null;
            int bestScore = int.MinValue;

            foreach (var token in legalTokens)
            {
                int score = 0;
                int projected = token.InYard ? 0 : token.RelativePosition + diceValue;

                // Strongly reward finishing a token.
                if (projected == BoardSystem.Finished) score += 100;

                // Reward captures: simulate landing cell and check opponents there.
                if (!token.InYard && projected < BoardSystem.StepsOnTrack)
                {
                    int landingGlobal = BoardSystem.RelativeToGlobal(color, projected);
                    bool wouldCapture = _state.AllTokens.Any(o => o.Owner != color && !o.InYard && !o.IsFinished
                                                                   && o.GlobalCell == landingGlobal
                                                                   && !BoardSystem.IsSafeCell(color, projected));
                    if (wouldCapture) score += 50;
                }

                // Reward getting a token out of the yard.
                if (token.InYard) score += 20;

                // Reward moving onto a safe cell.
                if (!token.InYard && BoardSystem.IsSafeCell(color, projected)) score += 10;

                // Slight preference for advancing the most at-risk (furthest but not yet home) token.
                score += projected;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = token;
                }
            }

            return best;
        }
    }
}
