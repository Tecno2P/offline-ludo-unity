using System.Collections;
using LudoGame.Core;
using UnityEngine;

namespace LudoGame.Rendering
{
    public class TokenView : MonoBehaviour
    {
        public PlayerColor TokenColor;
        public int TokenId;

        private SpriteRenderer _body;
        private SpriteRenderer _shadow;
        private Coroutine _activeAnimation;

        public static TokenView Create(Transform parent, PlayerColor color, int tokenId, Vector3 startPos)
        {
            var go = new GameObject($"Token_{color}_{tokenId}");
            go.transform.SetParent(parent, false);
            go.transform.position = startPos;

            var view = go.AddComponent<TokenView>();
            view.TokenColor = color;
            view.TokenId = tokenId;

            var shadowGo = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadowGo.transform.SetParent(go.transform, false);
            shadowGo.transform.localPosition = new Vector3(0.05f, -0.08f, 0.01f);
            shadowGo.sprite = ProceduralSprites.Circle(120, new Color(0, 0, 0, 0.25f), new Color(0, 0, 0, 0));
            shadowGo.transform.localScale = Vector3.one * 0.7f;
            shadowGo.sortingOrder = 4;
            view._shadow = shadowGo;

            var bodyGo = new GameObject("Body").AddComponent<SpriteRenderer>();
            bodyGo.transform.SetParent(go.transform, false);
            bodyGo.sprite = ProceduralSprites.Circle(160, BoardBuilder.GetColor(color), Color.black, 0.08f);
            bodyGo.transform.localScale = Vector3.one * 0.75f;
            bodyGo.sortingOrder = 5;
            view._body = bodyGo;

            return view;
        }

        // Smoothly hops through every intermediate cell rather than teleporting - spec item 14.
        public void AnimateMove(System.Collections.Generic.List<Vector3> waypoints, float perStepSeconds, System.Action onComplete = null)
        {
            if (_activeAnimation != null) StopCoroutine(_activeAnimation);
            _activeAnimation = StartCoroutine(MoveRoutine(waypoints, perStepSeconds, onComplete));
        }

        private IEnumerator MoveRoutine(System.Collections.Generic.List<Vector3> waypoints, float perStepSeconds, System.Action onComplete)
        {
            foreach (var target in waypoints)
            {
                Vector3 start = transform.position;
                float elapsed = 0f;
                const float hopHeight = 0.22f;

                while (elapsed < perStepSeconds)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / perStepSeconds);
                    float eased = 1f - (1f - t) * (1f - t); // ease-out, feels snappier than linear

                    Vector3 flatPos = Vector3.Lerp(start, target, eased);
                    float arc = Mathf.Sin(t * Mathf.PI) * hopHeight;
                    transform.position = flatPos + new Vector3(0, arc, 0);

                    yield return null;
                }
                transform.position = target;
                PlayLandingSquash();
                yield return new WaitForSeconds(0.03f);
            }

            _activeAnimation = null;
            onComplete?.Invoke();
        }

        private void PlayLandingSquash()
        {
            StartCoroutine(SquashRoutine());
        }

        private IEnumerator SquashRoutine()
        {
            Vector3 normal = Vector3.one * 0.75f;
            Vector3 squashed = new Vector3(0.9f, 0.55f, 1f) * 0.75f;
            float t = 0f;
            const float duration = 0.12f;

            while (t < duration)
            {
                t += Time.deltaTime;
                _body.transform.localScale = Vector3.Lerp(squashed, normal, t / duration);
                yield return null;
            }
            _body.transform.localScale = normal;
        }

        // Called when this token is captured - short reaction before SendHome moves it logically.
        public void PlayCapturedReaction(System.Action onComplete)
        {
            StartCoroutine(CapturedRoutine(onComplete));
        }

        private IEnumerator CapturedRoutine(System.Action onComplete)
        {
            Vector3 originalScale = _body.transform.localScale;
            float t = 0f;
            const float duration = 0.25f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float scale = Mathf.Lerp(1f, 0f, t / duration);
                _body.transform.localScale = originalScale * scale;
                _body.transform.Rotate(0, 0, 720f * Time.deltaTime);
                yield return null;
            }

            _body.transform.localScale = originalScale;
            _body.transform.rotation = Quaternion.identity;
            onComplete?.Invoke();
        }

        public void SetInteractable(bool highlighted)
        {
            _body.color = highlighted ? Color.Lerp(BoardBuilder.GetColor(TokenColor), Color.white, 0.3f) : BoardBuilder.GetColor(TokenColor);
        }
    }
}
