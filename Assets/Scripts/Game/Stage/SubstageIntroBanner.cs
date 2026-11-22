using PlumSword.Core.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Stage
{
    /// <summary>작은 스테이지 번호 패를 표시하고 등장과 퇴장 연출을 재생한다.</summary>
    public sealed class SubstageIntroBanner : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _group;
        private TextMeshProUGUI _number;
        private float _elapsed;
        private StageLabelImages _labelArt;
        private RectTransform _numberArt;
        private Image _bossNameArt;

        public static SubstageIntroBanner Create(Canvas canvas)
        {
            var root = new GameObject("SubstageIntroBanner", typeof(RectTransform), typeof(CanvasGroup));
            var rect = (RectTransform)root.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(360f, 120f);
            var banner = root.AddComponent<SubstageIntroBanner>();
            banner._rect = rect;
            banner._group = root.GetComponent<CanvasGroup>();
            banner._group.interactable = false;
            banner._group.blocksRaycasts = false;

            var plate = new GameObject("Plaque", typeof(RectTransform), typeof(Image));
            var plateRect = (RectTransform)plate.transform;
            plateRect.SetParent(rect, false);
            plateRect.anchorMin = Vector2.zero; plateRect.anchorMax = Vector2.one;
            plateRect.offsetMin = plateRect.offsetMax = Vector2.zero;
            var image = plate.GetComponent<Image>();
            image.sprite = Resources.Load<Sprite>("Art/UI/Substage/intro_plaque");
            image.preserveAspect = image.sprite != null;
            image.color = image.sprite != null ? Color.white : new Color(.17f, .035f, .09f, .94f);
            image.raycastTarget = false;

            var numberObject = new GameObject("SubstageNumber", typeof(RectTransform), typeof(TextMeshProUGUI));
            var numberRect = (RectTransform)numberObject.transform;
            numberRect.SetParent(rect, false);
            numberRect.anchorMin = numberRect.anchorMax = numberRect.pivot = new Vector2(.5f, .5f);
            numberRect.anchoredPosition = new Vector2(0f, -3f);
            numberRect.sizeDelta = new Vector2(210f, 52f);
            banner._number = numberObject.GetComponent<TextMeshProUGUI>();
            banner._number.fontSize = 38f;
            banner._number.enableAutoSizing = true;
            banner._number.fontSizeMin = 18f;
            banner._number.fontSizeMax = 38f;
            banner._number.fontStyle = FontStyles.Bold;
            banner._number.color = new Color(1f, .9f, .75f);
            banner._number.alignment = TextAlignmentOptions.Center;
            banner._number.textWrappingMode = TextWrappingModes.NoWrap;
            banner._number.raycastTarget = false;
            banner._labelArt = StageLabelImages.Load(false);
            if (banner._labelArt != null)
            {
                var center = new Vector2(.5f, .5f);
                banner._numberArt = StageLabelImages.MakeRect(rect, "SubstageNumberArt", center, new Vector2(0, -5.5f));
                banner._bossNameArt = StageLabelImages.MakeImage(rect, "BossNameArt", center, new Vector2(0, -5.5f));
                numberObject.SetActive(false);
            }
            root.SetActive(false);
            return banner;
        }

        public void Show(int stage, int substage)
        {
            _number.text = StageText.SubstageLabel(stage, substage);
            if (_labelArt != null)
                _labelArt.SetNumber(_numberArt, _bossNameArt, stage, substage, .78f * 360f / 2172f, 29f);
            _elapsed = 0f;
            gameObject.SetActive(true);
            ApplyAnimation();
        }

        public void Hide() => gameObject.SetActive(false);

        private void Update()
        {
            _elapsed += Time.deltaTime;
            ApplyAnimation();
        }

        private void ApplyAnimation()
        {
            const float enterDuration = .24f;
            const float exitDuration = .24f;
            float duration = (float)StageProgress.SubstageIntroSeconds;
            float enter = Mathf.Clamp01(_elapsed / enterDuration);
            float exit = Mathf.Clamp01((duration - _elapsed) / exitDuration);
            float eased = 1f - (1f - enter) * (1f - enter);
            _group.alpha = Mathf.Min(enter, exit);
            _rect.anchoredPosition = new Vector2(-24f * (1f - eased), 0f);
            _rect.localScale = Vector3.one * (.92f + .08f * eased);
        }
    }
}
