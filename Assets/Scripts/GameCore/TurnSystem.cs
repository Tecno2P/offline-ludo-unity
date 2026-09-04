using System.Collections.Generic;

namespace LudoGame.Core
{
    public class TurnSystem
    {
        private readonly List<PlayerColor> _turnOrder;
        private int _currentIndex;

        public PlayerColor CurrentPlayer => _turnOrder[_currentIndex];

        public TurnSystem(List<PlayerColor> playersInOrder)
        {
            _turnOrder = new List<PlayerColor>(playersInOrder);
            _currentIndex = 0;
        }

        // Call after a completed turn. extraTurn = true when the player rolled a 6,
        // captured a token, or got a token home - standard Ludo bonus-turn rules.
        public void AdvanceTurn(bool extraTurn)
        {
            if (extraTurn) return; // same player goes again
            _currentIndex = (_currentIndex + 1) % _turnOrder.Count;
        }

        public void RemovePlayer(PlayerColor color)
        {
            int idx = _turnOrder.IndexOf(color);
            if (idx < 0) return;
            _turnOrder.RemoveAt(idx);
            if (_turnOrder.Count == 0) return;
            if (idx <= _currentIndex && _currentIndex > 0) _currentIndex--;
            _currentIndex %= _turnOrder.Count;
        }

        public IReadOnlyList<PlayerColor> Order => _turnOrder;
    }
}
