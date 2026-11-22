using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using PlumSword.Core.Balance;

namespace PlumSword.BalanceSim
{
    /// <summary>
    /// 수치 표(BalanceTable)를 JSON 으로 읽고 쓴다.
    /// JSON 에는 바꾸고 싶은 값만 적으면 된다. 적지 않은 값은 기본 표(BalanceTable.Default) 그대로다.
    /// 곡선은 {"base": 20, "linear": 5, "growth": 1.0} 또는 [20, 5, 1.0].
    /// </summary>
    public static class TableJson
    {
        private static PropertyInfo[] Properties => typeof(BalanceTable)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToArray();

        public static BalanceTable Load(string path, out List<string> changed)
        {
            var table = BalanceTable.Default;
            var defaults = BalanceTable.Default;
            changed = new List<string>();
            using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });
            var props = Properties.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var item in doc.RootElement.EnumerateObject())
            {
                if (item.Name.StartsWith("_") || item.Name.StartsWith("//")) continue; // 설명용 칸
                if (!props.TryGetValue(item.Name, out var prop))
                    throw new InvalidDataException($"{path}: 모르는 항목 '{item.Name}'. 쓸 수 있는 항목: {string.Join(", ", props.Keys)}");
                object value = Read(prop.PropertyType, item.Value, $"{path}:{item.Name}");
                prop.SetValue(table, value);
                string before = Format(prop.GetValue(defaults));
                string after = Format(value);
                if (before != after) changed.Add($"{prop.Name}: {before} → {after}");
            }
            table.Validate();
            return table;
        }

        private static object Read(Type type, JsonElement e, string where)
        {
            try
            {
                if (type == typeof(GrowthCurve))
                {
                    if (e.ValueKind == JsonValueKind.Array)
                    {
                        var a = e.EnumerateArray().Select(x => x.GetDouble()).ToArray();
                        if (a.Length != 3) throw new InvalidDataException("곡선 배열은 [base, linear, growth] 세 칸이어야 한다");
                        return new GrowthCurve(a[0], a[1], a[2]);
                    }
                    double Get(string name, double fallback) =>
                        e.TryGetProperty(name, out var v) ? v.GetDouble() : fallback;
                    return new GrowthCurve(Get("base", 0), Get("linear", 0), Get("growth", 1));
                }
                if (type == typeof(long)) return e.GetInt64();
                if (type == typeof(int)) return e.GetInt32();
                if (type == typeof(double)) return e.GetDouble();
                if (type == typeof(int[])) return e.EnumerateArray().Select(x => x.GetInt32()).ToArray();
            }
            catch (Exception ex) when (!(ex is InvalidDataException))
            {
                throw new InvalidDataException($"{where}: 값을 읽을 수 없다 ({ex.Message})");
            }
            throw new InvalidDataException($"{where}: 지원하지 않는 타입 {type.Name}");
        }

        public static string Format(object value)
        {
            switch (value)
            {
                case GrowthCurve c:
                    return $"({Num(c.Base)}, {Num(c.Linear)}, {Num(c.Growth)})";
                case int[] a:
                    return "[" + string.Join(", ", a) + "]";
                case double d:
                    return Num(d);
                default:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        private static string Num(double d) => d.ToString("0.######", CultureInfo.InvariantCulture);

        /// <summary>기본 표 전체를 설명과 함께 JSON 으로 쓴다. 수치를 고칠 때 이 파일을 복사해 시작한다.</summary>
        public static string DumpDefault()
        {
            var t = BalanceTable.Default;
            var notes = new Dictionary<string, string>
            {
                ["AttackCost"] = "무력 강화 비용 곡선. 비용 = 내림((base + linear×단계) × growth^단계)",
                ["HealthCost"] = "체력 강화 비용 곡선",
                ["InnerPowerCost"] = "내공 강화 비용 곡선",
                ["AttackPerLevel"] = "무력 1단계당 공격력",
                ["HealthPerLevel"] = "체력 1단계당 최대 체력",
                ["DiscipleBaseAttack"] = "시작 공격력",
                ["DiscipleBaseMaxHealth"] = "시작 최대 체력",
                ["DiscipleAttackInterval"] = "제자 공격 간격(초)",
                ["SkillBasePercent"] = "스킬 피해 기본 % (공격력 대비)",
                ["SkillPercentPerInnerLevel"] = "내공 1단계당 스킬 %p",
                ["SkillCooldownSeconds"] = "스킬 재사용 대기(초)",
                ["SubstageStep"] = "구간 하나가 스테이지 몇 개만큼 어려워지는가. 0이면 구간과 무관. 1/7이면 7구간 = 1스테이지",
                ["NormalHealth"] = "일반 상대 체력 곡선. n = (스테이지-1) + SubstageStep×(구간-1)",
                ["NormalAttack"] = "일반 상대 공격력 곡선",
                ["BossHealth"] = "보스 체력 곡선",
                ["BossAttack"] = "보스 공격력 곡선",
                ["NormalAttackInterval"] = "일반 상대 공격 간격(초)",
                ["BossAttackInterval"] = "보스 공격 간격(초)",
                ["FinalBossHealth"] = "천마 체력 고정값. 0이면 보스 곡선",
                ["FinalBossAttack"] = "천마 공격력 고정값. 0이면 보스 곡선",
                ["NormalGold"] = "일반 상대 처치 금화 곡선",
                ["BossGold"] = "보스 처치 금화 곡선",
                ["RealmKillsRequired"] = "경지 돌파에 필요한 처치 수 [삼류→이류, 이류→일류, 일류→절정, 절정→초절정]",
                ["RealmBonusPercent"] = "경지별 공격력·체력 보너스 % [삼류, 이류, 일류, 절정, 초절정]",
            };
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"_설명\": \"바꾸고 싶은 값만 남기고 지워도 된다. 적지 않은 값은 기본값. 곡선은 [base, linear, growth].\",");
            var props = Properties;
            for (int i = 0; i < props.Length; i++)
            {
                var p = props[i];
                if (notes.TryGetValue(p.Name, out var note))
                    sb.AppendLine($"  \"_{p.Name}\": \"{note}\",");
                object v = p.GetValue(t);
                string json = v switch
                {
                    GrowthCurve c => $"[{Num(c.Base)}, {Num(c.Linear)}, {Num(c.Growth)}]",
                    int[] a => "[" + string.Join(", ", a) + "]",
                    double d => Num(d),
                    _ => Convert.ToString(v, CultureInfo.InvariantCulture),
                };
                sb.Append($"  \"{p.Name}\": {json}");
                sb.AppendLine(i < props.Length - 1 ? "," : "");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        public static List<KeyValuePair<string, string>> Describe(BalanceTable table) =>
            Properties.Select(p => new KeyValuePair<string, string>(p.Name, Format(p.GetValue(table)))).ToList();
    }
}
