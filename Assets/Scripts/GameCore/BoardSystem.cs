using System.Collections.Generic;

namespace LudoGame.Core
{
    public enum PlayerColor { Red = 0, Green = 1, Yellow = 2, Blue = 3 }

    // Pure data/logic describing the classic 52-cell Ludo track + 6-cell home stretch per color.
    // Relative position per token: -1 = in yard, 0..50 = on shared 51-step track, 51..56 = home stretch, 57 = finished.
    public static class BoardSystem
    {
        public const int TrackLength = 52;      // shared global cells
        public const int StepsOnTrack = 51;     // relative 0..50 before entering home column
        public const int HomeStretch = 6;       // relative 51..56
        public const int Finished = 57;

        // Global start offset for each color on the shared 52-cell ring.
        private static readonly Dictionary<PlayerColor, int> StartOffset = new Dictionary<PlayerColor, int>
        {
            { PlayerColor.Red, 0 },
            { PlayerColor.Green, 13 },
            { PlayerColor.Yellow, 26 },
            { PlayerColor.Blue, 39 },
        };

        // Star/safe cells on the shared ring (global indices) - includes every color's start cell.
        private static readonly HashSet<int> SafeCellsGlobal = new HashSet<int> { 0, 8, 13, 21, 26, 34, 39, 47 };

        public static int GetStartOffset(PlayerColor color) => StartOffset[color];

        // Converts a token's relative position (0..50) to its absolute cell on the shared ring.
        // Only valid while the token is still on the shared track (not in home stretch).
        public static int RelativeToGlobal(PlayerColor color, int relativePos)
        {
            if (relativePos < 0 || relativePos > StepsOnTrack - 1)
                return -1; // not on shared ring (in yard, home stretch, or finished)
            return (StartOffset[color] + relativePos) % TrackLength;
        }

        public static bool IsSafeCell(PlayerColor color, int relativePos)
        {
            if (relativePos < 0) return true; // in yard, irrelevant
            if (relativePos >= StepsOnTrack) return true; // home stretch is always safe
            int global = RelativeToGlobal(color, relativePos);
            return SafeCellsGlobal.Contains(global);
        }

        public static bool IsFinished(int relativePos) => relativePos == Finished;

        public static bool IsInHomeStretch(int relativePos) => relativePos >= StepsOnTrack && relativePos < Finished;
    }
}
