using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PlumSword.Core.StartMenu;
using PlumSword.Game.StartMenu;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PlumSword.Game.Tests.StartMenu
{
    public sealed class StartScreenViewTests
    {
        private GameObject canvasObject;
        private GameObject viewObject;
        private Canvas canvas;
        private StartScreenView view;

        [SetUp]
        public void SetUp()
        {
            canvasObject = new GameObject("TestCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            viewObject = new GameObject("TestStartScreenView");
            view = viewObject.AddComponent<StartScreenView>();
        }

        [TearDown]
        public void TearDown()
        {
            if (viewObject != null) UnityEngine.Object.DestroyImmediate(viewObject);
            if (canvasObject != null) UnityEngine.Object.DestroyImmediate(canvasObject);
        }

        [Test]
        public void Initialize_NullCanvas_Throws() => Assert.Throws<ArgumentNullException>(() => view.Initialize(null, StartButtonKind.Continue, () => { }, () => { }));
        [Test]
        public void Initialize_NullStart_Throws() => Assert.Throws<ArgumentNullException>(() => view.Initialize(canvas, StartButtonKind.Continue, null, () => { }));
        [Test]
        public void Initialize_NullSettings_Throws() => Assert.Throws<ArgumentNullException>(() => view.Initialize(canvas, StartButtonKind.Continue, () => { }, null));

        [Test]
        public void Initialize_Continue_CreatesFullScreenLastPanelAndLabels()
        {
            new GameObject("Other", typeof(RectTransform)).transform.SetParent(canvas.transform, false);
            view.Initialize(canvas, StartButtonKind.Continue, () => { }, () => { });
            var panel = canvas.transform.GetChild(canvas.transform.childCount - 1);
            Assert.AreEqual("StartScreenPanel", panel.name);
            var rect = (RectTransform)panel;
            Assert.AreEqual(Vector2.zero, rect.anchorMin);
            Assert.AreEqual(Vector2.one, rect.anchorMax);
            Assert.AreEqual(Vector2.zero, rect.offsetMin);
            Assert.AreEqual(Vector2.zero, rect.offsetMax);
            Assert.IsTrue(panel.GetComponent<Image>().raycastTarget);
            CollectionAssert.AreEquivalent(new[] { "PLUM BLOSSOM SWORD", "CONTINUE", "SETTINGS" }, Texts(panel));
        }

        [Test]
        public void Initialize_NewGame_ShowsStartLabel()
        {
            view.Initialize(canvas, StartButtonKind.NewGame, () => { }, () => { });
            Assert.That(Texts(canvas.transform.Find("StartScreenPanel/StartButton")), Is.EqualTo(new[] { "START" }));
        }

        [Test]
        public void Initialize_CreatesSpecifiedLayoutAndButtonStyles()
        {
            view.Initialize(canvas, StartButtonKind.NewGame, () => { }, () => { });
            var panel = canvas.transform.Find("StartScreenPanel");
            Assert.AreEqual(new Color(0.10f, 0.07f, 0.12f), panel.GetComponent<Image>().color);
            AssertRect(panel.Find("Title"), new Vector2(0, 70), new Vector2(600, 48));
            AssertButton(panel.Find("StartButton"), new Vector2(0, -20), new Vector2(200, 40), new Color(0.20f, 0.45f, 0.25f));
            AssertButton(panel.Find("SettingsButton"), new Vector2(0, -72), new Vector2(200, 32), new Color(0.25f, 0.25f, 0.30f));
        }

        private static void AssertButton(Transform button, Vector2 position, Vector2 size, Color color)
        {
            AssertRect(button, position, size);
            Assert.AreEqual(color, button.GetComponent<Image>().color);
            Assert.AreEqual(Selectable.Transition.None, button.GetComponent<Button>().transition);
        }

        private static void AssertRect(Transform transform, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)transform;
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.anchorMin);
            Assert.AreEqual(rect.anchorMin, rect.anchorMax);
            Assert.AreEqual(rect.anchorMin, rect.pivot);
            Assert.AreEqual(position, rect.anchoredPosition);
            Assert.AreEqual(size, rect.sizeDelta);
        }

        [Test]
        public void SettingsClickedTwice_InvokesCallbackTwice()
        {
            int calls = 0;
            view.Initialize(canvas, StartButtonKind.Continue, () => { }, () => calls++);
            var button = canvas.transform.Find("StartScreenPanel/SettingsButton").GetComponent<Button>();
            button.onClick.Invoke();
            button.onClick.Invoke();
            Assert.AreEqual(2, calls);
        }

        [UnityTest]
        public IEnumerator StartClickedTwice_InvokesOnceAndDestroysObjects()
        {
            int calls = 0;
            view.Initialize(canvas, StartButtonKind.Continue, () => calls++, () => { });
            var panel = canvas.transform.Find("StartScreenPanel").gameObject;
            var button = panel.transform.Find("StartButton").GetComponent<Button>();
            button.onClick.Invoke();
            button.onClick.Invoke();
            Assert.AreEqual(1, calls);
            yield return null;
            Assert.IsTrue(panel == null);
            Assert.IsTrue(viewObject == null);
        }

        [UnityTest]
        public IEnumerator DestroyView_DestroysPanel()
        {
            view.Initialize(canvas, StartButtonKind.Continue, () => { }, () => { });
            var panel = canvas.transform.Find("StartScreenPanel").gameObject;
            UnityEngine.Object.Destroy(viewObject);
            yield return null;
            yield return null;
            Assert.IsTrue(panel == null);
        }

        private static string[] Texts(Transform parent)
        {
            var textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
            var property = textType.GetProperty("text");
            return parent.GetComponentsInChildren(textType).Select(component => (string)property.GetValue(component)).ToArray();
        }
    }
}
