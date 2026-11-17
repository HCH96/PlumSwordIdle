using System;
using System.Collections.Generic;
using PlumSword.Core.StartMenu;
using PlumSword.Game.StartMenu;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Combat
{
    /// <summary>1스테이지 전투 화면에 작고 드문 매화 꽃잎을 표시한다.</summary>
    public sealed class CombatPetals : MonoBehaviour
    {
        private const int Count = 20;
        private const float SizeScale = 0.6f;
        private readonly List<RectTransform> rects = new List<RectTransform>();
        private readonly List<Image> images = new List<Image>();
        private readonly List<Sprite> sprites = new List<Sprite>();
        private RectTransform area;
        private PetalField field;
        private GameRoot root;

        public void Initialize(GameRoot gameRoot)
        {
            root = gameRoot ?? throw new ArgumentNullException(nameof(gameRoot));
            area = (RectTransform)transform;
            foreach (string path in StartScreenArt.PetalPaths)
            {
                var sprite = Resources.Load<Sprite>(path);
                if (sprite != null) sprites.Add(sprite);
            }
            if (sprites.Count == 0) return;
            var size = AreaSize();
            field = new PetalField(size.x, size.y, Count, sprites.Count, new System.Random());
            for (int i = 0; i < Count; i++)
            {
                var obj = new GameObject("Petal", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)obj.transform;
                rect.SetParent(area, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                var image = obj.GetComponent<Image>();
                image.raycastTarget = false;
                rects.Add(rect);
                images.Add(image);
            }
            Apply();
        }

        private void Update()
        {
            if (field == null) return;
            var size = AreaSize();
            if (!Mathf.Approximately((float)field.Width, size.x) || !Mathf.Approximately((float)field.Height, size.y))
                field.Resize(size.x, size.y);
            field.Tick(Time.unscaledDeltaTime);
            Apply();
        }

        private Vector2 AreaSize()
        {
            var size = area.rect.size;
            return new Vector2(size.x > 0 ? size.x : 640f, size.y > 0 ? size.y : 360f);
        }

        private void Apply()
        {
            bool visible = root.StageProgress != null && root.StageProgress.DisplayStageNumber == 1;
            for (int i = 0; i < images.Count; i++)
            {
                var petal = field.Petals[i];
                var image = images[i];
                image.enabled = visible && !petal.IsWaiting;
                if (!image.enabled) continue;
                var sprite = sprites[petal.Variant];
                image.sprite = sprite;
                var rect = rects[i];
                var spriteSize = sprite.rect.size;
                rect.sizeDelta = spriteSize * ((float)petal.Size * SizeScale / Mathf.Max(spriteSize.x, spriteSize.y));
                rect.anchoredPosition = new Vector2((float)petal.X, (float)petal.Y);
                rect.localRotation = Quaternion.Euler(0f, 0f, (float)petal.Rotation);
                rect.localScale = new Vector3((float)petal.FlipScale, 1f, 1f);
                image.color = new Color(1f, 1f, 1f, (float)petal.Alpha * 0.8f);
            }
        }
    }
}
