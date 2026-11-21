using System;
using System.Collections.Generic;
using PlumSword.Core.StartMenu;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Combat
{
    /// <summary>4스테이지 전투 화면에 바람에 실린 재를 표시한다.</summary>
    public sealed class CombatAsh : MonoBehaviour
    {
        private const int Count = 20;
        // 원본의 긴 투명 여백을 제외한 재 조각 주변. 좌표는 Unity 텍스처의 왼쪽 아래 기준.
        private static readonly Rect[] CropRects =
        {
            new Rect(670, 388, 220, 230), new Rect(600, 303, 330, 420),
            new Rect(645, 385, 250, 235), new Rect(485, 330, 530, 380),
            new Rect(660, 390, 215, 240)
        };

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
            for (int i = 0; i < CropRects.Length; i++)
            {
                var texture = Resources.Load<Texture2D>("Art/Effects/Stage4Ash/ash_0" + (i + 1));
                if (texture == null || texture.width < CropRects[i].xMax || texture.height < CropRects[i].yMax) continue;
                var sprite = Sprite.Create(texture, CropRects[i], new Vector2(0.5f, 0.5f), 32f);
                sprite.name = "stage4_ash_0" + (i + 1);
                sprites.Add(sprite);
            }
            if (sprites.Count == 0) return;
            var size = AreaSize();
            field = new PetalField(size.x, size.y, Count, sprites.Count, new System.Random());
            for (int i = 0; i < Count; i++)
            {
                var obj = new GameObject("Ash", typeof(RectTransform), typeof(Image));
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
            bool visible = root.StageProgress != null && root.StageProgress.DisplayStageNumber == 4;
            for (int i = 0; i < images.Count; i++)
            {
                var particle = field.Petals[i];
                var image = images[i];
                image.enabled = visible && !particle.IsWaiting;
                if (!image.enabled) continue;
                var sprite = sprites[particle.Variant];
                image.sprite = sprite;
                var rect = rects[i];
                var spriteSize = sprite.rect.size;
                rect.sizeDelta = spriteSize * ((float)particle.Size * 0.55f / Mathf.Max(spriteSize.x, spriteSize.y));
                rect.anchoredPosition = new Vector2((float)particle.X, (float)particle.Y);
                rect.localRotation = Quaternion.Euler(0f, 0f, (float)particle.Rotation);
                rect.localScale = new Vector3((float)particle.FlipScale, 1f, 1f);
                image.color = new Color(1f, 1f, 1f, (float)particle.Alpha * 0.75f);
            }
        }

        private void OnDestroy()
        {
            foreach (var sprite in sprites) if (sprite != null) Destroy(sprite);
        }
    }
}
