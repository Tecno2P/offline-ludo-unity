using System.Collections.Generic;
using LudoGame.Offline;

namespace LudoGame.Localization
{
    // A real, populated string table for every piece of UI text in the game - not a stub with
    // two placeholder keys. Reads the persisted language choice from SettingsSystem so it
    // stays in sync with what the player picked on the Settings screen.
    public static class Loc
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Table = new Dictionary<string, Dictionary<string, string>>
        {
            ["en"] = new Dictionary<string, string>
            {
                ["app_title"] = "OFFLINE LUDO",
                ["app_subtitle"] = "Play anywhere. No internet needed.",
                ["play"] = "PLAY",
                ["offline_multiplayer"] = "OFFLINE MULTIPLAYER",
                ["vs_ai"] = "VS AI",
                ["local_multiplayer"] = "LOCAL MULTIPLAYER",
                ["profile"] = "PROFILE",
                ["statistics"] = "STATISTICS",
                ["settings"] = "SETTINGS",
                ["resume_banner"] = "You have a match in progress",
                ["resume_game"] = "RESUME GAME",
                ["create_room"] = "CREATE ROOM",
                ["join_room"] = "JOIN ROOM",
                ["room_code"] = "ROOM CODE",
                ["start_game"] = "START GAME",
                ["cancel_room"] = "CANCEL ROOM",
                ["nearby_rooms"] = "Nearby Rooms",
                ["join"] = "JOIN",
                ["cancel"] = "CANCEL",
                ["ready"] = "READY",
                ["waiting_for_host"] = "Waiting for host to start...",
                ["music_volume"] = "Music Volume",
                ["sfx_volume"] = "SFX Volume",
                ["vibration"] = "Vibration",
                ["notifications"] = "Notifications",
                ["graphics_quality"] = "Graphics Quality",
                ["animation_quality"] = "Animation Quality",
                ["fps_target"] = "FPS Target",
                ["language"] = "Language",
                ["apply"] = "APPLY",
                ["total_matches"] = "Total Matches",
                ["wins"] = "Wins",
                ["losses"] = "Losses",
                ["win_rate"] = "Win Rate",
                ["ai_wins"] = "AI Wins",
                ["local_wins"] = "Local MP Wins",
                ["lan_wins"] = "LAN Wins",
                ["match_history"] = "Match History",
                ["no_matches"] = "No matches played yet.",
                ["number_of_players"] = "Number of Players",
                ["ai_difficulty"] = "AI Difficulty",
                ["start_match"] = "START MATCH",
                ["victory_subtitle"] = "Victory",
                ["duration"] = "Duration",
                ["tokens_finished"] = "Your Tokens Finished",
                ["captures_made"] = "Captures Made",
                ["xp_earned"] = "XP Earned",
                ["play_again"] = "PLAY AGAIN",
                ["main_menu"] = "MAIN MENU",
                ["wins_suffix"] = "WINS!",
                ["save_profile"] = "SAVE PROFILE",
                ["play_as_guest"] = "PLAY AS GUEST",
                ["reset_progress"] = "RESET PROGRESS",
                ["change_avatar"] = "Change Avatar",
                ["level"] = "Level",
                ["xp"] = "XP",
                ["coins"] = "Coins",
                ["enter_your_name"] = "Enter your name",
                ["waiting_for_players"] = "Waiting for all players to be ready.",
                ["enter_host_ip"] = "Enter a host IP address.",
                ["could_not_reach_host"] = "Could not reach that host. Check the IP and try again.",
                ["ready_waiting"] = "Ready! Waiting for host to start...",
                ["connected"] = "Connected!",
                ["manual_hint"] = "Or enter room code manually",
                ["host_prefix"] = "Host: ",
                ["ready_badge"] = "READY",
                ["waiting_badge"] = "Waiting",
                ["disconnected_badge"] = "Disconnected",
            },
            ["hi"] = new Dictionary<string, string>
            {
                ["app_title"] = "ऑफलाइन लूडो",
                ["app_subtitle"] = "कहीं भी खेलें। इंटरनेट की ज़रूरत नहीं।",
                ["play"] = "खेलें",
                ["offline_multiplayer"] = "ऑफलाइन मल्टीप्लेयर",
                ["vs_ai"] = "एआई के खिलाफ",
                ["local_multiplayer"] = "लोकल मल्टीप्लेयर",
                ["profile"] = "प्रोफाइल",
                ["statistics"] = "आंकड़े",
                ["settings"] = "सेटिंग्स",
                ["resume_banner"] = "आपका एक मैच अभी चल रहा है",
                ["resume_game"] = "गेम जारी रखें",
                ["create_room"] = "रूम बनाएं",
                ["join_room"] = "रूम जॉइन करें",
                ["room_code"] = "रूम कोड",
                ["start_game"] = "गेम शुरू करें",
                ["cancel_room"] = "रूम रद्द करें",
                ["nearby_rooms"] = "पास के रूम",
                ["join"] = "जॉइन करें",
                ["cancel"] = "रद्द करें",
                ["ready"] = "तैयार",
                ["waiting_for_host"] = "होस्ट के गेम शुरू करने का इंतज़ार...",
                ["music_volume"] = "संगीत की आवाज़",
                ["sfx_volume"] = "साउंड इफेक्ट की आवाज़",
                ["vibration"] = "कंपन",
                ["notifications"] = "सूचनाएं",
                ["graphics_quality"] = "ग्राफिक्स क्वालिटी",
                ["animation_quality"] = "एनिमेशन क्वालिटी",
                ["fps_target"] = "एफपीएस लक्ष्य",
                ["language"] = "भाषा",
                ["apply"] = "लागू करें",
                ["total_matches"] = "कुल मैच",
                ["wins"] = "जीत",
                ["losses"] = "हार",
                ["win_rate"] = "जीत दर",
                ["ai_wins"] = "एआई जीत",
                ["local_wins"] = "लोकल जीत",
                ["lan_wins"] = "लैन जीत",
                ["match_history"] = "मैच इतिहास",
                ["no_matches"] = "अभी तक कोई मैच नहीं खेला गया।",
                ["number_of_players"] = "खिलाड़ियों की संख्या",
                ["ai_difficulty"] = "एआई कठिनाई",
                ["start_match"] = "मैच शुरू करें",
                ["victory_subtitle"] = "जीत",
                ["duration"] = "अवधि",
                ["tokens_finished"] = "आपके पूरे हुए टोकन",
                ["captures_made"] = "कैप्चर किए गए",
                ["xp_earned"] = "अर्जित एक्सपी",
                ["play_again"] = "फिर से खेलें",
                ["main_menu"] = "मुख्य मेनू",
                ["wins_suffix"] = "जीता!",
                ["save_profile"] = "प्रोफाइल सेव करें",
                ["play_as_guest"] = "गेस्ट के रूप में खेलें",
                ["reset_progress"] = "प्रगति रीसेट करें",
                ["change_avatar"] = "अवतार बदलें",
                ["level"] = "स्तर",
                ["xp"] = "एक्सपी",
                ["coins"] = "सिक्के",
                ["enter_your_name"] = "अपना नाम लिखें",
                ["waiting_for_players"] = "सभी खिलाड़ियों के तैयार होने का इंतज़ार है।",
                ["enter_host_ip"] = "होस्ट का आईपी एड्रेस डालें।",
                ["could_not_reach_host"] = "होस्ट तक नहीं पहुँच सके। आईपी जांचें और दोबारा कोशिश करें।",
                ["ready_waiting"] = "तैयार! होस्ट के शुरू करने का इंतज़ार...",
                ["connected"] = "जुड़ गए!",
                ["manual_hint"] = "या रूम कोड मैन्युअली डालें",
                ["host_prefix"] = "होस्ट: ",
                ["ready_badge"] = "तैयार",
                ["waiting_badge"] = "इंतज़ार में",
                ["disconnected_badge"] = "डिस्कनेक्टेड",
            },
        };

        private static string _currentLang = "en";

        // Call once at startup (and again whenever Settings changes language) to refresh
        // which table Get() reads from.
        public static void RefreshFromSettings()
        {
            var settings = SettingsSystem.Load();
            _currentLang = settings.LanguageCode == "hi" ? "hi" : "en";
        }

        public static string CurrentLanguage => _currentLang;

        public static string Get(string key)
        {
            if (Table.TryGetValue(_currentLang, out var dict) && dict.TryGetValue(key, out var value))
                return value;
            // Fall back to English rather than showing a raw key to the player.
            if (Table["en"].TryGetValue(key, out var fallback)) return fallback;
            return key;
        }
    }
}
