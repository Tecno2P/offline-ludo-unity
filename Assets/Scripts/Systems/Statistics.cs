using LudoGame.Offline;

namespace LudoGame.Systems
{
    public static class Statistics
    {
        public static void RecordMatchResult(PlayerProfile profile, bool won, MatchRecord record)
        {
            profile.Matches++;
            if (won) { profile.Wins++; profile.Xp += 50; }
            else { profile.Losses++; profile.Xp += 10; }

            profile.MatchHistory.Add(record);
            if (profile.MatchHistory.Count > 200)
                profile.MatchHistory.RemoveAt(0); // keep local history bounded

            // Simple level curve: every 200 XP is a level.
            profile.Level = 1 + profile.Xp / 200;

            SaveSystem.Save(profile);
        }

        public static float WinRate(PlayerProfile profile)
        {
            if (profile.Matches == 0) return 0f;
            return (float)profile.Wins / profile.Matches;
        }
    }
}
