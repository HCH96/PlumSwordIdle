using System;
using PlumSword.Core.Combat;
using UnityEngine;

namespace PlumSword.Game.Combat
{
    /// <summary>
    /// 제자 스프라이트 시트 세 장(대기·달리기·공격)을 Resources 에서 읽어 프레임마다 Sprite 로 자른다.
    /// 시트는 가로 한 줄이고, 칸 크기와 발 기준점(피벗)은 동작마다 다르다. 피벗을 발 아래에 맞춰 두었으므로
    /// 동작이 바뀌어도 발 위치와 화면상 크기가 그대로다. 시트는 PPU 32로 자르고 화면에서 표시 크기를 정한다.
    /// 하나라도 없거나 크기가 예상과 다르면 TryLoad 가 null 을 돌려주고, CombatView 는 단색 사각형 대체 표시를 쓴다.
    /// 사양: docs/spec/combat.md "화면", 에셋: docs/dev/assets.md "캐릭터".
    /// </summary>
    internal sealed class DiscipleSprites : IDisposable
    {
        public const string IdlePath = "Art/Characters/disciple_idle";
        public const string RunPath = "Art/Characters/disciple_run";
        public const string AttackPath = "Art/Characters/disciple_attack";
        public const string DownPath = "Art/Characters/disciple_down";
        public const string SkillPath = "Art/Characters/disciple_skill";
        public const int SkillFrameCount = 32;
        public const float SkillFramesPerSecond = 24f;
        public const int DownFrameCount = 17;
        public const float DownFrameSeconds = 0.075f;
        public const float DownDuration = DownFrameCount * DownFrameSeconds;

        /// <summary>docs/dev/art.md "기본 숫자". 모든 스프라이트가 같은 값을 쓴다.</summary>
        public const float PixelsPerUnit = 32f;

        /// <summary>발에서 머리(묶은 머리 끝) 위까지의 높이. 대기 그림의 53px. 체력 바 높이를 정한다.</summary>
        public const float HeadHeight = 53f / PixelsPerUnit;

        // 칸 크기와 피벗(칸 왼쪽 아래에서 잰 px). 원본은 64×64 기본 그림에 여백을 더한 칸이라
        // 발은 가로 38px, 대기·공격은 아래에서 12px, 달리기는 아래에서 14px 높이에 선다.
        private static readonly Sheet IdleSheet = new Sheet(IdlePath, DiscipleAnimator.IdleFrameCount, 76, 76, 38, 12);
        private static readonly Sheet RunSheet = new Sheet(RunPath, DiscipleAnimator.RunFrameCount, 76, 80, 38, 14);
        private static readonly Sheet AttackSheet = new Sheet(AttackPath, DiscipleAnimator.AttackFrameCount, 76, 76, 38, 12);
        private static readonly Sheet DownSheet = new Sheet(DownPath, DownFrameCount, 76, 76, 38, 12);

        private readonly Sprite[] _idle;
        private readonly Sprite[] _run;
        private readonly Sprite[] _attack;
        private readonly Sprite[] _down;
        private readonly Sprite[] _skill;

        private DiscipleSprites(Sprite[] idle, Sprite[] run, Sprite[] attack, Sprite[] down, Sprite[] skill)
        {
            _idle = idle;
            _run = run;
            _attack = attack;
            _down = down;
            _skill = skill;
        }

        /// <summary>세 시트를 모두 읽으면 돌려주고, 하나라도 실패하면 경고를 남기고 null.</summary>
        public static DiscipleSprites TryLoad()
        {
            Sprite[] idle = Slice(IdleSheet);
            Sprite[] run = idle == null ? null : Slice(RunSheet);
            Sprite[] attack = run == null ? null : Slice(AttackSheet);
            if (attack != null)
            {
                // 쓰러짐 에셋이 없어도 기존 전투 스프라이트는 계속 표시한다.
                return new DiscipleSprites(idle, run, attack, Slice(DownSheet), SliceSkill());
            }

            DestroyAll(idle);
            DestroyAll(run);
            return null;
        }

        public Sprite Get(DiscipleMotion motion, int frame)
        {
            Sprite[] frames;
            switch (motion)
            {
                case DiscipleMotion.Run: frames = _run; break;
                case DiscipleMotion.Attack: frames = _attack; break;
                default: frames = _idle; break;
            }

            return frames[Mathf.Clamp(frame, 0, frames.Length - 1)];
        }

        public Sprite GetDown(int frame) => _down == null ? Get(DiscipleMotion.Idle, 0)
            : _down[Mathf.Clamp(frame, 0, _down.Length - 1)];

        public bool HasSkill => _skill != null;
        public Sprite GetSkill(int frame) => _skill[Mathf.Clamp(frame, 0, _skill.Length - 1)];

        private static Sprite[] SliceSkill()
        {
            var texture = Resources.Load<Texture2D>(SkillPath);
            const int width = 99, height = 105, columns = 8;
            if (texture == null || texture.width != width * columns || texture.height != height * 4)
                return null;

            var frames = new Sprite[SkillFrameCount];
            for (int i = 0; i < frames.Length; i++)
            {
                var rect = new Rect((i % columns) * width, (3 - i / columns) * height, width, height);
                frames[i] = Sprite.Create(texture, rect, new Vector2(58f / width, 18f / height), PixelsPerUnit, 0, SpriteMeshType.FullRect);
                frames[i].name = $"{texture.name}_{i:00}";
            }
            return frames;
        }

        public void Dispose()
        {
            DestroyAll(_idle);
            DestroyAll(_run);
            DestroyAll(_attack);
            DestroyAll(_down);
            DestroyAll(_skill);
        }

        private static Sprite[] Slice(Sheet sheet)
        {
            var texture = Resources.Load<Texture2D>(sheet.Path);
            if (texture == null)
            {
                Debug.LogWarning($"제자 스프라이트 '{sheet.Path}' 를 찾지 못해 대체 표시를 쓴다.");
                return null;
            }

            if (texture.width != sheet.FrameWidth * sheet.FrameCount || texture.height != sheet.FrameHeight)
            {
                Debug.LogWarning($"제자 스프라이트 '{sheet.Path}' 크기가 {texture.width}×{texture.height} 이다. " +
                                 $"{sheet.FrameWidth * sheet.FrameCount}×{sheet.FrameHeight} 이어야 해서 대체 표시를 쓴다.");
                return null;
            }

            var pivot = new Vector2(sheet.PivotX / (float)sheet.FrameWidth, sheet.PivotY / (float)sheet.FrameHeight);
            var frames = new Sprite[sheet.FrameCount];
            for (int i = 0; i < frames.Length; i++)
            {
                var rect = new Rect(i * sheet.FrameWidth, 0f, sheet.FrameWidth, sheet.FrameHeight);
                frames[i] = Sprite.Create(texture, rect, pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
                frames[i].name = $"{texture.name}_{i:00}";
            }

            return frames;
        }

        private static void DestroyAll(Sprite[] sprites)
        {
            if (sprites == null) return;
            foreach (var sprite in sprites)
            {
                if (sprite != null) UnityEngine.Object.Destroy(sprite);
            }
        }

        private readonly struct Sheet
        {
            public readonly string Path;
            public readonly int FrameCount;
            public readonly int FrameWidth;
            public readonly int FrameHeight;
            public readonly int PivotX;
            public readonly int PivotY;

            public Sheet(string path, int frameCount, int frameWidth, int frameHeight, int pivotX, int pivotY)
            {
                Path = path;
                FrameCount = frameCount;
                FrameWidth = frameWidth;
                FrameHeight = frameHeight;
                PivotX = pivotX;
                PivotY = pivotY;
            }
        }
    }
}
