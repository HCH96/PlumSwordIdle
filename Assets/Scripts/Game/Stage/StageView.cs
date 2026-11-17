using System;
using PlumSword.Core.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Stage
{
    /// <summary>
    /// 스테이지 화면: 전투 화면만 가리는 페이드 막, 스테이지 글자, 보스 이름, 진행 바 겸 보스 체력 바.
    /// 글자·보스 이름·바는 Initialize 에서 한 번, 그 뒤로 stage.Changed 가 올 때마다 다시 그린다.
    /// 페이드 막의 진하기만 매 프레임 Update 에서 stage.Tick 을 부른 뒤 맞춘다.
    /// 계산은 하지 않고 StageProgress 의 상태를 읽어 그리기만 한다.
    /// 사양: docs/spec/stage.md "화면".
    /// </summary>
    public sealed class StageView : MonoBehaviour
    {
        // 위치와 크기, 글자 크기, 색은 임시값 (docs/spec/stage.md "화면").
        private static readonly Vector2 StageLabelPosition = new Vector2(0f, -8f);
        private static readonly Vector2 StageLabelSize = new Vector2(600f, 16f);
        private const float StageLabelFontSize = 16f;

        private static readonly Vector2 BossNamePosition = new Vector2(0f, -26f);
        private static readonly Vector2 BossNameSize = new Vector2(300f, 16f);
        private const float BossNameFontSize = 14f;

        private static readonly Vector2 BarPosition = new Vector2(0f, -44f);
        private static readonly Vector2 BarSize = new Vector2(300f, 12f);
        private static readonly Color BarBackgroundColor = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color NormalFillColor = new Color(0.95f, 0.80f, 0.25f);
        private static readonly Color BossFillColor = new Color(0.85f, 0.20f, 0.20f);

        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);

        private StageProgress _stage;
        private Image _fade;
        private TextMeshProUGUI _stageLabel;
        private TextMeshProUGUI _bossName;
        private GameObject _bossNameObject;
        private RectTransform _barFillRect;
        private Image _barFill;
        private bool _warned;

        /// <summary>
        /// canvas 아래에 페이드 막, 스테이지 글자, 보스 이름, 진행 바를 만들고 지금 상태로 바로 표시한다.
        /// stage.Changed 를 구독하고 OnDestroy 에서 끊는다.
        /// </summary>
        public void Initialize(StageProgress stage, Canvas canvas)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));

            Unsubscribe();
            _stage = stage;

            CreateFade(canvas);
            CreateStageLabel(canvas);
            CreateBossName(canvas);
            CreateBar(canvas);

            Redraw();
            ApplyFade();

            _stage.Changed += OnChanged;
        }

        private void Update()
        {
            if (_stage == null) return;

            _stage.Tick(Time.deltaTime);
            ApplyFade();
        }

        private void CreateFade(Canvas canvas)
        {
            var fadeObject = new GameObject("StageFade", typeof(RectTransform));
            var rect = (RectTransform)fadeObject.transform;
            rect.SetParent(canvas.transform, false);
            StretchFull(rect);
            // 캔버스의 첫 번째 자식: 전투 화면만 가리고 다른 UI 는 막 위에 보인다.
            rect.SetAsFirstSibling();

            var image = fadeObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;
            _fade = image;
        }

        private void CreateStageLabel(Canvas canvas)
        {
            _stageLabel = CreateText(canvas, "StageLabel", StageLabelPosition, StageLabelSize, StageLabelFontSize);
        }

        private void CreateBossName(Canvas canvas)
        {
            _bossName = CreateText(canvas, "BossName", BossNamePosition, BossNameSize, BossNameFontSize);
            if (_bossName == null) return;

            _bossNameObject = _bossName.gameObject;
        }

        private TextMeshProUGUI CreateText(Canvas canvas, string name, Vector2 position, Vector2 size, float fontSize)
        {
            GameObject labelObject = null;
            try
            {
                labelObject = new GameObject(name, typeof(RectTransform));
                var rect = (RectTransform)labelObject.transform;
                rect.SetParent(canvas.transform, false);
                PlaceTopCenter(rect, position, size);

                var label = labelObject.AddComponent<TextMeshProUGUI>();
                label.fontSize = fontSize;
                label.color = Color.white;
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.raycastTarget = false;
                return label;
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return null;
            }
        }

        private void CreateBar(Canvas canvas)
        {
            var backgroundObject = new GameObject("StageBar", typeof(RectTransform));
            var backgroundRect = (RectTransform)backgroundObject.transform;
            backgroundRect.SetParent(canvas.transform, false);
            PlaceTopCenter(backgroundRect, BarPosition, BarSize);

            var background = backgroundObject.AddComponent<Image>();
            background.color = BarBackgroundColor;
            background.raycastTarget = false;

            var fillObject = new GameObject("Fill", typeof(RectTransform));
            var fillRect = (RectTransform)fillObject.transform;
            fillRect.SetParent(backgroundRect, false);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var fill = fillObject.AddComponent<Image>();
            fill.color = NormalFillColor;
            fill.raycastTarget = false;

            _barFillRect = fillRect;
            _barFill = fill;
        }

        private void OnChanged()
        {
            Redraw();
        }

        // 글자·보스 이름·바를 지금 상태로 다시 그린다. 비율과 보스 여부는 StageProgress 의 값을 그대로 쓴다.
        private void Redraw()
        {
            if (_stage == null) return;

            bool bossActive = _stage.IsBossActive;
            float ratio = Mathf.Clamp01((float)_stage.BarRatio);

            if (_barFillRect != null)
            {
                _barFillRect.anchorMin = Vector2.zero;
                _barFillRect.anchorMax = new Vector2(ratio, 1f);
                _barFillRect.offsetMin = Vector2.zero;
                _barFillRect.offsetMax = Vector2.zero;
            }

            if (_barFill != null)
            {
                _barFill.color = bossActive ? BossFillColor : NormalFillColor;
            }

            if (_bossNameObject != null && _bossNameObject.activeSelf != bossActive)
            {
                _bossNameObject.SetActive(bossActive);
            }

            if (_stageLabel != null)
            {
                try
                {
                    _stageLabel.text = StageText.RoundStageLabel(_stage.RoundNumber, _stage.DisplayStageNumber);
                }
                catch (Exception e)
                {
                    WarnOnce(e);
                }
            }

            if (_bossName != null)
            {
                try
                {
                    _bossName.text = StageText.ThemedBossName(_stage.DisplayStageNumber);
                }
                catch (Exception e)
                {
                    WarnOnce(e);
                }
            }
        }

        private void ApplyFade()
        {
            if (_fade == null || _stage == null) return;
            _fade.color = new Color(0f, 0f, 0f, Mathf.Clamp01((float)_stage.FadeAlpha));
        }

        private static void PlaceTopCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = TopCenter;
            rect.anchorMax = TopCenter;
            rect.pivot = TopCenter;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void WarnOnce(Exception e)
        {
            if (_warned) return;
            _warned = true;
            Debug.LogWarning(
                "StageView: 스테이지 글자를 표시하지 못했습니다. TMP Essentials 가 필요합니다 " +
                "(Window > TextMeshPro > Import TMP Essential Resources). " + e.Message);
        }

        private void Unsubscribe()
        {
            if (_stage != null)
            {
                _stage.Changed -= OnChanged;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _stage = null;
        }
    }
}
