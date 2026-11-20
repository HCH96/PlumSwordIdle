using System.Collections;
using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;
using PlumSword.Game.Combat;
using PlumSword.Game.Stage;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Combat
{
    public sealed class JongnamDownTests
    {
        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DefeatedOpponents_PlayTheirDownAnimationWhileBattleIsPaused()
        {
            var game = GameObject.Find(GameBootstrap.RootName);
            var battle = new Battle(new StatUpgrades(new Wallet(), 10000, 10000));
            battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            battle.AddEnemy(EnemyKind.Normal, 1, 0.8);
            battle.AddEnemy(EnemyKind.Boss, 1, 0.8);
            var obj = new GameObject("[DownTest]");
            obj.transform.SetParent(game.transform, false);
            var camera = new GameObject("Camera", typeof(Camera));
            camera.transform.SetParent(obj.transform, false);
            try
            {
                obj.AddComponent<CombatView>().Initialize(battle, camera.GetComponent<Camera>());
                var male = obj.transform.Find("[Enemy]/Body").GetComponent<SpriteRenderer>();
                SpriteRenderer female = null;
                foreach (Transform child in obj.transform)
                    if (child.name == "[Enemy]" && child.Find("Body").GetComponent<SpriteRenderer>() != male)
                        female = child.Find("Body").GetComponent<SpriteRenderer>();
                var boss = obj.transform.Find("[Boss]/Body").GetComponent<SpriteRenderer>();
                Assert.That(female, Is.Not.Null);
                for (int i = 0; i < 100 && battle.Enemies.Count > 0; i++) battle.Step();
                Assert.That(battle.Enemies, Is.Empty);
                battle.SetPaused(true);
                Assert.That(male.sprite.name, Is.EqualTo("jongnam_male_down_00"));
                Assert.That(female.sprite.name, Is.EqualTo("jongnam_female_down_00"));
                Assert.That(boss.sprite.name, Is.EqualTo("jongnam_boss_down_00"));
                Assert.That(male.transform.parent.Find("HealthBack").gameObject.activeSelf, Is.False);
                yield return new WaitForSeconds(1.3f);
                Assert.That(male.sprite.name, Is.EqualTo("jongnam_male_down_16"));
                Assert.That(female.sprite.name, Is.EqualTo("jongnam_female_down_16"));
                Assert.That(boss.sprite.name, Is.EqualTo("jongnam_boss_down_16"));
                battle.Reset();
                yield return null;
                Assert.That(obj.transform.Find("[Boss]"), Is.Null);
                Assert.That(obj.transform.Find("[Enemy]"), Is.Null);
            }
            finally { Object.DestroyImmediate(obj); }
        }

        [UnityTest]
        public IEnumerator LastOpponent_TransitionWaitsForDownAnimation()
        {
            var wallet = new Wallet();
            var upgrades = new StatUpgrades(wallet, 10000, 10000);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1));
            var obj = new GameObject("[DownTransitionTest]");
            var canvas = new GameObject("Canvas", typeof(Canvas));
            canvas.transform.SetParent(obj.transform, false);
            try
            {
                obj.AddComponent<StageView>().Initialize(stage, canvas.GetComponent<Canvas>());
                stage.Begin();
                stage.Tick(StageProgress.SubstageIntroSeconds);
                for (int i = 0; i < 5000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
                Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
                yield return new WaitForSeconds(0.4f);
                Assert.That(stage.FadeAlpha, Is.EqualTo(0).Within(0.001));
                yield return new WaitForSeconds(1.1f);
                Assert.That(stage.FadeAlpha, Is.GreaterThan(0));
            }
            finally { Object.DestroyImmediate(obj); }
        }

        [UnityTest]
        public IEnumerator BossVictory_TransitionWaitsForBossDownAnimation()
        {
            var wallet = new Wallet();
            var upgrades = new StatUpgrades(wallet, 10000, 10000);
            var battle = new Battle(upgrades);
            var stage = new StageProgress(wallet, upgrades, battle, new System.Random(1));
            var obj = new GameObject("[BossDownTransitionTest]");
            var canvas = new GameObject("Canvas", typeof(Canvas));
            canvas.transform.SetParent(obj.transform, false);
            try
            {
                obj.AddComponent<StageView>().Initialize(stage, canvas.GetComponent<Canvas>());
                stage.Begin();
                for (int n = 0; n < StageLayout.NormalSubstageCount; n++)
                {
                    stage.Tick(StageProgress.SubstageIntroSeconds);
                    for (int i = 0; i < 5000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
                    Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
                    stage.Tick(StageProgress.FadeSeconds * 2);
                }
                stage.Tick(StageProgress.SubstageIntroSeconds);
                for (int i = 0; i < 5000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
                Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
                yield return new WaitForSeconds(0.4f);
                Assert.That(stage.FadeAlpha, Is.EqualTo(0).Within(0.001));
                yield return new WaitForSeconds(1.1f);
                Assert.That(stage.FadeAlpha, Is.GreaterThan(0));
            }
            finally { Object.DestroyImmediate(obj); }
        }
    }
}
