using LudoGame.Core;
using LudoGame.Localization;
using LudoGame.Rendering;
using UnityEngine.UIElements;

namespace LudoGame.UI
{
    public class VictoryController
    {
        public VictoryController(VisualElement root, UIScreenManager manager, PlayerColor winner,
            int durationSeconds, int tokensFinished, int captures, int xpEarned)
        {
            root.Q<Label>("WinnerLabel").text = $"{winner.ToString().ToUpper()} {Loc.Get("wins_suffix")}";
            root.Q<Label>("VictorySubtitleLabel").text = Loc.Get("victory_subtitle");
            root.Q<Label>("DurationLabel").text = Loc.Get("duration");
            root.Q<Label>("TokensFinishedLabel").text = Loc.Get("tokens_finished");
            root.Q<Label>("CapturesLabel").text = Loc.Get("captures_made");
            root.Q<Label>("XpEarnedLabel").text = Loc.Get("xp_earned");
            root.Q<Button>("PlayAgainButton").text = Loc.Get("play_again");
            root.Q<Button>("MainMenuButton").text = Loc.Get("main_menu");

            var dot = root.Q<VisualElement>("WinnerColorDot");
            dot.style.backgroundColor = new StyleColor(BoardBuilder.GetColor(winner));

            int minutes = durationSeconds / 60;
            int seconds = durationSeconds % 60;
            root.Q<Label>("DurationValue").text = $"{minutes:00}:{seconds:00}";
            root.Q<Label>("TokensFinishedValue").text = tokensFinished.ToString();
            root.Q<Label>("CapturesValue").text = captures.ToString();
            root.Q<Label>("XpEarnedValue").text = $"+{xpEarned}";

            root.Q<Button>("PlayAgainButton").clicked += () => manager.ShowVsAiSetup();
            root.Q<Button>("MainMenuButton").clicked += () => manager.ShowMainMenu();
        }
    }
}
