using System;
using System.Collections.Generic;
using LudoGame.Core;
using LudoGame.Gameplay;
using LudoGame.Offline;
using LudoGame.Systems;
using UnityEngine;

namespace LudoGame.UI
{
    // Call this once right after a match starts, for any session type (VS AI, Local
    // Multiplayer, or LAN). It tracks real match duration and captures made by the local
    // human, records the result to the local profile on game-end, and hands off to the
    // Victory screen with real numbers - no mode-specific duplication needed elsewhere.
    public static class MatchStatsWiring
    {
        public static void Wire(ILudoGameSession session, PlayerColor humanColor, string modeLabel,
            List<string> playerNames, UIScreenManager manager)
        {
            float startTime = Time.realtimeSinceStartup;
            int capturesMade = 0;

            session.OnMoveApplied += args =>
            {
                if (args.CapturedOpponent && args.Color == humanColor) capturesMade++;
            };

            session.OnGameWon += winner =>
            {
                bool humanWon = winner == humanColor;
                var profile = SaveSystem.Load();
                int durationSeconds = Mathf.RoundToInt(Time.realtimeSinceStartup - startTime);

                var record = new MatchRecord
                {
                    DateIso = DateTime.UtcNow.ToString("o"),
                    Mode = modeLabel,
                    Players = playerNames,
                    Winner = humanWon ? profile.PlayerName : winner.ToString(),
                    DurationSeconds = durationSeconds,
                };

                Statistics.RecordMatchResult(profile, humanWon, record);
                int xpEarned = humanWon ? 50 : 10;

                manager.ShowVictory(winner, durationSeconds, tokensFinished: 4, captures: capturesMade, xpEarned: xpEarned);
            };
        }
    }
}
