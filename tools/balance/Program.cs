using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PlumSword.Core.Balance;

namespace PlumSword.BalanceSim
{
    public sealed class RunResult
    {
        public string Name;
        public List<string> Changed = new List<string>();
        public List<KeyValuePair<string, string>> Table;
        public List<SeedResult> Seeds = new List<SeedResult>();
    }

    public static class Program
    {
        private const string Usage = @"밸런스 시뮬레이터

  dotnet run --project tools/balance -- [옵션]

옵션
  --config <파일.json>   수치 표. 여러 번 주면 나란히 비교한다. 없으면 기본 표 하나.
  --seeds <수>           난수 시드 수 (기본 5). 무리 구성이 달라진다.
  --minutes <분>         최대 게임 시간 (기본 40). 천마를 잡으면 그 seed 는 먼저 끝난다.
  --out <폴더>           결과 폴더 (기본 tools/balance/out/<날짜-시간>)
  --dump-default <파일>  기본 표를 설명과 함께 JSON 으로 쓰고 끝낸다 (수치 고칠 때 시작점)

결과: <폴더>/report.html (그래프), data.json, CSV (seed 마다 1초 간격 기록)";

        public static int Main(string[] args)
        {
            var configs = new List<string>();
            int seeds = 5;
            double minutes = 40;
            string outDir = null;
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} 뒤에 값이 없다");
                    switch (args[i])
                    {
                        case "--config": configs.Add(Next()); break;
                        case "--seeds": seeds = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                        case "--minutes": minutes = double.Parse(Next(), CultureInfo.InvariantCulture); break;
                        case "--out": outDir = Next(); break;
                        case "--dump-default":
                            string path = Next();
                            File.WriteAllText(path, TableJson.DumpDefault(), new UTF8Encoding(false));
                            Console.WriteLine($"기본 표를 썼다: {Path.GetFullPath(path)}");
                            return 0;
                        case "-h":
                        case "--help":
                            Console.WriteLine(Usage);
                            return 0;
                        default:
                            throw new ArgumentException($"모르는 옵션: {args[i]}");
                    }
                }
                if (seeds < 1) throw new ArgumentException("--seeds 는 1 이상");
                if (!(minutes > 0)) throw new ArgumentException("--minutes 는 0 보다 커야 한다");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("오류: " + e.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine(Usage);
                return 2;
            }

            outDir ??= Path.Combine(ToolDir(), "out", DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(outDir);

            var runs = new List<RunResult>();
            var sources = configs.Count == 0 ? new List<string> { null } : configs;
            try
            {
                foreach (var config in sources)
                {
                    var run = new RunResult { Name = config == null ? "기본값" : Path.GetFileNameWithoutExtension(config) };
                    BalanceTable table = config == null ? BalanceTable.Default : TableJson.Load(config, out run.Changed);
                    BalanceTable.Current = table;
                    run.Table = TableJson.Describe(table);
                    Console.WriteLine($"[{run.Name}] 바꾼 값 {run.Changed.Count}개");
                    foreach (var c in run.Changed) Console.WriteLine("  " + c);
                    for (int s = 1; s <= seeds; s++)
                    {
                        var r = Simulation.Run(s, minutes * 60.0);
                        run.Seeds.Add(r);
                        Console.WriteLine($"  seed {s}: {(r.ClearTime.HasValue ? "천마 처치 " + Clock(r.ClearTime.Value) : "천마 못 잡음 (" + Clock(r.EndTime) + "까지)")}, " +
                                          $"회귀 {r.Regressions}회, 패배 {r.Fails}회, 끝 상태 {Where(r.Last)} 무력{r.Last.AtkLv}/체력{r.Last.HpLv}/내공{r.Last.InnLv}");
                        Csv.Write(Path.Combine(outDir, $"{Safe(run.Name)}_seed{s}.csv"), r);
                    }
                    runs.Add(run);
                }
            }
            catch (Exception e) when (e is InvalidDataException || e is ArgumentException || e is System.Text.Json.JsonException)
            {
                Console.Error.WriteLine("오류: " + e.Message);
                return 2;
            }
            finally
            {
                BalanceTable.Current = BalanceTable.Default;
            }

            Console.WriteLine();
            foreach (var run in runs) Console.WriteLine(Summary(run));

            string report = Path.Combine(outDir, "report.html");
            Report.Write(report, Path.Combine(outDir, "data.json"), runs, seeds, minutes);
            Console.WriteLine();
            Console.WriteLine($"보고서: {Path.GetFullPath(report)}");
            return 0;
        }

        public static string Summary(RunResult run)
        {
            var times = run.Seeds.Where(s => s.ClearTime.HasValue).Select(s => s.ClearTime.Value).OrderBy(x => x).ToList();
            string clear = times.Count == 0 ? "없음"
                : $"중앙값 {Clock(Median(times))} (범위 {Clock(times.First())}~{Clock(times.Last())})";
            double reg = run.Seeds.Average(s => s.Regressions);
            return $"[{run.Name}] 천마 처치 {times.Count}/{run.Seeds.Count} seed, {clear}, 평균 회귀 {reg:0.#}회";
        }

        public static double Median(List<double> sorted) =>
            sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2.0;

        public static string Clock(double seconds)
        {
            int s = (int)Math.Floor(seconds);
            return $"{s / 60:00}:{s % 60:00}";
        }

        private static string Where(Sample s) => s.Disp == 5 ? "FINAL" : $"{s.Disp}-{s.Sub}";

        private static string Safe(string name) =>
            string.Concat(name.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));

        private static string ToolDir()
        {
            // 실행 파일은 tools/balance/bin/... 에 있다. 위로 올라가며 BalanceSim.csproj 가 있는 폴더를 찾는다.
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "BalanceSim.csproj"))) dir = dir.Parent;
            return dir?.FullName ?? Directory.GetCurrentDirectory();
        }
    }

    public static class Csv
    {
        public static void Write(string path, SeedResult r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("seconds,stage,display_stage,substage,phase,gold,gold_earned,attack_lv,health_lv,inner_lv,attack,max_health,skill_damage,realm,regressions,enemy_health,enemy_attack");
            foreach (var s in r.Samples)
                sb.AppendLine(string.Join(",", new object[]
                {
                    s.T.ToString("0.###", CultureInfo.InvariantCulture), s.Stage, s.Disp, s.Sub, s.Phase, s.Gold, s.GoldEarned,
                    s.AtkLv, s.HpLv, s.InnLv, s.Attack, s.MaxHp, s.SkillDmg, s.Realm, s.Regress, s.EnemyHp, s.EnemyAtk,
                }));
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }
    }
}
