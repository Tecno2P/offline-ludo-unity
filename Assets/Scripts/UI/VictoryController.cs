using LudoGame.Core;
using LudoGame.Rendering;
using UnityEngine.UIElements;

namespace LudoGame.UI
{
    public class VictoryController
    {
        public VictoryController(VisualElement root, UIScreenManager manager, PlayerColor winner,
            int durationSeconds, int tokensFinished, int captures, int xpEarned)
        {
            root.Q<Label>("WinnerLabel").text = $"{winner.ToString().ToUpper()} WINS!";

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
