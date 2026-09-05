using LudoGame.Audio;
using LudoGame.Localization;
using LudoGame.Offline;
using UnityEngine.UIElements;

namespace LudoGame.UI
{
    public class SettingsController
    {
        public SettingsController(VisualElement root, UIScreenManager manager)
        {
            root.Q<Label>("TitleLabel").text = Loc.Get("settings");
            root.Q<Label>("MusicVolumeLabel").text = Loc.Get("music_volume");
            root.Q<Label>("SfxVolumeLabel").text = Loc.Get("sfx_volume");
            root.Q<Label>("VibrationLabel").text = Loc.Get("vibration");
            root.Q<Label>("NotificationsLabel").text = Loc.Get("notifications");
            root.Q<Label>("GraphicsQualityLabel").text = Loc.Get("graphics_quality");
            root.Q<Label>("AnimationQualityLabel").text = Loc.Get("animation_quality");
            root.Q<Label>("FpsTargetLabel").text = Loc.Get("fps_target");
            root.Q<Label>("LanguageLabel").text = Loc.Get("language");
            root.Q<Button>("ApplyButton").text = Loc.Get("apply");

            var settings = SettingsSystem.Load();

            var musicSlider = root.Q<Slider>("MusicSlider");
            var sfxSlider = root.Q<Slider>("SfxSlider");
            var vibrationToggle = root.Q<Toggle>("VibrationToggle");
            var notificationsToggle = root.Q<Toggle>("NotificationsToggle");
            var graphicsDropdown = root.Q<DropdownField>("GraphicsQualityDropdown");
            var animDropdown = root.Q<DropdownField>("AnimationQualityDropdown");
            var fpsDropdown = root.Q<DropdownField>("FpsDropdown");
            var languageDropdown = root.Q<DropdownField>("LanguageDropdown");

            musicSlider.value = settings.MusicVolume;
            sfxSlider.value = settings.SfxVolume;
            vibrationToggle.value = settings.Vibration;
            notificationsToggle.value = settings.Notifications;
            graphicsDropdown.index = settings.GraphicsQualityIndex;
            animDropdown.index = settings.AnimationQualityIndex;
            fpsDropdown.index = settings.FpsTargetIndex;
            languageDropdown.index = settings.LanguageCode == "hi" ? 1 : 0;

            // Live-apply audio sliders immediately so the player hears the change while dragging.
            sfxSlider.RegisterValueChangedCallback(evt => AudioManager.Instance.SfxVolume = evt.newValue);
            musicSlider.RegisterValueChangedCallback(evt => AudioManager.Instance.MusicVolume = evt.newValue);

            root.Q<Button>("ApplyButton").clicked += () =>
            {
                settings.MusicVolume = musicSlider.value;
                settings.SfxVolume = sfxSlider.value;
                settings.Vibration = vibrationToggle.value;
                settings.Notifications = notificationsToggle.value;
                settings.GraphicsQualityIndex = graphicsDropdown.index;
                settings.AnimationQualityIndex = animDropdown.index;
                settings.FpsTargetIndex = fpsDropdown.index;
                settings.LanguageCode = languageDropdown.index == 1 ? "hi" : "en";

                SettingsSystem.Save(settings);
                Loc.RefreshFromSettings(); // language may have just changed - re-read it immediately

                UnityEngine.Application.targetFrameRate = settings.FpsTargetIndex == 0 ? 30 : 60;
                UnityEngine.QualitySettings.SetQualityLevel(settings.GraphicsQualityIndex, true);

                manager.ShowMainMenu();
            };

            root.Q<Button>("BackButton").clicked += () => manager.ShowMainMenu();
        }
    }
}
