using System.Collections.Generic;
using LudoGame.Core;
using LudoGame.LAN;
using LudoGame.Localization;
using LudoGame.Offline;
using UnityEngine.UIElements;

namespace LudoGame.UI
{
    public class LobbyController
    {
        private readonly VisualElement _root;
        private readonly UIScreenManager _manager;
        private LanHostFlow _hostFlow;
        private LanJoinFlow _joinFlow;
        private readonly Dictionary<string, RoomAdvertisement> _discovered = new Dictionary<string, RoomAdvertisement>();

        public LobbyController(VisualElement root, UIScreenManager manager)
        {
            _root = root;
            _manager = manager;

            root.Q<Label>("TitleLabel").text = Loc.Get("offline_multiplayer");
            root.Q<Button>("CreateRoomButton").text = Loc.Get("create_room");
            root.Q<Button>("JoinRoomButton").text = Loc.Get("join_room");
            root.Q<Label>("RoomCodeCaption").text = Loc.Get("room_code");
            root.Q<Button>("StartGameButton").text = Loc.Get("start_game");
            root.Q<Button>("CancelHostButton").text = Loc.Get("cancel_room");
            root.Q<Label>("NearbyRoomsLabel").text = Loc.Get("nearby_rooms");
            root.Q<Label>("ManualHintLabel").text = Loc.Get("manual_hint");
            root.Q<Button>("JoinManualButton").text = Loc.Get("join");
            root.Q<Button>("CancelJoinButton").text = Loc.Get("cancel");
            root.Q<Label>("ConnectedLabel").text = Loc.Get("connected");
            root.Q<Button>("ReadyButton").text = Loc.Get("ready");
            root.Q<Label>("WaitingLabel").text = Loc.Get("waiting_for_host");

            var modeSelect = root.Q<VisualElement>("ModeSelectPanel");
            var hostPanel = root.Q<VisualElement>("HostPanel");
            var joinPanel = root.Q<VisualElement>("JoinPanel");
            var connectedPanel = root.Q<VisualElement>("ConnectedPanel");
            var statusLabel = root.Q<Label>("StatusLabel");

            root.Q<Button>("BackButton").clicked += () =>
            {
                _hostFlow?.CloseRoom();
                _joinFlow?.Leave();
                manager.ShowMainMenu();
            };

            root.Q<Button>("CreateRoomButton").clicked += () =>
            {
                var profile = SaveSystem.Load();
                _hostFlow = new LanHostFlow(profile.PlayerName, maxPlayers: 4);
                _hostFlow.OpenRoom();

                root.Q<Label>("RoomCodeLabel").text = _hostFlow.RoomCode;
                root.Q<Label>("HostNameLabel").text = Loc.Get("host_prefix") + profile.PlayerName;

                _hostFlow.Server.OnPlayerJoined += _ => RefreshHostPlayerList(root);
                _hostFlow.Server.OnPlayerDisconnected += _ => RefreshHostPlayerList(root);
                RefreshHostPlayerList(root);

                Switch(modeSelect, hostPanel, joinPanel, connectedPanel);
            };

            root.Q<Button>("StartGameButton").clicked += () =>
            {
                if (_hostFlow == null) return;
                if (!_hostFlow.Server.Room.AllReady)
                {
                    statusLabel.text = Loc.Get("waiting_for_players");
                    return;
                }
                _hostFlow.StartMatch();

                var hostPlayerNames = _hostFlow.Server.Room.Players.ConvertAll(p => p.Name);
                MatchStatsWiring.Wire(_hostFlow.Session, _hostFlow.Session.HostColor, "LAN", hostPlayerNames, _manager);

                _manager.EnterGameplay(_hostFlow.Session);
            };

            root.Q<Button>("CancelHostButton").clicked += () =>
            {
                _hostFlow?.CloseRoom();
                _hostFlow = null;
                Switch(modeSelect, hostPanel, joinPanel, connectedPanel);
            };

            root.Q<Button>("JoinRoomButton").clicked += () =>
            {
                _joinFlow = new LanJoinFlow();
                _joinFlow.OnRoomDiscovered += ad => OnRoomDiscovered(root, ad);
                _joinFlow.StartScanning();
                Switch(joinPanel, modeSelect, hostPanel, connectedPanel);
            };

            root.Q<Button>("JoinManualButton").clicked += () =>
            {
                var ip = root.Q<TextField>("ManualIpField").value;
                var name = root.Q<TextField>("PlayerNameJoinField").value;
                if (string.IsNullOrWhiteSpace(ip)) { statusLabel.text = Loc.Get("enter_host_ip"); return; }
                AttemptJoin(ip, string.IsNullOrWhiteSpace(name) ? SaveSystem.Load().PlayerName : name,
                    modeSelect, hostPanel, joinPanel, connectedPanel, statusLabel);
            };

            root.Q<Button>("CancelJoinButton").clicked += () =>
            {
                _joinFlow?.Leave();
                _joinFlow = null;
                Switch(modeSelect, hostPanel, joinPanel, connectedPanel);
            };

            root.Q<Button>("ReadyButton").clicked += () =>
            {
                _joinFlow?.SendReady();
                root.Q<Button>("ReadyButton").SetEnabled(false);
                root.Q<Label>("WaitingLabel").text = Loc.Get("ready_waiting");
            };
        }

        private void OnRoomDiscovered(VisualElement root, RoomAdvertisement ad)
        {
            if (_discovered.ContainsKey(ad.RoomCode)) return; // don't spam duplicate broadcast pings
            _discovered[ad.RoomCode] = ad;

            var list = root.Q<VisualElement>("DiscoveredRoomList");
            var row = new VisualElement();
            row.AddToClassList("player-row");

            var label = new Label($"{ad.HostName}'s room  •  {ad.RoomCode}  •  {ad.PlayerCount}/{ad.MaxPlayers}");
            label.AddToClassList("player-name-label");
            row.Add(label);

            var joinButton = new Button(() =>
            {
                var name = root.Q<TextField>("PlayerNameJoinField").value;
                AttemptJoin(ad.HostIp, string.IsNullOrWhiteSpace(name) ? SaveSystem.Load().PlayerName : name,
                    root.Q<VisualElement>("ModeSelectPanel"), root.Q<VisualElement>("HostPanel"),
                    root.Q<VisualElement>("JoinPanel"), root.Q<VisualElement>("ConnectedPanel"),
                    root.Q<Label>("StatusLabel"));
            }) { text = Loc.Get("join") };
            joinButton.AddToClassList("secondary-button");
            row.Add(joinButton);

            list.Add(row);
        }

        private void AttemptJoin(string ip, string playerName, VisualElement modeSelect, VisualElement hostPanel,
            VisualElement joinPanel, VisualElement connectedPanel, Label statusLabel)
        {
            if (_joinFlow == null) _joinFlow = new LanJoinFlow();
            bool ok = _joinFlow.JoinByIp(ip, playerName);
            if (!ok)
            {
                statusLabel.text = Loc.Get("could_not_reach_host");
                return;
            }

            Switch(connectedPanel, modeSelect, hostPanel, joinPanel);

            // Hand off to gameplay the moment the host broadcasts GAME_START.
            _joinFlow.Client.OnMessageReceived += msg =>
            {
                if (msg.Type == MessageType.GAME_START)
                {
                    var playerNames = new List<string> { playerName }; // client only knows its own name reliably pre-match
                    MatchStatsWiring.Wire(_joinFlow.Session, _joinFlow.Session.LocalColor, "LAN", playerNames, _manager);
                    _manager.EnterGameplay(_joinFlow.Session);
                }
            };
        }

        private void RefreshHostPlayerList(VisualElement root)
        {
            var list = root.Q<VisualElement>("HostPlayerList");
            list.Clear();

            foreach (var player in _hostFlow.Server.Room.Players)
            {
                var row = new VisualElement();
                row.AddToClassList("player-row");

                var dot = new VisualElement();
                dot.AddToClassList("player-color-dot");
                dot.AddToClassList(ColorClass(player.Color));
                row.Add(dot);

                var label = new Label(player.Name);
                label.AddToClassList("player-name-label");
                row.Add(label);

                var badge = new Label(player.Ready ? Loc.Get("ready_badge") : (player.Connected ? Loc.Get("waiting_badge") : Loc.Get("disconnected_badge")));
                badge.AddToClassList(player.Ready ? "ready-badge" : "waiting-badge");
                row.Add(badge);

                list.Add(row);
            }
        }

        private static string ColorClass(PlayerColor color) => color switch
        {
            PlayerColor.Red => "dot-red",
            PlayerColor.Green => "dot-green",
            PlayerColor.Yellow => "dot-yellow",
            _ => "dot-blue",
        };

        private static void Switch(VisualElement show, params VisualElement[] hide)
        {
            show.style.display = DisplayStyle.Flex;
            foreach (var el in hide) el.style.display = DisplayStyle.None;
        }
    }
}
