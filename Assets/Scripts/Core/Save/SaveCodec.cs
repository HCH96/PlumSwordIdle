using System;
using System.Collections.Generic;
using System.Globalization;

namespace PlumSword.Core.Save
{
    public static class SaveCodec
    {
        public static string Encode(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var culture = CultureInfo.InvariantCulture;
            return "version=" + SaveData.CurrentVersion.ToString(culture) + "\n" +
                "gold=" + data.Gold.ToString(culture) + "\n" +
                "attackLevel=" + data.AttackLevel.ToString(culture) + "\n" +
                "healthLevel=" + data.HealthLevel.ToString(culture) + "\n" +
                "attackSpeedLevel=" + data.AttackSpeedLevel.ToString(culture) + "\n" +
                "stageNumber=" + data.StageNumber.ToString(culture) + "\n" +
                "seenIntroStage=" + data.SeenIntroStage.ToString(culture) + "\n" +
                "savedAtUtcTicks=" + data.SavedAtUtcTicks.ToString(culture) + "\n";
        }

        public static SaveLoadResult Decode(string text)
        {
            if (text == null) return new SaveLoadResult(SaveLoadStatus.Empty, null);

            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var original in text.Split('\n'))
            {
                var line = original.EndsWith("\r", StringComparison.Ordinal)
                    ? original.Substring(0, original.Length - 1) : original;
                if (line.Length == 0) continue;
                int separator = line.IndexOf('=');
                if (separator < 0) return new SaveLoadResult(SaveLoadStatus.Corrupt, null);
                string name = line.Substring(0, separator);
                if (fields.ContainsKey(name)) return new SaveLoadResult(SaveLoadStatus.Corrupt, null);
                fields.Add(name, line.Substring(separator + 1));
            }

            if (!TryInt(fields, "version", out int version)) return new SaveLoadResult(SaveLoadStatus.Corrupt, null);
            if (version != SaveData.CurrentVersion) return new SaveLoadResult(SaveLoadStatus.UnsupportedVersion, null);
            if (!TryLong(fields, "gold", out long gold) ||
                !TryInt(fields, "attackLevel", out int attack) ||
                !TryInt(fields, "healthLevel", out int health) ||
                !TryInt(fields, "attackSpeedLevel", out int speed) ||
                !TryInt(fields, "stageNumber", out int stage) ||
                !TryInt(fields, "seenIntroStage", out int seen) ||
                !TryLong(fields, "savedAtUtcTicks", out long ticks))
                return new SaveLoadResult(SaveLoadStatus.Corrupt, null);
            return new SaveLoadResult(SaveLoadStatus.Loaded, new SaveData(gold, attack, health, speed, stage, seen, ticks));
        }

        private static bool TryInt(Dictionary<string, string> fields, string name, out int value)
        {
            value = 0;
            return fields.TryGetValue(name, out string text) &&
                int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryLong(Dictionary<string, string> fields, string name, out long value)
        {
            value = 0;
            return fields.TryGetValue(name, out string text) &&
                long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }
    }
}
