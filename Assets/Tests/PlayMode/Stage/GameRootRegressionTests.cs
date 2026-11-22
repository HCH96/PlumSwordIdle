using System.Collections;
using NUnit.Framework;
using PlumSword.Game.Tests.Flow;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Stage
{
    public sealed class GameRootRegressionTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return GameFlow.ResetToFreshLaunch();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return GameFlow.ResetToFreshLaunch();
        }

        [UnityTest]
        public IEnumerator FirstScene_EnablesRegressionAndCreatesPlayTime()
        {
            var root = GameFlow.FindRoot();
            Assert.That(root.StageProgress.UseRegression, Is.True);
            Assert.That(root.PlayTime, Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FreshGame_AfterTwoSeconds_AccumulatesPlayTime()
        {
            var root = GameFlow.FindRoot();
            yield return new WaitForSecondsRealtime(2f);
            Assert.That(root.PlayTime.Seconds, Is.GreaterThan(1.0));
        }
    }
}
