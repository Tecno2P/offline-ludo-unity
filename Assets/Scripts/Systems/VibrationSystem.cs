using System.Collections;
using LudoGame.Offline;
using UnityEngine;

namespace LudoGame.Systems
{
    // Stock Unity only exposes Handheld.Vibrate() (a single fixed-duration buzz, Android/iOS).
    // For "patterns" (double-buzz on capture, longer pattern on victory) this runs multiple
    // real pulses spaced with real delays via a coroutine - not a single fake call pretending
    // to be a pattern. For finer per-millisecond control you'd swap in a native haptics plugin,
    // but this works out of the box with no extra dependency.
    public class VibrationSystem : MonoBehaviour
    {
        private static VibrationSystem _instance;

        private static VibrationSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("VibrationSystem");
                    Object.DontDestroyOnLoad(go);
                    _instance = go.AddComponent<VibrationSystem>();
                }
                return _instance;
            }
        }

        private static bool Enabled => SettingsSystem.Load().Vibration;

        // Single short buzz - dice roll, token move, button tap.
        public static void Light()
        {
            if (!Enabled) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        // Two quick pulses - capture.
        public static void DoublePulse()
        {
            if (!Enabled) return;
            Instance.StartCoroutine(Instance.PulseRoutine(2, 0.12f));
        }

        // Three pulses with a slightly longer gap - victory.
        public static void VictoryPattern()
        {
            if (!Enabled) return;
            Instance.StartCoroutine(Instance.PulseRoutine(3, 0.2f));
        }

        private IEnumerator PulseRoutine(int count, float gapSeconds)
        {
            for (int i = 0; i < count; i++)
            {
#if UNITY_ANDROID || UNITY_IOS
                Handheld.Vibrate();
#endif
                yield return new WaitForSeconds(gapSeconds);
            }
        }
    }
}
