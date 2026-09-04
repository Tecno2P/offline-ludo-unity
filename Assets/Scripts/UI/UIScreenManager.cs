using LudoGame.Gameplay;
using LudoGame.LAN;
using UnityEngine;
using UnityEngine.UIElements;

namespace LudoGame.UI
{
    // Attach to a GameObject with a UIDocument component. Assign every VisualTreeAsset in
    // the inspector (drag the .uxml files from Assets/UI/Screens). This is the single place
    // that knows how to get from one screen to another - every controller calls back into it.
    public class UIScreenManager : MonoBehaviour
    {
        public UIDocument Document;

        public VisualTreeAsset MainMenuAsset;
        public VisualTreeAsset ProfileAsset;
        public VisualTreeAsset LobbyAsset;
        public VisualTreeAsset SettingsAsset;
        public VisualTreeAsset StatisticsAsset;
        public VisualTreeAsset VsAiSetupAsset;
        public VisualTreeAsset VictoryAsset;

        // Assign your gameplay scene's controller here (see Rendering/GameSceneController).
        // When a match starts, the screen manager hides UI and hands control to gameplay.
        public LudoGame.Rendering.GameSceneController GameScene;

        private VisualElement _root;

        private void Awake()
        {
            _root = Document.rootVisualElement;
            ShowMainMenu();
        }

        private VisualElement Load(VisualTreeAsset asset)
        {
            _root.Clear();
            var instance = asset.Instantiate();
            _root.Add(instance);
            return instance;
        }

        public void ShowMainMenu()
        {
            var root = Load(MainMenuAsset);
            new MainMenuController(root, this);
        }

        public void ShowProfile()
        {
            var root = Load(ProfileAsset);
            new ProfileController(root, this);
        }

        public void ShowLobby()
        {
            var root = Load(LobbyAsset);
            new LobbyController(root, this);
        }

        public void ShowSettings()
        {
            var root = Load(SettingsAsset);
            new SettingsController(root, this);
        }

        public void ShowStatistics()
        {
            var root = Load(StatisticsAsset);
            new StatisticsController(root, this);
        }

        public void ShowVsAiSetup(bool isVsAi = true)
        {
            var root = Load(VsAiSetupAsset);
            new VsAiSetupController(root, this, isVsAi);
        }

        public void ShowVictory(PlayerColor winner, int durationSeconds, int tokensFinished, int captures, int xpEarned)
        {
            var root = Load(VictoryAsset);
            new VictoryController(root, this, winner, durationSeconds, tokensFinished, captures, xpEarned);
        }

        // Hides the menu UI entirely and starts the actual board/token gameplay for the
        // given session (VS AI, Local Multiplayer, or LAN - all speak ILudoGameSession).
        public void EnterGameplay(ILudoGameSession session)
        {
            _root.Clear();
            GameScene.gameObject.SetActive(true);
            GameScene.Initialize(session);
        }
    }
}
