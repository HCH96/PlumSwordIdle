// [Game] 루트는 테스트 사이에 남는다. 흐름 테스트는 실패한 경우에도 실행되는
// [UnityTearDown]에서 반드시 GameFlow.ResetToFreshLaunch()를 호출한다.
using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using PlumSword.Core.Save;
using PlumSword.Core.StartMenu;
using PlumSword.Core.StatUpgrade;
using UnityEngine;

namespace PlumSword.Game.Tests.Flow
{
    public static class GameFlow
    {
        public static GameRoot FindRoot()
        {
            var game = GameObject.Find(GameBootstrap.RootName);
            return game != null ? game.GetComponent<GameRoot>() : null;
        }

        public static IEnumerator Relaunch(MemorySaveStore store, StartEntry entry, bool autoUpgrade = false)
        {
            var previous = FindRoot();
            Assert.That(previous, Is.Not.Null, "재생성할 게임 루트가 없습니다.");
            GameRoot.BatchSaveStore = store;
            // 씬 카메라는 다음 씬 로드에서 파괴된다. 새 루트가 자체 카메라를
            // 만들도록 재생성 동안 외부 메인 카메라의 태그를 잠시 감춘다.
            var sceneCameras = new List<Camera>();
            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera.CompareTag("MainCamera") && !camera.transform.IsChildOf(previous.transform))
                {
                    sceneCameras.Add(camera);
                    camera.tag = "Untagged";
                }
            }
            try
            {
                previous.RecreateAs(entry);
                GameRoot current = null;
                for (var frame = 0; frame < 300; frame++)
                {
                    current = FindRoot();
                    if (current != null && current != previous) break;
                    yield return null;
                }
                Assert.That(current != null && current != previous, Is.True, "300프레임 안에 새 게임 루트가 생기지 않았습니다.");
                if (!autoUpgrade) current.AutoUpgrade.SetEnabled(false);
            }
            finally
            {
                foreach (var camera in sceneCameras)
                    if (camera != null) camera.tag = "MainCamera";
            }
            for (var frame = 0; frame < 3; frame++) yield return null;
        }

        public static IEnumerator ResetToFreshLaunch()
        {
            Time.timeScale = 1f;
            yield return Relaunch(null, StartEntry.Launch, true);
            GameRoot.BatchSaveStore = null;
        }

        public static IEnumerator PowerUp(GameRoot root, int levels)
        {
            root.AutoUpgrade.SetEnabled(false);
            foreach (var stat in new[] { StatType.Attack, StatType.Health })
            {
                for (var level = 0; level < levels; level++)
                {
                    var frame = 0;
                    while (root.StatUpgrades.Locked && frame < 600)
                    {
                        frame++;
                        yield return null;
                    }
                    Assert.That(root.StatUpgrades.Locked, Is.False, "600프레임 안에 강화 잠금이 풀리지 않았습니다.");
                    root.Wallet.Add(root.StatUpgrades.GetCost(stat));
                    Assert.That(root.StatUpgrades.TryUpgrade(stat), Is.True);
                }
            }
        }

        public static IEnumerator WaitUntil(Func<bool> condition, float maxRealSeconds, Func<string> describe)
        {
            var start = Time.realtimeSinceStartup;
            while (!condition())
            {
                if (Time.realtimeSinceStartup - start > maxRealSeconds) Assert.Fail(describe());
                yield return null;
            }
        }
    }
}
