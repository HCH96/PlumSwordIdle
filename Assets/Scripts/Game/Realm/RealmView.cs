using System;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;
using PlumSword.Game.StatUpgrade;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Realm
{
    public sealed class RealmView : MonoBehaviour
    {
        private RealmProgress realm;
        private Wallet wallet;
        private bool initialized;
        private Image realmImage, frame, icon, coin;
        private TextMeshProUGUI realmLabel, fallbackLabel, title, nameLabel, cost;
        private Button button;
        private StatUpgradeButtonEffect effect;
        private bool artwork;

        public void Initialize(RealmProgress realm, Wallet wallet, Canvas canvas)
        {
            if (realm == null) throw new ArgumentNullException(nameof(realm));
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (initialized) throw new InvalidOperationException("RealmView is already initialized.");
            initialized = true;
            this.realm = realm;
            this.wallet = wallet;

            var labelRect = Rect(canvas.transform, "RealmLabel", new Vector2(0, 1), new Vector2(8, -8), new Vector2(200, 16));
            realmImage = labelRect.gameObject.AddComponent<Image>();
            realmImage.preserveAspect = true;
            realmImage.raycastTarget = false;
            var textRect = Rect(labelRect, "Text", new Vector2(0, 1), Vector2.zero, new Vector2(200, 16));
            realmLabel = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            realmLabel.fontSize = 14;
            realmLabel.alignment = TextAlignmentOptions.MidlineLeft;
            realmLabel.textWrappingMode = TextWrappingModes.NoWrap;
            realmLabel.color = new Color(1f, .95f, .8f);
            realmLabel.raycastTarget = false;

            var buttonRect = Rect(canvas.transform, "RealmButton", Vector2.zero, new Vector2(220, 62), new Vector2(200, 48));
            var background = buttonRect.gameObject.AddComponent<Image>();
            button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnClicked);
            var sprite = Resources.Load<Sprite>("Art/UI/Realm/button") ?? Resources.Load<Sprite>("Art/UI/StatUpgrade/button");
            var coinSprite = Resources.Load<Sprite>("Art/UI/StatUpgrade/coin");
            artwork = sprite != null && coinSprite != null;
            if (artwork)
            {
                background.color = Color.clear;
                frame = Art(buttonRect, "Visual", sprite, Vector2.zero, new Vector2(200, 48));
                frame.preserveAspect = false;
                var visual = frame.rectTransform;
                visual.pivot = new Vector2(.5f, .5f);
                visual.anchoredPosition = new Vector2(100, 24);
                icon = Art(visual, "Icon", Resources.Load<Sprite>("Art/UI/Realm/icon"), new Vector2(9, 8), new Vector2(32, 32));
                icon.enabled = icon.sprite != null;
                coin = Art(visual, "Coin", coinSprite, new Vector2(145, 15), new Vector2(17, 17));
                title = ArtLabel(visual, "Title", new Vector2(48, 23), new Vector2(82, 18), 15, 9);
                nameLabel = ArtLabel(visual, "Name", new Vector2(48, 8), new Vector2(82, 15), 11, 8);
                cost = ArtLabel(visual, "Cost", new Vector2(164, 11), new Vector2(31, 25), 12, 8);
                effect = buttonRect.gameObject.AddComponent<StatUpgradeButtonEffect>();
                effect.Initialize(button, visual, frame, icon, nameLabel);
            }
            else
            {
                frame = background;
                var rect = Rect(buttonRect, "Label", Vector2.zero, Vector2.zero, Vector2.zero);
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                fallbackLabel = rect.gameObject.AddComponent<TextMeshProUGUI>();
                fallbackLabel.fontSize = 14;
                fallbackLabel.alignment = TextAlignmentOptions.Center;
                fallbackLabel.raycastTarget = false;
            }
            Refresh();
            wallet.BalanceChanged += OnBalanceChanged;
            realm.BrokeThrough += OnBrokeThrough;
            realm.LockedChanged += OnLockedChanged;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image Art(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            var image = Rect(parent, name, Vector2.zero, position, size).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI ArtLabel(Transform parent, string name, Vector2 position, Vector2 size, float fontSize, float minSize)
        {
            var label = Rect(parent, name, Vector2.zero, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.enableAutoSizing = true;
            label.fontSizeMin = minSize;
            label.fontSizeMax = fontSize;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        private void Refresh()
        {
            var sprite = Resources.Load<Sprite>("Art/UI/Realm/name_" + RealmText.AssetId(realm.Current));
            realmImage.sprite = sprite;
            realmImage.enabled = sprite != null;
            realmLabel.gameObject.SetActive(sprite == null);
            realmLabel.text = RealmText.Label(realm.Current);
            realmImage.rectTransform.sizeDelta = sprite != null
                ? new Vector2(Mathf.Min(160f, 24f * sprite.rect.width / sprite.rect.height), 24f)
                : new Vector2(200, 16);

            bool enabled = realm.CanBreakthrough();
            button.interactable = enabled;
            if (artwork)
            {
                var tint = enabled ? Color.white : new Color(.6f, .6f, .6f);
                effect.SetEnabledAppearance(enabled);
                frame.color = enabled ? new Color(1, 1, 1, .86f) : new Color(.55f, .55f, .55f, .86f);
                icon.color = coin.color = title.color = nameLabel.color = cost.color = tint;
                coin.gameObject.SetActive(!realm.IsMax);
                title.text = RealmText.ButtonTitle;
                nameLabel.text = RealmText.Name(realm.Current);
                cost.text = realm.IsMax ? "MAX" : GoldText.FormatNumber(realm.NextCost);
            }
            else
            {
                frame.color = enabled ? new Color(.55f, .25f, .35f) : new Color(.25f, .25f, .25f);
                fallbackLabel.color = enabled ? Color.white : new Color(.6f, .6f, .6f);
                fallbackLabel.text = RealmText.ButtonLabel(realm.Current, realm.NextCost, realm.IsMax);
            }
        }

        private void OnClicked() { if (realm.TryBreakthrough()) effect?.PlaySuccess(); }
        private void OnBalanceChanged(long balance) { Refresh(); }
        private void OnBrokeThrough(RealmLevel level) { Refresh(); }
        private void OnLockedChanged(bool locked) { Refresh(); }

        private void OnDestroy()
        {
            if (wallet != null) wallet.BalanceChanged -= OnBalanceChanged;
            if (realm != null)
            {
                realm.BrokeThrough -= OnBrokeThrough;
                realm.LockedChanged -= OnLockedChanged;
            }
        }
    }
}
