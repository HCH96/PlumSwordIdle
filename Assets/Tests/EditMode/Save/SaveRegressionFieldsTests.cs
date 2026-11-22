using System;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;
using PlumSword.Core.Save;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Tests.Save
{
    public sealed class SaveRegressionFieldsTests
    {
        private const string OldSave = "version=2\ngold=0\nattackLevel=0\nhealthLevel=0\ninnerPowerLevel=0\nrealmLevel=0\nstageNumber=1\nseenIntroStage=0\nsavedAtUtcTicks=0\n";

        [Test]
        public void EncodeDecode_RegressionFields_RoundTrips()
        {
            var data = new SaveData(0, 0, 0, 0, 0, 1, 0, 0, regressionCount: 3, playTimeMs: 754900, autoUpgrade: false);
            var text = SaveCodec.Encode(data);
            Assert.That(text, Does.EndWith("regressionCount=3\nplayTimeMs=754900\nautoUpgrade=0\n"));
            var result = SaveCodec.Decode(text);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(result.Data.RegressionCount, Is.EqualTo(3));
            Assert.That(result.Data.PlayTimeMs, Is.EqualTo(754900));
            Assert.That(result.Data.AutoUpgrade, Is.False);
        }

        [Test]
        public void Decode_OldVersionTwo_DefaultsNewFields()
        {
            var result = SaveCodec.Decode(OldSave);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(result.Data.RegressionCount, Is.Zero);
            Assert.That(result.Data.PlayTimeMs, Is.Zero);
            Assert.That(result.Data.AutoUpgrade, Is.True);
        }

        [TestCase("regressionCount=abc\n")]
        [TestCase("playTimeMs=abc\n")]
        [TestCase("autoUpgrade=abc\n")]
        public void Decode_InvalidNewField_IsCorrupt(string field)
        {
            Assert.That(SaveCodec.Decode(OldSave + field).Status, Is.EqualTo(SaveLoadStatus.Corrupt));
        }

        [Test]
        public void Decode_NonzeroAutoUpgrade_IsTrue()
        {
            Assert.That(SaveCodec.Decode(OldSave + "autoUpgrade=5\n").Data.AutoUpgrade, Is.True);
        }

        [Test]
        public void EncodeDecode_NegativeValues_ArePreserved()
        {
            var data = new SaveData(0, 0, 0, 0, 0, 1, 0, 0, regressionCount: -3, playTimeMs: -12);
            var decoded = SaveCodec.Decode(SaveCodec.Encode(data)).Data;
            Assert.That(decoded.RegressionCount, Is.EqualTo(-3));
            Assert.That(decoded.PlayTimeMs, Is.EqualTo(-12));
        }

        [Test]
        public void Capture_NewValuesAndNullOptions_CopiesExpectedValues()
        {
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 10, 100);
            var realm = new RealmProgress(wallet);
            var stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new Random(0), regressionCount: 2);
            var auto = new AutoUpgrade(upgrades, wallet, false);
            var data = SaveData.Capture(wallet, upgrades, realm, stage, 0, new PlayTime(12.5), auto);
            Assert.That(data.RegressionCount, Is.EqualTo(2));
            Assert.That(data.PlayTimeMs, Is.EqualTo(12500));
            Assert.That(data.AutoUpgrade, Is.False);
            var defaults = SaveData.Capture(wallet, upgrades, realm, stage, 0, null, null);
            Assert.That(defaults.PlayTimeMs, Is.Zero);
            Assert.That(defaults.AutoUpgrade, Is.True);
            auto.Dispose();
        }

        [Test]
        public void MemoryStore_Delete_ClearsText()
        {
            var store = new MemorySaveStore();
            store.TryWrite("a");
            Assert.That(store.TryDelete(), Is.True);
            Assert.That(store.Text, Is.Null);
        }

        [Test]
        public void Disable_StopsWritesUntilEnabled()
        {
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 10, 100);
            var stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new Random(0));
            var store = new MemorySaveStore();
            var save = new AutoSave(store, wallet, upgrades, new RealmProgress(wallet), stage);
            save.Disable();
            wallet.Add(1);
            Assert.That(save.Flush(0), Is.False);
            Assert.That(save.SaveNow(0), Is.False);
            Assert.That(store.Text, Is.Null);
            save.Enable();
            Assert.That(save.SaveNow(0), Is.True);
        }

        [Test]
        public void AutoSave_OptionalFields_AreWritten()
        {
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 10, 100);
            var stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new Random(0), regressionCount: 2);
            var auto = new AutoUpgrade(upgrades, wallet, false);
            var store = new MemorySaveStore();
            var save = new AutoSave(store, wallet, upgrades, new RealmProgress(wallet), stage, playTime: new PlayTime(12.5), autoUpgrade: auto);
            Assert.That(save.SaveNow(0), Is.True);
            var data = SaveCodec.Decode(store.Text).Data;
            Assert.That(data.RegressionCount, Is.EqualTo(2));
            Assert.That(data.PlayTimeMs, Is.EqualTo(12500));
            Assert.That(data.AutoUpgrade, Is.False);
            auto.Dispose();
        }
    }
}
