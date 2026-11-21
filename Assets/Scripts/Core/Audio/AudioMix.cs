namespace PlumSword.Core.Audio
{
    public static class AudioMix
    {
        public static double MusicVolume(MusicTrack track, double bgmScale, double weight)
        {
            return AudioCatalog.MusicBaseVolume(track) * Clamp01(bgmScale) * Clamp01(weight);
        }

        public static double SfxVolume(SfxId id, double sfxScale)
        {
            return AudioCatalog.SfxBaseVolume(id) * Clamp01(sfxScale);
        }

        public static bool ShouldPlaySfx(double sfxScale) { return sfxScale > 0; }
        public static bool ShouldPauseAll(bool applicationPaused, bool hasFocus) { return applicationPaused || !hasFocus; }

        private static double Clamp01(double value)
        {
            if (double.IsNaN(value) || value < 0) return 0;
            return value > 1 ? 1 : value;
        }
    }
}
