using System.Collections;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;
using PlumSword.Game.Stage;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Stage
{
    public sealed class StageClearWaitViewTests
    {
        private GameObject root;
        private GameObject canvas;
        private StageProgress stage;

        [TearDown]
        public void Cleanup()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (canvas != null) Object.DestroyImmediate(canvas);
        }

        private void BeginAndWin(int startStage)
        {
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 1000, 10000);
            var battle = new Battle(upgrades);
            stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), startStage, 5, 1.5);
            root = new GameObject("[ClearWaitView]");
            canvas = new GameObject("[ClearWaitCanvas]", typeof(Canvas));
            root.AddComponent<StageView>().Initialize(stage, canvas.GetComponent<Canvas>());
            stage.Begin();
            stage.Tick(StageProgress.SubstageIntroSeconds);
            for (int i = 0; i < 5000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.ClearWait));
        }

        [UnityTest]
        public IEnumerator FirstScene_GameRoot_UsesDefaultClearWait()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
            var game = GameObject.Find(GameBootstrap.RootName);
            Assert.That(game, Is.Not.Null);
            Assert.That(game.GetComponent<GameRoot>().StageProgress.ClearWaitSeconds, Is.EqualTo(1.5));
        }

        [UnityTest]
        public IEnumerator ClearWait_AfterOnePointTwoSeconds_HasNoFade()
        {
            BeginAndWin(2);
            yield return new WaitForSeconds(1.2f);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.ClearWait));
            Assert.That(stage.FadeAlpha, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator ClearWait_AfterTwoSeconds_HasEnded()
        {
            BeginAndWin(2);
            yield return new WaitForSeconds(2f);
            Assert.That(stage.Phase, Is.Not.EqualTo(StagePhase.ClearWait));
        }

        [UnityTest]
        public IEnumerator FinalBoss_ClearWait_OnlyWaitsForRemainingDefeatAnimation()
        {
            var sheet = Resources.Load<Texture2D>("Art/Enemies/tianmo_final_boss_down");
            Assert.That(sheet, Is.Not.Null);
            Assert.That(sheet.width, Is.EqualTo(2600));
            Assert.That(sheet.height, Is.EqualTo(104));
            BeginAndWin(5);
            yield return new WaitForSeconds(1.7f);
            Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
            Assert.That(stage.FadeAlpha, Is.EqualTo(0).Within(0.1));
            yield return new WaitForSeconds(0.5f);
            Assert.That(stage.FadeAlpha, Is.GreaterThan(0));
        }
    }
}
