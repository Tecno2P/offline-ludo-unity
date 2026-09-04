using System;
using System.Collections.Generic;
using System.Linq;

namespace LudoGame.Core
{
    [Serializable]
    public class GameState
    {
        public string MatchId;
        public List<PlayerColor> ActiveColors = new List<PlayerColor>();
        public Dictionary<PlayerColor, List<Token>> TokensByColor = new Dictionary<PlayerColor, List<Token>>();
        public int TurnNumber;
        public PlayerColor CurrentTurn;
        public int LastDiceValue;
        public PlayerColor? Winner;

        public IEnumerable<Token> AllTokens => TokensByColor.Values.SelectMany(t => t);

        public static GameState CreateNew(string matchId, List<PlayerColor> players)
        {
            var state = new GameState { MatchId = matchId, ActiveColors = players, TurnNumber = 0 };
            foreach (var color in players)
            {
                state.TokensByColor[color] = Enumerable.Range(0, 4)
                    .Select(i => new Token(i, color))
                    .ToList();
            }
            state.CurrentTurn = players[0];
            return state;
        }

        public List<Token> GetTokens(PlayerColor color) => TokensByColor[color];
    }
}
