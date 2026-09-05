using LudoGame.Gameplay;
using LudoGame.Localization;
using LudoGame.Offline;
using UnityEngine.UIElements;

namespace LudoGame.UI
{
    public class MainMenuController
    {
        public MainMenuController(VisualElement root, UIScreenManager manager)
        {
            root.Q<Label>("TitleLabel").text = Loc.Get("app_title");
            root.Q<Label>("SubtitleLabel").text = Loc.Get("app_subtitle");
            root.Q<Button>("PlayButton").text = Loc.Get("play");
            root.Q<Button>("OfflineMultiplayerButton").text = Loc.Get("offline_multiplayer");
            root.Q<Button>("VsAiButton").text = Loc.Get("vs_ai");
            root.Q<Button>("LocalMultiplayerButton").text = Loc.Get("local_multiplayer");
            root.Q<Button>("ProfileButton").text = Loc.Get("profile");
            root.Q<Button>("StatisticsButton").text = Loc.Get("statistics");
            root.Q<Button>("SettingsButton").text = Loc.Get("settings");
            root.Q<Label>("ResumeBannerLabel").text = Loc.Get("resume_banner");
            root.Q<Button>("ResumeButton").text = Loc.Get("resume_game");

            root.Q<Button>("PlayButton").clicked += () => manager.ShowVsAiSetup(isVsAi: true);
            root.Q<Button>("OfflineMultiplayerButton").clicked += () => manager.ShowLobby();
            root.Q<Button>("VsAiButton").clicked += () => manager.ShowVsAiSetup(isVsAi: true);
            root.Q<Button>("LocalMultiplayerButton").clicked += () => manager.ShowVsAiSetup(isVsAi: false);
            root.Q<Button>("ProfileButton").clicked += () => manager.ShowProfile();
            root.Q<Button>("StatisticsButton").clicked += () => manager.ShowStatistics();
            root.Q<Button>("SettingsButton").clicked += () => manager.ShowSettings();

            var resumeBanner = root.Q<VisualElement>("ResumeBanner");
            if (MatchSaveSystem.HasSavedMatch())
            {
                resumeBanner.style.display = DisplayStyle.Flex;
                root.Q<Button>("ResumeButton").clicked += () =>
                {
                    var resumed = MatchSaveSystem.Resume();
                    if (resumed != null)
                    {
                        resumed.StartMatch();
                        manager.EnterGameplay(resumed);
                    }
                };
            }
            else
            {
                resumeBanner.style.display = DisplayStyle.None;
            }
        }
    }
}
