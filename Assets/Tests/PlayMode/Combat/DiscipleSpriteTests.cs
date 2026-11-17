using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Combat
{
    /// <summary>
    /// 전투 화면의 제자가 Resources 의 스프라이트 시트로 그려지는지 확인한다.
    /// 시트가 없으면 단색 사각형 대체 표시로 돌아가므로, 이 테스트가 실패하면 에셋 가져오기나 크기를 먼저 본다.
    /// </summary>
    public sealed class DiscipleSpriteTests
    {
        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            yield return SceneManager.LoadSceneAsync(0);
            yield return null;
        }

        [Test]
        public void Sheets_ImportedAsPointFilteredTextures()
        {
            foreach (var path in new[] { "Art/Characters/disciple_idle", "Art/Characters/disciple_run", "Art/Characters/disciple_attack" })
            {
                var texture = Resources.Load<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point), path);
                Assert.That(texture.mipmapCount, Is.EqualTo(1), path);
            }
        }

        [UnityTest]
        public IEnumerator Disciple_UsesSpriteFramesAtFootPivot()
        {
            var body = FindDiscipleBody();
            Assert.That(body, Is.Not.Null, "[Disciple]/Body 가 없다");
            Assert.That(body.sprite, Is.Not.Null);
            Assert.That(body.sprite.name, Does.StartWith("disciple_"), "대체 표시가 쓰이고 있다");
            Assert.That(body.sprite.pixelsPerUnit, Is.EqualTo(32f));
            Assert.That(body.transform.localScale, Is.EqualTo(new Vector3(1.5f, 1.5f, 1f)));
            Assert.That(body.transform.localPosition, Is.EqualTo(Vector3.zero));

            // 피벗이 발 아래라 스프라이트 아래 끝은 발보다 아래(여백), 위 끝은 머리 위에 있다.
            Assert.That(body.sprite.bounds.min.y, Is.LessThan(0f));
            Assert.That(body.sprite.bounds.max.y, Is.GreaterThan(1.5f));

            // 게임이 흘러가면 다른 프레임으로 바뀐다 (화면 없는 실행은 프레임이 매우 짧아 게임 시간으로 기다린다).
            var first = body.sprite;
            yield return new WaitForSeconds(0.6f);
            Assert.That(body.sprite, Is.Not.SameAs(first));
            Assert.That(body.sprite.name, Does.StartWith("disciple_"));
        }

        [Test]
        public void MainCamera_HasPixelPerfectCamera()
        {
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            // 테스트 어셈블리가 URP 2D 를 참조하지 않도록 이름으로 찾는다.
            Assert.That(camera.GetComponent("PixelPerfectCamera"), Is.Not.Null);
            Assert.That(camera.orthographic, Is.True);
        }

        [Test]
        public void EnemyDisplay_SitsSlightlyFartherWithoutChangingCombatPosition()
        {
            var upgrades = new PlumSword.Core.StatUpgrade.StatUpgrades(
                new PlumSword.Core.Currency.Wallet(0), 10, 100, 1.0);
            var battle = new PlumSword.Core.Combat.Battle(upgrades);
            var enemy = battle.AddEnemy(PlumSword.Core.Combat.EnemyKind.Normal, 1, 0.8);
            var viewObject = new GameObject("[TestCombatView]");
            var cameraObject = new GameObject("[TestCamera]");
            try
            {
                var view = viewObject.AddComponent<PlumSword.Game.Combat.CombatView>();
                view.Initialize(battle, cameraObject.AddComponent<Camera>());
                var disciple = viewObject.transform.Find("[Disciple]");
                var opponent = viewObject.transform.Find("[Enemy]");
                Assert.That(opponent.localPosition.x - disciple.localPosition.x,
                    Is.EqualTo(enemy.Position - battle.DisciplePosition + 0.4).Within(1e-5));
                Assert.That(enemy.Position, Is.EqualTo(0.8).Within(1e-9));
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [UnityTest]
        public IEnumerator StageOne_CombatPetalsAreSmallAndDoNotBlockButtons()
        {
            var game = GameObject.Find(GameBootstrap.RootName);
            var root = game.GetComponent<GameRoot>();
            Assert.That(root.StageProgress.DisplayStageNumber, Is.EqualTo(1));
            yield return null;
            var petals = root.UiCanvas.transform.Find("[CombatPetals]");
            Assert.That(petals, Is.Not.Null);
            Assert.That(petals.childCount, Is.EqualTo(20));
            int visible = 0;
            foreach (Transform child in petals)
            {
                var image = child.GetComponent<UnityEngine.UI.Image>();
                Assert.That(image.raycastTarget, Is.False);
                if (!image.enabled) continue;
                visible++;
                var size = ((RectTransform)child).sizeDelta;
                Assert.That(Mathf.Max(size.x, size.y), Is.InRange(4.19f, 8.41f));
            }
            Assert.That(visible, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator StageOne_JongnamOpponentsUseBothAnimatedSprites()
        {
            var maleAttack = Resources.Load<Texture2D>("Art/Enemies/jongnam_male_attack");
            var femaleAttack = Resources.Load<Texture2D>("Art/Enemies/jongnam_female_attack");
            Assert.That(maleAttack, Is.Not.Null);
            Assert.That(femaleAttack, Is.Not.Null);
            Assert.That(maleAttack.width, Is.EqualTo(92 * 24), "가져오는 동안 시트가 축소되면 안 된다");
            Assert.That(femaleAttack.width, Is.EqualTo(96 * 24));
            Assert.That(maleAttack.filterMode, Is.EqualTo(FilterMode.Point));

            var game = GameObject.Find(GameBootstrap.RootName);
            Assert.That(game, Is.Not.Null);
            var combat = CreateIsolatedCombat(game.transform);
            try
            {
                bool male = false, female = false;
                foreach (Transform child in combat.transform)
                {
                    if (child.name != "[Enemy]") continue;
                    var body = child.Find("Body").GetComponent<SpriteRenderer>();
                    male |= body.sprite.name.StartsWith("jongnam_male_");
                    female |= body.sprite.name.StartsWith("jongnam_female_");
                }
                Assert.That(male && female, Is.True, "1스테이지에는 남녀 종남 제자가 모두 보여야 한다");
                yield return null;
            }
            finally { Object.DestroyImmediate(combat); }
        }

        [UnityTest]
        public IEnumerator StageOne_OpponentsKeepDistinctDrawOrderAsFramesChange()
        {
            var game = GameObject.Find(GameBootstrap.RootName);
            var combat = CreateIsolatedCombat(game.transform);
            try
            {
            var renderers = new List<SpriteRenderer>();
            var orders = new HashSet<int>();
            foreach (Transform child in combat.transform)
            {
                if (child.name != "[Enemy]") continue;
                var body = child.Find("Body").GetComponent<SpriteRenderer>();
                renderers.Add(body);
                Assert.That(orders.Add(body.sortingOrder), Is.True, "적마다 그리는 순서가 달라야 한다");
            }
            Assert.That(renderers.Count, Is.GreaterThanOrEqualTo(2));
            var originalOrders = new int[renderers.Count];
            for (int i = 0; i < renderers.Count; i++) originalOrders[i] = renderers[i].sortingOrder;
            var firstFrame = renderers[0].sprite;
            var rect = firstFrame.rect;
            var nextFrame = Sprite.Create(firstFrame.texture,
                new Rect((rect.x + rect.width) % firstFrame.texture.width, rect.y, rect.width, rect.height),
                new Vector2(0.5f, 0.5f), firstFrame.pixelsPerUnit);
            renderers[0].sprite = nextFrame;
            Assert.That(renderers[0].sprite, Is.Not.SameAs(firstFrame));
            for (int i = 0; i < renderers.Count; i++)
                Assert.That(renderers[i].sortingOrder, Is.EqualTo(originalOrders[i]));
            yield return null;
            Object.Destroy(nextFrame);
            }
            finally { Object.DestroyImmediate(combat); }
        }

        private static GameObject CreateIsolatedCombat(Transform game)
        {
            var wallet = new PlumSword.Core.Currency.Wallet();
            var upgrades = new PlumSword.Core.StatUpgrade.StatUpgrades(wallet, 10, 100, 1);
            var battle = new PlumSword.Core.Combat.Battle(upgrades);
            battle.AddEnemy(PlumSword.Core.Combat.EnemyKind.Normal, 1, 5);
            battle.AddEnemy(PlumSword.Core.Combat.EnemyKind.Normal, 1, 7);
            battle.SetPaused(true);
            var obj = new GameObject("[IsolatedCombatView]");
            obj.transform.SetParent(game, false);
            var camera = new GameObject("Camera", typeof(Camera));
            camera.transform.SetParent(obj.transform, false);
            obj.AddComponent<PlumSword.Game.Combat.CombatView>().Initialize(battle, camera.GetComponent<Camera>());
            return obj;
        }

        private static SpriteRenderer FindDiscipleBody()
        {
            var game = GameObject.Find(GameBootstrap.RootName);
            if (game == null) return null;
            var body = game.transform.Find("[CombatView]/[Disciple]/Body");
            return body != null ? body.GetComponent<SpriteRenderer>() : null;
        }
    }
}
