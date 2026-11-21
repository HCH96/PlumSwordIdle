using System.Collections.Generic;
using UnityEngine;

namespace PlumSword.Game.Audio
{
    public sealed class AudioClipCache
    {
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        public AudioClip Load(string path)
        {
            if (path == null) return null;
            if (clips.TryGetValue(path, out var clip)) return clip;
            clip = Resources.Load<AudioClip>(path);
            clips.Add(path, clip);
            if (clip == null) Debug.LogWarning("[Audio] 소리 파일을 찾지 못했습니다: " + path);
            return clip;
        }
    }
}
