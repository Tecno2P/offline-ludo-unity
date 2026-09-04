using System;

namespace LudoGame.Core
{
    // The dice must only ever be "truthfully" rolled on the host. Clients receive the result,
    // they never generate their own number - this is what keeps LAN play from desyncing.
    public class DiceSystem
    {
        private readonly Random _rng;
        public int LastValue { get; private set; }
        public int ConsecutiveSixes { get; private set; }

        public DiceSystem(int? seed = null)
        {
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        // Call only on the host. Returns the rolled value (1-6) and whether the turn
        // should be forfeited for rolling three sixes in a row.
        public (int value, bool forfeitTurn) Roll()
        {
            int value = _rng.Next(1, 7);
            LastValue = value;

            if (value == 6)
            {
                ConsecutiveSixes++;
            }
            else
            {
                ConsecutiveSixes = 0;
            }

            bool forfeit = ConsecutiveSixes >= 3;
            if (forfeit) ConsecutiveSixes = 0;

            return (value, forfeit);
        }

        public void ResetStreak() => ConsecutiveSixes = 0;
    }
}
