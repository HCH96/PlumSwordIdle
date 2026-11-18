using System;
using PlumSword.Core.Currency;

namespace PlumSword.Core.Realm
{
    public static class RealmText
    {
        public const string ButtonTitle = "BREAKTHROUGH";

        public static string Name(RealmLevel realm)
        {
            switch (realm)
            {
                case RealmLevel.ThirdRate: return "THIRD RATE";
                case RealmLevel.SecondRate: return "SECOND RATE";
                case RealmLevel.FirstRate: return "FIRST RATE";
                case RealmLevel.Peak: return "PEAK";
                case RealmLevel.Transcendent: return "TRANSCENDENT";
                default: throw new ArgumentOutOfRangeException(nameof(realm));
            }
        }

        public static string Label(RealmLevel realm) => "REALM: " + Name(realm);

        public static string AssetId(RealmLevel realm)
        {
            switch (realm)
            {
                case RealmLevel.ThirdRate: return "third_rate";
                case RealmLevel.SecondRate: return "second_rate";
                case RealmLevel.FirstRate: return "first_rate";
                case RealmLevel.Peak: return "peak";
                case RealmLevel.Transcendent: return "transcendent";
                default: throw new ArgumentOutOfRangeException(nameof(realm));
            }
        }

        public static string Cost(long cost, bool isMax) => isMax ? "MAX" : "G " + GoldText.FormatNumber(cost);

        public static string ButtonLabel(RealmLevel current, long cost, bool isMax) =>
            ButtonTitle + "\n" + Name(current) + "\n" + Cost(cost, isMax);
    }
}
