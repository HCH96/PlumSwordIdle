using System;
using NUnit.Framework;
using PlumSword.Core.Audio;

namespace PlumSword.Core.Tests.Audio
{
    public class MusicRulesTests
    {
        [TestCase(true, 3, true, MusicTrack.Title)]
        [TestCase(false, 1, false, MusicTrack.BattleNormal)]
        [TestCase(false, 4, true, MusicTrack.BattleBoss)]
        [TestCase(false, 5, true, MusicTrack.BattleFinalBoss)]
        [TestCase(false, 5, false, MusicTrack.BattleNormal)]
        public void TrackFor_ValidState_ReturnsExpectedTrack(bool start, int stage, bool boss, MusicTrack expected)
        {
            Assert.That(MusicRules.TrackFor(start, stage, boss), Is.EqualTo(expected));
        }

        [TestCase(false, 0)]
        [TestCase(true, 6)]
        public void TrackFor_InvalidStage_ThrowsEvenOnStartScreen(bool start, int stage)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MusicRules.TrackFor(start, stage, false));
        }
    }
}
