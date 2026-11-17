using System;
using PlumSword.Core.Save;
using TMPro;
using UnityEngine;

namespace PlumSword.Game.Save
{
    public sealed class SaveView : MonoBehaviour
    {
        private AutoSave autoSave;
        private GameObject label;

        public void Initialize(AutoSave autoSave, Canvas canvas)
        {
            if (autoSave == null) throw new ArgumentNullException(nameof(autoSave));
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (this.autoSave != null) this.autoSave.FailedChanged -= SetFailed;
            if (label != null) Destroy(label);
            this.autoSave = autoSave;
            label = new GameObject("SaveFailedText", typeof(RectTransform));
            var rect = (RectTransform)label.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-8, -62);
            rect.sizeDelta = new Vector2(160, 16);
            try
            {
                var text = label.AddComponent<TextMeshProUGUI>();
                text.fontSize = 14;
                text.color = new Color(1f, 0.35f, 0.35f);
                text.alignment = TextAlignmentOptions.Right;
                text.raycastTarget = false;
                text.text = SaveText.SaveFailed;
            }
            catch (Exception e) { Debug.LogWarning("SaveView: TMP Essentials required: " + e.Message); }
            SetFailed(autoSave.HasFailed);
            autoSave.FailedChanged += SetFailed;
        }

        private void SetFailed(bool failed) { if (label != null) label.SetActive(failed); }
        private void LateUpdate()
        {
            if (autoSave == null) return;
            autoSave.Tick(Time.unscaledDeltaTime);
            autoSave.Flush(DateTime.UtcNow.Ticks);
        }
        private void OnApplicationPause(bool paused) { if (paused && autoSave != null) autoSave.SaveNow(DateTime.UtcNow.Ticks); }
        private void OnApplicationQuit() { if (autoSave != null) autoSave.SaveNow(DateTime.UtcNow.Ticks); }
        private void OnDestroy() { if (autoSave != null) autoSave.FailedChanged -= SetFailed; }
    }
}
