using System;
using System.Collections.Generic;
using PlumSword.Core.Combat;
using PlumSword.Core.Realm;
using UnityEngine;

namespace PlumSword.Game.Realm
{
    [DefaultExecutionOrder(100)]
    public sealed class RealmAuraView : MonoBehaviour
    {
        private const int MaxWisps = 4;
        private const int MaxPetals = 5;
        private const float CenterAboveFeet = 1.25f;
        private const float WispFps = 10f;

        private RealmProgress realm;
        private Battle battle;
        private bool initialized;

        // 돌파 연출 시간. 062 의 돌파 부품도 이 값을 쓴다.
        private bool playing;
        private float effectStart;
        private RealmLevel effectRealm;
        private RealmLevel previousRealm;
        private RealmLevel shownRealm;
        private bool hasShownRealm;

        // 기본 빛
        private SpriteRenderer auraRenderer;
        private Texture2D fallbackTexture;
        private Sprite fallbackSprite;
        private Sprite[] frames;
        private bool artwork;
        private float animationStart;

        // 기운
        private readonly SpriteRenderer[] wispRenderers = new SpriteRenderer[MaxWisps];
        private Sprite[] wispFrames;
        private Texture2D wispFallbackTexture;
        private Sprite wispFallbackSprite;

        // 꽃잎 (062 의 돌파 꽃잎도 쓴다)
        private readonly SpriteRenderer[] petalRenderers = new SpriteRenderer[MaxPetals];
        private Sprite[] petalSprites;
        private Texture2D petalFallbackTexture;
        private Sprite petalFallbackSprite;

        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        public void Initialize(RealmProgress realm, Battle battle)
        {
            if (realm == null) throw new ArgumentNullException(nameof(realm));
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            if (initialized) throw new InvalidOperationException("RealmAuraView is already initialized.");
            initialized = true;
            this.realm = realm;
            this.battle = battle;

            auraRenderer = CreateRenderer("RealmAura", 9990);
            for (int i = 0; i < MaxWisps; i++)
                wispRenderers[i] = CreateRenderer("AuraWisp" + i, 9991);
            for (int i = 0; i < MaxPetals; i++)
                petalRenderers[i] = CreateRenderer("AuraPetal" + i, 10002);

            LoadWispSprites();
            LoadPetalSprites();

            realm.BrokeThrough += OnBrokeThrough;
            UpdateDisplay();
        }

        private SpriteRenderer CreateRenderer(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = false;
            return renderer;
        }

        // ---------- 그림 읽기 ----------

        private void LoadWispSprites()
        {
            const string path = "Art/Effects/aura_wisp";
            var texture = Resources.Load<Texture2D>(path);
            if (texture != null && (texture.width % 32 != 0 || texture.width == 0 || texture.height != 64))
            {
                Debug.LogWarning("Invalid aura wisp sprite sheet dimensions: " + path);
                texture = null;
            }

            if (texture != null)
            {
                wispFrames = new Sprite[texture.width / 32];
                // PPU 64: 칸 높이가 1 유닛이 되도록 해서 배율 3.0 × Scale 로 높이 3.0 × Scale 유닛을 만든다.
                for (int i = 0; i < wispFrames.Length; i++)
                    wispFrames[i] = Track(Sprite.Create(texture, new Rect(i * 32, 0, 32, 64), new Vector2(.5f, .5f), 64f));
                return;
            }

            wispFallbackTexture = Track(NewTexture(8, 32));
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 8; x++)
            {
                float dx = x + .5f - 4f;
                float a = 1f - Mathf.Abs(dx) / 4f;
                float alpha = Mathf.Clamp01(a * a * Mathf.Sin(Mathf.PI * (y + .5f) / 32f));
                wispFallbackTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            wispFallbackTexture.Apply();
            wispFallbackSprite = Track(Sprite.Create(wispFallbackTexture, new Rect(0, 0, 8, 32), new Vector2(.5f, .5f), 32f));
        }

        private void LoadPetalSprites()
        {
            const string path = "Art/Effects/petal";
            var texture = Resources.Load<Texture2D>(path);
            if (texture != null && (texture.width % 8 != 0 || texture.width == 0 || texture.height != 8))
            {
                Debug.LogWarning("Invalid petal sprite sheet dimensions: " + path);
                texture = null;
            }

            if (texture != null)
            {
                petalSprites = new Sprite[texture.width / 8];
                for (int i = 0; i < petalSprites.Length; i++)
                    petalSprites[i] = Track(Sprite.Create(texture, new Rect(i * 8, 0, 8, 8), new Vector2(.5f, .5f), 32f));
                return;
            }

            petalFallbackTexture = Track(NewTexture(3, 3));
            for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
            {
                bool corner = x != 1 && y != 1;
                petalFallbackTexture.SetPixel(x, y, new Color(1f, 1f, 1f, corner ? 0f : 1f));
            }
            petalFallbackTexture.Apply();
            petalFallbackSprite = Track(Sprite.Create(petalFallbackTexture, new Rect(0, 0, 3, 3), new Vector2(.5f, .5f), 32f));
        }

        private static Texture2D NewTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }

        private T Track<T>(T obj) where T : UnityEngine.Object
        {
            createdObjects.Add(obj);
            return obj;
        }

        // 꽃잎 칸 i (그림이 있으면 i % 칸 수, 없으면 대체 그림).
        private Sprite PetalSprite(int index)
        {
            return petalSprites != null ? petalSprites[index % petalSprites.Length] : petalFallbackSprite;
        }

        // ---------- 기본 빛 ----------

        private void RefreshAura(RealmLevel level)
        {
            ClearFrames();
            artwork = false;
            if (!RealmAura.HasAura(level)) return;

            string path = "Art/Effects/aura_" + RealmText.AssetId(level);
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

        // ---------- 매 프레임 ----------

        private void LateUpdate()
        {
            if (initialized) UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            float now = Time.time;
            float t = now - effectStart;
            if (playing && !BreakthroughEffect.IsPlaying(t)) playing = false;

            RealmLevel level = playing && !BreakthroughEffect.ShowsNewAura(t) ? previousRealm : realm.Current;
            if (!hasShownRealm || level != shownRealm)
            {
                hasShownRealm = true;
                shownRealm = level;
                RefreshAura(level);
            }

            var center = new Vector3((float)battle.DisciplePosition - 2.25f, -1.5f + CenterAboveFeet, 0f);
            bool visible = RealmAura.HasAura(level);
            var style = BreakthroughEffect.Style(level);
            var c = RealmAura.Color(level);

            UpdateBaseLight(level, visible, center, playing, t, now);
            UpdateWisps(level, visible ? style.AuraWisps : 0, center, c, now);
            UpdatePetals(level, visible ? style.AuraPetals : 0, center, c, now);
        }

        private void UpdateBaseLight(RealmLevel level, bool visible, Vector3 center, bool isPlaying, float t, float now)
        {
            auraRenderer.enabled = visible;
            if (!visible) return;

            auraRenderer.transform.position = center;
            float scale = isPlaying && t >= (float)BreakthroughEffect.BurstSeconds
                ? (float)RealmAura.BurstScale(t - BreakthroughEffect.BurstSeconds)
                : 1f;
            if (artwork)
            {
                int frame = (int)((now - animationStart) * 10f) % frames.Length;
                auraRenderer.sprite = frames[frame];
                auraRenderer.color = Color.white;
                auraRenderer.transform.localScale = new Vector3(1.5f * scale, 1.5f * scale, 1f);
            }
            else
            {
                var color = RealmAura.Color(level);
                auraRenderer.color = new Color((float)color.R, (float)color.G, (float)color.B,
                    (float)RealmAura.PulseAlpha(level, now));
                auraRenderer.transform.localScale = new Vector3(1.2f * scale, 1.5f * scale, 1f);
            }
        }

        private void UpdateWisps(RealmLevel level, int count, Vector3 center, AuraColor color, float now)
        {
            Sprite sprite = null;
            if (count > 0 && wispFrames != null)
                sprite = wispFrames[(int)(now * WispFps) % wispFrames.Length];

            for (int i = 0; i < MaxWisps; i++)
            {
                var renderer = wispRenderers[i];
                if (i >= count)
                {
                    renderer.enabled = false;
                    continue;
                }

                var p = RealmAura.Wisp(level, i, now);
                float s = (float)p.Scale;
                renderer.enabled = true;
                renderer.transform.position = center + new Vector3((float)p.X, (float)p.Y, 0f);
                if (wispFrames != null)
                {
                    renderer.sprite = sprite;
                    renderer.color = new Color(1f, 1f, 1f, (float)p.Alpha);
                    renderer.transform.localScale = new Vector3(3f * s, 3f * s, 1f);
                }
                else
                {
                    renderer.sprite = wispFallbackSprite;
                    renderer.color = new Color((float)color.R, (float)color.G, (float)color.B, (float)p.Alpha);
                    // 8×32 픽셀 @ PPU 32 = 0.25 × 1 유닛 → 0.5 × 2.0 × Scale 유닛
                    renderer.transform.localScale = new Vector3(2f, 2f * s, 1f);
                }
            }
        }

        private void UpdatePetals(RealmLevel level, int count, Vector3 center, AuraColor color, float now)
        {
            for (int i = 0; i < MaxPetals; i++)
            {
                var renderer = petalRenderers[i];
                if (i >= count)
                {
                    renderer.enabled = false;
                    continue;
                }

                var p = RealmAura.FloatingPetal(level, i, now);
                float s = 1.5f * (float)p.Scale;
                renderer.enabled = true;
                renderer.sprite = PetalSprite(i);
                renderer.transform.position = center + new Vector3((float)p.X, (float)p.Y, 0f);
                renderer.transform.localScale = new Vector3(s, s, 1f);
                renderer.color = petalSprites != null
                    ? new Color(1f, 1f, 1f, (float)p.Alpha)
                    : new Color((float)color.R, (float)color.G, (float)color.B, (float)p.Alpha);
            }
        }

        private void OnBrokeThrough(RealmLevel level)
        {
            effectStart = Time.time;
            effectRealm = level;
            previousRealm = (RealmLevel)Math.Max(0, (int)level - 1);
            playing = true;
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
            foreach (var obj in createdObjects)
                if (obj != null) Destroy(obj);
            createdObjects.Clear();
        }
    }
}
