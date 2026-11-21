using System;

namespace PlumSword.Core.Audio
{
    public static class AudioCatalog
    {
        public const double CrossfadeSeconds = 1.0;
        public const double SfxMinIntervalSeconds = 0.05;
        public const int MaxSfxVoices = 8;

        private struct SfxEntry
        {
            public readonly string Name;
            public readonly double Volume;
            public readonly bool Enabled;
            public SfxEntry(string name, double volume, bool enabled)
            {
                Name = name; Volume = volume; Enabled = enabled;
            }
        }

        // Temporary values are kept together for listening and tuning.
        private static readonly SfxEntry[] Sfx =
        {
            new SfxEntry("sword_swing", 0.6, true),
            new SfxEntry("attack_hit", 0.6, true),
            new SfxEntry("upgrade_success", 1.0, true),
            new SfxEntry("plum_sword_skill", 1.0, true),
            new SfxEntry("realm_breakthrough", 1.0, true),
            new SfxEntry("ui_confirm", 1.0, true),
            new SfxEntry("boss_appear", 1.0, true),
            new SfxEntry("boss_defeated", 1.0, true),
            new SfxEntry("player_defeated", 1.0, true),
            new SfxEntry("offline_reward", 1.0, true),
            new SfxEntry("enemy_defeated", 0.6, true),
            new SfxEntry("upgrade_unavailable", 1.0, true),
            new SfxEntry("window_open_close", 1.0, true),
            new SfxEntry("stage_transition", 1.0, true)
        };

        public static string MusicPath(MusicTrack track)
        {
            switch (track)
            {
                case MusicTrack.None: return null;
                case MusicTrack.Title: return "Audio/Music/title";
                case MusicTrack.BattleNormal: return "Audio/Music/battle_normal";
                case MusicTrack.BattleBoss: return "Audio/Music/battle_boss";
                case MusicTrack.BattleFinalBoss: return "Audio/Music/battle_final_boss";
                default: throw new ArgumentOutOfRangeException(nameof(track));
            }
        }

        public static string SfxPath(SfxId id) { return "Audio/SFX/" + Entry(id).Name; }
        public static double MusicBaseVolume(MusicTrack track)
        {
            return MusicPath(track) == null ? 0.0 : 0.6;
        }
        public static double SfxBaseVolume(SfxId id) { return Entry(id).Volume; }
        public static bool IsSfxEnabled(SfxId id) { return Entry(id).Enabled; }

        private static SfxEntry Entry(SfxId id)
        {
            int index = (int)id;
            if (index < 0 || index >= Sfx.Length) throw new ArgumentOutOfRangeException(nameof(id));
            return Sfx[index];
        }
    }
}
