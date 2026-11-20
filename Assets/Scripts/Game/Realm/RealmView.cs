using System;
using PlumSword.Core.Currency;
using PlumSword.Core.Realm;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlumSword.Game.Realm
{
    public sealed class RealmView : MonoBehaviour
    {
        private RealmProgress realm;
        private Image nameImage;
        private TextMeshProUGUI fallback;

        public void Initialize(RealmProgress realm, Wallet wallet, Canvas canvas)
        {
            if (realm == null) throw new ArgumentNullException(nameof(realm));
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            if (this.realm != null) throw new InvalidOperationException("RealmView is already initialized.");
            this.realm = realm;
            var background = CreateImage(canvas.transform, "RealmStatus", new Vector2(4, -2), new Vector2(140, 47));
            background.sprite = Resources.Load<Sprite>("Art/UI/RealmStatus/brush_plum");
            background.enabled = background.sprite != null;
            nameImage = CreateImage(canvas.transform, "RealmName", new Vector2(24, -9), new Vector2(100, 33));
            var rect = MakeRect(canvas.transform, "RealmFallback", new Vector2(30, -8), new Vector2(160, 20));
            fallback = rect.gameObject.AddComponent<TextMeshProUGUI>();
            fallback.fontSize = 12;
            fallback.color = new Color(1f, .95f, .8f);
            fallback.raycastTarget = false;
            realm.BrokeThrough += OnBrokeThrough;
            Refresh();
        }

        private static RectTransform MakeRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var image = MakeRect(parent, name, position, size).gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private void Refresh()
        {
            nameImage.sprite = Resources.Load<Sprite>("Art/UI/RealmStatus/name_" + RealmText.AssetId(realm.Current));
            nameImage.enabled = nameImage.sprite != null;
            fallback.gameObject.SetActive(nameImage.sprite == null);
            fallback.text = RealmText.Label(realm.Current);
        }

        private void OnBrokeThrough(RealmLevel level) => Refresh();
        private void OnDestroy()
        {
            if (realm != null) realm.BrokeThrough -= OnBrokeThrough;
        }
    }
}
