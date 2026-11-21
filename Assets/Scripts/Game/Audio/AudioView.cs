using System;
using System.Collections.Generic;
using PlumSword.Core.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PlumSword.Game.Audio
{
    public sealed class AudioView : MonoBehaviour
    {
        private GameRoot root;
        private MusicFader fader;
        private SfxQueue queue;
        private SfxTriggers triggers;
        private UiSoundWatch watch;
        private SfxVoicePool voices;
        private AudioClipCache cache;
        private readonly AudioSource[] music = new AudioSource[2];
        private AudioSource[] sfx;
        private SfxId[] sfxIds;
        private int currentIndex;
        private int previousIndex = -1;
        private bool paused;
        private bool focused = true;

        public MusicTrack CurrentTrack => fader == null ? MusicTrack.None : fader.Current;
        public AudioSource CurrentMusicSource => fader == null || fader.Current == MusicTrack.None ? null : music[currentIndex];
        public IReadOnlyList<AudioSource> SfxSources => sfx;

        public void Initialize(GameRoot root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (this.root != null) throw new InvalidOperationException("AudioView is already initialized.");
            this.root = root;
            for (int i = 0; i < music.Length; i++) music[i] = CreateSource(true);
            sfx = new AudioSource[AudioCatalog.MaxSfxVoices];
            sfxIds = new SfxId[sfx.Length];
            for (int i = 0; i < sfx.Length; i++) sfx[i] = CreateSource(false);
            queue = new SfxQueue();
            triggers = new SfxTriggers(queue, root.Battle, root.StageProgress, root.StatUpgrades, root.Realm);
            watch = new UiSoundWatch(queue);
            fader = new MusicFader();
            voices = new SfxVoicePool();
            cache = new AudioClipCache();
            root.Volume.Changed += OnVolumeChanged;
            paused = false;
            focused = true;
            AudioListener.pause = false;
        }

        private AudioSource CreateSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0;
            return source;
        }

        private void LateUpdate()
        {
            if (root == null) return;
            UpdateMusic();
            UpdatePointer();
            watch.UpdateWindows(root.Settings.IsOpen,
                root.GetComponentInChildren<PlumSword.Game.OfflineReward.OfflineRewardView>(true) != null,
                root.GetComponentInChildren<PlumSword.Game.Save.SaveErrorView>(true) != null,
                root.OfflineRewardClaim != null && root.OfflineRewardClaim.HasPaid);
            double now = Time.unscaledTimeAsDouble;
            var pending = queue.Drain(now);
            if (!AudioMix.ShouldPlaySfx(root.Volume.SfxScale)) return;
            foreach (var id in pending)
            {
                var clip = cache.Load(AudioCatalog.SfxPath(id));
                if (clip == null) continue;
                int index = voices.Acquire(now, clip.length);
                var source = sfx[index];
                source.Stop();
                source.clip = clip;
                source.volume = (float)AudioMix.SfxVolume(id, root.Volume.SfxScale);
                sfxIds[index] = id;
                source.Play();
            }
        }

        private void UpdateMusic()
        {
            var desired = MusicRules.TrackFor(root.IsStartScreenShowing, root.StageProgress.DisplayStageNumber, root.StageProgress.Boss != null);
            var oldCurrent = fader.Current;
            if (fader.Request(desired))
            {
                if (previousIndex >= 0 && oldCurrent != MusicTrack.None)
                {
                    music[previousIndex].Stop();
                    previousIndex = -1;
                }
                if (oldCurrent != MusicTrack.None) previousIndex = currentIndex;
                if (desired != MusicTrack.None)
                {
                    currentIndex = previousIndex == 0 ? 1 : 0;
                    var source = music[currentIndex];
                    source.Stop();
                    source.clip = cache.Load(AudioCatalog.MusicPath(desired));
                    source.volume = 0;
                    if (source.clip != null) source.Play();
                }
            }
            fader.Tick(Time.unscaledDeltaTime);
            if (fader.Current != MusicTrack.None)
                music[currentIndex].volume = (float)AudioMix.MusicVolume(fader.Current, root.Volume.BgmScale, fader.CurrentWeight);
            if (previousIndex >= 0)
            {
                if (fader.Previous == MusicTrack.None)
                {
                    music[previousIndex].Stop();
                    previousIndex = -1;
                }
                else music[previousIndex].volume = (float)AudioMix.MusicVolume(fader.Previous, root.Volume.BgmScale, fader.PreviousWeight);
            }
        }

        private void UpdatePointer()
        {
            var pointer = Pointer.current;
            var events = EventSystem.current;
            if (pointer == null || events == null) return;
            if (pointer.press.wasPressedThisFrame)
            {
                var data = new PointerEventData(events) { position = pointer.position.ReadValue() };
                var results = new List<RaycastResult>();
                events.RaycastAll(data, results);
                var button = results.Count == 0 ? null : results[0].gameObject.GetComponentInParent<Button>();
                watch.PressStarted(button != null, button != null && button.IsInteractable());
            }
            if (pointer.press.wasReleasedThisFrame) watch.PressEnded();
        }

        private void OnVolumeChanged()
        {
            for (int i = 0; i < sfx.Length; i++)
                if (sfx[i].isPlaying) sfx[i].volume = (float)AudioMix.SfxVolume(sfxIds[i], root.Volume.SfxScale);
        }

        private void OnApplicationPause(bool pause) { paused = pause; AudioListener.pause = AudioMix.ShouldPauseAll(paused, focused); }
        private void OnApplicationFocus(bool focus) { focused = focus; AudioListener.pause = AudioMix.ShouldPauseAll(paused, focused); }
        private void OnDestroy()
        {
            triggers?.Dispose();
            if (root != null && root.Volume != null) root.Volume.Changed -= OnVolumeChanged;
        }
    }
}
