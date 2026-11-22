using System;
using PlumSword.Core.StartMenu;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlumSword.Game.StartMenu
{
    public sealed class BackKeyWatcher : MonoBehaviour
    {
        private GameRoot root;
        private SettingsView settings;

        public void Initialize(GameRoot root, SettingsView settings)
        {
            this.root = root != null ? root : throw new ArgumentNullException(nameof(root));
            this.settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        private void Update()
        {
            if (root == null || root.Ending != null || settings == null || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            switch (StartMenuRules.BackKey(settings.IsOpen, root.IsStartScreenShowing, root.StageProgress.HasBegun))
            {
                case BackKeyAction.OpenSettings:
                    settings.Open(SettingsMode.InGame);
                    break;
                case BackKeyAction.CloseSettings:
                    settings.Close();
                    break;
            }
        }
    }
}
