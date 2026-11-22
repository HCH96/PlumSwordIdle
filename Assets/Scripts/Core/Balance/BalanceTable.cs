using System;

namespace PlumSword.Core.Balance
{
    public sealed class BalanceTable
    {
        private static BalanceTable current = Default;

        public static BalanceTable Default => new BalanceTable
        {
            AttackCost = new GrowthCurve(20, 5, 1), HealthCost = new GrowthCurve(20, 5, 1), InnerPowerCost = new GrowthCurve(20, 5, 1),
            AttackPerLevel = 2, HealthPerLevel = 20, DiscipleBaseAttack = 10, DiscipleBaseMaxHealth = 100,
            DiscipleAttackInterval = 1, SkillBasePercent = 200, SkillPercentPerInnerLevel = 10, SkillCooldownSeconds = 5, SubstageStep = 0,
            NormalHealth = new GrowthCurve(30, 10, 1), NormalAttack = new GrowthCurve(5, 2, 1),
            BossHealth = new GrowthCurve(100, 30, 1), BossAttack = new GrowthCurve(10, 4, 1),
            NormalAttackInterval = 2, BossAttackInterval = 2, FinalBossHealth = 0, FinalBossAttack = 0,
            NormalGold = new GrowthCurve(10, 2, 1), BossGold = new GrowthCurve(50, 10, 1),
            RealmKillsRequired = new[] { 30, 50, 70, 100 }, RealmBonusPercent = new[] { 0, 10, 20, 30, 40 }
        };

        public static BalanceTable Current
        {
            get => current;
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                value.Validate();
                current = value;
            }
        }

        public GrowthCurve AttackCost { get; set; }
        public GrowthCurve HealthCost { get; set; }
        public GrowthCurve InnerPowerCost { get; set; }
        public long AttackPerLevel { get; set; }
        public long HealthPerLevel { get; set; }
        public long DiscipleBaseAttack { get; set; }
        public long DiscipleBaseMaxHealth { get; set; }
        public double DiscipleAttackInterval { get; set; }
        public int SkillBasePercent { get; set; }
        public int SkillPercentPerInnerLevel { get; set; }
        public double SkillCooldownSeconds { get; set; }
        public double SubstageStep { get; set; }
        public GrowthCurve NormalHealth { get; set; }
        public GrowthCurve NormalAttack { get; set; }
        public GrowthCurve BossHealth { get; set; }
        public GrowthCurve BossAttack { get; set; }
        public double NormalAttackInterval { get; set; }
        public double BossAttackInterval { get; set; }
        public long FinalBossHealth { get; set; }
        public long FinalBossAttack { get; set; }
        public GrowthCurve NormalGold { get; set; }
        public GrowthCurve BossGold { get; set; }
        public int[] RealmKillsRequired { get; set; }
        public int[] RealmBonusPercent { get; set; }

        public void Validate()
        {
            Check(AttackCost != null, nameof(AttackCost)); Check(HealthCost != null, nameof(HealthCost));
            Check(InnerPowerCost != null, nameof(InnerPowerCost)); Check(NormalHealth != null, nameof(NormalHealth));
            Check(NormalAttack != null, nameof(NormalAttack)); Check(BossHealth != null, nameof(BossHealth));
            Check(BossAttack != null, nameof(BossAttack)); Check(NormalGold != null, nameof(NormalGold)); Check(BossGold != null, nameof(BossGold));
            Check(AttackPerLevel >= 0, nameof(AttackPerLevel)); Check(HealthPerLevel >= 0, nameof(HealthPerLevel));
            Check(DiscipleBaseAttack >= 1, nameof(DiscipleBaseAttack)); Check(DiscipleBaseMaxHealth >= 1, nameof(DiscipleBaseMaxHealth));
            Check(PositiveFinite(DiscipleAttackInterval), nameof(DiscipleAttackInterval));
            Check(SkillBasePercent >= 0, nameof(SkillBasePercent)); Check(SkillPercentPerInnerLevel >= 0, nameof(SkillPercentPerInnerLevel));
            Check(PositiveFinite(SkillCooldownSeconds), nameof(SkillCooldownSeconds));
            Check(!double.IsNaN(SubstageStep) && !double.IsInfinity(SubstageStep) && SubstageStep >= 0, nameof(SubstageStep));
            Check(PositiveFinite(NormalAttackInterval), nameof(NormalAttackInterval)); Check(PositiveFinite(BossAttackInterval), nameof(BossAttackInterval));
            Check(FinalBossHealth >= 0, nameof(FinalBossHealth)); Check(FinalBossAttack >= 0, nameof(FinalBossAttack));
            ValidateArray(RealmKillsRequired, 4, 1, nameof(RealmKillsRequired));
            ValidateArray(RealmBonusPercent, 5, 0, nameof(RealmBonusPercent));
        }

        public double SubstageIndex(int stage, int substage)
        {
            if (stage < 1) throw new ArgumentOutOfRangeException(nameof(stage));
            if (substage < 1 || substage > 7) throw new ArgumentOutOfRangeException(nameof(substage));
            return (stage - 1) + SubstageStep * (substage - 1);
        }

        private static bool PositiveFinite(double value) => value > 0 && !double.IsInfinity(value) && !double.IsNaN(value);
        private static void Check(bool valid, string name) { if (!valid) throw new ArgumentOutOfRangeException(name); }
        private static void ValidateArray(int[] values, int length, int minimum, string name)
        {
            Check(values != null && values.Length == length, name);
            foreach (int value in values) Check(value >= minimum, name);
        }
    }
}
