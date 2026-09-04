using System;

namespace LudoGame.Core
{
    [Serializable]
    public class Token
    {
        public int TokenId;          // 0-3, unique within owner's color
        public PlayerColor Owner;
        public int RelativePosition = -1; // -1 = in yard

        public bool InYard => RelativePosition < 0;
        public bool IsFinished => RelativePosition == BoardSystem.Finished;

        public Token(int id, PlayerColor owner)
        {
            TokenId = id;
            Owner = owner;
            RelativePosition = -1;
        }

        public int GlobalCell => BoardSystem.RelativeToGlobal(Owner, RelativePosition);

        public bool CanMove(int diceValue)
        {
            if (IsFinished) return false;
            if (InYard) return diceValue == 6;
            return RelativePosition + diceValue <= BoardSystem.Finished;
        }

        public void Move(int diceValue)
        {
            if (InYard)
            {
                if (diceValue != 6) throw new InvalidOperationException("Token can only leave yard on a 6.");
                RelativePosition = 0;
                return;
            }
            RelativePosition = Math.Min(RelativePosition + diceValue, BoardSystem.Finished);
        }

        public void SendHome()
        {
            RelativePosition = -1;
        }
    }
}
