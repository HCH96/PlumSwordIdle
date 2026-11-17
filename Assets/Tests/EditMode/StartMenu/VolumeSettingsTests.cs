using NUnit.Framework;
using PlumSword.Core.StartMenu;

namespace PlumSword.Core.Tests.StartMenu
{
    public class VolumeSettingsTests
    {
        [Test]
        public void Constants_HaveSpecifiedValues()
        {
            Assert.That(VolumeSettings.MinVolume, Is.EqualTo(0));
            Assert.That(VolumeSettings.MaxVolume, Is.EqualTo(100));
            Assert.That(VolumeSettings.DefaultVolume, Is.EqualTo(80));
        }

        [Test]
        public void Constructor_Defaults_ReturnsDefaultValuesAndScales()
        {
            var settings = new VolumeSettings();
            Assert.That(settings.Bgm, Is.EqualTo(80));
            Assert.That(settings.Sfx, Is.EqualTo(80));
            Assert.That(settings.BgmScale, Is.EqualTo(0.8).Within(1e-9));
            Assert.That(settings.SfxScale, Is.EqualTo(0.8).Within(1e-9));
        }

        [TestCase(-5, 150)]
        [TestCase(int.MinValue, int.MaxValue)]
        public void Constructor_OutOfRange_ClampsWithoutThrowing(int bgm, int sfx)
        {
            var settings = new VolumeSettings(bgm, sfx);
            Assert.That(settings.Bgm, Is.EqualTo(0));
            Assert.That(settings.Sfx, Is.EqualTo(100));
        }

        [Test]
        public void Constructor_InRange_PreservesValuesAndScales()
        {
            var settings = new VolumeSettings(30, 55);
            Assert.That(settings.Bgm, Is.EqualTo(30));
            Assert.That(settings.Sfx, Is.EqualTo(55));
            Assert.That(settings.BgmScale, Is.EqualTo(0.3).Within(1e-9));
            Assert.That(settings.SfxScale, Is.EqualTo(0.55).Within(1e-9));
        }

        [Test]
        public void SetVolumes_ChangesAndClampedDuplicates_NotifyOnlyAfterChanges()
        {
            var settings = new VolumeSettings();
            int notifications = 0;
            int observedBgm = -1;
            int observedSfx = -1;
            settings.Changed += () =>
            {
                notifications++;
                observedBgm = settings.Bgm;
                observedSfx = settings.Sfx;
            };

            settings.SetBgm(30);
            Assert.That(settings.Bgm, Is.EqualTo(30));
            Assert.That(settings.Sfx, Is.EqualTo(80));
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(observedBgm, Is.EqualTo(30));
            Assert.That(observedSfx, Is.EqualTo(80));

            settings.SetBgm(30);
            Assert.That(notifications, Is.EqualTo(1));

            settings.SetSfx(150);
            Assert.That(settings.Sfx, Is.EqualTo(100));
            Assert.That(notifications, Is.EqualTo(2));
            Assert.That(observedSfx, Is.EqualTo(100));

            settings.SetSfx(int.MaxValue);
            Assert.That(settings.Sfx, Is.EqualTo(100));
            Assert.That(notifications, Is.EqualTo(2));

            settings.SetBgm(-1);
            Assert.That(settings.Bgm, Is.EqualTo(0));
            Assert.That(notifications, Is.EqualTo(3));
            Assert.That(observedBgm, Is.EqualTo(0));

            settings.SetBgm(int.MinValue);
            Assert.That(settings.Bgm, Is.EqualTo(0));
            Assert.That(notifications, Is.EqualTo(3));
        }
    }
}
