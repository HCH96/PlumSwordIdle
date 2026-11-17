using System;
using UnityEngine;

namespace PlumSword.Game.Combat
{
    /// <summary>선택한 종남 제자 시트를 원본 픽셀 크기로 잘라 공유한다.</summary>
    internal sealed class JongnamSprites : IDisposable
    {
        public readonly Sprite[] Idle;
        public readonly Sprite[] Walk;
        public readonly Sprite[] Attack;

        private JongnamSprites(Sprite[] idle, Sprite[] walk, Sprite[] attack)
        {
            Idle = idle;
            Walk = walk;
            Attack = attack;
        }

        public static JongnamSprites TryLoad(bool female)
        {
            string prefix = "Art/Enemies/jongnam_" + (female ? "female_" : "male_");
            var idle = Slice(prefix + "idle", female ? 76 : 68, 76, female ? 9 : 16, female ? 12 : 11);
            var walk = Slice(prefix + "walk", 72, 84, 24, female ? 16 : 15);
            var attack = Slice(prefix + "attack", female ? 96 : 92, female ? 92 : 96, 24, female ? 20 : 21);
            if (idle != null && walk != null && attack != null) return new JongnamSprites(idle, walk, attack);
            DestroyFrames(idle); DestroyFrames(walk); DestroyFrames(attack);
            return null;
        }

        private static Sprite[] Slice(string path, int width, int height, int count, int bottom)
        {
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null || texture.width != width * count || texture.height != height) return null;
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = Sprite.Create(texture, new Rect(i * width, 0, width, height),
                    new Vector2(0.5f, (float)bottom / height), 32f, 0, SpriteMeshType.FullRect);
                frames[i].name = texture.name + "_" + i.ToString("00");
            }
            return frames;
        }

        private static void DestroyFrames(Sprite[] frames)
        {
            if (frames == null) return;
            foreach (var frame in frames) if (frame != null) UnityEngine.Object.Destroy(frame);
        }

        public void Dispose()
        {
            DestroyFrames(Idle); DestroyFrames(Walk); DestroyFrames(Attack);
        }
    }
}
