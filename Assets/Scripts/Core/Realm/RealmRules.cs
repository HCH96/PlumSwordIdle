using System;

namespace PlumSword.Core.Realm
{
    public static class RealmRules
    {
        public const int LevelCount = 5;

        public static int BonusPercent(RealmLevel realm)
        {
            switch (realm)
            {
                case RealmLevel.ThirdRate: return 0;
                case RealmLevel.SecondRate: return 10;
                case RealmLevel.FirstRate: return 20;
                case RealmLevel.Peak: return 30;
                case RealmLevel.Transcendent: return 40;
                default: throw new ArgumentOutOfRangeException(nameof(realm));
            }
        }

        public static long BreakthroughCost(RealmLevel from)
        {
            switch (from)
            {
                case RealmLevel.ThirdRate: return 200;
                case RealmLevel.SecondRate: return 500;
                case RealmLevel.FirstRate: return 1000;
                case RealmLevel.Peak: return 2000;
                case RealmLevel.Transcendent: return 0;
                default: throw new ArgumentOutOfRangeException(nameof(from));
            }
        }
    }
}
