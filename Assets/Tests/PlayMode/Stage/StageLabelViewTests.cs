using NUnit.Framework;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;
using PlumSword.Game.Stage;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Tests.Stage
{
    public sealed class StageLabelViewTests
    {
        private GameObject canvas, root;
        private StageProgress stage;
        private Battle battle;

        private void Begin(int start)
        {
            var wallet = new Wallet(0);
            var upgrades = new StatUpgrades(wallet, 1000, 10000);
            battle = new Battle(upgrades);
            stage = new StageProgress(wallet, upgrades, battle, new System.Random(1), start);
            canvas = new GameObject("[LabelCanvas]", typeof(Canvas));
            root = new GameObject("[LabelView]");
            root.AddComponent<StageView>().Initialize(stage, canvas.GetComponent<Canvas>());
            stage.Begin();
        }

        [TearDown]
        public void Cleanup()
        {
            if (root != null) Object.DestroyImmediate(root);
            if (canvas != null) Object.DestroyImmediate(canvas);
        }

        private Transform Find(string path) => canvas.transform.Find(path);

        private static void AssertGlyphs(Transform container, params string[] names)
        {
            Assert.That(container.childCount, Is.EqualTo(names.Length));
            for (int i = 0; i < names.Length; i++)
                Assert.That(container.GetChild(i).name, Is.EqualTo(names[i]));
        }

        [Test]
        public void StageTwo_DisplaysHeaderTitleAndCenteredNumberArt()
        {
            Begin(2);
            Assert.That(Find("SubstagePreview/Header").GetComponent<Image>().sprite.name, Is.EqualTo("header_blank"));
            var title = Find("SubstagePreview/StageTitle");
            Assert.That(title.gameObject.activeSelf, Is.True);
            Assert.That(title.GetComponent<Image>().sprite.name, Is.EqualTo("title_2"));
            var number = Find("SubstagePreview/SubstageNumberArt");
            Assert.That(number.gameObject.activeSelf, Is.True);
            AssertGlyphs(number, "2", "-", "1");
            Assert.That(((RectTransform)number).anchoredPosition.y, Is.EqualTo(-61f).Within(.1f));
            Assert.That(Find("SubstagePreview/SubstageNumber").gameObject.activeSelf, Is.False);
            AssertGlyphs(Find("SubstageIntroBanner/SubstageNumberArt"), "2", "-", "1");
        }

        [Test]
        public void StageFive_DisplaysFinalBossArtAndTitle()
        {
            Begin(5);
            var boss = Find("SubstagePreview/BossNameArt");
            Assert.That(boss.gameObject.activeSelf, Is.True);
            Assert.That(boss.GetComponent<Image>().sprite.name, Is.EqualTo("boss_name_5"));
            Assert.That(Find("SubstagePreview/SubstageNumberArt").gameObject.activeSelf, Is.False);
            Assert.That(Find("SubstageIntroBanner/BossNameArt").GetComponent<Image>().sprite.name, Is.EqualTo("boss_name_5"));
            Assert.That(Find("SubstagePreview/StageTitle").GetComponent<Image>().sprite.name, Is.EqualTo("title_5"));
        }

        [Test]
        public void StageThree_AfterSixSubstages_DisplaysBossName()
        {
            Begin(3);
            for (int n = 0; n < 6; n++)
            {
                stage.Tick(StageProgress.SubstageIntroSeconds);
                for (int i = 0; i < 5000 && stage.Phase == StagePhase.Playing; i++) battle.Step();
                Assert.That(stage.Phase, Is.EqualTo(StagePhase.FadingOut));
                stage.Tick(StageProgress.FadeSeconds * 2);
            }
            Assert.That(Find("SubstagePreview/BossNameArt").GetComponent<Image>().sprite.name, Is.EqualTo("boss_name_3"));
        }
    }
}
