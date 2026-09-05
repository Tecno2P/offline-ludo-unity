using LudoGame.Localization;
using LudoGame.Offline;
using LudoGame.Systems;
using UnityEngine.UIElements;

namespace LudoGame.UI
{
    public class ProfileController
    {
        public ProfileController(VisualElement root, UIScreenManager manager)
        {
            var profile = SaveSystem.Load();

            root.Q<Label>("LevelLabel").text = Loc.Get("level");
            root.Q<Label>("XpLabel").text = Loc.Get("xp");
            root.Q<Label>("CoinsLabel").text = Loc.Get("coins");
            root.Q<Label>("WinsLabel").text = Loc.Get("wins");
            root.Q<Label>("LossesLabel").text = Loc.Get("losses");
            root.Q<Label>("WinRateLabel").text = Loc.Get("win_rate");
            root.Q<Button>("ChangeAvatarButton").text = Loc.Get("change_avatar");
            root.Q<Button>("SaveProfileButton").text = Loc.Get("save_profile");
            root.Q<Button>("PlayAsGuestButton").text = Loc.Get("play_as_guest");
            root.Q<Button>("ResetProgressButton").text = Loc.Get("reset_progress");

            var nameField = root.Q<TextField>("PlayerNameField");
            nameField.value = profile.PlayerName;

            var avatarInitial = root.Q<Label>("AvatarInitialLabel");
            avatarInitial.text = string.IsNullOrEmpty(profile.PlayerName) ? "P" : profile.PlayerName.Substring(0, 1).ToUpper();

            nameField.RegisterValueChangedCallback(evt =>
                avatarInitial.text = string.IsNullOrEmpty(evt.newValue) ? "P" : evt.newValue.Substring(0, 1).ToUpper());

            root.Q<Label>("LevelValue").text = profile.Level.ToString();
            root.Q<Label>("XpValue").text = profile.Xp.ToString();
            root.Q<Label>("CoinsValue").text = profile.Coins.ToString();
            root.Q<Label>("WinsValue").text = profile.Wins.ToString();
            root.Q<Label>("LossesValue").text = profile.Losses.ToString();
            root.Q<Label>("WinRateValue").text = $"{Statistics.WinRate(profile) * 100f:0}%";

            // Simple deterministic "avatar" rotation - a real, if minimal, avatar system:
            // cycles through a small palette rather than requiring uploaded art.
            root.Q<Button>("ChangeAvatarButton").clicked += () =>
            {
                var avatars = new[] { "default", "sun", "moon", "star", "leaf", "wave" };
                int idx = System.Array.IndexOf(avatars, profile.AvatarId);
                profile.AvatarId = avatars[(idx + 1) % avatars.Length];
            };

            root.Q<Button>("SaveProfileButton").clicked += () =>
            {
                profile.PlayerName = string.IsNullOrWhiteSpace(nameField.value) ? "Player" : nameField.value.Trim();
                SaveSystem.Save(profile);
                manager.ShowMainMenu();
            };

            root.Q<Button>("PlayAsGuestButton").clicked += () =>
            {
                // Guest mode: skip persistence entirely for this session rather than writing
                // a "Guest" profile over the player's real saved one.
                manager.ShowVsAiSetup(isVsAi: true);
            };

            root.Q<Button>("ResetProgressButton").clicked += () =>
            {
                SaveSystem.ResetProgress();
                manager.ShowProfile(); // reload fresh
            };

            root.Q<Button>("BackButton").clicked += () => manager.ShowMainMenu();
        }
    }
}
