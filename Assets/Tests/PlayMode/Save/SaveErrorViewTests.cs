using System;
using System.Collections;
using NUnit.Framework;
using PlumSword.Game.Save;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PlumSword.Game.Tests.Save
{
    public sealed class SaveErrorViewTests
    {
        private GameObject canvasObject, viewObject;
        private Canvas canvas;
        private SaveErrorView view;
        [SetUp] public void SetUp()
        {
            canvasObject = new GameObject("TestCanvas", typeof(Canvas));
            canvas = canvasObject.GetComponent<Canvas>();
            viewObject = new GameObject("TestErrorView");
            view = viewObject.AddComponent<SaveErrorView>();
        }
        [TearDown] public void TearDown() { if (viewObject != null) UnityEngine.Object.DestroyImmediate(viewObject); UnityEngine.Object.DestroyImmediate(canvasObject); }
        [Test] public void Initialize_NullArguments_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => view.Initialize(canvas, null));
            Assert.Throws<ArgumentNullException>(() => view.Initialize(null, () => { }));
        }
        [Test] public void Initialize_CreatesLastFullScreenBlockingPanelAndButton()
        {
            new GameObject("Other", typeof(RectTransform)).transform.SetParent(canvas.transform, false);
            view.Initialize(canvas, () => { });
            var panel = canvas.transform.GetChild(canvas.transform.childCount - 1);
            Assert.AreEqual("SaveErrorPanel", panel.name);
            Assert.IsTrue(panel.GetComponent<Image>().raycastTarget);
            var rect = (RectTransform)panel;
            Assert.AreEqual(Vector2.zero, rect.anchorMin);
            Assert.AreEqual(Vector2.one, rect.anchorMax);
            Assert.IsNotNull(panel.Find("SaveErrorWindow/NewGameButton").GetComponent<Button>());
        }
        [UnityTest] public IEnumerator ClickTwice_InvokesOnceAndDestroysObjects()
        {
            int calls = 0;
            view.Initialize(canvas, () => calls++);
            var panel = canvas.transform.Find("SaveErrorPanel").gameObject;
            var button = panel.transform.Find("SaveErrorWindow/NewGameButton").GetComponent<Button>();
            button.onClick.Invoke(); button.onClick.Invoke();
            Assert.AreEqual(1, calls);
            yield return null; yield return null;
            Assert.IsTrue(panel == null);
            Assert.IsTrue(viewObject == null);
        }
    }
}
