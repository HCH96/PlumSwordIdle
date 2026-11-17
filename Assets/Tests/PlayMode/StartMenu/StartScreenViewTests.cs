using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using PlumSword.Core.StartMenu;
using PlumSword.Game.StartMenu;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace PlumSword.Game.Tests.StartMenu
{
    public sealed class StartScreenViewTests
    {
        private GameObject canvasObject;
        private GameObject viewObject;
        private Canvas canvas;
        private StartScreenView view;

        [SetUp]
        public void SetUp()
        {
            canvasObject = new GameObject("TestCanvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            viewObject = new GameObject("TestStartScreenView");
            view = viewObject.AddComponent<StartScreenView>();
        }

        [TearDown]
        public void TearDown()
        {
            if (viewObject != null) UnityEngine.Object.DestroyImmediate(viewObject);
            if (canvasObject != null) UnityEngine.Object.DestroyImmediate(canvasObject);
        }

        [Test]
        public void Initialize_NullCanvas_Throws() => Assert.Throws<ArgumentNullException>(() => view.Initialize(null, StartButtonKind.Continue, () => { }, () => { }));
        [Test]
        public void Initialize_NullStart_Throws() => Assert.Throws<ArgumentNullException>(() => view.Initialize(canvas, StartButtonKind.Continue, null, () => { }));
        [Test]
        public void Initialize_NullSettings_Throws() => Assert.Throws<ArgumentNullException>(() => view.Initialize(canvas, StartButtonKind.Continue, () => { }, null));

        [Test]
        public void Initialize_Continue_CreatesFullScreenLastPanelAndLabels()
        {
            new GameObject("Other", typeof(RectTransform)).transform.SetParent(canvas.transform, false);
            view.Initialize(canvas, StartButtonKind.Continue, () => { }, () => { }, null);
            var panel = canvas.transform.GetChild(canvas.transform.childCount - 1);
            Assert.AreEqual("StartScreenPanel", panel.name);
            var rect = (RectTransform)panel;
            Assert.AreEqual(Vector2.zero, rect.anchorMin);
            Assert.AreEqual(Vector2.one, rect.anchorMax);
            Assert.AreEqual(Vector2.zero, rect.offsetMin);
            Assert.AreEqual(Vector2.zero, rect.offsetMax);
            Assert.IsTrue(panel.GetComponent<Image>().raycastTarget);
            CollectionAssert.AreEquivalent(new[] { "PLUM BLOSSOM SWORD", "CONTINUE", "SETTINGS" }, Texts(panel));
        }

        [Test]
        public void Initialize_NewGame_ShowsStartLabel()
        {
            view.Initialize(canvas, StartButtonKind.NewGame, () => { }, () => { }, null);
            Assert.That(Texts(canvas.transform.Find("StartScreenPanel/StartButton")), Is.EqualTo(new[] { "START" }));
        }

        [Test]
        public void Initialize_CreatesSpecifiedLayoutAndButtonStyles()
        {
            view.Initialize(canvas, StartButtonKind.NewGame, () => { }, () => { }, null);
            var panel = canvas.transform.Find("StartScreenPanel");
            Assert.AreEqual(new Color(0.10f, 0.07f, 0.12f), panel.GetComponent<Image>().color);
            AssertRect(panel.Find("Title"), new Vector2(0, 70), new Vector2(600, 48));
            AssertButton(panel.Find("StartButton"), new Vector2(0, -20), new Vector2(200, 40), new Color(0.20f, 0.45f, 0.25f));
            AssertButton(panel.Find("SettingsButton"), new Vector2(0, -72), new Vector2(200, 32), new Color(0.25f, 0.25f, 0.30f));
        }

        // 배경 그림(1672×941)에서 제목 글자(붓 획 포함)가 차지하는 곳, 제자가 서 있는 곳. 그림 왼쪽 위 기준 비율.
        // 제목 위의 매화 가지(y 70~125)는 장식이라 긴 가로 화면에서 조금 잘려도 된다.
        private static readonly Rect TitleArea = Rect.MinMaxRect(540f / 1672, 125f / 941, 1385f / 1672, 400f / 941);
        private static readonly Rect DiscipleArea = Rect.MinMaxRect(100f / 1672, 90f / 941, 520f / 1672, 850f / 941);

        [Test]
        public void ArtLoad_FindsBackgroundAndButtonSprites()
        {
            var art = StartScreenArt.Load();
            Assert.IsNotNull(art);
            Assert.AreEqual(new Vector2(1672, 941), art.Background.rect.size);
            Assert.AreEqual(new Vector2(1436, 262), art.StartButton.rect.size);
            Assert.AreEqual(new Vector2(1436, 262), art.ContinueButton.rect.size);
            Assert.AreEqual(new Vector2(1436, 262), art.SettingsButton.rect.size);
            Assert.AreSame(art.ContinueButton, art.StartButtonSprite(StartButtonKind.Continue));
            Assert.AreSame(art.StartButton, art.StartButtonSprite(StartButtonKind.NewGame));
            Assert.AreSame(art.StartButton, art.StartButtonSprite((StartButtonKind)99));
        }

        [TestCase(StartButtonKind.Continue, "button_continue")]
        [TestCase(StartButtonKind.NewGame, "button_start")]
        public void Initialize_WithArt_UsesTitleBackgroundAndButtonSprites(StartButtonKind kind, string startSprite)
        {
            view.Initialize(canvas, kind, () => { }, () => { });
            var panel = canvas.transform.Find("StartScreenPanel");
            Assert.IsTrue(panel.GetComponent<Image>().raycastTarget);
            var background = panel.Find("Background");
            Assert.AreEqual("title_background", background.GetComponent<Image>().sprite.name);
            Assert.IsFalse(background.GetComponent<Image>().raycastTarget);
            Assert.AreEqual(AspectRatioFitter.AspectMode.EnvelopeParent, background.GetComponent<AspectRatioFitter>().aspectMode);
            Assert.AreEqual(startSprite, panel.Find("StartButton/Art").GetComponent<Image>().sprite.name);
            Assert.AreEqual("button_settings", panel.Find("SettingsButton/Art").GetComponent<Image>().sprite.name);
            // 제목은 배경 그림에 있으므로 글자로 다시 쓰지 않는다. 버튼 글자도 그림에 있다.
            Assert.IsNull(panel.Find("Title"));
            Assert.IsEmpty(Texts(panel));
        }

        [Test]
        public void Initialize_WithArt_CreatesSpecifiedButtonLayout()
        {
            view.Initialize(canvas, StartButtonKind.NewGame, () => { }, () => { });
            var panel = canvas.transform.Find("StartScreenPanel");
            AssertRect(panel.Find("StartButton"), new Vector2(44, -28), new Vector2(230, 42));
            AssertRect(panel.Find("SettingsButton"), new Vector2(44, -80), new Vector2(230, 42));
            foreach (var name in new[] { "StartButton", "SettingsButton" })
            {
                var button = panel.Find(name);
                // 버튼 자신은 투명한 눌리는 영역, 그림은 자식 Art (같은 크기, 눌림을 받지 않음).
                var hitArea = button.GetComponent<Image>();
                Assert.AreEqual(0f, hitArea.color.a);
                Assert.IsTrue(hitArea.raycastTarget);
                Assert.IsNull(hitArea.sprite);
                var art = button.Find("Art");
                AssertRect(art, Vector2.zero, new Vector2(230, 42));
                var artImage = art.GetComponent<Image>();
                Assert.IsTrue(artImage.preserveAspect);
                Assert.IsFalse(artImage.raycastTarget);
                var b = (float)ButtonHoverState.NormalBrightness;
                Assert.AreEqual(new Color(b, b, b, 1), artImage.color);
                Assert.AreEqual(Vector3.one, art.localScale);
                Assert.AreEqual(Selectable.Transition.None, button.GetComponent<Button>().transition);
                Assert.IsNotNull(button.GetComponent<StartButtonHover>());
            }
        }

        // 마우스를 올리면 그림만 커지고 밝아지며 위로 뜬다. 버튼(눌리는 영역)은 움직이지 않는다. 내리면 돌아온다.
        [UnityTest]
        public IEnumerator Hover_EnlargesBrightensAndLiftsArtOnly([Values("StartButton", "SettingsButton")] string name)
        {
            view.Initialize(canvas, StartButtonKind.Continue, () => { }, () => { });
            var button = (RectTransform)canvas.transform.Find("StartScreenPanel/" + name);
            var art = (RectTransform)button.Find("Art");
            var hover = button.GetComponent<StartButtonHover>();
            var artImage = art.GetComponent<Image>();

            hover.OnPointerEnter(null);
            float until = Time.realtimeSinceStartup + 0.5f;
            while (Time.realtimeSinceStartup < until) yield return null;
            Assert.AreEqual((float)ButtonHoverState.HoverScale, art.localScale.x, 0.01f);
            Assert.AreEqual(art.localScale.x, art.localScale.y);
            Assert.AreEqual((float)ButtonHoverState.HoverLift, art.anchoredPosition.y, 0.1f);
            Assert.AreEqual((float)ButtonHoverState.HoverBrightness, artImage.color.r, 0.01f);
            Assert.AreEqual(new Vector2(44, name == "StartButton" ? -28 : -80), button.anchoredPosition);
            Assert.AreEqual(Vector3.one, button.localScale);

            hover.OnPointerDown(null);
            until = Time.realtimeSinceStartup + 0.5f;
            while (Time.realtimeSinceStartup < until) yield return null;
            Assert.AreEqual((float)ButtonHoverState.PressScale, art.localScale.x, 0.01f);

            hover.OnPointerUp(null);
            hover.OnPointerExit(null);
            until = Time.realtimeSinceStartup + 0.6f;
            while (Time.realtimeSinceStartup < until) yield return null;
            Assert.AreEqual(1f, art.localScale.x, 0.01f);
            Assert.AreEqual(0f, art.anchoredPosition.y, 0.05f);
            Assert.AreEqual((float)ButtonHoverState.NormalBrightness, artImage.color.r, 0.01f);
        }

        // 그림이 없을 때(대체 버튼)는 호버 반응을 붙이지 않는다 (예전 모양 그대로).
        [Test]
        public void Initialize_WithoutArt_HasNoHover()
        {
            view.Initialize(canvas, StartButtonKind.NewGame, () => { }, () => { }, null);
            var panel = canvas.transform.Find("StartScreenPanel");
            Assert.IsNull(panel.Find("StartButton").GetComponent<StartButtonHover>());
            Assert.IsNull(panel.Find("SettingsButton").GetComponent<StartButtonHover>());
        }

        // 4:3(480), 기준 640×360, 19.5:9(780), 20:9(800), 21:9(840) 화면에서 배경이 빈틈없이 덮고,
        // 제목과 두 버튼이 화면 안에 있으며 버튼이 제목·제자와 겹치지 않는다.
        [UnityTest]
        public IEnumerator Initialize_WithArt_KeepsTitleAndButtonsVisible([Values(480, 640, 780, 800, 840)] int width)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(width, 360);
            view.Initialize(canvas, StartButtonKind.Continue, () => { }, () => { });
            yield return null;
            Canvas.ForceUpdateCanvases();

            var panel = canvas.transform.Find("StartScreenPanel");
            var screen = CanvasRect(panel);
            Assert.AreEqual(new Vector2(width, 360), screen.size);
            var background = CanvasRect(panel.Find("Background"));
            Assert.That(background.xMin, Is.LessThanOrEqualTo(screen.xMin + 0.5f));
            Assert.That(background.xMax, Is.GreaterThanOrEqualTo(screen.xMax - 0.5f));
            Assert.That(background.yMin, Is.LessThanOrEqualTo(screen.yMin + 0.5f));
            Assert.That(background.yMax, Is.GreaterThanOrEqualTo(screen.yMax - 0.5f));
            Assert.AreEqual(1672f / 941f, background.width / background.height, 0.01f);

            var title = InBackground(background, TitleArea);
            var disciple = InBackground(background, DiscipleArea);
            AssertInside(screen, title, "title");
            foreach (var name in new[] { "StartButton", "SettingsButton" })
            {
                var button = CanvasRect(panel.Find(name));
                AssertInside(screen, button, name);
                Assert.IsFalse(button.Overlaps(title), name + " overlaps title");
                Assert.IsFalse(button.Overlaps(disciple), name + " overlaps disciple");
            }
        }

        [Test]
        public void ArtLoad_FindsThreePetalSprites()
        {
            var art = StartScreenArt.Load();
            Assert.That(art.Petals.Count, Is.EqualTo(3));
            Assert.That(art.Petals.Select(p => p.name), Is.EqualTo(new[] { "petal_open", "petal_curled", "petal_folded" }));
            Assert.That(art.Petals.Select(p => p.rect.size), Is.EqualTo(new[] { new Vector2(64, 62), new Vector2(64, 29), new Vector2(64, 59) }));
        }

        [Test]
        public void Initialize_WithArt_PutsPetalsBetweenBackgroundAndButtons()
        {
            view.Initialize(canvas, StartButtonKind.NewGame, () => { }, () => { });
            var panel = canvas.transform.Find("StartScreenPanel");
            Assert.That(Enumerable.Range(0, panel.childCount).Select(i => panel.GetChild(i).name),
                Is.EqualTo(new[] { "Background", "Petals", "StartButton", "SettingsButton" }));
            var petals = (RectTransform)panel.Find("Petals");
            Assert.AreEqual(Vector2.zero, petals.anchorMin);
            Assert.AreEqual(Vector2.one, petals.anchorMax);
            Assert.That(petals.childCount, Is.EqualTo(PetalField.DefaultCount));
            foreach (Transform petal in petals)
            {
                var image = petal.GetComponent<Image>();
                Assert.IsFalse(image.raycastTarget);
                Assert.That(image.sprite.name, Does.StartWith("petal_"));
            }
        }

        [Test]
        public void Initialize_WithArtWithoutPetals_HasNoPetals()
        {
            var loaded = StartScreenArt.Load();
            var art = new StartScreenArt(loaded.Background, loaded.StartButton, loaded.ContinueButton, loaded.SettingsButton);
            view.Initialize(canvas, StartButtonKind.NewGame, () => { }, () => { }, art);
            Assert.IsNull(canvas.transform.Find("StartScreenPanel/Petals"));
            Assert.IsNotNull(canvas.transform.Find("StartScreenPanel/StartButton"));
        }

        [UnityTest]
        public IEnumerator Petals_MoveEachFrameAndFollowField()
        {
            view.Initialize(canvas, StartButtonKind.NewGame, () => { }, () => { });
            var petals = canvas.transform.Find("StartScreenPanel/Petals");
            var field = petals.GetComponent<StartScreenPetals>().Field;
            var before = Enumerable.Range(0, petals.childCount).Select(i => ((RectTransform)petals.GetChild(i)).anchoredPosition).ToArray();
            for (int i = 0; i < 5; i++) yield return null;
            Assert.That(field.Elapsed, Is.GreaterThan(0));
            int moved = 0;
            for (int i = 0; i < petals.childCount; i++)
            {
                var rect = (RectTransform)petals.GetChild(i);
                var petal = field.Petals[i];
                Assert.AreEqual(!petal.IsWaiting, rect.GetComponent<Image>().enabled);
                if (petal.IsWaiting) { moved++; continue; }
                Assert.AreEqual((float)petal.X, rect.anchoredPosition.x, 1e-3f);
                Assert.AreEqual((float)petal.Y, rect.anchoredPosition.y, 1e-3f);
                Assert.AreEqual((float)petal.FlipScale, rect.localScale.x, 1e-4f);
                Assert.AreEqual((float)petal.Alpha, rect.GetComponent<Image>().color.a, 1e-4f);
                Assert.AreEqual((float)petal.Size, Mathf.Max(rect.sizeDelta.x, rect.sizeDelta.y), 1e-3f);
                if (rect.anchoredPosition != before[i]) moved++;
            }
            // 기다리는 꽃잎을 빼고 모두 움직였다 (방금 다시 나타나 기다리기 시작한 꽃잎은 위치가 바뀌었다).
            Assert.That(moved, Is.EqualTo(petals.childCount));
        }

        // GameRoot 처럼 CanvasScaler 를 붙인 그 프레임에 시작 화면을 만들면, 처음에는 캔버스가 화면 픽셀 크기였다가
        // 다음 프레임에 640×360 기준으로 줄어든다. 그래도 꽃잎이 한꺼번에 위에서 몰려오지 않고 화면 안에 고르게 남아야 한다.
        [UnityTest]
        public IEnumerator Petals_CanvasScaledAfterInitialize_StayEvenlySpread()
        {
            var scaled = new GameObject("ScaledCanvas", typeof(Canvas));
            try
            {
                var scaledCanvas = scaled.GetComponent<Canvas>();
                scaledCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = scaled.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(640, 360);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 1f;
                view.Initialize(scaledCanvas, StartButtonKind.NewGame, () => { }, () => { });
                var petals = scaledCanvas.transform.Find("StartScreenPanel/Petals");
                var field = petals.GetComponent<StartScreenPetals>().Field;
                yield return null;
                yield return null;
                yield return null;

                var area = ((RectTransform)petals).rect;
                Assert.AreEqual(360f, area.height, 1f, "canvas scaled to 360 high");
                Assert.AreEqual(area.width, (float)field.Width, 0.5f);
                Assert.AreEqual(area.height, (float)field.Height, 0.5f);
                var visible = field.Petals.Where(p => Math.Abs(p.X) <= field.Width / 2 && Math.Abs(p.Y) <= field.Height / 2).ToArray();
                int active = field.Petals.Count(p => !p.IsWaiting);
                Assert.That(visible.Length, Is.GreaterThanOrEqualTo(active * 8 / 10), "most moving petals on screen right away");
                Assert.That(visible.Length, Is.GreaterThanOrEqualTo(20));
                Assert.That(visible.Count(p => p.Y < 0), Is.GreaterThanOrEqualTo(visible.Length / 4), "lower half has petals too");
                Assert.That(visible.Count(p => p.Y > 0), Is.GreaterThanOrEqualTo(visible.Length / 4), "upper half has petals too");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(scaled);
            }
        }

        // 캔버스 좌표(가운데 원점, y 위쪽)의 사각형.
        private Rect CanvasRect(Transform target)
        {
            var corners = new Vector3[4];
            ((RectTransform)target).GetWorldCorners(corners);
            var min = canvas.transform.InverseTransformPoint(corners[0]);
            var max = canvas.transform.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        // 그림 왼쪽 위 기준 비율 영역을 배경 사각형 안의 캔버스 좌표로 바꾼다.
        private static Rect InBackground(Rect background, Rect area) => Rect.MinMaxRect(
            background.xMin + area.xMin * background.width,
            background.yMax - area.yMax * background.height,
            background.xMin + area.xMax * background.width,
            background.yMax - area.yMin * background.height);

        private static void AssertInside(Rect screen, Rect target, string what)
        {
            Assert.That(target.xMin, Is.GreaterThanOrEqualTo(screen.xMin), what + " left");
            Assert.That(target.xMax, Is.LessThanOrEqualTo(screen.xMax), what + " right");
            Assert.That(target.yMin, Is.GreaterThanOrEqualTo(screen.yMin), what + " bottom");
            Assert.That(target.yMax, Is.LessThanOrEqualTo(screen.yMax), what + " top");
        }

        private static void AssertButton(Transform button, Vector2 position, Vector2 size, Color color)
        {
            AssertRect(button, position, size);
            Assert.AreEqual(color, button.GetComponent<Image>().color);
            Assert.AreEqual(Selectable.Transition.None, button.GetComponent<Button>().transition);
        }

        private static void AssertRect(Transform transform, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)transform;
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.anchorMin);
            Assert.AreEqual(rect.anchorMin, rect.anchorMax);
            Assert.AreEqual(rect.anchorMin, rect.pivot);
            Assert.AreEqual(position, rect.anchoredPosition);
            Assert.AreEqual(size, rect.sizeDelta);
        }

        [Test]
        public void SettingsClickedTwice_InvokesCallbackTwice()
        {
            int calls = 0;
            view.Initialize(canvas, StartButtonKind.Continue, () => { }, () => calls++);
            var button = canvas.transform.Find("StartScreenPanel/SettingsButton").GetComponent<Button>();
            button.onClick.Invoke();
            button.onClick.Invoke();
            Assert.AreEqual(2, calls);
        }

        [UnityTest]
        public IEnumerator StartClickedTwice_InvokesOnceAndDestroysObjects()
        {
            int calls = 0;
            view.Initialize(canvas, StartButtonKind.Continue, () => calls++, () => { });
            var panel = canvas.transform.Find("StartScreenPanel").gameObject;
            var button = panel.transform.Find("StartButton").GetComponent<Button>();
            button.onClick.Invoke();
            button.onClick.Invoke();
            Assert.AreEqual(1, calls);
            yield return null;
            Assert.IsTrue(panel == null);
            Assert.IsTrue(viewObject == null);
        }

        [UnityTest]
        public IEnumerator DestroyView_DestroysPanel()
        {
            view.Initialize(canvas, StartButtonKind.Continue, () => { }, () => { });
            var panel = canvas.transform.Find("StartScreenPanel").gameObject;
            UnityEngine.Object.Destroy(viewObject);
            yield return null;
            yield return null;
            Assert.IsTrue(panel == null);
        }

        private static string[] Texts(Transform parent)
        {
            var textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro", true);
            var property = textType.GetProperty("text");
            return parent.GetComponentsInChildren(textType).Select(component => (string)property.GetValue(component)).ToArray();
        }
    }
}
