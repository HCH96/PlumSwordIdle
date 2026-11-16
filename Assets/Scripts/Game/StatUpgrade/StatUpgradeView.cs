using System;
using PlumSword.Core.Currency;
using PlumSword.Core.StatUpgrade;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.StatUpgrade
{
    /// <summary>
    /// 강화 버튼 세 개(ATK, HP, SPD)를 보여 준다. 누르면 StatUpgrades.TryUpgrade 를 부른다.
    /// 글자는 StatUpgradeText.ButtonLabel, 누를 수 있는지는 StatUpgrades.CanUpgrade 그대로 쓴다.
    /// 처음 한 번, 그 뒤로 Wallet.BalanceChanged, Upgraded, LockedChanged 가 올 때마다 다시 그린다.
    /// 사양: docs/spec/stat_upgrade.md "화면".
    /// </summary>
    public sealed class StatUpgradeView : MonoBehaviour
    {
        // 위치·크기·글자 크기·색은 임시값 (docs/spec/stat_upgrade.md "화면").
        private static readonly Vector2 ButtonSize = new Vector2(200f, 88f);
        private const float LabelFontSize = 16f;
        private static readonly Color EnabledButtonColor = new Color(0.20f, 0.45f, 0.25f);
        private static readonly Color EnabledLabelColor = Color.white;
        private static readonly Color DisabledButtonColor = new Color(0.25f, 0.25f, 0.25f);
        private static readonly Color DisabledLabelColor = new Color(0.6f, 0.6f, 0.6f);

        // 왼쪽부터 ATK, HP, SPD.
        private static readonly StatType[] Stats = { StatType.Attack, StatType.Health, StatType.AttackSpeed };
        private static readonly Vector2[] ButtonPositions =
        {
            new Vector2(8f, 8f),
            new Vector2(220f, 8f),
            new Vector2(432f, 8f),
        };

        private sealed class StatButton
        {
            public StatType Stat;
            public Image Image;
            public Button Button;
            public TextMeshProUGUI Label;
        }

        private PlumSword.Core.StatUpgrade.StatUpgrades _upgrades;
        private Wallet _wallet;
        private StatButton[] _buttons;
        private bool _warned;

        /// <summary>
        /// canvas 아래에 강화 버튼 세 개를 만들고 지금 상태로 바로 표시한다.
        /// wallet.BalanceChanged, upgrades.Upgraded, upgrades.LockedChanged 를 구독하고, OnDestroy 에서 구독을 끊는다.
        /// </summary>
        public void Initialize(PlumSword.Core.StatUpgrade.StatUpgrades upgrades, Wallet wallet, Canvas canvas)
        {
            if (upgrades == null) throw new ArgumentNullException(nameof(upgrades));
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));

            Unsubscribe();
            _upgrades = upgrades;
            _wallet = wallet;

            _buttons = new StatButton[Stats.Length];
            for (int i = 0; i < Stats.Length; i++)
            {
                _buttons[i] = CreateButton(canvas, Stats[i], ButtonPositions[i]);
            }

            RefreshAll();

            _wallet.BalanceChanged += OnBalanceChanged;
            _upgrades.Upgraded += OnUpgraded;
            _upgrades.LockedChanged += OnLockedChanged;
        }

        private StatButton CreateButton(Canvas canvas, StatType stat, Vector2 position)
        {
            var buttonObject = new GameObject("StatButton_" + stat, typeof(RectTransform));
            var rect = (RectTransform)buttonObject.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.sizeDelta = ButtonSize;
            rect.anchoredPosition = position;

            var image = buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => OnClicked(stat));

            return new StatButton
            {
                Stat = stat,
                Image = image,
                Button = button,
                Label = CreateLabel(rect),
            };
        }

        private TextMeshProUGUI CreateLabel(RectTransform parent)
        {
            try
            {
                var labelObject = new GameObject("Label", typeof(RectTransform));
                var rect = (RectTransform)labelObject.transform;
                rect.SetParent(parent, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var label = labelObject.AddComponent<TextMeshProUGUI>();
                label.fontSize = LabelFontSize;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                return label;
            }
            catch (Exception e)
            {
                WarnOnce(e);
                return null;
            }
        }

        private void OnClicked(StatType stat)
        {
            if (_upgrades == null) return;
            _upgrades.TryUpgrade(stat);
        }

        private void OnBalanceChanged(long balance)
        {
            RefreshAll();
        }

        private void OnUpgraded(StatType stat)
        {
            RefreshAll();
        }

        private void OnLockedChanged(bool locked)
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (_upgrades == null || _buttons == null) return;

            foreach (var statButton in _buttons)
            {
                Refresh(statButton);
            }
        }

        private void Refresh(StatButton statButton)
        {
            var stat = statButton.Stat;
            bool canUpgrade = _upgrades.CanUpgrade(stat);

            if (statButton.Button != null) statButton.Button.interactable = canUpgrade;
            if (statButton.Image != null)
            {
                statButton.Image.color = canUpgrade ? EnabledButtonColor : DisabledButtonColor;
            }

            if (statButton.Label == null) return;

            try
            {
                statButton.Label.text = StatUpgradeText.ButtonLabel(
                    stat,
                    _upgrades.GetLevel(stat),
                    _upgrades.GetCost(stat),
                    _upgrades.IsMaxLevel(stat));
                statButton.Label.color = canUpgrade ? EnabledLabelColor : DisabledLabelColor;
            }
            catch (Exception e)
            {
                WarnOnce(e);
            }
        }

        private void WarnOnce(Exception e)
        {
            if (_warned) return;
            _warned = true;
            Debug.LogWarning(
                "StatUpgradeView: 강화 버튼 글자를 표시하지 못했습니다. TMP Essentials 가 필요합니다 " +
                "(Window > TextMeshPro > Import TMP Essential Resources). " + e.Message);
        }

        private void Unsubscribe()
        {
            if (_wallet != null)
            {
                _wallet.BalanceChanged -= OnBalanceChanged;
            }

            if (_upgrades != null)
            {
                _upgrades.Upgraded -= OnUpgraded;
                _upgrades.LockedChanged -= OnLockedChanged;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _wallet = null;
            _upgrades = null;
        }
    }
}
