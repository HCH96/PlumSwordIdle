using System;
using NUnit.Framework;
using PlumSword.Core.Audio;

namespace PlumSword.Core.Tests.Audio
{
    public class MusicFaderTests
    {
        private const double Tolerance = 1e-9;

        [Test]
        public void Constructor_InitialStateAndInvalidDurations_AreChecked()
        {
            var fader = new MusicFader();
            AssertState(fader, MusicTrack.None, 0, MusicTrack.None, 0);
            foreach (double value in new[] { 0.0, -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
                Assert.Throws<ArgumentOutOfRangeException>(() => new MusicFader(value));
        }

        [Test]
        public void Request_FirstTrackAndSameTrack_FadesAndKeepsState()
        {
            var fader = new MusicFader();
            Assert.That(fader.Request(MusicTrack.Title), Is.True);
            AssertState(fader, MusicTrack.Title, 0, MusicTrack.None, 0);
            fader.Tick(0.5);
            AssertState(fader, MusicTrack.Title, 0.5, MusicTrack.None, 0);
            fader.Tick(0.6);
            Assert.That(fader.Request(MusicTrack.Title), Is.False);
            AssertState(fader, MusicTrack.Title, 1, MusicTrack.None, 0);
        }

        [Test]
        public void Request_SecondTrack_CrossfadesBothTracks()
        {
            var fader = FullTitle();
            Assert.That(fader.Request(MusicTrack.BattleNormal), Is.True);
            AssertState(fader, MusicTrack.BattleNormal, 0, MusicTrack.Title, 1);
            fader.Tick(0.25);
            AssertState(fader, MusicTrack.BattleNormal, 0.25, MusicTrack.Title, 0.75);
        }

        [Test]
        public void Request_DuringCrossfade_DiscardsOldPrevious()
        {
            var fader = FullTitle();
            fader.Request(MusicTrack.BattleNormal);
            fader.Tick(0.25);
            Assert.That(fader.Request(MusicTrack.BattleBoss), Is.True);
            AssertState(fader, MusicTrack.BattleBoss, 0, MusicTrack.BattleNormal, 0.25);
            fader.Tick(0.25);
            AssertState(fader, MusicTrack.BattleBoss, 0.25, MusicTrack.None, 0);
        }

        [Test]
        public void Request_None_FadesOutWithoutFadingIn()
        {
            var fader = FullTitle();
            Assert.That(fader.Request(MusicTrack.None), Is.True);
            AssertState(fader, MusicTrack.None, 0, MusicTrack.Title, 1);
            fader.Tick(1);
            AssertState(fader, MusicTrack.None, 0, MusicTrack.None, 0);
        }

        [Test]
        public void InvalidInputsAndZeroTick_LeaveStateUnchanged()
        {
            var fader = FullTitle();
            fader.Request(MusicTrack.BattleNormal);
            fader.Tick(0.25);
            foreach (double value in new[] { -0.1, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => fader.Tick(value));
                AssertState(fader, MusicTrack.BattleNormal, 0.25, MusicTrack.Title, 0.75);
            }
            fader.Tick(0);
            AssertState(fader, MusicTrack.BattleNormal, 0.25, MusicTrack.Title, 0.75);
            Assert.Throws<ArgumentOutOfRangeException>(() => fader.Request((MusicTrack)99));
            AssertState(fader, MusicTrack.BattleNormal, 0.25, MusicTrack.Title, 0.75);
        }

        [Test]
        public void Request_AfterNone_PreservesFadingPreviousAndUsesCustomDuration()
        {
            var fader = new MusicFader(2);
            fader.Request(MusicTrack.Title);
            fader.Tick(2);
            fader.Request(MusicTrack.None);
            fader.Tick(0.5);
            Assert.That(fader.Request(MusicTrack.BattleBoss), Is.True);
            Assert.That(fader.Current, Is.EqualTo(MusicTrack.BattleBoss));
            Assert.That(fader.CurrentWeight, Is.EqualTo(0).Within(Tolerance));
            Assert.That(fader.Previous, Is.EqualTo(MusicTrack.Title));
            Assert.That(fader.PreviousWeight, Is.EqualTo(0.75).Within(Tolerance));
            fader.Tick(0.5);
            Assert.That(fader.Current, Is.EqualTo(MusicTrack.BattleBoss));
            Assert.That(fader.CurrentWeight, Is.EqualTo(0.25).Within(Tolerance));
            Assert.That(fader.Previous, Is.EqualTo(MusicTrack.Title));
            Assert.That(fader.PreviousWeight, Is.EqualTo(0.5).Within(Tolerance));
        }

        private static MusicFader FullTitle()
        {
            var fader = new MusicFader();
            fader.Request(MusicTrack.Title);
            fader.Tick(0.5);
            fader.Tick(0.6);
            fader.Request(MusicTrack.Title);
            return fader;
        }

        private static void AssertState(MusicFader fader, MusicTrack current, double currentWeight,
            MusicTrack previous, double previousWeight)
        {
            Assert.That(fader.Current, Is.EqualTo(current));
            Assert.That(fader.CurrentWeight, Is.EqualTo(currentWeight).Within(Tolerance));
            Assert.That(fader.Previous, Is.EqualTo(previous));
            Assert.That(fader.PreviousWeight, Is.EqualTo(previousWeight).Within(Tolerance));
        }
    }
}
