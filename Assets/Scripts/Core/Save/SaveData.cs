using System;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.Core.Save
{
    public sealed class SaveData
    {
        public const int CurrentVersion = 2;

        public SaveData(long gold, int attackLevel, int healthLevel, int innerPowerLevel, int realmLevel,
            int stageNumber, int seenIntroStage, long savedAtUtcTicks, int realmKills = 0)
        {
            Gold = gold;
            AttackLevel = attackLevel;
            HealthLevel = healthLevel;
            InnerPowerLevel = innerPowerLevel;
            RealmLevel = realmLevel;
            RealmKills = realmKills;
            StageNumber = stageNumber;
            SeenIntroStage = seenIntroStage;
            SavedAtUtcTicks = savedAtUtcTicks;
        }

        public long Gold { get; }
        public int AttackLevel { get; }
        public int HealthLevel { get; }
        public int InnerPowerLevel { get; }
        public int RealmLevel { get; }
        public int RealmKills { get; }
        public int StageNumber { get; }
        public int SeenIntroStage { get; }
        public long SavedAtUtcTicks { get; }

        public static SaveData Capture(Wallet wallet, StatUpgrades upgrades, RealmProgress realm, StageProgress stage, long savedAtUtcTicks)
        {
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            if (upgrades == null) throw new ArgumentNullException(nameof(upgrades));
            if (realm == null) throw new ArgumentNullException(nameof(realm));
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            return new SaveData(wallet.Balance, upgrades.GetLevel(StatType.Attack),
                upgrades.GetLevel(StatType.Health), upgrades.GetLevel(StatType.InnerPower), (int)realm.Current,
                stage.StageNumber, stage.SeenIntroStage, savedAtUtcTicks, realm.Kills);
        }
    }
}
