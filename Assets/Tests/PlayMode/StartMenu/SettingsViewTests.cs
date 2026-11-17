using System;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Save;
using PlumSword.Core.Stage;
using PlumSword.Core.StartMenu;
using PlumSword.Core.StatUpgrade;
using PlumSword.Game.StartMenu;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Tests.StartMenu
{
    public sealed class SettingsViewTests
    {
        private sealed class FakeStore : ISaveStore
        {
            public bool Succeeds = true;
            public bool TryWrite(string text) => Succeeds;
        }

        private GameObject canvasObject, viewObject;
        private Canvas canvas;
        private SettingsView view;
        private VolumeSettings volume;
        private LeaveGame leave;
        private FakeStore store;
        private int titleCalls, quitCalls, saveCalls;

        [SetUp] public void SetUp()
        {
            canvasObject = new GameObject("TestCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            viewObject = new GameObject("TestSettingsView"); view = viewObject.AddComponent<SettingsView>();
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 10, 100, 1.0);
            var stage = new StageProgress(wallet, upgrades, new Battle(upgrades), new System.Random(0), 1, 1);
            store = new FakeStore();
            leave = new LeaveGame(new AutoSave(store, wallet, upgrades, stage, enabled: true));
            volume = new VolumeSettings();
            titleCalls = quitCalls = saveCalls = 0;
        }

        [TearDown] public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(viewObject);
            UnityEngine.Object.DestroyImmediate(canvasObject);
        }

        private void Initialize() => view.Initialize(canvas, volume, leave, () => titleCalls++, () => quitCalls++, () => saveCalls++);
        private Transform Panel => canvas.transform.Find("SettingsPanel");
        private Transform Item(string name) => Panel.Find("Window/" + name);
        private void Click(string name) => Item(name).GetComponent<Button>().onClick.Invoke();
        private string Label(string name)
        {
            var type = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
            return (string)type.GetProperty("text").GetValue(Item(name).GetComponent(type));
        }

        [Test] public void Initialize_NullArguments_Throws()
        {
            Action callback = () => { };
            Assert.Throws<ArgumentNullException>(() => view.Initialize(null, volume, leave, callback, callback, callback));
            Assert.Throws<ArgumentNullException>(() => view.Initialize(canvas, null, leave, callback, callback, callback));
            Assert.Throws<ArgumentNullException>(() => view.Initialize(canvas, volume, null, callback, callback, callback));
            Assert.Throws<ArgumentNullException>(() => view.Initialize(canvas, volume, leave, null, callback, callback));
            Assert.Throws<ArgumentNullException>(() => view.Initialize(canvas, volume, leave, callback, null, callback));
            Assert.Throws<ArgumentNullException>(() => view.Initialize(canvas, volume, leave, callback, callback, null));
        }

        [Test] public void Initialize_GameButton_OpensInGame()
        {
            Initialize();
            Assert.IsFalse(view.IsOpen); Assert.AreEqual(SettingsMode.Title, view.Mode);
            Assert.IsNotNull(Panel); Assert.IsFalse(Panel.gameObject.activeSelf);
            var button = canvas.transform.Find("GameSettingsButton"); Assert.IsTrue(button.gameObject.activeSelf);
            button.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(view.IsOpen); Assert.AreEqual(SettingsMode.InGame, view.Mode);
        }

        [Test] public void Open_SwitchModes_RefreshesAndMovesPanelToFront()
        {
            Initialize();
            new GameObject("Other", typeof(RectTransform)).transform.SetParent(canvas.transform, false);
            int changes = 0; volume.Changed += () => changes++;
            view.Open(SettingsMode.Title);
            Assert.IsTrue(Panel.gameObject.activeSelf);
            Assert.AreEqual(Panel, canvas.transform.GetChild(canvas.transform.childCount - 1));
            Assert.IsFalse(Item("ToTitleButton").gameObject.activeSelf);
            Assert.IsFalse(Item("QuitButton").gameObject.activeSelf);
            Assert.IsFalse(Item("LeaveFailedMessage").gameObject.activeSelf);
            Assert.AreEqual("BGM 80", Label("BgmLabel")); Assert.AreEqual(0, changes);
            view.Open(SettingsMode.InGame);
            Assert.IsTrue(view.IsOpen); Assert.AreEqual(SettingsMode.InGame, view.Mode);
            Assert.IsTrue(Item("ToTitleButton").gameObject.activeSelf); Assert.IsTrue(Item("QuitButton").gameObject.activeSelf);
            Assert.IsFalse(Item("LeaveFailedMessage").gameObject.activeSelf); Assert.AreEqual(0, changes);
        }

        [Test] public void SlidersAndClose_UpdateVolumeLabelsAndSaveOnce()
        {
            Initialize(); view.Open(SettingsMode.InGame);
            Item("BgmSlider").GetComponent<Slider>().value = 30;
            Assert.AreEqual(30, volume.Bgm); Assert.AreEqual("BGM 30", Label("BgmLabel"));
            Item("SfxSlider").GetComponent<Slider>().value = 0;
            Assert.AreEqual(0, volume.Sfx); Assert.AreEqual("SFX 0", Label("SfxLabel"));
            Click("CloseButton");
            Assert.IsFalse(view.IsOpen); Assert.IsFalse(Panel.gameObject.activeSelf); Assert.AreEqual(1, saveCalls);
            view.Close(); Assert.AreEqual(1, saveCalls);
        }

        [Test] public void LeaveFailure_ReopenAndRecover_ShowsFailureAndSavesBeforeTitle()
        {
            Initialize(); store.Succeeds = false; view.Open(SettingsMode.InGame);
            Click("ToTitleButton");
            Assert.AreEqual(0, titleCalls); Assert.AreEqual(0, saveCalls); Assert.IsTrue(view.IsOpen);
            Assert.IsTrue(Item("LeaveFailedMessage").gameObject.activeSelf);
            Click("QuitButton"); Assert.AreEqual(0, quitCalls); Assert.AreEqual(0, saveCalls);
            view.Close(); view.Open(SettingsMode.InGame);
            Assert.IsTrue(Item("LeaveFailedMessage").gameObject.activeSelf);
            store.Succeeds = true; Click("ToTitleButton");
            Assert.AreEqual(1, titleCalls); Assert.AreEqual(2, saveCalls);
        }

        [Test] public void Quit_Success_SavesAndInvokesQuit()
        {
            Initialize(); view.Open(SettingsMode.InGame); Click("QuitButton");
            Assert.AreEqual(1, quitCalls); Assert.AreEqual(1, saveCalls); Assert.AreEqual(0, titleCalls);
        }

        [Test] public void ApplicationEvents_SaveOnlyOnPauseTrueAndQuit()
        {
            viewObject.SendMessage("OnApplicationPause", true); viewObject.SendMessage("OnApplicationQuit");
            Initialize(); viewObject.SendMessage("OnApplicationPause", false); Assert.AreEqual(0, saveCalls);
            viewObject.SendMessage("OnApplicationPause", true); viewObject.SendMessage("OnApplicationQuit");
            Assert.AreEqual(2, saveCalls);
        }
    }
}
