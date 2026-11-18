using System;
using PlumSword.Core.Combat;
using PlumSword.Core.Realm;
using UnityEngine;

namespace PlumSword.Game.Realm
{
    public sealed class RealmAuraView : MonoBehaviour
    {
        private RealmProgress realm;
        private Battle battle;
        private SpriteRenderer auraRenderer;
        private Texture2D fallbackTexture;
        private Sprite fallbackSprite;
        private Sprite[] frames;
        private bool initialized;
        private bool artwork;
        private bool bursting;
        private float burstStart;
        private float animationStart;

        public void Initialize(RealmProgress realm, Battle battle)
        {
            if (realm == null) throw new ArgumentNullException(nameof(realm));
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            if (initialized) throw new InvalidOperationException("RealmAuraView is already initialized.");
            initialized = true;
            this.realm = realm;
            this.battle = battle;

            var aura = new GameObject("RealmAura");
            aura.transform.SetParent(transform, false);
            auraRenderer = aura.AddComponent<SpriteRenderer>();
            auraRenderer.sortingOrder = 9990;
            RefreshAura();
            realm.BrokeThrough += OnBrokeThrough;
            UpdateDisplay();
        }

        private void RefreshAura()
        {
            ClearFrames();
            artwork = false;
            auraRenderer.enabled = RealmAura.HasAura(realm.Current);
            if (!auraRenderer.enabled) return;

            string path = "Art/Effects/aura_" + RealmText.AssetId(realm.Current);
            var texture = Resources.Load<Texture2D>(path);
            if (texture != null && texture.width % texture.height != 0)
            {
                Debug.LogWarning("Invalid aura sprite sheet dimensions: " + path);
                texture = null;
            }

            if (texture != null)
            {
                int size = texture.height;
                frames = new Sprite[texture.width / size];
                for (int i = 0; i < frames.Length; i++)
                    frames[i] = Sprite.Create(texture, new Rect(i * size, 0, size, size), new Vector2(.5f, .5f), 32f);
                artwork = true;
                animationStart = Time.time;
                auraRenderer.sprite = frames[0];
            }
            else
            {
                EnsureFallback();
                auraRenderer.sprite = fallbackSprite;
            }
        }

        private void EnsureFallback()
        {
            if (fallbackTexture != null) return;
            fallbackTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            fallbackTexture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float r = new Vector2(x + .5f - 32f, y + .5f - 32f).magnitude / 32f;
                float alpha = r >= 1f ? 0f : (1f - r) * (1f - r);
                fallbackTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            fallbackTexture.Apply();
            fallbackSprite = Sprite.Create(fallbackTexture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 32f);
        }

        private void LateUpdate()
        {
            if (initialized) UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            auraRenderer.transform.position = new Vector3((float)battle.DisciplePosition - 2.25f, -.25f, 0f);
            float scale = bursting ? (float)RealmAura.BurstScale(Time.time - burstStart) : 1f;
            if (artwork)
            {
                int frame = (int)((Time.time - animationStart) * 10f) % frames.Length;
                auraRenderer.sprite = frames[frame];
                auraRenderer.color = Color.white;
                auraRenderer.transform.localScale = new Vector3(1.5f * scale, 1.5f * scale, 1f);
            }
            else if (auraRenderer.enabled)
            {
                var color = RealmAura.Color(realm.Current);
                auraRenderer.color = new Color((float)color.R, (float)color.G, (float)color.B,
                    (float)RealmAura.PulseAlpha(realm.Current, Time.time));
                auraRenderer.transform.localScale = new Vector3(1.2f * scale, 1.5f * scale, 1f);
            }
        }

        private void OnBrokeThrough(RealmLevel level)
        {
            burstStart = Time.time;
            bursting = true;
            RefreshAura();
            UpdateDisplay();
        }

        private void ClearFrames()
        {
            if (frames == null) return;
            foreach (var frame in frames) Destroy(frame);
            frames = null;
        }

        private void OnDestroy()
        {
            if (realm != null) realm.BrokeThrough -= OnBrokeThrough;
            ClearFrames();
            if (fallbackSprite != null) Destroy(fallbackSprite);
            if (fallbackTexture != null) Destroy(fallbackTexture);
        }
    }
}
