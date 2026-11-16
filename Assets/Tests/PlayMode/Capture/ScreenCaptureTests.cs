using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PlumSword.Game.Tests.Capture
{
    /// <summary>
    /// 루프가 통합 브랜치에 합치기 직전에 게임 화면을 PNG 로 남긴다 (docs/dev/loop.md "증거").
    /// 환경 변수 PLUMSWORD_CAPTURE_DIR 이 있을 때만 찍는다. tools/loop/run_unity_tests.py --capture-dir 가 정해 준다.
    /// 평소 테스트 실행에서는 아무것도 쓰지 않고 지나간다.
    /// </summary>
    public sealed class ScreenCaptureTests
    {
        private const string DirectoryVariable = "PLUMSWORD_CAPTURE_DIR";
        private const int Width = 1280;
        private const int Height = 720;
        private const int FramesPerSecond = 30;

        // 실행 후 지난 게임 시간(초). 이 시점마다 한 장씩 찍는다.
        private static readonly int[] Marks = { 1, 10, 30 };

        [UnityTest]
        public IEnumerator Capture_WhenDirectoryIsSet_SavesOneImagePerMark()
        {
            var directory = Environment.GetEnvironmentVariable(DirectoryVariable);
            if (string.IsNullOrEmpty(directory))
            {
                Assert.That(directory, Is.Null.Or.Empty);
                yield break;
            }

            Directory.CreateDirectory(directory);
            yield return SceneManager.LoadSceneAsync(0);

            // 프레임마다 게임 시간이 1/30초씩 흐르게 해서, 기계 속도와 상관없이 같은 시점을 찍는다.
            Time.captureFramerate = FramesPerSecond;
            try
            {
                var frame = 0;
                foreach (var mark in Marks)
                {
                    while (frame < mark * FramesPerSecond)
                    {
                        frame++;
                        yield return null;
                    }

                    yield return Save(Path.Combine(directory, $"game_{mark:000}s.png"));
                }
            }
            finally
            {
                Time.captureFramerate = 0;
            }

            Assert.That(Directory.GetFiles(directory, "game_*.png").Length, Is.EqualTo(Marks.Length));
        }

        private static IEnumerator Save(string path)
        {
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null, "화면을 찍을 MainCamera 가 없다");

            var target = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            camera.targetTexture = target;

            // 오버레이 캔버스는 카메라 렌더링에 들어가지 않으므로, 찍는 동안만 카메라 캔버스로 바꾼다.
            var overlays = new List<(Canvas canvas, int order)>();
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    continue;
                }

                overlays.Add((canvas, canvas.sortingOrder));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 0.5f;
                canvas.sortingOrder = short.MaxValue;
            }

            // 캔버스 크기 계산(CanvasScaler)이 새 화면 크기를 반영할 때까지 기다린다.
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            camera.Render();

            var previousActive = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();
            RenderTexture.active = previousActive;
            File.WriteAllBytes(path, image.EncodeToPNG());
            UnityEngine.Object.Destroy(image);

            foreach (var (canvas, order) in overlays)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.worldCamera = null;
                canvas.sortingOrder = order;
            }

            camera.targetTexture = previousTarget;
            RenderTexture.ReleaseTemporary(target);
            yield return null;
        }
    }
}
