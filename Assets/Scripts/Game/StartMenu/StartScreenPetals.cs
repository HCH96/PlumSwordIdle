using System;
using System.Collections.Generic;
using PlumSword.Core.StartMenu;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.StartMenu
{
    /// <summary>
    /// 시작 화면의 매화 꽃잎 흩날림을 그린다. 움직임은 Core 의 PetalField 가 계산하고, 여기서는 꽃잎 Image 를 맞추기만 한다.
    /// 영역(자기 RectTransform) 크기가 바뀌면 PetalField.Resize 를 부른다. 시간은 Time.unscaledDeltaTime 을 쓴다.
    /// </summary>
    public sealed class StartScreenPetals : MonoBehaviour
    {
        private readonly List<RectTransform> images = new List<RectTransform>();
        private readonly List<Image> graphics = new List<Image>();
        private IReadOnlyList<Sprite> sprites;
        private RectTransform area;

        public PetalField Field { get; private set; }

        // sprites 가 null 이거나 비었으면 ArgumentException. count 장의 꽃잎 Image 를 자기 자식으로 만든다.
        public void Initialize(IReadOnlyList<Sprite> sprites, int count, int seed)
        {
            if (sprites == null || sprites.Count == 0) throw new ArgumentException("petal sprites required", nameof(sprites));
            this.sprites = sprites;
            area = (RectTransform)transform;
            var size = AreaSize();
            Field = new PetalField(size.x, size.y, count, sprites.Count, new System.Random(seed));
            for (int i = 0; i < count; i++)
            {
                var obj = new GameObject("Petal", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)obj.transform;
                rect.SetParent(area, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                var image = obj.GetComponent<Image>();
                image.raycastTarget = false;
                images.Add(rect);
                graphics.Add(image);
            }
            Apply();
        }

        private void Update()
        {
            if (Field == null) return;
            var size = AreaSize();
            if (!Mathf.Approximately((float)Field.Width, size.x) || !Mathf.Approximately((float)Field.Height, size.y))
                Field.Resize(size.x, size.y);
            Field.Tick(Time.unscaledDeltaTime);
            Apply();
        }

        // 영역이 아직 계산되지 않았으면(0) 기준 해상도 640×360 으로 둔다.
        private Vector2 AreaSize()
        {
            var size = area.rect.size;
            return new Vector2(size.x > 0 ? size.x : 640, size.y > 0 ? size.y : 360);
        }

        private void Apply()
        {
            for (int i = 0; i < images.Count; i++)
            {
                var petal = Field.Petals[i];
                var image = graphics[i];
                var sprite = sprites[petal.Variant];
                if (image.sprite != sprite) image.sprite = sprite;
                image.enabled = !petal.IsWaiting;
                if (petal.IsWaiting) continue;
                var rect = images[i];
                float longSide = (float)petal.Size;
                var spriteSize = sprite.rect.size;
                float scale = longSide / Mathf.Max(spriteSize.x, spriteSize.y);
                rect.sizeDelta = spriteSize * scale;
                rect.anchoredPosition = new Vector2((float)petal.X, (float)petal.Y);
                rect.localRotation = Quaternion.Euler(0, 0, (float)petal.Rotation);
                rect.localScale = new Vector3((float)petal.FlipScale, 1, 1);
                image.color = new Color(1, 1, 1, (float)petal.Alpha);
            }
        }
    }
}
