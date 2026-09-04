using System.Collections;
using UnityEngine;

namespace LudoGame.Rendering
{
    public class DiceView : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Sprite[] _faces; // index 0 unused, 1-6 are the pip faces
        private bool _rolling;

        public static DiceView Create(Transform parent, Vector3 position)
        {
            var go = new GameObject("Dice");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var view = go.AddComponent<DiceView>();
            view._renderer = go.AddComponent<SpriteRenderer>();
            view._renderer.sortingOrder = 10;

            view._faces = new Sprite[7];
            var faceColor = new Color(0.98f, 0.97f, 0.94f);
            var pipColor = new Color(0.15f, 0.15f, 0.15f);
            for (int i = 1; i <= 6; i++)
                view._faces[i] = ProceduralSprites.DiceFace(220, i, faceColor, pipColor);

            view._renderer.sprite = view._faces[1];
            go.transform.localScale = Vector3.one;
            return view;
        }

        // Runs a believable shake+spin roll, landing on finalValue - the actual number always
        // comes from DiceSystem (host-authoritative); this only ever animates a known result.
        public void PlayRoll(int finalValue, System.Action onComplete = null)
        {
            if (_rolling) return;
            StartCoroutine(RollRoutine(finalValue, onComplete));
        }

        private IEnumerator RollRoutine(int finalValue, System.Action onComplete)
        {
            _rolling = true;
            Vector3 originalPos = transform.position;
            const float duration = 0.65f;
            float elapsed = 0f;
            var rng = new System.Random();

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // Shake: decaying random jitter, and cycle through random faces for a "tumbling" feel.
                float shakeMagnitude = Mathf.Lerp(0.12f, 0f, progress);
                transform.position = originalPos + new Vector3(
                    ((float)rng.NextDouble() - 0.5f) * shakeMagnitude,
                    ((float)rng.NextDouble() - 0.5f) * shakeMagnitude,
                    0f);
                transform.rotation = Quaternion.Euler(0, 0, ((float)rng.NextDouble()) * 360f * (1f - progress));

                if (Time.frameCount % 3 == 0)
                    _renderer.sprite = _faces[rng.Next(1, 7)];

                yield return null;
            }

            transform.position = originalPos;
            transform.rotation = Quaternion.identity;
            _renderer.sprite = _faces[Mathf.Clamp(finalValue, 1, 6)];

            // Small landing bounce so the final result feels like it "settled" rather than snapping.
            yield return LandBounce();

            _rolling = false;
            onComplete?.Invoke();
        }

        private IEnumerator LandBounce()
        {
            Vector3 baseScale = Vector3.one;
            float t = 0f;
            const float duration = 0.15f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float s = 1f + Mathf.Sin((t / duration) * Mathf.PI) * 0.15f;
                transform.localScale = baseScale * s;
                yield return null;
            }
            transform.localScale = baseScale;
        }
    }
}
