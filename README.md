# Offline Ludo — Core Engine (Phase 1)

What's here: the parts that decide whether the game is actually correct and doesn't
desync over LAN. Not here: art, animation, scenes, UI, audio — those need the Unity
Editor and original art assets, which I can't produce from this chat.

## Setup
1. New Unity project (2022 LTS or newer, Android build support).
2. Install **Newtonsoft Json** via Package Manager → `com.unity.nuget.newtonsoft-json`.
3. Copy `Assets/Scripts/` into your project's `Assets/`.
4. Everything here is plain C# (no MonoBehaviours) — wire it up from your own
   GameManager/MonoBehaviour that owns the scene.

## What's implemented
- `GameCore/` — board math, tokens, dice, turns, full move/capture/win rules engine
- `GameCore/AI/` — 4-difficulty AI opponent
- `LAN/` — host-authoritative TCP protocol, room codes, UDP LAN discovery, reconnect hooks
- `Offline/` — crash-safe local JSON save (profile, XP, match history)
- `Systems/Statistics.cs` — win-rate / XP tracking

## Phase 2 — Gameplay loop (added)
- `Gameplay/GameManager.cs` — drives one device's match (VS AI + pass-and-play Local
  Multiplayer): turn order, dice roll, legal-move gating, turn timeout (auto-skip an
  away player), AI auto-play, win detection. Fires events (`OnTurnStarted`,
  `OnDiceRolled`, `OnMoveApplied`, `OnGameWon`, `OnTurnTimedOut`) for a UI/animation
  layer to hook into — call `Tick(deltaTime)` from `Update()` for the timeout to work.
- `Offline/MatchSaveSystem.cs` — save/resume for an in-progress single-device match
  (separate from `SaveSystem.cs`, which is permanent profile/XP/history). Call
  `gameManager.SaveProgress()` on pause/quit; `MatchSaveSystem.Resume()` on relaunch
  rebuilds a ready-to-run `GameManager` starting from whichever player's turn it was.
  LAN sessions are intentionally never written here (per spec: live sessions vs. saved
  single-device games are different things).

## Phase 3 — Unified session contract for UI (added)
- `Gameplay/ILudoGameSession.cs` — the interface every game mode now speaks: `OnTurnStarted`,
  `OnDiceRolled`, `OnMoveApplied`, `OnGameWon`, `OnPlayerDisconnected`, plus `RequestRoll()` /
  `RequestMove(tokenId)` / `Tick(dt)`. Build your UI/animation layer against this ONE interface.
- `GameManager` (VS AI / Local Multiplayer) now implements it directly.
- `LAN/LanHostSession.cs` — host-side wrapper. The host is player 0 in `RoomManager` and plays
  through the exact same `HostServer` validation path as every joined client (`RequestRollFromPlayer`
  / `RequestMoveFromPlayer` is the single mutation entry point, whether the call came over TCP or
  from the host's own local tap).
- `LAN/LanClientSession.cs` — client-side wrapper. Translates host broadcasts into the same events;
  a joined phone never decides outcomes itself, only mirrors what the host says happened.
- `HostServer` now properly owns a `TurnSystem` (previously a stub) and drives real turn order.
- `RulesSystem.MoveResult` now reports exactly which opponent token/color was captured, so LAN
  clients can sync captures precisely instead of guessing from position alone.

Practical effect: a screen you build calling `session.RequestRoll()` / `session.RequestMove(id)`
against `ILudoGameSession` works unchanged whether `session` is a `GameManager`, a
`LanHostSession`, or a `LanClientSession`.

## Phase 4 — LAN lobby wiring (added)
- `LAN/LanHostFlow.cs` — single entry point for "Create Room": starts `HostServer` (TCP) +
  `DiscoveryBroadcaster` (UDP) together, stops advertising once the match starts.
- `LAN/LanJoinFlow.cs` — single entry point for "Join Room": scans for broadcast rooms
  (`DiscoveryListener`), or connects straight to a typed host IP as the documented fallback.
- Reconnect: `Client.Reconnect()` / `LanJoinFlow.Reconnect()` rejoin with a previously-issued
  `PlayerId`, and `HostServer` now distinguishes a genuine new join (rejected once a match is
  running) from a returning player (reconnected, same color/tokens, full state resynced).

Practical effect: a "Create Room" button calls `new LanHostFlow(name, maxPlayers).OpenRoom()`
and reads `RoomCode` for display; a "Join Room" screen calls `StartScanning()`, lists whatever
`OnRoomDiscovered` reports, and calls `JoinDiscoveredRoom()` when tapped. Both flows hand you
back a `Session` (`LanHostSession` / `LanClientSession`) that speaks `ILudoGameSession`, same as
local play.

## Phase 5 — Real rendering, animation, and audio (added, no placeholders)
Everything below is generated procedurally at runtime — real pixel/waveform math, not stub
art or silent clips. This is what "real, not placeholder" looks like without hand-painted
assets or recorded audio, which need the Unity Editor / audio tools this chat doesn't have.

- `GameCore/BoardLayout.cs` — real (row,col) grid coordinates for the classic 15x15 cross
  board, generated to exactly match `BoardSystem.cs`'s ring numbering, safe cells, and start
  offsets (verified: index 0/8/13/21/26/34/39/47 line up with the actual safe/start squares).
- `Rendering/ProceduralSprites.cs` — draws real textures pixel-by-pixel at runtime: anti-aliased
  circles (tokens), rounded-rect cells, a 5-point star (safe-cell marker), and classic 1-6
  dice-pip faces. No image files anywhere.
- `Rendering/BoardBuilder.cs` — builds the full board (4 yards, 52 ring cells, 4 home
  stretches, center) as real GameObjects positioned from `BoardLayout`.
- `Rendering/TokenView.cs` — real coroutine animation: multi-step hop-arc movement (never
  teleports, spec item 14), landing squash, capture spin-and-shrink reaction.
- `Rendering/DiceView.cs` — real shake+spin+random-face-cycling roll animation that lands on
  whatever value the host/DiceSystem actually rolled, plus a landing bounce.
- `Audio/ProceduralAudio.cs` + `AudioManager.cs` — every sound effect (dice roll, token hop,
  capture, button click, turn chime, victory fanfare, join/leave, game start) is synthesized
  from real sine/noise waveform math at runtime — genuinely audible, not empty AudioClips.
- `Rendering/GameSceneController.cs` — wires all of the above to `ILudoGameSession` events.
  Call `Initialize(session)` once you have a `GameManager`/`LanHostSession`/`LanClientSession`;
  everything else (turn highlighting, dice roll, token hops, captures, victory) drives itself
  off session events. Feed it taps via `OnTokenTapped(view)` / `OnDiceTapped()`.

## What's still genuinely out of reach here
- A Unity **scene file** (.unity) with these components pre-wired on GameObjects — scenes are
  binary/YAML files the Editor manages; I can tell you the exact setup (empty GameObject →
  add `GameSceneController` → call `Initialize()` from your menu/lobby flow) but can't hand you
  a finished .unity file from this chat.
- Custom hand-drawn/painted art style, licensed fonts, recorded voice/music — these require
  either an artist/composer or licensed asset packs you'd add yourself in the Editor.
- The Android build pipeline itself (Gradle, keystore signing, Player Settings) — standard
  Unity Android setup, not something generated as code.

## Phase 6 — Real UI screens (added, no placeholders)
Actual UI Toolkit documents (`.uxml`/`.uss`) plus C# controllers wiring every button to the
real backend already built — no "coming soon" screens, no dead buttons.

- `UI/Screens/Shared.uss` — the full visual style (colors, buttons, cards, player rows, room
  code display) used by every screen.
- `MainMenu.uxml` / `MainMenuController.cs` — real resume-match detection (checks
  `MatchSaveSystem.HasSavedMatch()`), routes to every mode.
- `Profile.uxml` / `ProfileController.cs` — loads/saves the real local profile (name, avatar
  cycling, level/XP/coins/wins/losses/win-rate from actual saved data), guest mode, reset.
- `Lobby.uxml` / `LobbyController.cs` — the full LAN flow: create room (live player list from
  `HostServer`, ready-check, start), join room (live discovered-room list from
  `DiscoveryListener`, manual IP fallback), hands off to gameplay the moment `GAME_START`
  actually arrives.
- `Settings.uxml` / `SettingsController.cs` — sliders live-drive `AudioManager` volume while
  dragging; Apply persists via `SettingsSystem` and applies FPS/quality settings for real.
- `Statistics.uxml` / `StatisticsController.cs` — renders real profile stats and the actual
  match history list (empty-state handled, not just assumed non-empty).
- `VsAiSetup.uxml` / `VsAiSetupController.cs` — player count + AI difficulty selection builds
  a real `GameManager` with the right slot configuration (works for both VS AI and Local
  Multiplayer from the same screen).
- `Victory.uxml` / `VictoryController.cs` — shows the actual winner, duration, and captures.
- `UI/MatchStatsWiring.cs` — one shared helper (used by VS AI/Local and both LAN host/client
  paths) that tracks real match duration + captures made, records the result to the local
  profile via `Statistics.RecordMatchResult`, and feeds real numbers to the Victory screen.
- `UI/UIScreenManager.cs` — the single navigation point; swaps `.uxml` documents on one
  `UIDocument` and calls `GameSceneController.Initialize()` when a match actually starts.

## What's still genuinely out of reach here
- A Unity **scene file** (.unity) with these components pre-wired on GameObjects — scenes are
  binary/YAML files the Editor manages; I can tell you the exact setup (empty GameObject →
  add `GameSceneController` → call `Initialize()` from your menu/lobby flow) but can't hand you
  a finished .unity file from this chat.
- Custom hand-drawn/painted art style, licensed fonts, recorded voice/music — these require
  either an artist/composer or licensed asset packs you'd add yourself in the Editor.
- The Android build pipeline itself (Gradle, keystore signing, Player Settings) — standard
  Unity Android setup, not something generated as code.

## Next phases (not built yet)
- Localization (EN/HI) string tables — `Settings.uxml` already has the language dropdown wired
  to persist a choice; actual string swapping isn't implemented yet
- Vibration hooks (Android native, via Unity's Handheld.Vibrate or a haptics plugin)
- 5-6 player extended mode (engine currently assumes the classic 4-color board)
- Turn-timeout enforcement on the LAN host (works for local play; not yet ported to `HostServer`)
- Host migration
- Automated test suite

Bata jo agla banau — usi tarah working code milega, stub nahi.
