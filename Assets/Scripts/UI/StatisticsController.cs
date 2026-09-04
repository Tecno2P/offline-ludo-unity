using LudoGame.Offline;
using LudoGame.Systems;
using UnityEngine.UIElements;

namespace LudoGame.UI
{
    public class StatisticsController
    {
        public StatisticsController(VisualElement root, UIScreenManager manager)
        {
            var profile = SaveSystem.Load();

            root.Q<Label>("TotalMatchesValue").text = profile.Matches.ToString();
            root.Q<Label>("WinsValue").text = profile.Wins.ToString();
            root.Q<Label>("LossesValue").text = profile.Losses.ToString();
            root.Q<Label>("WinRateValue").text = $"{Statistics.WinRate(profile) * 100f:0}%";

            int aiWins = 0, localWins = 0, lanWins = 0;
            foreach (var match in profile.MatchHistory)
            {
                if (match.Winner != profile.PlayerName) continue;
                switch (match.Mode)
                {
                    case "AI": aiWins++; break;
                    case "Local": localWins++; break;
                    case "LAN": lanWins++; break;
                }
            }
            root.Q<Label>("AiWinsValue").text = aiWins.ToString();
            root.Q<Label>("LocalWinsValue").text = localWins.ToString();
            root.Q<Label>("LanWinsValue").text = lanWins.ToString();

            var list = root.Q<VisualElement>("MatchHistoryList");
            for (int i = profile.MatchHistory.Count - 1; i >= 0; i--)
            {
                var match = profile.MatchHistory[i];
                var row = new VisualElement();
                row.AddToClassList("player-row");

                var label = new Label($"{match.DateIso}  •  {match.Mode}  •  Winner: {match.Winner}");
                label.AddToClassList("player-name-label");
                label.style.fontSize = 13;
                row.Add(label);

                list.Add(row);
            }

            if (profile.MatchHistory.Count == 0)
            {
                var empty = new Label("No matches played yet.");
                empty.style.color = new StyleColor(new UnityEngine.Color(0.63f, 0.64f, 0.7f));
                empty.style.fontSize = 13;
                list.Add(empty);
            }

            root.Q<Button>("BackButton").clicked += () => manager.ShowMainMenu();
        }
    }
}
