using System.Collections.Generic;
using LudoGame.Core;
using LudoGame.Core.AI;
using LudoGame.Gameplay;
using UnityEngine.UIElements;

namespace LudoGame.UI
{
    public class VsAiSetupController
    {
        public VsAiSetupController(VisualElement root, UIScreenManager manager, bool isVsAi)
        {
            var titleLabel = root.Q<Label>("TitleLabel");
            titleLabel.text = isVsAi ? "VS AI" : "LOCAL MULTIPLAYER";

            var playerCountGroup = root.Q<RadioButtonGroup>("PlayerCountGroup");
            var difficultyGroup = root.Q<RadioButtonGroup>("DifficultyGroup");

            // Local Multiplayer is pure pass-and-play - no AI difficulty to choose.
            var difficultyLabel = root.Q<Label>("DifficultyLabel");
            var difficultyDisplay = isVsAi ? DisplayStyle.Flex : DisplayStyle.None;
            difficultyGroup.style.display = difficultyDisplay;
            difficultyLabel.style.display = difficultyDisplay;

            root.Q<Button>("StartMatchButton").clicked += () =>
            {
                int playerCount = playerCountGroup.value + 2; // radio index 0 -> 2 players, etc.
                var difficulty = (AIDifficulty)difficultyGroup.value;

                var colors = new[] { PlayerColor.Red, PlayerColor.Green, PlayerColor.Yellow, PlayerColor.Blue };
                var slots = new List<PlayerSlot>();
                for (int i = 0; i < playerCount; i++)
                {
                    slots.Add(new PlayerSlot
                    {
                        Color = colors[i],
                        IsAI = isVsAi && i > 0, // slot 0 is always the local human; rest are AI in VS AI mode
                        Difficulty = difficulty,
                        DisplayName = isVsAi && i > 0 ? $"AI {i}" : $"Player {i + 1}",
                    });
                }

                var mode = isVsAi ? MatchMode.VsAI : MatchMode.LocalMultiplayer;
                var gameManager = new GameManager(mode, slots);

                var humanColor = slots[0].Color; // slot 0 is always the local human (see loop above)
                var playerNames = slots.ConvertAll(s => s.DisplayName);
                MatchStatsWiring.Wire(gameManager, humanColor, isVsAi ? "AI" : "Local", playerNames, manager);

                gameManager.StartMatch();
                manager.EnterGameplay(gameManager);
            };

            root.Q<Button>("BackButton").clicked += () => manager.ShowMainMenu();
        }
    }
}
