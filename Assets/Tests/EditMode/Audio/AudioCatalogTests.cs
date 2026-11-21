using System;
using System.IO;
using NUnit.Framework;
using PlumSword.Core.Audio;

namespace PlumSword.Core.Tests.Audio
{
    public class AudioCatalogTests
    {
        private static readonly string[] MusicPaths = { null, "Audio/Music/title", "Audio/Music/battle_normal", "Audio/Music/battle_boss", "Audio/Music/battle_final_boss" };
        private static readonly string[] SfxNames = { "sword_swing", "attack_hit", "upgrade_success", "plum_sword_skill", "realm_breakthrough", "ui_confirm", "boss_appear", "boss_defeated", "player_defeated", "offline_reward", "enemy_defeated", "upgrade_unavailable", "window_open_close", "stage_transition" };

        [Test]
        public void MusicPath_AllTracks_ReturnsSpecifiedPaths()
        {
            for (int i = 0; i < MusicPaths.Length; i++)
                Assert.That(AudioCatalog.MusicPath((MusicTrack)i), Is.EqualTo(MusicPaths[i]));
        }

        [Test]
        public void SfxPath_AllSounds_ReturnsSpecifiedPaths()
        {
            for (int i = 0; i < SfxNames.Length; i++)
                Assert.That(AudioCatalog.SfxPath((SfxId)i), Is.EqualTo("Audio/SFX/" + SfxNames[i]));
        }

        [Test]
        public void Paths_AllSounds_HaveWavFiles()
        {
            for (int i = 1; i < MusicPaths.Length; i++)
            {
                string file = WavFile(AudioCatalog.MusicPath((MusicTrack)i));
                Assert.That(File.Exists(file), Is.True, file);
            }
            for (int i = 0; i < SfxNames.Length; i++)
            {
                string file = WavFile(AudioCatalog.SfxPath((SfxId)i));
                Assert.That(File.Exists(file), Is.True, file);
            }
        }

        [Test]
        public void VolumesAndEnabled_AllSounds_MatchSpec()
        {
            Assert.That(AudioCatalog.CrossfadeSeconds, Is.EqualTo(1.0).Within(1e-9));
            Assert.That(AudioCatalog.SfxMinIntervalSeconds, Is.EqualTo(0.05).Within(1e-9));
            Assert.That(AudioCatalog.MaxSfxVoices, Is.EqualTo(8));
            for (int i = 0; i < MusicPaths.Length; i++)
                Assert.That(AudioCatalog.MusicBaseVolume((MusicTrack)i), Is.EqualTo(i == 0 ? 0.0 : 0.6).Within(1e-9));
            for (int i = 0; i < SfxNames.Length; i++)
            {
                double expected = i == 0 || i == 1 || i == 10 ? 0.6 : 1.0;
                Assert.That(AudioCatalog.SfxBaseVolume((SfxId)i), Is.EqualTo(expected).Within(1e-9));
                Assert.That(AudioCatalog.IsSfxEnabled((SfxId)i), Is.True);
            }
        }

        [TestCase(-1)]
        [TestCase(99)]
        public void InvalidIds_AllCatalogMethods_Throw(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AudioCatalog.MusicPath((MusicTrack)value));
            Assert.Throws<ArgumentOutOfRangeException>(() => AudioCatalog.MusicBaseVolume((MusicTrack)value));
            Assert.Throws<ArgumentOutOfRangeException>(() => AudioCatalog.SfxPath((SfxId)value));
            Assert.Throws<ArgumentOutOfRangeException>(() => AudioCatalog.SfxBaseVolume((SfxId)value));
            Assert.Throws<ArgumentOutOfRangeException>(() => AudioCatalog.IsSfxEnabled((SfxId)value));
        }

        private static string WavFile(string path)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Resources", path + ".wav");
        }
    }
}
