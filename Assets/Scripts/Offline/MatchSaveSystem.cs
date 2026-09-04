using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LudoGame.Core;
using LudoGame.Gameplay;
using Newtonsoft.Json;
using UnityEngine;

namespace LudoGame.Offline
{
    // Snapshot of everything needed to resume a single-device match. Deliberately separate
    // from PlayerProfile - this is transient session state, not permanent progress.
    [Serializable]
    public class MatchSaveData
    {
        public MatchMode Mode;
        public List<PlayerSlotData> Slots;
        public GameState State;
        public int SavedAtUnixSeconds;
    }

    [Serializable]
    public class PlayerSlotData
    {
        public PlayerColor Color;
        public bool IsAI;
        public int Difficulty; // cast from AIDifficulty
        public string DisplayName;
    }

    // Only ever applies to locally-saved single-device games (VS AI / pass-and-play), per spec
    // item 25 - LAN sessions are live and are never written here.
    public static class MatchSaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "match_in_progress.json");

        public static bool HasSavedMatch() => File.Exists(SavePath);

        public static void Save(MatchMode mode, List<PlayerSlot> slots, GameState state)
        {
            if (mode != MatchMode.VsAI && mode != MatchMode.LocalMultiplayer)
                return; // guard against accidentally persisting a LAN session

            var data = new MatchSaveData
            {
                Mode = mode,
                Slots = slots.Select(s => new PlayerSlotData
                {
                    Color = s.Color,
                    IsAI = s.IsAI,
                    Difficulty = (int)s.Difficulty,
                    DisplayName = s.DisplayName,
                }).ToList(),
                State = state,
                SavedAtUnixSeconds = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };

            try
            {
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                var tempPath = SavePath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Copy(tempPath, SavePath, overwrite: true);
                File.Delete(tempPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"MatchSaveSystem: failed to save match - {e.Message}");
            }
        }

        public static MatchSaveData Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                var json = File.ReadAllText(SavePath);
                return JsonConvert.DeserializeObject<MatchSaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"MatchSaveSystem: corrupted save, discarding - {e.Message}");
                Clear();
                return null;
            }
        }

        public static void Clear()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); }
            catch (Exception e) { Debug.LogError($"MatchSaveSystem: failed to clear - {e.Message}"); }
        }

        // Convenience: load the save file straight into a ready-to-run GameManager.
        // Returns null if there's nothing to resume.
        public static GameManager Resume()
        {
            var data = Load();
            if (data == null) return null;

            var slots = data.Slots.Select(s => new PlayerSlot
            {
                Color = s.Color,
                IsAI = s.IsAI,
                Difficulty = (LudoGame.Core.AI.AIDifficulty)s.Difficulty,
                DisplayName = s.DisplayName,
            }).ToList();

            return new GameManager(data.Mode, slots, data.State);
        }
    }
}
