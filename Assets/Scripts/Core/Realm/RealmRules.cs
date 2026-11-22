using System;
using PlumSword.Core.Balance;

namespace PlumSword.Core.Realm
{
    public static class RealmRules
    {
        public const int LevelCount = 5;

        public static int BonusPercent(RealmLevel realm)
        {
            switch (realm)
            {
                case RealmLevel.ThirdRate:
                case RealmLevel.SecondRate:
                case RealmLevel.FirstRate:
                case RealmLevel.Peak:
                case RealmLevel.Transcendent: return BalanceTable.Current.RealmBonusPercent[(int)realm];
                default: throw new ArgumentOutOfRangeException(nameof(realm));
            }
        }

        public static int KillsRequired(RealmLevel from)
        {
            switch (from)
            {
                case RealmLevel.ThirdRate:
                case RealmLevel.SecondRate:
                case RealmLevel.FirstRate:
                case RealmLevel.Peak: return BalanceTable.Current.RealmKillsRequired[(int)from];
                case RealmLevel.Transcendent: return 0;
                default: throw new ArgumentOutOfRangeException(nameof(from));
            }
        }
    }
}
