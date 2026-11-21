using System.Collections;
using System.Linq;
using NUnit.Framework;
using PlumSword.Core.Audio;
using PlumSword.Core.Combat;
using PlumSword.Game.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Audio
{
    public sealed class AudioViewTests
    {
        private GameRoot root;
        private GameObject sharedRoot;
        private int sharedStage;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            sharedRoot = GameObject.Find(GameBootstrap.RootName);
            if (sharedRoot != null)
            {
                sharedStage = sharedRoot.GetComponent<GameRoot>().StageProgress.StageNumber;
                sharedRoot.SetActive(false);
            }
            yield return SceneManager.LoadSceneAsync(0);
            var testRoot = new GameObject(GameBootstrap.RootName);
            root = testRoot.AddComponent<GameRoot>();
            for (int i = 0; i < 3; i++) yield return null;
            Assert.That(GameObject.Find(GameBootstrap.RootName).GetComponent<GameRoot>(), Is.SameAs(root));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (root != null) Object.DestroyImmediate(root.gameObject);
            if (sharedRoot != null)
            {
                sharedRoot.SetActive(true);
                Assert.That(sharedRoot.GetComponent<GameRoot>().StageProgress.StageNumber, Is.EqualTo(sharedStage));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Boot_AssemblesOneAudioView()
        {
            var views = root.GetComponentsInChildren<AudioView>(true);
            Assert.That(views.Length, Is.EqualTo(1));
            Assert.That(views[0].transform.parent, Is.EqualTo(root.transform));
            Assert.That(views[0].name, Is.EqualTo("[AudioView]"));
            Assert.That(root.Audio, Is.SameAs(views[0]));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Boot_PlaysNormalBattleTrack()
        {
            Assert.That(root.Audio.CurrentTrack, Is.EqualTo(MusicTrack.BattleNormal));
            Assert.That(root.Audio.CurrentMusicSource.clip, Is.Not.Null);
            Assert.That(root.Audio.CurrentMusicSource.clip.name, Is.EqualTo("battle_normal"));
            Assert.That(root.Audio.CurrentMusicSource.loop, Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BossAdded_PlaysAppearanceClip()
        {
            root.Battle.AddEnemy(EnemyKind.Boss, 1, root.Battle.DisciplePosition + 10);
            yield return null;
            Assert.That(root.Audio.SfxSources.Any(source => source.clip != null && source.clip.name == "boss_appear"), Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BgmVolume_ChangesWithoutReplacingClip()
        {
            yield return new WaitForSecondsRealtime(1.6f);
            var clip = root.Audio.CurrentMusicSource.clip;
            Assert.That(clip, Is.Not.Null);
            root.Volume.SetBgm(0);
            yield return null;
            Assert.That(root.Audio.CurrentMusicSource.volume, Is.EqualTo(0f).Within(1e-3));
            Assert.That(root.Audio.CurrentMusicSource.clip, Is.SameAs(clip));
            root.Volume.SetBgm(100);
            yield return null;
            Assert.That(root.Audio.CurrentMusicSource.volume, Is.EqualTo(0.6f).Within(1e-3));
        }

        [UnityTest]
        public IEnumerator MissingClip_WarnsOnlyOnce()
        {
            var cache = new AudioClipCache();
            LogAssert.Expect(LogType.Warning, "[Audio] 소리 파일을 찾지 못했습니다: Audio/SFX/__missing__");
            Assert.That(cache.Load("Audio/SFX/__missing__"), Is.Null);
            Assert.That(cache.Load("Audio/SFX/__missing__"), Is.Null);
            LogAssert.NoUnexpectedReceived();
            yield return null;
        }

        [UnityTest]
        public IEnumerator NullPath_ReturnsNullWithoutLogging()
        {
            Assert.That(new AudioClipCache().Load(null), Is.Null);
            LogAssert.NoUnexpectedReceived();
            yield return null;
        }
    }
}
