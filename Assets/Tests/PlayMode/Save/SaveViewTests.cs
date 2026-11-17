using System;
using System.Collections;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Save;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;
using PlumSword.Game.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Save
{
    public sealed class SaveViewTests
    {
        private sealed class FakeStore : ISaveStore
        {
            public int Attempts;
            public bool Succeeds = true;
            public bool TryWrite(string text) { Attempts++; return Succeeds; }
        }
        private GameObject canvasObject, viewObject;
        private Canvas canvas;
        private Wallet wallet;
        private FakeStore store;
        private AutoSave autoSave;
        private SaveView view;

        [SetUp] public void SetUp()
        {
            canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            canvas = canvasObject.GetComponent<Canvas>();
            viewObject = new GameObject("TestSaveView");
            view = viewObject.AddComponent<SaveView>();
            wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 10, 100, 1.0);
            var stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new System.Random(0));
            store = new FakeStore();
            autoSave = new AutoSave(store, wallet, upgrades, stage);
        }
        [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(viewObject); UnityEngine.Object.DestroyImmediate(canvasObject); }

        [Test] public void Initialize_CreatesHiddenLabel()
        {
            view.Initialize(autoSave, canvas);
            var label = canvas.transform.Find("SaveFailedText");
            Assert.IsNotNull(label);
            Assert.IsFalse(label.gameObject.activeSelf);
        }
        [UnityTest] public IEnumerator Change_FlushesAtFrameEnd()
        {
            view.Initialize(autoSave, canvas);
            wallet.Add(10);
            yield return null; yield return null;
            Assert.AreEqual(1, store.Attempts);
            Assert.IsFalse(autoSave.IsDirty);
        }
        [UnityTest] public IEnumerator FailureAndRecovery_UpdatesLabel()
        {
            view.Initialize(autoSave, canvas);
            store.Succeeds = false;
            wallet.Add(10);
            yield return null; yield return null;
            Assert.IsTrue(autoSave.HasFailed);
            var label = canvas.transform.Find("SaveFailedText").gameObject;
            Assert.IsTrue(label.activeSelf);
            store.Succeeds = true;
            autoSave.SaveNow(1);
            Assert.IsFalse(label.activeSelf);
        }
        [Test] public void Pause_OnlyTrueSaves()
        {
            view.Initialize(autoSave, canvas);
            viewObject.SendMessage("OnApplicationPause", true);
            Assert.AreEqual(1, store.Attempts);
            viewObject.SendMessage("OnApplicationPause", false);
            Assert.AreEqual(1, store.Attempts);
        }
        [Test] public void Initialize_NullArguments_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => view.Initialize(null, canvas));
            Assert.Throws<ArgumentNullException>(() => view.Initialize(autoSave, null));
        }
    }
}
