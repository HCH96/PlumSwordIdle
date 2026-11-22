using System.Collections;
using NUnit.Framework;
using PlumSword.Game.Stage;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PlumSword.Game.Tests.Stage
{
    public sealed class EndingViewTests
    {
        [UnityTest]
        public IEnumerator Ending_FadesToArtThenReturnsAutomatically()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas));
            var viewObject = new GameObject("EndingView");
            try
            {
                int calls = 0;
                viewObject.AddComponent<EndingView>().Initialize(canvasObject.GetComponent<Canvas>(), 754.9, 3, () => calls++);
                var panel = canvasObject.transform.Find("EndingPanel");
                Assert.That(panel, Is.Not.Null);
                var art = panel.Find("EndingArt");
                var fade = panel.Find("EndingFade").GetComponent<Image>();
                Assert.That(art.GetComponent<Image>().sprite, Is.Not.Null);
                Assert.That(art.gameObject.activeSelf, Is.False);
                Assert.That(panel.childCount, Is.EqualTo(2));
                yield return new WaitForSecondsRealtime(EndingView.FadeSeconds + 0.2f);
                Assert.That(art.gameObject.activeSelf, Is.True);
                Assert.That(fade.color.a, Is.GreaterThan(0.5f));
                yield return new WaitForSecondsRealtime(EndingView.FadeSeconds + 0.2f);
                Assert.That(fade.color.a, Is.LessThan(0.05f));
                Assert.That(calls, Is.Zero);
                yield return new WaitForSecondsRealtime(EndingView.HoldSeconds + EndingView.FadeSeconds + 0.2f);
                Assert.That(fade.color.a, Is.EqualTo(1f).Within(0.01f));
                Assert.That(calls, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
