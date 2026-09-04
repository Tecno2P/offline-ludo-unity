using LudoGame.Gameplay;
using LudoGame.Offline;
using UnityEngine.UIElements;

namespace LudoGame.UI
{
    public class MainMenuController
    {
        public MainMenuController(VisualElement root, UIScreenManager manager)
        {
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
