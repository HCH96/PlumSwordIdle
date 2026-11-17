using System;

namespace PlumSword.Core.StartMenu
{
    public sealed class VolumeSettings
    {
        public const int MinVolume = 0;
        public const int MaxVolume = 100;
        public const int DefaultVolume = 80;

        public VolumeSettings(int bgm = DefaultVolume, int sfx = DefaultVolume)
        {
            Bgm = Clamp(bgm);
            Sfx = Clamp(sfx);
        }

        public int Bgm { get; private set; }
        public int Sfx { get; private set; }
        public double BgmScale => Bgm / 100.0;
        public double SfxScale => Sfx / 100.0;

        public event Action Changed;

        public void SetBgm(int volume)
        {
            int value = Clamp(volume);
            if (Bgm == value) return;
            Bgm = value;
            Changed?.Invoke();
        }

        public void SetSfx(int volume)
        {
            int value = Clamp(volume);
            if (Sfx == value) return;
            Sfx = value;
            Changed?.Invoke();
        }

        private static int Clamp(int volume)
        {
            if (volume < MinVolume) return MinVolume;
            if (volume > MaxVolume) return MaxVolume;
            return volume;
        }
    }
}
