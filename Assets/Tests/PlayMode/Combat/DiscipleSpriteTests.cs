using System.Collections;
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
            Assert.That(body.transform.localScale, Is.EqualTo(Vector3.one));
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

        private static SpriteRenderer FindDiscipleBody()
        {
            var game = GameObject.Find(GameBootstrap.RootName);
            if (game == null) return null;
            var body = game.transform.Find("[CombatView]/[Disciple]/Body");
            return body != null ? body.GetComponent<SpriteRenderer>() : null;
        }
    }
}
