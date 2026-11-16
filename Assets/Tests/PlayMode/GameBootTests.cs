using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests
{
    /// <summary>
    /// 게임이 실제로 켜지는지 확인한다. 화면 코드는 에딧 모드 테스트가 닿지 않으므로,
    /// 조립 중에 예외나 오류 로그가 나면 여기서 잡는다 (테스트 중 오류 로그는 실패로 처리된다).
    /// </summary>
    public sealed class GameBootTests
    {
        [UnityTest]
        public IEnumerator Boot_FirstScene_AssemblesGameRootWithoutErrors()
        {
            yield return SceneManager.LoadSceneAsync(0);
            for (var i = 0; i < 60; i++)
            {
                yield return null;
            }

            var root = GameObject.Find(GameBootstrap.RootName);
            Assert.That(root, Is.Not.Null, "[Game] 루트 오브젝트가 없다");
            Assert.That(root.GetComponent<GameRoot>(), Is.Not.Null, "[Game] 에 GameRoot 가 붙어 있지 않다");
            Assert.That(Camera.main, Is.Not.Null, "첫 씬에 MainCamera 가 없다");
        }
    }
}
