using System;
using NUnit.Framework;
using PlumSword.Game.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Tests.Stage
{
    public sealed class EndingViewTests
    {
        [Test]
        public void Initialize_ShowsEndingAndReturnsOnce()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas));
            var viewObject = new GameObject("EndingView");
            try
            {
                int calls = 0;
                viewObject.AddComponent<EndingView>().Initialize(canvasObject.GetComponent<Canvas>(), 754.9, 3, () => calls++);
                var panel = canvasObject.transform.Find("EndingPanel");
                Assert.That(panel, Is.Not.Null);
                Assert.That(ReadText(panel, "EndingTitle"), Is.EqualTo("천마 토벌"));
                Assert.That(ReadText(panel, "EndingPlayTime"), Is.EqualTo("플레이 시간 12:34"));
                Assert.That(ReadText(panel, "EndingRegressions"), Is.EqualTo("회귀 3회"));
                var button = panel.Find("EndingReturnButton");
                Assert.That(button, Is.Not.Null);
                button.GetComponent<Button>().onClick.Invoke();
                button.GetComponent<Button>().onClick.Invoke();
                Assert.That(calls, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(viewObject);
                UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        private static string ReadText(Transform parent, string name)
        {
            var type = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
            return (string)type.GetProperty("text").GetValue(parent.Find(name).GetComponent(type));
        }
    }
}
