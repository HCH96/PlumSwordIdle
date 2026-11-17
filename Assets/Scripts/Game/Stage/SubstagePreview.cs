using TMPro;
using PlumSword.Core.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Stage
{
    /// <summary>작은 스테이지 번호와 완료·현재·대기 표식을 보여 준다.</summary>
    public sealed class SubstagePreview : MonoBehaviour
    {
        private RectTransform current;
        private Image currentImage;
        private Image[] markers;
        private Image bossMarker;
        private Sprite completed, active, empty;
        private TextMeshProUGUI number;
        private RectTransform trackFill;

        public static SubstagePreview Create(Canvas canvas)
        {
            var header = Resources.Load<Sprite>("Art/UI/Substage/header");
            var active = Resources.Load<Sprite>("Art/UI/Substage/current");
            var empty = Resources.Load<Sprite>("Art/UI/Substage/empty");
            var completed = Resources.Load<Sprite>("Art/UI/Substage/completed");
            var boss = Resources.Load<Sprite>("Art/UI/Substage/boss");
            if (header == null || active == null || empty == null || completed == null || boss == null) return null;

            var root = new GameObject("SubstagePreview", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(300, 112);
            var preview = root.AddComponent<SubstagePreview>();
            preview.completed = completed; preview.active = active; preview.empty = empty;
            preview.markers = new Image[StageLayout.NormalSubstageCount];
            ImageAt(rect, "Header", header, new Vector2(0, -42), new Vector2(260, 85));

            var numberObj = new GameObject("SubstageNumber", typeof(RectTransform), typeof(TextMeshProUGUI));
            var numberRect = (RectTransform)numberObj.transform;
            numberRect.SetParent(rect, false); numberRect.anchorMin = numberRect.anchorMax = numberRect.pivot = new Vector2(.5f, 1f);
            numberRect.anchoredPosition = new Vector2(0, -45); numberRect.sizeDelta = new Vector2(120, 23);
            var number = numberObj.GetComponent<TextMeshProUGUI>(); preview.number = number;
            number.text = "1-1"; number.fontSize = 22; number.alignment = TextAlignmentOptions.Center;
            number.color = new Color(1f, .87f, .77f); number.raycastTarget = false;

            var rail = ImageAt(rect, "Track", null, new Vector2(0, -96), new Vector2(240, 4));
            rail.color = new Color(.45f, .24f, .19f, .95f);
            var line = ImageAt(rect, "TrackHighlight", null, new Vector2(0, -95.5f), new Vector2(240, 1));
            line.color = new Color(.83f, .65f, .42f, .9f);
            var fill = ImageAt(rect, "Progress", null, new Vector2(-120, -96), new Vector2(0, 2));
            fill.rectTransform.pivot = new Vector2(0, .5f);
            fill.color = new Color(1f, .42f, .58f, .95f); preview.trackFill = fill.rectTransform;
            for (int i = 0; i < 6; i++)
            {
                var marker = ImageAt(rect, "Marker" + (i + 1), i == 0 ? active : empty,
                    new Vector2(-120 + 40 * i, -96), i == 0 ? new Vector2(31, 31) : new Vector2(16, 16));
                marker.rectTransform.pivot = new Vector2(.5f, .5f);
                marker.rectTransform.anchoredPosition = new Vector2(-120 + 40 * i, -96);
                preview.markers[i] = marker;
                if (i == 0) { preview.current = marker.rectTransform; preview.currentImage = marker; }
            }
            preview.bossMarker = ImageAt(rect, "BossMarker", boss, new Vector2(120, -96), new Vector2(24, 26));
            return preview;
        }

        public void SetProgress(int stage, int substage)
        {
            number.text = stage + "-" + substage;
            bossMarker.rectTransform.localScale = Vector3.one;
            bossMarker.color = Color.white;
            for (int i = 0; i < markers.Length; i++)
            {
                var item = markers[i];
                item.sprite = i + 1 < substage ? completed : i + 1 == substage ? active : empty;
                item.rectTransform.sizeDelta = i + 1 == substage ? new Vector2(31, 31) :
                    i + 1 < substage ? new Vector2(24, 24) : new Vector2(16, 16);
                item.rectTransform.localScale = Vector3.one; item.color = Color.white;
            }
            currentImage = substage <= markers.Length ? markers[substage - 1] : bossMarker;
            current = currentImage.rectTransform;
            trackFill.sizeDelta = new Vector2(Mathf.Clamp(substage - 1, 0, 6) * 40, 2);
        }

        private static Image ImageAt(RectTransform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = position + new Vector2(0, size.y * .5f); rect.sizeDelta = size;
            var image = obj.GetComponent<Image>(); image.sprite = sprite; image.preserveAspect = sprite != null;
            image.raycastTarget = false; return image;
        }

        private void Update()
        {
            if (current == null || currentImage == null) return;
            float pulse = .5f + .5f * Mathf.Sin(Time.unscaledTime * 3f);
            current.localScale = Vector3.one * (1f + .06f * pulse);
            currentImage.color = new Color(1, 1, 1, .82f + .18f * pulse);
        }
    }
}
