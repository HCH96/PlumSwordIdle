using System;
using UnityEngine;

namespace PlumSword.Game.Combat
{
    /// <summary>상대 애니메이션 시트를 원본 픽셀 크기로 잘라 공유한다.</summary>
    internal sealed class JongnamSprites : IDisposable
    {
        public readonly Sprite[] Idle;
        public readonly Sprite[] Walk;
        public readonly Sprite[] Attack;
        public readonly Sprite[] Skill;
        public readonly Sprite[] Down;
        public readonly int AttackHitFrame;
        public readonly int SkillHitFrame;
        public readonly float WalkFramesPerSecond;
        public const int DownFrameCount = 17;
        public const float DownFrameSeconds = 0.075f;
        public const float DownDuration = DownFrameCount * DownFrameSeconds;
        private static string TianmoFinalBossPrefix = "Art/Enemies/tianmo_final_boss_";
        private static string TianmoFinalBossSkillPath = "Art/Enemies/tianmo_final_boss_skill";

        private JongnamSprites(Sprite[] idle, Sprite[] walk, Sprite[] attack, Sprite[] skill = null, Sprite[] down = null, int attackHitFrame = 10, float walkFramesPerSecond = 12f, int skillHitFrame = 14)
        {
            Idle = idle;
            Walk = walk;
            Attack = attack;
            Skill = skill;
            Down = down;
            AttackHitFrame = attackHitFrame;
            SkillHitFrame = skillHitFrame;
            WalkFramesPerSecond = walkFramesPerSecond;
        }

        public static JongnamSprites TryLoadTianmoFinalBoss()
        {
            var idle = Slice(TianmoFinalBossPrefix + "idle", 104, 104, 25, 16);
            var walk = Slice(TianmoFinalBossPrefix + "walk", 104, 104, 25, 16);
            var attack = Slice(TianmoFinalBossPrefix + "attack", 104, 104, 25, 16);
            var down = Slice(TianmoFinalBossPrefix + "down", 104, 104, 25, 16);
            var skill = SliceGrid(TianmoFinalBossSkillPath, 260, 80, 5, 5, 222, 5);
            if (idle != null && walk != null && attack != null && down != null)
                return new JongnamSprites(idle, walk, attack, skill, down, attackHitFrame: 11, walkFramesPerSecond: 10f, skillHitFrame: 15);
            DestroyFrames(idle); DestroyFrames(walk); DestroyFrames(attack); DestroyFrames(skill); DestroyFrames(down);
            return null;
        }

        public static JongnamSprites TryLoadTianmoBoss()
        {
            const string prefix = "Art/Enemies/tianmo_boss_";
            var idle = Slice(prefix + "idle", 104, 104, 17, 18);
            var walk = Slice(prefix + "walk", 104, 104, 25, 18);
            var attack = Slice(prefix + "attack", 104, 104, 25, 18);
            var down = Slice(prefix + "down", 104, 104, 17, 18);
            var skill = Slice(prefix + "skill", 104, 104, 37, 18);
            if (idle != null && walk != null && attack != null && down != null)
                return new JongnamSprites(idle, walk, attack, skill, down, attackHitFrame: 11, walkFramesPerSecond: 10f, skillHitFrame: 7);
            DestroyFrames(idle); DestroyFrames(walk); DestroyFrames(attack); DestroyFrames(skill); DestroyFrames(down);
            return null;
        }

        public static JongnamSprites TryLoadTianmo(bool guard)
        {
            string prefix = "Art/Enemies/tianmo_" + (guard ? "guard_" : "swordsman_");
            int bottom = guard ? 5 : 6;
            var idle = Slice(prefix + "idle", 64, 64, 17, bottom);
            var walk = Slice(prefix + "walk", 64, 64, 25, bottom);
            var attack = Slice(prefix + "attack", 64, 64, 25, bottom);
            var down = Slice(prefix + "down", 64, 64, 17, bottom);
            if (idle != null && walk != null && attack != null && down != null)
                return new JongnamSprites(idle, walk, attack, down: down, attackHitFrame: guard ? 11 : 9, walkFramesPerSecond: 10f);
            DestroyFrames(idle); DestroyFrames(walk); DestroyFrames(attack); DestroyFrames(down);
            return null;
        }

        public static JongnamSprites TryLoadNamgungBoss()
        {
            const string prefix = "Art/Enemies/namgung_boss_";
            var idle = Slice(prefix + "idle", 100, 100, 17, 15);
            var walk = Slice(prefix + "walk", 100, 100, 25, 15);
            var attack = Slice(prefix + "attack", 100, 100, 25, 15);
            var down = Slice(prefix + "down", 100, 100, 17, 15);
            var skill = SliceGrid(prefix + "skill", 246, 138, 5, 5, 209, 15);
            if (idle != null && walk != null && attack != null && down != null)
                return new JongnamSprites(idle, walk, attack, skill, down, attackHitFrame: 8, walkFramesPerSecond: 10f, skillHitFrame: 7);
            DestroyFrames(idle); DestroyFrames(walk); DestroyFrames(attack); DestroyFrames(skill); DestroyFrames(down);
            return null;
        }

        public static JongnamSprites TryLoadNamgung(bool female)
        {
            string prefix = "Art/Enemies/namgung_" + (female ? "female_" : "male_");
            int bottom = female ? 13 : 12;
            var idle = Slice(prefix + "idle", 76, 76, 17, bottom);
            var walk = Slice(prefix + "walk", 76, 76, 25, bottom);
            var attack = Slice(prefix + "attack", 76, 76, 25, bottom);
            var down = Slice(prefix + "down", 76, 76, 17, bottom);
            if (idle != null && walk != null && attack != null)
                return new JongnamSprites(idle, walk, attack, down: down, attackHitFrame: female ? 6 : 8, walkFramesPerSecond: 10f);
            DestroyFrames(idle); DestroyFrames(walk); DestroyFrames(attack); DestroyFrames(down);
            return null;
        }

        public static JongnamSprites TryLoadGaebang(bool unarmed)
        {
            string prefix = "Art/Enemies/gaebang_" + (unarmed ? "unarmed_" : "staff_");
            var idle = Slice(prefix + "idle", unarmed ? 64 : 96, unarmed ? 84 : 96, 17, unarmed ? 13 : 24);
            var walk = Slice(prefix + "walk", unarmed ? 84 : 96, unarmed ? 84 : 96, unarmed ? 24 : 25, unarmed ? 13 : 24);
            var attack = Slice(prefix + "attack", unarmed ? 84 : 68, unarmed ? 84 : 68, 24, unarmed ? 13 : 10);
            var down = Slice(prefix + "down", unarmed ? 104 : 96, unarmed ? 104 : 96, 16, unarmed ? 23 : 24);
            if (idle != null && walk != null && attack != null)
                return new JongnamSprites(idle, walk, attack, down: down, attackHitFrame: unarmed ? 5 : 16, walkFramesPerSecond: 10f);
            DestroyFrames(idle); DestroyFrames(walk); DestroyFrames(attack); DestroyFrames(down);
            return null;
        }

        public static JongnamSprites TryLoadBoss()
        {
            const string prefix = "Art/Enemies/jongnam_boss_";
            var idle = Slice(prefix + "idle", 96, 100, 24, 15);
            var walk = Slice(prefix + "walk", 96, 104, 24, 18);
            var attack = Slice(prefix + "attack", 100, 100, 24, 15);
            var skill = Slice(prefix + "skill", 100, 100, 24, 15);
            var down = Slice(prefix + "down", 100, 100, DownFrameCount, 15);
            if (idle != null && walk != null && attack != null && skill != null)
                return new JongnamSprites(idle, walk, attack, skill, down, attackHitFrame: 13);
            DestroyFrames(idle); DestroyFrames(walk); DestroyFrames(attack); DestroyFrames(skill); DestroyFrames(down);
            return null;
        }

        public static JongnamSprites TryLoadGaebangBoss()
        {
            const string prefix = "Art/Enemies/gaebang_boss_";
            var idle = Slice(prefix + "idle", 96, 96, 9, 16);
            var walk = Slice(prefix + "walk", 96, 96, 25, 16);
            var attack = Slice(prefix + "attack", 96, 96, 25, 16);
            var skill = Slice(prefix + "skill", 96, 96, 33, 16);
            var down = Slice(prefix + "down", 96, 96, 25, 16);
            if (idle != null && walk != null && attack != null && down != null)
                return new JongnamSprites(idle, walk, attack, skill, down, attackHitFrame: 8, walkFramesPerSecond: 10f, skillHitFrame: 19);
            DestroyFrames(idle); DestroyFrames(walk); DestroyFrames(attack); DestroyFrames(skill); DestroyFrames(down);
            return null;
        }

        public static JongnamSprites TryLoad(bool female)
        {
            string prefix = "Art/Enemies/jongnam_" + (female ? "female_" : "male_");
            var idle = Slice(prefix + "idle", female ? 76 : 68, 76, female ? 9 : 16, female ? 12 : 11);
            var walk = Slice(prefix + "walk", 72, 84, 24, female ? 16 : 15);
            var attack = Slice(prefix + "attack", female ? 96 : 92, female ? 92 : 96, 24, female ? 20 : 21);
            var down = Slice(prefix + "down", 76, 76, DownFrameCount, female ? 12 : 11);
            if (idle != null && walk != null && attack != null) return new JongnamSprites(idle, walk, attack, down: down);
            DestroyFrames(idle); DestroyFrames(walk); DestroyFrames(attack); DestroyFrames(down);
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

        private static Sprite[] SliceGrid(string path, int width, int height, int columns, int rows, int pivotX, int bottom)
        {
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null || texture.width != width * columns || texture.height != height * rows) return null;
            var frames = new Sprite[columns * rows];
            for (int i = 0; i < frames.Length; i++)
            {
                var rect = new Rect((i % columns) * width, (rows - 1 - i / columns) * height, width, height);
                frames[i] = Sprite.Create(texture, rect, new Vector2((float)pivotX / width, (float)bottom / height), 32f, 0, SpriteMeshType.FullRect);
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
            DestroyFrames(Idle); DestroyFrames(Walk); DestroyFrames(Attack); DestroyFrames(Skill); DestroyFrames(Down);
        }
    }
}
