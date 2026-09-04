using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace LudoGame.Offline
{
    [Serializable]
    public class MatchRecord
    {
        public string DateIso;
        public string Mode; // "AI", "Local", "LAN"
        public List<string> Players;
        public string Winner;
        public int DurationSeconds;
    }

    [Serializable]
    public class PlayerProfile
    {
        public string PlayerName = "Player";
        public string AvatarId = "default";
        public int Coins;
        public int Xp;
        public int Level = 1;
        public int Wins;
        public int Losses;
        public int Matches;
        public List<string> Achievements = new List<string>();
        public List<MatchRecord> MatchHistory = new List<MatchRecord>();
    }

    // Simple, crash-safe local persistence: write to a temp file then atomically replace,
    // so a mid-write crash never corrupts the previous good save.
    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "profile.json");
        private static string BackupPath => Path.Combine(Application.persistentDataPath, "profile.backup.json");

        public static PlayerProfile Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var json = File.ReadAllText(SavePath);
                    var profile = JsonConvert.DeserializeObject<PlayerProfile>(json);
                    if (profile != null) return profile;
                }
            }
            catch (Exception)
            {
                // primary save corrupted - fall through to backup
            }

            try
            {
                if (File.Exists(BackupPath))
                {
                    var json = File.ReadAllText(BackupPath);
                    var profile = JsonConvert.DeserializeObject<PlayerProfile>(json);
                    if (profile != null) return profile;
                }
            }
            catch (Exception)
            {
                // backup also unreadable - fall back to a fresh profile rather than crashing
            }

            return new PlayerProfile();
        }

        public static void Save(PlayerProfile profile)
        {
            try
            {
                if (File.Exists(SavePath))
                    File.Copy(SavePath, BackupPath, overwrite: true);

                var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                var tempPath = SavePath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Copy(tempPath, SavePath, overwrite: true);
                File.Delete(tempPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveSystem: failed to save profile - {e.Message}");
            }
        }

        public static void ResetProgress()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveSystem: failed to reset - {e.Message}");
            }
        }
    }
}
