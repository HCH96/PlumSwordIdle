using System;
using PlumSword.Core.StartMenu;
using UnityEngine;

namespace PlumSword.Game.StartMenu
{
    public static class VolumePrefs
    {
        public const string BgmKey = "PlumSword.BgmVolume";
        public const string SfxKey = "PlumSword.SfxVolume";

        public static VolumeSettings Load() => new VolumeSettings(PlayerPrefs.GetInt(BgmKey, 80), PlayerPrefs.GetInt(SfxKey, 80));

        public static void Save(VolumeSettings volume)
        {
            if (volume == null) throw new ArgumentNullException(nameof(volume));
            PlayerPrefs.SetInt(BgmKey, volume.Bgm);
            PlayerPrefs.SetInt(SfxKey, volume.Sfx);
            PlayerPrefs.Save();
        }
    }
}
