using System;
using PlumSword.Core.StartMenu;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.StartMenu
{
    public sealed class SettingsView : MonoBehaviour
    {
        private GameObject panel, toTitle, quit, failure;
        private Slider bgmSlider, sfxSlider;
        private TextMeshProUGUI bgmLabel, sfxLabel;
        private TextMeshProUGUI bgmValue, sfxValue;
        private VolumeSettings volume;
        private LeaveGame leaveGame;
        private Action saveVolume;

        public bool IsOpen => panel != null && panel.activeSelf;
        public SettingsMode Mode { get; private set; } = SettingsMode.Title;

        public void Initialize(Canvas canvas, VolumeSettings volume, LeaveGame leaveGame, Action onReturnToTitle, Action onQuit, Action saveVolume)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (volume == null) throw new ArgumentNullException(nameof(volume));
            if (leaveGame == null) throw new ArgumentNullException(nameof(leaveGame));
            if (onReturnToTitle == null) throw new ArgumentNullException(nameof(onReturnToTitle));
            if (onQuit == null) throw new ArgumentNullException(nameof(onQuit));
            if (saveVolume == null) throw new ArgumentNullException(nameof(saveVolume));
            this.volume = volume;
            this.leaveGame = leaveGame;
            this.saveVolume = saveVolume;

            var gameButton = Button("GameSettingsButton", canvas.transform, new Vector2(8, -62), new Vector2(72, 20), new Color(.25f, .25f, .30f), StartMenuText.SettingsButton, 12);
            var gameRect = (RectTransform)gameButton.transform;
            gameRect.anchorMin = gameRect.anchorMax = Vector2.zero;
            gameRect.pivot = new Vector2(1, 0);
            gameRect.anchoredPosition = new Vector2(632, 60);
            gameButton.onClick.AddListener(() => Open(SettingsMode.InGame));

            panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            var overlay = (RectTransform)panel.transform;
            overlay.SetParent(canvas.transform, false);
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            var overlayImage = panel.GetComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, .5f); overlayImage.raycastTarget = true;
            var window = Rect("Window", overlay, Vector2.zero, new Vector2(360, 230));
            window.gameObject.AddComponent<Image>().color = new Color(.12f, .12f, .12f);
            Text("Title", window, new Vector2(0, 92), new Vector2(340, 24), StartMenuText.SettingsTitle, 18);
            bgmLabel = Text("BgmLabel", window, new Vector2(-120, 56), new Vector2(100, 18), StartMenuText.BgmLine(volume.Bgm), 14, true);
            bgmSlider = MakeSlider("BgmSlider", window, new Vector2(50, 56));
            sfxLabel = Text("SfxLabel", window, new Vector2(-120, 24), new Vector2(100, 18), StartMenuText.SfxLine(volume.Sfx), 14, true);
            sfxSlider = MakeSlider("SfxSlider", window, new Vector2(50, 24));
            bgmSlider.onValueChanged.AddListener(value => { volume.SetBgm((int)value); UpdateLabels(); });
            sfxSlider.onValueChanged.AddListener(value => { volume.SetSfx((int)value); UpdateLabels(); });
            var titleButton = Button("ToTitleButton", window, new Vector2(-85, -18), new Vector2(150, 32), new Color(.25f, .25f, .30f), StartMenuText.ToTitleButton, 14);
            var quitButton = Button("QuitButton", window, new Vector2(85, -18), new Vector2(150, 32), new Color(.45f, .20f, .20f), StartMenuText.QuitButton, 14);
            toTitle = titleButton.gameObject; quit = quitButton.gameObject;
            var failRect = Rect("LeaveFailedMessage", window, new Vector2(0, -48), new Vector2(340, 16));
            failure = failRect.gameObject;
            AddText(failRect, StartMenuText.LeaveFailedMessage, 12, false, new Color(1, .35f, .35f));
            titleButton.onClick.AddListener(() => Leave(onReturnToTitle));
            quitButton.onClick.AddListener(() => Leave(onQuit));
            var closeButton = Button("CloseButton", window, new Vector2(0, -86), new Vector2(120, 28), new Color(.25f, .25f, .30f), StartMenuText.CloseButton, 14);
            closeButton.onClick.AddListener(Close);
            ApplyArtwork(gameButton, window, titleButton, quitButton, closeButton);
            foreach (var item in new[] { gameButton, titleButton, quitButton, closeButton })
                item.gameObject.AddComponent<SettingsControlEffect>().InitializeButton(item);
            foreach (var slider in new[] { bgmSlider, sfxSlider })
                slider.gameObject.AddComponent<SettingsControlEffect>().InitializeSlider(slider);
            toTitle.SetActive(false); quit.SetActive(false); failure.SetActive(false);
            panel.SetActive(false);
        }

        public void Open(SettingsMode mode)
        {
            Mode = mode;
            bgmSlider.SetValueWithoutNotify(volume.Bgm); sfxSlider.SetValueWithoutNotify(volume.Sfx);
            UpdateLabels();
            bool inGame = mode == SettingsMode.InGame;
            toTitle.SetActive(inGame); quit.SetActive(inGame); failure.SetActive(inGame && leaveGame.HasFailed);
            panel.SetActive(true); panel.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (!IsOpen) return;
            panel.SetActive(false);
            saveVolume();
        }

        private void Leave(Action callback)
        {
            if (!leaveGame.TryLeave(DateTime.UtcNow.Ticks)) { failure.SetActive(true); return; }
            saveVolume(); callback();
        }

        private void OnApplicationPause(bool paused) { if (paused) saveVolume?.Invoke(); }
        private void OnApplicationQuit() { saveVolume?.Invoke(); }
        private void UpdateLabels()
        {
            if (bgmLabel != null) bgmLabel.text = StartMenuText.BgmLine(volume.Bgm);
            if (sfxLabel != null) sfxLabel.text = StartMenuText.SfxLine(volume.Sfx);
            if (bgmValue != null) bgmValue.text = volume.Bgm.ToString();
            if (sfxValue != null) sfxValue.text = volume.Sfx.ToString();
        }

        private void ApplyArtwork(Button gameButton, RectTransform window, Button titleButton, Button quitButton, Button closeButton)
        {
            var background = Art("window");
            var track = Art("slider_track");
            var handle = Art("slider_handle");
            var bgmArt = Art("label_bgm");
            var sfxArt = Art("label_sfx");
            var titleArt = Art("button_title");
            var quitArt = Art("button_quit");
            var closeArt = Art("button_close");
            var settingsArt = Art("button_settings");
            if (settingsArt != null)
            {
                ((RectTransform)gameButton.transform).sizeDelta = new Vector2(112, 32);
                SetButtonArt(gameButton, settingsArt);
            }
            if (background == null || track == null || handle == null || bgmArt == null || sfxArt == null
                || titleArt == null || quitArt == null || closeArt == null) return;

            window.sizeDelta = new Vector2(350, 233.33f);
            var windowImage = window.GetComponent<Image>();
            windowImage.sprite = background; windowImage.color = Color.white;
            window.Find("Title").gameObject.SetActive(false);
            if (bgmLabel != null) bgmLabel.color = Color.clear;
            if (sfxLabel != null) sfxLabel.color = Color.clear;
            ArtImage("BgmArt", window, new Vector2(-91, 40), new Vector2(54, 15), bgmArt);
            ArtImage("SfxArt", window, new Vector2(-91, 10), new Vector2(42, 15), sfxArt);
            bgmValue = Text("BgmValue", window, new Vector2(112, 40), new Vector2(30, 18), volume.Bgm.ToString(), 12);
            sfxValue = Text("SfxValue", window, new Vector2(112, 10), new Vector2(30, 18), volume.Sfx.ToString(), 12);
            StyleSlider(bgmSlider, track, handle, new Vector2(15, 40));
            StyleSlider(sfxSlider, track, handle, new Vector2(15, 10));
            PlaceButton(titleButton, new Vector2(-69, -30), new Vector2(120, 28), titleArt);
            PlaceButton(quitButton, new Vector2(69, -30), new Vector2(120, 28), quitArt);
            PlaceButton(closeButton, new Vector2(0, -68), new Vector2(108, 26), closeArt);
            var fail = (RectTransform)failure.transform;
            fail.anchoredPosition = new Vector2(0, -52);
        }

        private static Sprite Art(string name) => Resources.Load<Sprite>("Art/UI/Settings/" + name);

        private static void ArtImage(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite)
        {
            var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
        }

        private static void SetButtonArt(Button button, Sprite sprite)
        {
            var image = button.GetComponent<Image>();
            image.sprite = sprite; image.color = Color.white; image.preserveAspect = true;
            var text = button.transform.Find("Text");
            if (text != null) text.gameObject.SetActive(false);
        }

        private static void PlaceButton(Button button, Vector2 position, Vector2 size, Sprite sprite)
        {
            var rect = (RectTransform)button.transform;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            SetButtonArt(button, sprite);
        }

        private static void StyleSlider(Slider slider, Sprite track, Sprite handle, Vector2 position)
        {
            var rect = (RectTransform)slider.transform;
            rect.anchoredPosition = position; rect.sizeDelta = new Vector2(130, 14);
            var background = slider.GetComponent<Image>();
            background.sprite = track; background.color = Color.white; background.preserveAspect = true;
            var fillArea = (RectTransform)rect.Find("Fill Area");
            fillArea.offsetMin = new Vector2(4, 3); fillArea.offsetMax = new Vector2(-4, -3);
            slider.fillRect.GetComponent<Image>().color = new Color(.84f, .48f, .55f);
            var handleImage = slider.handleRect.GetComponent<Image>();
            handleImage.sprite = handle; handleImage.preserveAspect = true; handleImage.color = Color.white;
            slider.handleRect.sizeDelta = new Vector2(17, 18);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }

        private static TextMeshProUGUI Text(string name, Transform parent, Vector2 position, Vector2 size, string value, float fontSize, bool left = false)
            => AddText(Rect(name, parent, position, size), value, fontSize, left, Color.white);

        private static TextMeshProUGUI AddText(RectTransform rect, string value, float fontSize, bool left, Color color)
        {
            try
            {
                var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
                text.fontSize = fontSize; text.color = color; text.alignment = left ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
                text.textWrappingMode = TextWrappingModes.NoWrap; text.raycastTarget = false; text.text = value; return text;
            }
            catch (Exception e) { Debug.LogWarning("SettingsView: TMP Essentials required: " + e.Message); return null; }
        }

        private static Button Button(string name, Transform parent, Vector2 position, Vector2 size, Color color, string label, float fontSize)
        {
            var rect = Rect(name, parent, position, size);
            rect.gameObject.AddComponent<Image>().color = color;
            var button = rect.gameObject.AddComponent<Button>(); button.transition = Selectable.Transition.None;
            Text("Text", rect, Vector2.zero, size, label, fontSize); return button;
        }

        private static Slider MakeSlider(string name, Transform parent, Vector2 position)
        {
            var rect = Rect(name, parent, position, new Vector2(200, 16));
            rect.gameObject.AddComponent<Image>().color = new Color(.30f, .30f, .30f);
            var fillArea = Rect("Fill Area", rect, Vector2.zero, Vector2.zero);
            fillArea.anchorMin = Vector2.zero; fillArea.anchorMax = Vector2.one; fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;
            var fill = Rect("Fill", fillArea, Vector2.zero, Vector2.zero);
            fill.gameObject.AddComponent<Image>().color = new Color(.20f, .45f, .25f);
            var handleArea = Rect("Handle Slide Area", rect, Vector2.zero, Vector2.zero);
            handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(6, 0); handleArea.offsetMax = new Vector2(-6, 0);
            var handle = Rect("Handle", handleArea, Vector2.zero, new Vector2(12, 20));
            var handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = Color.white;
            var slider = rect.gameObject.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 100; slider.wholeNumbers = true;
            slider.direction = Slider.Direction.LeftToRight; slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handleImage;
            return slider;
        }
    }
}
