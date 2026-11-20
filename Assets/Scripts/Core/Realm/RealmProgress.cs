using System;
using PlumSword.Core.Currency;

namespace PlumSword.Core.Realm
{
    public sealed class RealmProgress
    {
        public RealmProgress(Wallet wallet, int startLevel = 0, int kills = 0)
        {
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            Current = (RealmLevel)Math.Max(0, Math.Min(RealmRules.LevelCount - 1, startLevel));
            Kills = IsMax ? 0 : Math.Max(0, Math.Min(RealmRules.KillsRequired(Current) - 1, kills));
        }

        public RealmLevel Current { get; private set; }
        public bool IsMax => Current == RealmLevel.Transcendent;
        public int BonusPercent => RealmRules.BonusPercent(Current);
        public int Kills { get; private set; }
        public int KillsRequired => RealmRules.KillsRequired(Current);

        public event Action<RealmLevel> BrokeThrough;
        public event Action ProgressChanged;

        public void RecordKill()
        {
            if (IsMax)
            {
                return;
            }
            Kills++;
            if (Kills >= KillsRequired)
            {
                Kills = 0;
                Current = (RealmLevel)((int)Current + 1);
                BrokeThrough?.Invoke(Current);
            }
            ProgressChanged?.Invoke();
        }
    }
}
