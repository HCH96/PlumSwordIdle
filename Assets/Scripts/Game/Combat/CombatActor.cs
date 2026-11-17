using UnityEngine;

namespace PlumSword.Game.Combat
{
    /// <summary>
    /// 전투 필드에 그리는 캐릭터 하나(제자, 일반 몬스터, 보스)의 오브젝트 묶음.
    /// Root 는 크기가 없는 빈 오브젝트로 발 아래 가운데에 놓이고, 몸 사각형과 체력 바가 그 형제 자식이다.
    /// 부모의 localScale 이 체력 바 크기를 틀어지게 하지 않도록 크기는 몸 사각형에만 준다.
    /// </summary>
    internal sealed class CombatActor
    {
        public GameObject Root;
        public SpriteRenderer Body;
        public Color BodyColor;

        /// <summary>피격 번쩍임 동안의 몸 색. 단색 사각형은 흰색, 스프라이트는 곱하는 색이라 붉은 기운으로 둔다.</summary>
        public Color FlashColor = Color.white;

        /// <summary>체력 바 바탕. 체력 바가 없는 캐릭터(보스)는 null.</summary>
        public Transform HealthBack;

        /// <summary>체력 바 채움. 체력 바가 없는 캐릭터(보스)는 null.</summary>
        public Transform HealthFill;

        /// <summary>체력 바 세로 가운데의 높이(발 기준, 유닛).</summary>
        public float HealthBarY;

        /// <summary>피격 번쩍임이 남은 시간(초). 0 이하면 번쩍이지 않는다.</summary>
        public float FlashRemaining;

        public JongnamSprites Jongnam;
        public double PreviousPosition;
        public double PreviousCooldown;
        public float WalkGrace;
        public float AnimationTime;
    }
}
