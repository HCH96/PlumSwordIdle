using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Stage
{
    public sealed class EndingView : MonoBehaviour
    {
        public const float FadeSeconds = 1.25f;
        public const float HoldSeconds = 5f;
        private GameObject panel;

        public void Initialize(Canvas canvas, double playSeconds, int regressionCount, Action onReturn)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (onReturn == null) throw new ArgumentNullException(nameof(onReturn));

            panel = new GameObject("EndingPanel", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(canvas.transform, false);
            Stretch(rect);
            var backdrop = panel.GetComponent<Image>();
            backdrop.color = Color.clear;
            backdrop.raycastTarget = true;

            var artObject = new GameObject("EndingArt", typeof(RectTransform), typeof(Image));
            var artRect = (RectTransform)artObject.transform;
            artRect.SetParent(rect, false);
            Stretch(artRect);
            var art = artObject.GetComponent<Image>();
            art.sprite = Resources.Load<Sprite>("Art/UI/ending_dawn");
            art.color = art.sprite != null ? Color.white : Color.black;
            art.raycastTarget = false;
            if (art.sprite != null)
            {
                var aspect = artObject.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                aspect.aspectRatio = art.sprite.rect.width / art.sprite.rect.height;
            }
            artObject.SetActive(false);

            var fadeObject = new GameObject("EndingFade", typeof(RectTransform), typeof(Image));
            var fadeRect = (RectTransform)fadeObject.transform;
            fadeRect.SetParent(rect, false);
            Stretch(fadeRect);
            var fade = fadeObject.GetComponent<Image>();
            fade.color = Color.clear;
            fade.raycastTarget = false;
            StartCoroutine(PlayEnding(artObject, fade, onReturn));
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static IEnumerator PlayEnding(GameObject art, Image fade, Action onReturn)
        {
            yield return Fade(fade, 0f, 1f);
            art.SetActive(true);
            yield return Fade(fade, 1f, 0f);
            yield return new WaitForSecondsRealtime(HoldSeconds);
            yield return Fade(fade, 0f, 1f);
            onReturn();
        }

        private static IEnumerator Fade(Image image, float from, float to)
        {
            float elapsed = 0f;
            image.color = new Color(0f, 0f, 0f, from);
            while (elapsed < FadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                image.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, elapsed / FadeSeconds));
                yield return null;
            }
            image.color = new Color(0f, 0f, 0f, to);
        }

        private void LateUpdate()
        {
            if (panel != null) panel.transform.SetAsLastSibling();
        }

        private void OnDestroy()
        {
            if (panel != null) Destroy(panel);
        }
    }
}
