using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace LudoGame.Offline
{
    [Serializable]
    public class GameSettings
    {
        public float MusicVolume = 0.8f;
        public float SfxVolume = 1f;
        public bool Vibration = true;
        public bool Notifications = true;
        public int GraphicsQualityIndex = 1; // Low/Medium/High
        public int AnimationQualityIndex = 2;
        public int FpsTargetIndex = 1; // 30/60
        public string LanguageCode = "en"; // "en" or "hi"
    }

    public static class SettingsSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "settings.json");

        public static GameSettings Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var json = File.ReadAllText(SavePath);
                    var settings = JsonConvert.DeserializeObject<GameSettings>(json);
                    if (settings != null) return settings;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"SettingsSystem: failed to load, using defaults - {e.Message}");
            }
            return new GameSettings();
        }

        public static void Save(GameSettings settings)
        {
            try
            {
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"SettingsSystem: failed to save - {e.Message}");
            }
        }
    }
}
