using System;
using PlumSword.Core.Currency;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Save
{
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public SaveData(long gold, int attackLevel, int healthLevel, int attackSpeedLevel,
            int stageNumber, int seenIntroStage, long savedAtUtcTicks)
        {
            Gold = gold;
            AttackLevel = attackLevel;
            HealthLevel = healthLevel;
            AttackSpeedLevel = attackSpeedLevel;
            StageNumber = stageNumber;
            SeenIntroStage = seenIntroStage;
            SavedAtUtcTicks = savedAtUtcTicks;
        }

        public long Gold { get; }
        public int AttackLevel { get; }
        public int HealthLevel { get; }
        public int AttackSpeedLevel { get; }
        public int StageNumber { get; }
        public int SeenIntroStage { get; }
        public long SavedAtUtcTicks { get; }

        public static SaveData Capture(Wallet wallet, StatUpgrades upgrades, StageProgress stage, long savedAtUtcTicks)
        {
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            if (upgrades == null) throw new ArgumentNullException(nameof(upgrades));
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            return new SaveData(wallet.Balance, upgrades.GetLevel(StatType.Attack),
                upgrades.GetLevel(StatType.Health), upgrades.GetLevel(StatType.AttackSpeed),
                stage.StageNumber, stage.SeenIntroStage, savedAtUtcTicks);
        }
    }
}
