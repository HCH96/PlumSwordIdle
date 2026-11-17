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
            gameRect.anchorMin = gameRect.anchorMax = gameRect.pivot = new Vector2(0, 1);
            gameRect.anchoredPosition = new Vector2(8, -62);
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
            Button("CloseButton", window, new Vector2(0, -86), new Vector2(120, 28), new Color(.25f, .25f, .30f), StartMenuText.CloseButton, 14).onClick.AddListener(Close);
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
