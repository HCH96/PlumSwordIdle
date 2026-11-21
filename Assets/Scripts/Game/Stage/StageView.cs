using System;
using PlumSword.Game.Combat;
using PlumSword.Core.Stage;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Stage
{
    /// <summary>
    /// 스테이지 화면: 전투 화면만 가리는 페이드 막, 스테이지 글자, 보스 이름, 진행 바 겸 보스 체력 바, 소개 패널.
    /// 글자·보스 이름·바·소개 패널은 Initialize 에서 한 번, 그 뒤로 stage.Changed 가 올 때마다 다시 그린다.
    /// 페이드 막의 진하기만 매 프레임 Update 에서 stage.Tick 을 부른 뒤 맞춘다.
    /// 계산은 하지 않고 StageProgress 의 상태를 읽어 그리기만 한다.
    /// 사양: docs/spec/stage.md "화면".
    /// </summary>
    public sealed class StageView : MonoBehaviour
    {
        // 위치와 크기, 글자 크기, 색은 임시값 (docs/spec/stage.md "화면").
        private static readonly Vector2 StageLabelPosition = new Vector2(0f, -7f);
        private static readonly Vector2 StageLabelSize = new Vector2(290f, 16f);
        private const float StageLabelFontSize = 13f;

        private static readonly Vector2 BossNamePosition = new Vector2(0f, -22f);
        private static readonly Vector2 BossNameSize = new Vector2(260f, 14f);
        private const float BossNameFontSize = 11f;

        private static readonly Vector2 BarPosition = new Vector2(0f, -40f);
        private static readonly Vector2 BarSize = new Vector2(276f, 7f);
        private static readonly Color BarBackgroundColor = new Color(0.09f, 0.055f, 0.08f);
        private static readonly Color NormalFillColor = new Color(0.80f, 0.38f, 0.48f);
        private static readonly Color BossFillColor = new Color(0.72f, 0.16f, 0.20f);
        private static readonly Color JongnamFillColor = new Color(0.58f, 0.72f, 0.86f);
        private static readonly Color PanelColor = new Color(0.12f, 0.075f, 0.12f, 0.94f);
        private static readonly Color BorderColor = new Color(0.66f, 0.47f, 0.34f);
        private static readonly Color PlumColor = new Color(0.91f, 0.58f, 0.66f);
        private const float JongnamHeaderWidth = 260f;
        private const float JongnamHudScale = JongnamHeaderWidth / 380f;

        private static readonly Vector2 IntroPanelSize = new Vector2(640f, 72f);
        private static readonly Color IntroPanelColor = new Color(0f, 0f, 0f, 0.6f);
        private const float IntroTitleFontSize = 22f;
        private const float IntroLineFontSize = 14f;

        private static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        private StageProgress _stage;
        private float _defeatAnimationRemaining;
        private StagePhase _previousPhase;
        private bool _failedTransition;
        private Image _fade;
        private TextMeshProUGUI _stageLabel;
        private TextMeshProUGUI _bossName;
        private GameObject _fallbackHeader;
        private GameObject _jongnamHeader;
        private GameObject _jongnamTitle;
        private RectTransform _barRect;
        private GameObject _bossNameObject;
        private RectTransform _barFillRect;
        private Image _barFill;
        private GameObject _introPanelObject;
        private TextMeshProUGUI _introTitle;
        private TextMeshProUGUI _introLine;
        private bool _warned;
        private SubstagePreview _substagePreview;
        private SubstageIntroBanner _substageIntro;

        /// <summary>
        /// canvas 아래에 페이드 막, 스테이지 글자, 보스 이름, 진행 바, 소개 패널을 만들고 지금 상태로 바로 표시한다.
        /// stage.Changed 를 구독하고 OnDestroy 에서 끊는다.
        /// </summary>
        public void Initialize(StageProgress stage, Canvas canvas)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));

            Unsubscribe();
            _stage = stage;
            _defeatAnimationRemaining = 0f;
            _previousPhase = stage.Phase;
            _failedTransition = false;

            CreateFade(canvas);
            CreateHeaderPanel(canvas);
            CreateJongnamHeader(canvas);
            CreateStageLabel(canvas);
            CreateJongnamTitle(canvas);
            CreateBossName(canvas);
            CreateBar(canvas);
            _substagePreview = SubstagePreview.Create(canvas);
            // 스테이지 글자·보스 이름·진행 바보다 나중에 만들어 그 위에 그린다.
            CreateIntroPanel(canvas);
            _substageIntro = SubstageIntroBanner.Create(canvas);

            Redraw();
            ApplyFade();

            _stage.Changed += OnChanged;
            _stage.StageFailed += OnStageFailed;
        }

        private void Update()
        {
            if (_stage == null) return;

            float deltaTime = Time.deltaTime;
            if (_defeatAnimationRemaining > 0f)
            {
                float used = Mathf.Min(deltaTime, _defeatAnimationRemaining);
                _defeatAnimationRemaining -= used;
                deltaTime -= used;
            }
            _stage.Tick(deltaTime);
            ApplyFade();
        }

        private void OnStageFailed(int stageNumber)
        {
            _failedTransition = true;
            // 패배 판정과 전투 정지는 즉시, 화면 전환만 쓰러짐 연출 뒤에 진행한다.
            if (Resources.Load<Texture2D>(DiscipleSprites.DownPath) != null)
                _defeatAnimationRemaining = DiscipleSprites.DownDuration;
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
            if (_stageLabel != null) _stageLabel.color = new Color(0.98f, 0.88f, 0.78f);
        }

        private void CreateHeaderPanel(Canvas canvas)
        {
            var panel = new GameObject("StageHeader", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(canvas.transform, false);
            PlaceTopCenter(rect, new Vector2(0f, -3f), new Vector2(340f, 53f));
            var background = panel.GetComponent<Image>();
            _fallbackHeader = panel;
            background.color = PanelColor;
            background.raycastTarget = false;

            // 각진 금속 테두리와 다섯 꽃잎 장식은 UI 요소로 조립한다.
            AddHeaderPiece(rect, "Top", new Vector2(0f, -1f), new Vector2(324f, 1f), BorderColor);
            AddHeaderPiece(rect, "Bottom", new Vector2(0f, -51f), new Vector2(324f, 1f), BorderColor);
            AddHeaderPiece(rect, "Left", new Vector2(-168f, -26f), new Vector2(1f, 39f), BorderColor);
            AddHeaderPiece(rect, "Right", new Vector2(168f, -26f), new Vector2(1f, 39f), BorderColor);
            AddFlower(rect, -153f);
            AddFlower(rect, 153f);
            AddHeaderPiece(rect, "BarFrame", new Vector2(0f, -35f), new Vector2(280f, 11f), BorderColor);
        }

        private void CreateJongnamHeader(Canvas canvas)
        {
            var sprite = Resources.Load<Sprite>("Art/UI/jongnam_stage_hud");
            if (sprite == null) return;
            var panel = new GameObject("JongnamStageHeader", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(canvas.transform, false);
            PlaceTopCenter(rect, Vector2.zero, new Vector2(JongnamHeaderWidth, JongnamHeaderWidth * 724f / 2172f));
            var image = panel.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            _jongnamHeader = panel;
        }

        private void CreateJongnamTitle(Canvas canvas)
        {
            var sprite = Resources.Load<Sprite>("Art/UI/jongnam_title_plum_duel");
            if (sprite == null) return;
            var title = new GameObject("JongnamStageTitle", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)title.transform;
            rect.SetParent(canvas.transform, false);
            PlaceTopCenter(rect, new Vector2(0f, -44f * JongnamHudScale - 3f),
                new Vector2(110f * JongnamHudScale, 18f * JongnamHudScale));
            var image = title.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            _jongnamTitle = title;
        }

        private static void AddFlower(RectTransform parent, float x)
        {
            var flower = new GameObject("PlumFlower", typeof(RectTransform));
            var rect = (RectTransform)flower.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = TopCenter;
            rect.anchoredPosition = new Vector2(x, -25f);
            rect.sizeDelta = Vector2.zero;
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 2f / 5f;
                var petal = AddHeaderPiece(rect, "Petal", new Vector2(Mathf.Sin(angle) * 3f, Mathf.Cos(angle) * 3f), new Vector2(4f, 4f), PlumColor);
                petal.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            AddHeaderPiece(rect, "Center", Vector2.zero, new Vector2(3f, 3f), BorderColor);
        }

        private static RectTransform AddHeaderPiece(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = TopCenter;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = obj.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
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
            _barRect = backgroundRect;
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

        // 화면 가운데의 소개 패널: 반투명 검은 바탕, 위쪽 절반에 제목, 아래쪽 절반에 문구.
        // 글자를 만들지 못해도 바탕은 남고, 보이기는 IsIntroShowing 을 따른다.
        private void CreateIntroPanel(Canvas canvas)
        {
            var panelObject = new GameObject("StageIntroPanel", typeof(RectTransform));
            var panelRect = (RectTransform)panelObject.transform;
            panelRect.SetParent(canvas.transform, false);
            panelRect.anchorMin = Center;
            panelRect.anchorMax = Center;
            panelRect.pivot = Center;
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = IntroPanelSize;

            var background = panelObject.AddComponent<Image>();
            background.color = IntroPanelColor;
            background.raycastTarget = false;

            _introPanelObject = panelObject;
            _introTitle = CreatePanelText(panelRect, "Title", new Vector2(0f, 0.5f), Vector2.one, IntroTitleFontSize);
            _introLine = CreatePanelText(panelRect, "Line", Vector2.zero, new Vector2(1f, 0.5f), IntroLineFontSize);
        }

        private TextMeshProUGUI CreatePanelText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float fontSize)
        {
            try
            {
                var textObject = new GameObject(name, typeof(RectTransform));
                var rect = (RectTransform)textObject.transform;
                rect.SetParent(parent, false);
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var label = textObject.AddComponent<TextMeshProUGUI>();
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

        private void OnChanged()
        {
            if (_stage.Phase == StagePhase.FadingOut && _previousPhase == StagePhase.Playing && !_failedTransition)
            {
                if (_stage.DisplayStageNumber == 1 && HasJongnamDownSheets())
                    _defeatAnimationRemaining = JongnamSprites.DownDuration;
                else if (_stage.DisplayStageNumber == 2 && _stage.SubstageNumber > StageLayout.NormalSubstageCount
                    && HasDownSheet("Art/Enemies/gaebang_boss_down", 96, 25))
                    _defeatAnimationRemaining = 25 * JongnamSprites.DownFrameSeconds;
                else if (_stage.DisplayStageNumber == 2 && _stage.SubstageNumber <= StageLayout.NormalSubstageCount
                    && HasDownSheet("Art/Enemies/gaebang_staff_down", 96, 16)
                    && HasDownSheet("Art/Enemies/gaebang_unarmed_down", 104, 16))
                    _defeatAnimationRemaining = 16 * JongnamSprites.DownFrameSeconds;
                else if (_stage.DisplayStageNumber == 3 && _stage.SubstageNumber <= StageLayout.NormalSubstageCount
                    && HasDownSheet("Art/Enemies/namgung_male_down", 76, 17)
                    && HasDownSheet("Art/Enemies/namgung_female_down", 76, 17))
                    _defeatAnimationRemaining = JongnamSprites.DownDuration;
                else if (_stage.DisplayStageNumber == 3 && _stage.SubstageNumber > StageLayout.NormalSubstageCount
                    && HasDownSheet("Art/Enemies/namgung_boss_down", 100, 17))
                    _defeatAnimationRemaining = JongnamSprites.DownDuration;
                else if (_stage.DisplayStageNumber == 4 && _stage.SubstageNumber > StageLayout.NormalSubstageCount
                    && HasDownSheet("Art/Enemies/tianmo_boss_down", 104, 17))
                    _defeatAnimationRemaining = JongnamSprites.DownDuration;
                else if (_stage.DisplayStageNumber == 4 && _stage.SubstageNumber <= StageLayout.NormalSubstageCount
                    && HasDownSheet("Art/Enemies/tianmo_swordsman_down", 64, 17)
                    && HasDownSheet("Art/Enemies/tianmo_guard_down", 64, 17))
                    _defeatAnimationRemaining = JongnamSprites.DownDuration;
            }
            if (_stage.Phase != StagePhase.FadingOut) _failedTransition = false;
            _previousPhase = _stage.Phase;
            Redraw();
        }

        private bool HasJongnamDownSheets()
        {
            if (_stage.SubstageNumber > StageLayout.NormalSubstageCount)
                return HasDownSheet("Art/Enemies/jongnam_boss_down", 100);
            return HasDownSheet("Art/Enemies/jongnam_male_down", 76)
                && HasDownSheet("Art/Enemies/jongnam_female_down", 76);
        }

        private static bool HasDownSheet(string path, int size, int count = JongnamSprites.DownFrameCount)
        {
            var texture = Resources.Load<Texture2D>(path);
            return texture != null && texture.width == size * count && texture.height == size;
        }

        // 글자·보스 이름·바를 지금 상태로 다시 그린다. 비율과 보스 여부는 StageProgress 의 값을 그대로 쓴다.
        private void Redraw()
        {
            if (_stage == null) return;

            bool bossActive = _stage.IsBossActive;
            float ratio = Mathf.Clamp01((float)_stage.BarRatio);
            bool useJongnamArt = _jongnamHeader != null;
            bool useJongnamTitle = useJongnamArt && !bossActive && _jongnamTitle != null;
            bool usePreview = _substagePreview != null;
            if (_substagePreview != null) _substagePreview.gameObject.SetActive(usePreview);
            if (usePreview) _substagePreview.SetProgress(_stage.DisplayStageNumber, _stage.SubstageNumber);
            if (_fallbackHeader != null) _fallbackHeader.SetActive(!useJongnamArt && !usePreview);
            if (_jongnamHeader != null) _jongnamHeader.SetActive(useJongnamArt && !usePreview);
            if (_jongnamTitle != null) _jongnamTitle.SetActive(useJongnamTitle && !usePreview);
            if (_barRect != null) _barRect.gameObject.SetActive(!usePreview || bossActive);
            if (_stageLabel != null)
            {
                _stageLabel.gameObject.SetActive(!usePreview && (!useJongnamArt || (!bossActive && !useJongnamTitle)));
                _stageLabel.fontSize = useJongnamArt ? 10f : StageLabelFontSize;
                PlaceTopCenter((RectTransform)_stageLabel.transform, useJongnamArt ? new Vector2(0f, -44f * JongnamHudScale - 3f) : StageLabelPosition,
                    useJongnamArt ? new Vector2(250f * JongnamHudScale, 16f * JongnamHudScale) : StageLabelSize);
            }
            if (_bossName != null)
            {
                _bossName.fontSize = useJongnamArt ? 9f : BossNameFontSize;
                PlaceTopCenter((RectTransform)_bossName.transform, useJongnamArt ? new Vector2(0f, -44f * JongnamHudScale - 3f) : BossNamePosition,
                    useJongnamArt ? new Vector2(260f * JongnamHudScale, 16f * JongnamHudScale) : BossNameSize);
                if (usePreview) PlaceTopCenter((RectTransform)_bossName.transform, new Vector2(0, -111), BossNameSize);
            }
            if (_barRect != null)
                PlaceTopCenter(_barRect, useJongnamArt ? new Vector2(0f, -76f * JongnamHudScale) : BarPosition,
                    useJongnamArt ? new Vector2(264f * JongnamHudScale, 12.2f * JongnamHudScale) : BarSize);
            if (_barRect != null && usePreview && bossActive)
                PlaceTopCenter(_barRect, new Vector2(0, -126), new Vector2(180, 7));

            if (_barFillRect != null)
            {
                _barFillRect.anchorMin = Vector2.zero;
                _barFillRect.anchorMax = new Vector2(ratio, 1f);
                _barFillRect.offsetMin = Vector2.zero;
                _barFillRect.offsetMax = Vector2.zero;
            }

            if (_barFill != null)
            {
                _barFill.color = bossActive ? BossFillColor : (useJongnamArt ? JongnamFillColor : NormalFillColor);
            }

            if (_bossNameObject != null && _bossNameObject.activeSelf != bossActive)
            {
                _bossNameObject.SetActive(bossActive);
            }

            if (_stageLabel != null)
            {
                try
                {
                    _stageLabel.text = StageText.RoundStageLabel(_stage.RoundNumber, _stage.DisplayStageNumber)
                        + " · " + _stage.DisplayStageNumber + "-" + _stage.SubstageNumber;
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

            RedrawIntro();
        }

        // 소개 패널: IsIntroShowing 일 때만 보이고, 제목과 문구는 지금 표시 번호의 것이다.
        private void RedrawIntro()
        {
            if (_introPanelObject == null) return;

            bool introShowing = _stage.IsIntroShowing;
            if (_introPanelObject.activeSelf != introShowing)
            {
                _introPanelObject.SetActive(introShowing);
            }

            if (_introTitle != null)
            {
                _introTitle.gameObject.SetActive(_stage.IsIntroShowing);
                try
                {
                    _introTitle.text = StageText.ThemeName(_stage.DisplayStageNumber);
                }
                catch (Exception e)
                {
                    WarnOnce(e);
                }
            }

            if (_introLine != null)
            {
                _introLine.gameObject.SetActive(_stage.IsIntroShowing);
                try
                {
                    _introLine.text = StageText.IntroLine(_stage.DisplayStageNumber);
                }
                catch (Exception e)
                {
                    WarnOnce(e);
                }
            }
            if (_substageIntro != null)
            {
                if (_stage.IsSubstageIntroShowing)
                {
                    if (!_substageIntro.gameObject.activeSelf)
                        _substageIntro.Show(_stage.DisplayStageNumber, _stage.SubstageNumber);
                }
                else if (_substageIntro.gameObject.activeSelf) _substageIntro.Hide();
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
                _stage.StageFailed -= OnStageFailed;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _stage = null;
        }
    }
}
