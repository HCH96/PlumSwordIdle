using System;
using System.Globalization;
using NUnit.Framework;
using PlumSword.Core.Save;

namespace PlumSword.Core.Tests.Save
{
    public class SaveCodecTests
    {
        private const string Standard = "version=2\ngold=1234\nattackLevel=3\nhealthLevel=2\ninnerPowerLevel=4\nrealmLevel=2\nstageNumber=7\nseenIntroStage=5\nsavedAtUtcTicks=638000000000000000\n";

        [Test]
        public void Encode_StandardData_ProducesExactText() =>
            Assert.That(SaveCodec.Encode(new SaveData(1234, 3, 2, 4, 2, 7, 5, 638000000000000000L)), Is.EqualTo(Standard));

        [Test]
        public void Encode_Null_Throws() => Assert.Throws<ArgumentNullException>(() => SaveCodec.Encode(null));

        [Test]
        public void Decode_StandardText_LoadsAllValues()
        {
            var result = SaveCodec.Decode(Standard);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(result.HasSaveData, Is.True);
            Assert.That(result.Data.Gold, Is.EqualTo(1234));
            Assert.That(result.Data.AttackLevel, Is.EqualTo(3));
            Assert.That(result.Data.HealthLevel, Is.EqualTo(2));
            Assert.That(result.Data.InnerPowerLevel, Is.EqualTo(4));
            Assert.That(result.Data.RealmLevel, Is.EqualTo(2));
            Assert.That(result.Data.StageNumber, Is.EqualTo(7));
            Assert.That(result.Data.SeenIntroStage, Is.EqualTo(5));
            Assert.That(result.Data.SavedAtUtcTicks, Is.EqualTo(638000000000000000L));
        }

        [Test]
        public void Decode_Null_IsEmpty()
        {
            var result = SaveCodec.Decode(null);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Empty));
            Assert.That(result.Data, Is.Null);
            Assert.That(result.HasSaveData, Is.False);
        }

        [TestCase("")]
        [TestCase("hello")]
        public void Decode_EmptyOrMalformed_IsCorrupt(string text)
        {
            var result = SaveCodec.Decode(text);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Corrupt));
            Assert.That(result.Data, Is.Null);
            Assert.That(result.HasSaveData, Is.False);
        }

        [TestCase("version=2\n", "")]
        [TestCase("version=2", "version=abc")]
        [TestCase("innerPowerLevel=4\n", "")]
        [TestCase("realmLevel=2\n", "")]
        [TestCase("realmLevel=2", "realmLevel=2a")]
        [TestCase("gold=1234\n", "")]
        [TestCase("savedAtUtcTicks=638000000000000000\n", "")]
        [TestCase("gold=1234", "gold=12a")]
        [TestCase("gold=1234", "gold= 12")]
        [TestCase("gold=1234", "gold=99999999999999999999")]
        [TestCase("attackLevel=3", "attackLevel=3000000000")]
        public void Decode_MissingOrInvalidField_IsCorrupt(string from, string to)
        {
            var result = SaveCodec.Decode(Standard.Replace(from, to));
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Corrupt));
            Assert.That(result.Data, Is.Null);
            Assert.That(result.HasSaveData, Is.False);
        }

        [TestCase("1")]
        [TestCase("3")]
        [TestCase("0")]
        public void Decode_UnknownVersion_IsUnsupported(string version)
        {
            var result = SaveCodec.Decode(Standard.Replace("version=2", "version=" + version));
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.UnsupportedVersion));
            Assert.That(result.Data, Is.Null);
            Assert.That(result.HasSaveData, Is.False);
        }

        [Test]
        public void Decode_UnknownVersionAlone_IsUnsupported()
        {
            var result = SaveCodec.Decode("version=1\n");
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.UnsupportedVersion));
            Assert.That(result.Data, Is.Null);
            Assert.That(result.HasSaveData, Is.False);
        }

        [TestCase("gold=5\n")]
        [TestCase("oops\n")]
        public void Decode_DuplicateOrMalformedLine_IsCorrupt(string extra)
        {
            var result = SaveCodec.Decode(Standard + extra);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Corrupt));
            Assert.That(result.Data, Is.Null);
            Assert.That(result.HasSaveData, Is.False);
        }

        [Test]
        public void Decode_Crlf_LoadsAllValues()
        {
            var result = SaveCodec.Decode(Standard.Replace("\n", "\r\n"));
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(result.HasSaveData, Is.True);
            Assert.That(result.Data.Gold, Is.EqualTo(1234));
            Assert.That(result.Data.AttackLevel, Is.EqualTo(3));
            Assert.That(result.Data.HealthLevel, Is.EqualTo(2));
            Assert.That(result.Data.InnerPowerLevel, Is.EqualTo(4));
            Assert.That(result.Data.RealmLevel, Is.EqualTo(2));
            Assert.That(result.Data.StageNumber, Is.EqualTo(7));
            Assert.That(result.Data.SeenIntroStage, Is.EqualTo(5));
            Assert.That(result.Data.SavedAtUtcTicks, Is.EqualTo(638000000000000000L));
        }

        [Test]
        public void Decode_BlankAndExtraLines_LoadsAllValues()
        {
            var result = SaveCodec.Decode(Standard.Replace("gold=1234\n", "gold=1234\n\nextra=1\n"));
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(result.HasSaveData, Is.True);
            Assert.That(result.Data.Gold, Is.EqualTo(1234));
            Assert.That(result.Data.AttackLevel, Is.EqualTo(3));
            Assert.That(result.Data.HealthLevel, Is.EqualTo(2));
            Assert.That(result.Data.InnerPowerLevel, Is.EqualTo(4));
            Assert.That(result.Data.RealmLevel, Is.EqualTo(2));
            Assert.That(result.Data.StageNumber, Is.EqualTo(7));
            Assert.That(result.Data.SeenIntroStage, Is.EqualTo(5));
            Assert.That(result.Data.SavedAtUtcTicks, Is.EqualTo(638000000000000000L));
        }

        [Test]
        public void RoundTrip_OutOfRangeValues_ArePreserved()
        {
            var result = SaveCodec.Decode(SaveCodec.Encode(new SaveData(-5, 0, 0, -3, 7, 0, 9, 0)));
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(result.Data.Gold, Is.EqualTo(-5));
            Assert.That(result.Data.InnerPowerLevel, Is.EqualTo(-3));
            Assert.That(result.Data.RealmLevel, Is.EqualTo(7));
            Assert.That(result.Data.StageNumber, Is.Zero);
            Assert.That(result.Data.SeenIntroStage, Is.EqualTo(9));
            Assert.That(result.Data.SavedAtUtcTicks, Is.Zero);
        }

        [Test]
        public void RoundTrip_GermanCulture_UsesInvariantNumbers()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                string text = SaveCodec.Encode(new SaveData(1234567, 0, 0, 0, 0, 1, 0, -1));
                Assert.That(text, Does.Contain("gold=1234567\n"));
                Assert.That(text, Does.Contain("savedAtUtcTicks=-1\n"));
                var result = SaveCodec.Decode(text);
                Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
                Assert.That(result.Data.Gold, Is.EqualTo(1234567));
                Assert.That(result.Data.SavedAtUtcTicks, Is.EqualTo(-1));
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }

        [Test]
        public void Decode_UnknownVersionWithMalformedOrDuplicateLine_IsCorrupt()
        {
            var malformed = SaveCodec.Decode("version=2\noops\n");
            Assert.That(malformed.Status, Is.EqualTo(SaveLoadStatus.Corrupt));
            Assert.That(malformed.Data, Is.Null);
            Assert.That(malformed.HasSaveData, Is.False);
            var duplicate = SaveCodec.Decode("version=2\nextra=1\nextra=2\n");
            Assert.That(duplicate.Status, Is.EqualTo(SaveLoadStatus.Corrupt));
            Assert.That(duplicate.Data, Is.Null);
            Assert.That(duplicate.HasSaveData, Is.False);
        }

        [Test]
        public void Decode_VersionOneSave_IsUnsupported()
        {
            var result = SaveCodec.Decode("version=1\ngold=1234\nattackLevel=3\nhealthLevel=2\nattackSpeedLevel=10\nstageNumber=7\nseenIntroStage=5\nsavedAtUtcTicks=638000000000000000\n");
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.UnsupportedVersion));
            Assert.That(result.Data, Is.Null);
            Assert.That(result.HasSaveData, Is.False);
        }

        [Test]
        public void Decode_VersionTwoWithOldField_IgnoresIt()
        {
            var result = SaveCodec.Decode(Standard + "attackSpeedLevel=10\n");
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(result.HasSaveData, Is.True);
            Assert.That(result.Data.Gold, Is.EqualTo(1234));
            Assert.That(result.Data.AttackLevel, Is.EqualTo(3));
            Assert.That(result.Data.HealthLevel, Is.EqualTo(2));
            Assert.That(result.Data.InnerPowerLevel, Is.EqualTo(4));
            Assert.That(result.Data.RealmLevel, Is.EqualTo(2));
            Assert.That(result.Data.StageNumber, Is.EqualTo(7));
            Assert.That(result.Data.SeenIntroStage, Is.EqualTo(5));
            Assert.That(result.Data.SavedAtUtcTicks, Is.EqualTo(638000000000000000L));
        }
    }
}
