using System;
using NUnit.Framework;
using PlumSword.Core.Audio;

namespace PlumSword.Core.Tests.Audio
{
    public class AudioMixTests
    {
        [TestCase(MusicTrack.BattleNormal, 0.8, 1.0, 0.48)]
        [TestCase(MusicTrack.BattleBoss, 0.8, 0.25, 0.12)]
        [TestCase(MusicTrack.Title, 0.0, 1.0, 0.0)]
        [TestCase(MusicTrack.None, 1.0, 1.0, 0.0)]
        [TestCase(MusicTrack.Title, 1.5, 2.0, 0.6)]
        [TestCase(MusicTrack.Title, double.NaN, 1.0, 0.0)]
        [TestCase(MusicTrack.Title, 1.0, double.NaN, 0.0)]
        [TestCase(MusicTrack.Title, -1.0, 1.0, 0.0)]
        public void MusicVolume_ScalesAndClamps(MusicTrack track, double scale, double weight, double expected)
        {
            Assert.That(AudioMix.MusicVolume(track, scale, weight), Is.EqualTo(expected).Within(1e-9));
        }

        [TestCase(SfxId.AttackHit, 0.8, 0.48)]
        [TestCase(SfxId.BossAppear, 0.8, 0.8)]
        [TestCase(SfxId.UiConfirm, 0.0, 0.0)]
        [TestCase(SfxId.UiConfirm, double.NaN, 0.0)]
        [TestCase(SfxId.UiConfirm, -1.0, 0.0)]
        [TestCase(SfxId.UiConfirm, 2.0, 1.0)]
        public void SfxVolume_ScalesAndClamps(SfxId id, double scale, double expected)
        {
            Assert.That(AudioMix.SfxVolume(id, scale), Is.EqualTo(expected).Within(1e-9));
        }

        [TestCase(0.0, false)]
        [TestCase(0.01, true)]
        [TestCase(double.NaN, false)]
        public void ShouldPlaySfx_UsesPositiveScale(double scale, bool expected)
        {
            Assert.That(AudioMix.ShouldPlaySfx(scale), Is.EqualTo(expected));
        }

        [TestCase(false, true, false)]
        [TestCase(true, true, true)]
        [TestCase(false, false, true)]
        [TestCase(true, false, true)]
        public void ShouldPauseAll_PauseOrFocusLost(bool paused, bool focus, bool expected)
        {
            Assert.That(AudioMix.ShouldPauseAll(paused, focus), Is.EqualTo(expected));
        }

        [TestCase(-1)]
        [TestCase(99)]
        public void Volume_InvalidIds_ThrowsEvenAtZeroScale(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AudioMix.MusicVolume((MusicTrack)value, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => AudioMix.SfxVolume((SfxId)value, 0));
        }
    }
}
