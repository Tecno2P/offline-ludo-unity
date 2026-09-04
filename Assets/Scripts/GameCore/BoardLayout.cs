using System.Collections.Generic;

namespace LudoGame.Core
{
    // Maps BoardSystem's abstract 0-51 ring index (and each color's home stretch) onto real
    // (row, col) positions on a 15x15 grid - the standard classic Ludo cross layout. This is
    // geometric/structural data about how a Ludo board is laid out (a public-domain game
    // format), not copied artwork - BoardBuilder draws the actual visuals from scratch.
    public static class BoardLayout
    {
        public const int GridSize = 15;
        public static readonly (int row, int col) Center = (7, 7);

        // Ring cell 0 = (6,1), the classic Red exit square, then proceeds clockwise.
        // Index i here corresponds exactly to BoardSystem's global cell i.
        public static readonly (int row, int col)[] RingCells =
        {
            (6,1),(6,2),(6,3),(6,4),(6,5),
            (5,6),(4,6),(3,6),(2,6),(1,6),(0,6),
            (0,7),
            (0,8),(1,8),(2,8),(3,8),(4,8),(5,8),
            (6,9),(6,10),(6,11),(6,12),(6,13),(6,14),
            (7,14),
            (8,14),(8,13),(8,12),(8,11),(8,10),(8,9),
            (9,8),(10,8),(11,8),(12,8),(13,8),(14,8),
            (14,7),
            (14,6),(13,6),(12,6),(11,6),(10,6),(9,6),
            (8,5),(8,4),(8,3),(8,2),(8,1),(8,0),
            (7,0),
            (6,0),
        };

        // Each color's 6-cell home stretch, walked in order, ending just before the center.
        public static (int row, int col)[] GetHomeStretch(PlayerColor color)
        {
            switch (color)
            {
                case PlayerColor.Red:
                    return new[] { (7,1), (7,2), (7,3), (7,4), (7,5), (7,6) };
                case PlayerColor.Green:
                    return new[] { (1,7), (2,7), (3,7), (4,7), (5,7), (6,7) };
                case PlayerColor.Yellow:
                    return new[] { (7,13), (7,12), (7,11), (7,10), (7,9), (7,8) };
                case PlayerColor.Blue:
                default:
                    return new[] { (13,7), (12,7), (11,7), (10,7), (9,7), (8,7) };
            }
        }

        // Top-left corner of each color's 6x6 yard quadrant.
        public static (int row, int col) GetYardOrigin(PlayerColor color)
        {
            switch (color)
            {
                case PlayerColor.Red: return (0, 0);
                case PlayerColor.Green: return (0, 9);
                case PlayerColor.Yellow: return (9, 9);
                case PlayerColor.Blue:
                default: return (9, 0);
            }
        }

        // The 4 resting slots for a color's tokens while InYard, arranged 2x2 inside the yard.
        public static (int row, int col) GetYardSlot(PlayerColor color, int tokenId)
        {
            var (originRow, originCol) = GetYardOrigin(color);
            int slotRow = 1 + (tokenId / 2) * 3; // rows 1 and 4 within the 6x6 quadrant
            int slotCol = 1 + (tokenId % 2) * 3; // cols 1 and 4 within the 6x6 quadrant
            return (originRow + slotRow, originCol + slotCol);
        }

        // Full ordered path a token walks for animation purposes: yard -> ring (from its
        // start offset, wrapping) -> home stretch -> center. relativePos is BoardSystem's
        // -1..57 value; returns every grid cell the token passes through between two positions.
        public static List<(int row, int col)> GetCellSequence(PlayerColor color, int fromRelative, int toRelative)
        {
            var cells = new List<(int row, int col)>();
            for (int r = fromRelative + 1; r <= toRelative; r++)
                cells.Add(GetCellForRelativePosition(color, r));
            return cells;
        }

        public static (int row, int col) GetCellForRelativePosition(PlayerColor color, int relativePos)
        {
            if (relativePos < 0)
                return GetYardOrigin(color); // caller should use GetYardSlot for a specific token instead

            if (relativePos < BoardSystem.StepsOnTrack)
            {
                int global = BoardSystem.RelativeToGlobal(color, relativePos);
                return RingCells[global];
            }

            if (relativePos < BoardSystem.Finished)
            {
                int stretchIndex = relativePos - BoardSystem.StepsOnTrack;
                return GetHomeStretch(color)[stretchIndex];
            }

            return Center;
        }
    }
}
