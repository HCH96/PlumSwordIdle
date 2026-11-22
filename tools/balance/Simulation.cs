using System;
using System.Collections.Generic;
using PlumSword.Core.Balance;
using PlumSword.Core.Combat;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;
using PlumSword.Core.Stage;
using PlumSword.Core.StatUpgrade;

namespace PlumSword.BalanceSim
{
    /// <summary>1초마다 찍는 상태 한 줄.</summary>
    public sealed class Sample
    {
        public double T;          // 지난 게임 시간(초)
        public int Stage;         // 전체 진행 번호
        public int Disp;          // 화면상 스테이지 1~5
        public int Sub;           // 구간 1~7
        public string Phase;
        public long Gold;
        public int AtkLv, HpLv, InnLv;
        public long Attack, MaxHp, SkillDmg;
        public int Realm;
        public int Regress;
        public long EnemyHp, EnemyAtk;  // 지금 구간 상대 하나의 능력치
        public long GoldEarned;   // 누적 획득 금화
    }

    public sealed class SimEvent
    {
        public double T;
        public string Type;       // clear, fail, regress, breakthrough, gameClear
        public int Disp;          // 화면상 스테이지 1~5
        public string Label;      // 예: "3-7", "FINAL"
        public string Detail;
    }

    /// <summary>큰 스테이지 한 번의 시도 (시작 ~ 성공/실패).</summary>
    public sealed class Attempt
    {
        public int Disp;
        public int Stage;
        public double Start, End;
        public string Result;     // clear, fail, regress, timeout
    }

    public sealed class SeedResult
    {
        public int Seed;
        public double? ClearTime;  // 천마 첫 처치 시각(초). 시간 안에 못 잡으면 null
        public int Regressions;
        public int Fails;
        public double EndTime;
        public List<Sample> Samples = new List<Sample>();
        public List<SimEvent> Events = new List<SimEvent>();
        public List<Attempt> Attempts = new List<Attempt>();
        public Sample Last;
    }

    /// <summary>
    /// 게임(GameRoot)과 같은 조립으로 Core 객체를 만들고 프레임마다 Tick 한다.
    /// 화면이 끼워 넣는 쓰러짐 연출 대기(StageView)도 흉내 낸다. 자동 강화는 켠다.
    /// </summary>
    public static class Simulation
    {
        public const double FrameSeconds = 1.0 / 60.0;
        private const double SampleEvery = 1.0;

        // StageView 의 쓰러짐 연출 시간 (그림이 모두 있다고 본다). DownFrameSeconds 0.075.
        private const double DownFrame = 0.075;
        private const double DiscipleDown = 17 * DownFrame;

        public static SeedResult Run(int seed, double maxSeconds)
        {
            var result = new SeedResult { Seed = seed };
            var table = BalanceTable.Current;

            var wallet = new Wallet(0);
            var realm = new RealmProgress(wallet, 0, 0);
            var upgrades = new StatUpgrades(wallet, table.DiscipleBaseAttack, table.DiscipleBaseMaxHealth, 0, 0, 0, realm);
            var auto = new AutoUpgrade(upgrades, wallet, true);
            var battle = new Battle(upgrades, realm, true);
            battle.EnemyDefeated += enemy => realm.RecordKill();
            var stage = new StageProgress(wallet, upgrades, battle, new Random(seed), 1, 0,
                StageProgress.DefaultClearWaitSeconds, true, 0);

            double t = 0.0;
            long earned = 0;
            long lastBalance = 0;
            wallet.BalanceChanged += balance =>
            {
                if (balance > lastBalance) earned += balance - lastBalance;
                lastBalance = balance;
            };

            // 화면의 연출 대기 흉내 (StageView.OnStageFailed / OnChanged).
            double viewDelay = 0.0;
            bool failedTransition = false;
            StagePhase previousPhase = stage.Phase;
            Attempt current = null;

            string Label() => stage.DisplayStageNumber == StageLayout.FinalDisplayStage
                ? "FINAL" : $"{stage.DisplayStageNumber}-{stage.SubstageNumber}";

            stage.StageStarted += number =>
            {
                current = new Attempt { Stage = number, Disp = (number - 1) % 5 + 1, Start = t };
                result.Attempts.Add(current);
            };
            stage.StageCleared += number =>
            {
                result.Events.Add(new SimEvent { T = t, Type = "clear", Disp = (number - 1) % 5 + 1, Label = Label(), Detail = $"{(number - 1) % 5 + 1}스테이지 완료" });
                if (current != null) { current.End = t; current.Result = "clear"; }
            };
            stage.StageFailed += number =>
            {
                failedTransition = true;
                viewDelay = DiscipleDown;
                result.Fails++;
                result.Events.Add(new SimEvent { T = t, Type = "fail", Disp = (number - 1) % 5 + 1, Label = Label(), Detail = $"{(number - 1) % 5 + 1}스테이지 패배" });
                if (current != null) { current.End = t; current.Result = "fail"; }
            };
            stage.Regressed += count =>
            {
                result.Regressions = count;
                result.Events.Add(new SimEvent { T = t, Type = "regress", Disp = 5, Label = "FINAL", Detail = $"회귀 {count}회" });
                if (current != null) current.Result = "regress";
            };
            stage.GameCleared += () =>
            {
                result.ClearTime = t;
                result.Events.Add(new SimEvent { T = t, Type = "gameClear", Disp = 5, Label = "FINAL", Detail = "천마 처치" });
            };
            realm.BrokeThrough += level =>
                result.Events.Add(new SimEvent { T = t, Type = "breakthrough", Disp = stage.DisplayStageNumber, Label = Label(), Detail = $"돌파: {RealmName(level)}" });
            stage.Changed += () =>
            {
                if (stage.Phase == StagePhase.FadingOut
                    && (previousPhase == StagePhase.Playing || previousPhase == StagePhase.ClearWait) && !failedTransition)
                {
                    viewDelay = EnemyDownSeconds(stage);
                    if (previousPhase == StagePhase.ClearWait)
                        viewDelay = Math.Max(0.0, viewDelay - stage.ClearWaitSeconds);
                }
                if (stage.Phase != StagePhase.FadingOut) failedTransition = false;
                previousPhase = stage.Phase;
            };

            battle.SetPaused(false);
            stage.Begin();

            double nextSample = 0.0;
            while (t < maxSeconds)
            {
                if (t >= nextSample - 1e-9)
                {
                    result.Samples.Add(Snapshot(t, stage, upgrades, wallet, realm, battle, earned));
                    nextSample += SampleEvery;
                }
                if (stage.Phase == StagePhase.Cleared) break;

                double dt = FrameSeconds;
                battle.Tick(dt);                       // CombatView.Update
                double stageDt = dt;                   // StageView.Update
                if (viewDelay > 0.0)
                {
                    double used = Math.Min(stageDt, viewDelay);
                    viewDelay -= used;
                    stageDt -= used;
                }
                stage.Tick(stageDt);
                t += dt;
            }

            if (current != null && current.Result == null) { current.End = t; current.Result = "timeout"; }
            result.EndTime = t;
            result.Last = Snapshot(t, stage, upgrades, wallet, realm, battle, earned);
            if (result.Samples.Count == 0 || result.Samples[result.Samples.Count - 1].T < t - 1e-9)
                result.Samples.Add(result.Last);
            auto.Dispose();
            return result;
        }

        // StageView.OnChanged 의 성공 전환 연출 시간 (모든 그림이 있다고 본다).
        private static double EnemyDownSeconds(StageProgress stage)
        {
            bool boss = stage.SubstageNumber > StageLayout.NormalSubstageCount;
            switch (stage.DisplayStageNumber)
            {
                case 1: return 17 * DownFrame;
                case 2: return boss ? 25 * DownFrame : 16 * DownFrame;
                case 3: return 17 * DownFrame;
                case 4: return 17 * DownFrame;
                case 5: return boss ? 25 * DownFrame : 0.0;
                default: return 0.0;
            }
        }

        private static Sample Snapshot(double t, StageProgress stage, StatUpgrades upgrades, Wallet wallet,
            RealmProgress realm, Battle battle, long earned)
        {
            int sub = stage.SubstageNumber;
            var kind = sub > StageLayout.NormalSubstageCount ? EnemyKind.Boss : EnemyKind.Normal;
            int enemySub = Math.Max(1, Math.Min(7, sub));
            return new Sample
            {
                T = Math.Round(t, 3),
                Stage = stage.StageNumber,
                Disp = stage.DisplayStageNumber,
                Sub = sub,
                Phase = stage.Phase.ToString(),
                Gold = wallet.Balance,
                AtkLv = upgrades.GetLevel(StatType.Attack),
                HpLv = upgrades.GetLevel(StatType.Health),
                InnLv = upgrades.GetLevel(StatType.InnerPower),
                Attack = upgrades.Attack,
                MaxHp = upgrades.MaxHealth,
                SkillDmg = battle.SkillDamage,
                Realm = (int)realm.Current,
                Regress = stage.RegressionCount,
                EnemyHp = EnemyStats.MaxHealth(kind, stage.StageNumber, enemySub),
                EnemyAtk = EnemyStats.Attack(kind, stage.StageNumber, enemySub),
                GoldEarned = earned,
            };
        }

        public static string RealmName(RealmLevel level)
        {
            switch (level)
            {
                case RealmLevel.ThirdRate: return "삼류";
                case RealmLevel.SecondRate: return "이류";
                case RealmLevel.FirstRate: return "일류";
                case RealmLevel.Peak: return "절정";
                case RealmLevel.Transcendent: return "초절정";
                default: return level.ToString();
            }
        }
    }
}
