using System;
using LudoGame.Core;

namespace LudoGame.Gameplay
{
    public struct DiceRolledArgs
    {
        public PlayerColor Color;
        public int Value;
        public bool ForfeitTurn;
    }

    public struct MoveAppliedArgs
    {
        public PlayerColor Color;
        public int TokenId;
        public int NewRelativePosition;
        public bool CapturedOpponent;
        public PlayerColor CapturedColor;
        public int CapturedTokenId;
        public bool TokenFinished;
        public bool ExtraTurn;
    }

    // Implemented by GameManager (VS AI / Local Multiplayer) AND by LanHostSession /
    // LanClientSession. UI/animation code should be written against this interface only,
    // so a screen built for pass-and-play works unchanged once pointed at a LAN session.
    public interface ILudoGameSession
    {
        GameState State { get; }
        PlayerColor CurrentTurn { get; }

        // True when the local device/player is allowed to act right now. For VS AI and
        // Local Multiplayer this is true whenever it's a human's turn (any color, since
        // it's pass-and-play on one device). For LAN it's true only when CurrentTurn
        // equals this device's assigned color.
        bool IsMyTurn { get; }

        event Action<PlayerColor> OnTurnStarted;
        event Action<DiceRolledArgs> OnDiceRolled;
        event Action<MoveAppliedArgs> OnMoveApplied;
        event Action<PlayerColor> OnGameWon;
        event Action<PlayerColor> OnPlayerDisconnected; // never fires for local sessions
        event Action<PlayerColor> OnTurnTimedOut; // never fires for local VS AI turns (AI never times out)

        void RequestRoll();
        void RequestMove(int tokenId);

        // Call every frame (Update()) so turn-timeout logic can run where applicable.
        void Tick(float deltaTime);
    }
}
