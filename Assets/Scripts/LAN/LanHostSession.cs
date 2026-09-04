using System;
using LudoGame.Core;
using LudoGame.Gameplay;
using Newtonsoft.Json;

namespace LudoGame.LAN
{
    // Wraps HostServer for the host device's own UI. The host is player 0 in RoomManager -
    // this class lets the host's screen use the exact same ILudoGameSession contract as
    // GameManager (local play) and LanClientSession (joiners).
    public class LanHostSession : ILudoGameSession
    {
        private readonly HostServer _server;
        public const int HostPlayerId = 0;

        public GameState State => _server.State;
        public PlayerColor CurrentTurn => _server.State.CurrentTurn;
        public bool IsMyTurn => _server.State != null && CurrentTurn == HostColor;
        public PlayerColor HostColor { get; private set; }

        public event Action<PlayerColor> OnTurnStarted;
        public event Action<DiceRolledArgs> OnDiceRolled;
        public event Action<MoveAppliedArgs> OnMoveApplied;
        public event Action<PlayerColor> OnGameWon;
        public event Action<PlayerColor> OnPlayerDisconnected;

        private PlayerColor _lastBroadcastTurn;

        public LanHostSession(HostServer server)
        {
            _server = server;
            _server.OnBroadcastSent += HandleBroadcast;
            _server.OnPlayerDisconnected += id =>
            {
                foreach (var p in _server.Room.Players)
                    if (p.PlayerId == id) { OnPlayerDisconnected?.Invoke(p.Color); return; }
            };
        }

        public void StartMatch()
        {
            HostColor = _server.Room.Players.Find(p => p.PlayerId == HostPlayerId).Color;
            _server.StartMatch();
        }

        public void RequestRoll() => _server.RequestRollFromPlayer(HostPlayerId);
        public void RequestMove(int tokenId) => _server.RequestMoveFromPlayer(HostPlayerId, tokenId);

        // The host has no separate timeout timer here - client turn-timeout (if you want it)
        // should be enforced host-side by calling RequestRollFromPlayer/skip logic on a server
        // timer per player. Left as a hook: call this from your MonoBehaviour's Update().
        public void Tick(float deltaTime) { }

        // Every state change flows through here, whether it originated from the host's own
        // moves or a client's - so this is the single place turning host broadcasts into events.
        private void HandleBroadcast(NetMessage msg)
        {
            switch (msg.Type)
            {
                case MessageType.TURN_START:
                    var turn = JsonConvert.DeserializeObject<PlayerColor>(msg.PayloadJson);
                    if (turn != _lastBroadcastTurn || _lastBroadcastTurn == default)
                    {
                        _lastBroadcastTurn = turn;
                        OnTurnStarted?.Invoke(turn);
                    }
                    break;

                case MessageType.ROLL_RESULT:
                    var rollPayload = JsonConvert.DeserializeObject<RollResultPayload>(msg.PayloadJson);
                    var rollerColor = FindColorByPlayerId(msg.SenderPlayerId);
                    OnDiceRolled?.Invoke(new DiceRolledArgs { Color = rollerColor, Value = rollPayload.DiceValue, ForfeitTurn = rollPayload.ForfeitTurn });
                    break;

                case MessageType.MOVE_RESULT:
                    var movePayload = JsonConvert.DeserializeObject<MoveResultPayload>(msg.PayloadJson);
                    OnMoveApplied?.Invoke(new MoveAppliedArgs
                    {
                        Color = (PlayerColor)movePayload.PlayerColor,
                        TokenId = movePayload.TokenId,
                        NewRelativePosition = movePayload.NewRelativePosition,
                        CapturedOpponent = movePayload.CapturedOpponent,
                        CapturedColor = movePayload.CapturedOpponentColor >= 0 ? (PlayerColor)movePayload.CapturedOpponentColor : default,
                        CapturedTokenId = movePayload.CapturedOpponentTokenId,
                        TokenFinished = movePayload.TokenFinished,
                        ExtraTurn = movePayload.ExtraTurn,
                    });
                    break;

                case MessageType.GAME_END:
                    var winner = JsonConvert.DeserializeObject<PlayerColor>(msg.PayloadJson);
                    OnGameWon?.Invoke(winner);
                    break;
            }
        }

        private PlayerColor FindColorByPlayerId(int playerId)
        {
            foreach (var p in _server.Room.Players)
                if (p.PlayerId == playerId) return p.Color;
            return default;
        }
    }
}
