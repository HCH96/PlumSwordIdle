using System;
using System.Collections.Generic;
using PlumSword.Core.Combat;
using PlumSword.Core.Realm;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PlumSword.Game.Realm
{
    [DefaultExecutionOrder(100)]
    public sealed class RealmAuraView : MonoBehaviour
    {
        private const int MaxWisps = 4;
        private const int MaxPetals = 5;
        private const int MaxBurstPetals = 36;
        private const float CenterAboveFeet = 1.25f;
        private const float WispFps = 10f;
        private const float FeetOffsetX = -2.25f;
        private const float FeetY = -1.5f;
        private const float PartsPpu = 32f;

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

        // 돌파 연출 부품 (062)
        private SpriteRenderer dimRenderer;
        private SpriteRenderer ringRenderer;
        private SpriteRenderer pillarRenderer;
        private SpriteRenderer shockwaveRenderer;
        private SpriteRenderer flashRenderer;
        private readonly SpriteRenderer[] burstPetalRenderers = new SpriteRenderer[MaxBurstPetals];
        private Sprite ringFallbackSprite;
        private Sprite pillarFallbackSprite;
        private Sprite[] ringFrames;
        private Sprite[] pillarFrames;
        private Sprite[] shockwaveFrames;
        private Light2D pointLight;
        private Light2D ownGlobalLight;
        private Bloom bloom;
        private bool partsShown;
        private UniversalAdditionalCameraData savedCameraData;
        private bool savedPostProcessing;
        private bool hasSavedPostProcessing;

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
            CreateBreakthroughParts();

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
            if (!initialized) return;
            UpdateDisplay();
            ApplyShake();
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

            var feet = new Vector3((float)battle.DisciplePosition + FeetOffsetX, FeetY, 0f);
            UpdateBreakthroughParts(center, feet, t);
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

        // ---------- 돌파 연출 부품 (062) ----------

        private void CreateBreakthroughParts()
        {
            dimRenderer = CreateRenderer("BreakthroughDim", 9980);
            ringRenderer = CreateRenderer("BreakthroughRing", 9992);
            pillarRenderer = CreateRenderer("BreakthroughPillar", 9993);
            shockwaveRenderer = CreateRenderer("BreakthroughShockwave", 9994);
            for (int i = 0; i < MaxBurstPetals; i++)
                burstPetalRenderers[i] = CreateRenderer("BurstPetal" + i, 10003);
            flashRenderer = CreateRenderer("BreakthroughFlash", 29990);

            // 어둡게: 128×64, PPU 2 (1 픽셀 = 0.5 유닛), Bilinear.
            var dimTexture = Track(NewTexture(128, 64));
            dimTexture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 128; x++)
            {
                double dx = (x + .5 - 64.0) * .5;
                double dy = (y + .5 - 32.0) * .5;
                float alpha = (float)BreakthroughEffect.DimMask(Math.Sqrt(dx * dx + dy * dy));
                dimTexture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
            dimTexture.Apply();
            dimRenderer.sprite = Track(Sprite.Create(dimTexture, new Rect(0, 0, 128, 64), new Vector2(.5f, .5f), 2f));

            // 고리·충격파 대체: 64×16 타원 테두리.
            var ringTexture = Track(NewTexture(64, 16));
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 64; x++)
            {
                float u = (x + .5f - 32f) / 32f;
                float v = (y + .5f - 8f) / 8f;
                float d = Mathf.Sqrt(u * u + v * v);
                float alpha = Mathf.Max(0f, 1f - Mathf.Abs(d - .85f) / .15f);
                ringTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            ringTexture.Apply();
            ringFallbackSprite = Track(Sprite.Create(ringTexture, new Rect(0, 0, 64, 16), new Vector2(.5f, .5f), PartsPpu));

            // 기둥 대체: 16×64, 피벗 아래 가운데.
            var pillarTexture = Track(NewTexture(16, 64));
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 16; x++)
            {
                float dx = x + .5f - 8f;
                float a = 1f - Mathf.Abs(dx) / 8f;
                float alpha = Mathf.Clamp01(a * a * Mathf.Min(1f, (63.5f - y) / 16f));
                pillarTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            pillarTexture.Apply();
            pillarFallbackSprite = Track(Sprite.Create(pillarTexture, new Rect(0, 0, 16, 64), new Vector2(.5f, 0f), PartsPpu));

            // 번쩍임: 1×1 흰색 (1 유닛).
            var flashTexture = Track(NewTexture(1, 1));
            flashTexture.SetPixel(0, 0, Color.white);
            flashTexture.Apply();
            flashRenderer.sprite = Track(Sprite.Create(flashTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f));
            flashRenderer.transform.localScale = new Vector3(30f, 12f, 1f);

            ringFrames = LoadPartSheet("Art/Effects/breakthrough_ring", 160, 48, new Vector2(.5f, .5f));
            pillarFrames = LoadPartSheet("Art/Effects/breakthrough_pillar", 64, 192, new Vector2(.5f, 0f));
            shockwaveFrames = LoadPartSheet("Art/Effects/breakthrough_shockwave", 192, 64, new Vector2(.5f, .5f));

            CreateLights();
            CreateBloom();
        }

        private Sprite[] LoadPartSheet(string path, int frameWidth, int frameHeight, Vector2 pivot)
        {
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            if (texture.width == 0 || texture.width % frameWidth != 0 || texture.height != frameHeight)
            {
                Debug.LogWarning("Invalid breakthrough sprite sheet dimensions: " + path);
                return null;
            }

            var sheet = new Sprite[texture.width / frameWidth];
            for (int i = 0; i < sheet.Length; i++)
                sheet[i] = Track(Sprite.Create(texture, new Rect(i * frameWidth, 0, frameWidth, frameHeight), pivot, PartsPpu));
            return sheet;
        }

        private void CreateLights()
        {
            var layers = SortingLayer.layers;
            var layerIds = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++) layerIds[i] = layers[i].id;

            var pointObject = new GameObject("BreakthroughLight");
            pointObject.transform.SetParent(transform, false);
            pointLight = pointObject.AddComponent<Light2D>();
            pointLight.lightType = Light2D.LightType.Point;
            pointLight.pointLightInnerRadius = .5f;
            pointLight.pointLightOuterRadius = 5f;
            pointLight.color = Color.white;
            pointLight.intensity = 0f;
            pointLight.targetSortingLayers = layerIds;
            pointLight.enabled = false;

            if (OtherGlobalLightExists()) return;

            // 꺼 둔 채로 전역으로 바꾼다 (켜질 때만 전역 광원 중복 검사를 하므로).
            // [Game] 은 씬을 넘어 살아 있어서, 켜 두면 나중에 불러온 씬의 전역 광원과 겹쳐 URP 가 오류를 남긴다.
            // 그래서 점광원이 켜지는 연출 중에만, 그때도 다른 전역 광원이 없을 때만 켠다.
            var globalObject = new GameObject("GlobalLight2D");
            globalObject.transform.SetParent(transform, false);
            ownGlobalLight = globalObject.AddComponent<Light2D>();
            ownGlobalLight.enabled = false;
            ownGlobalLight.lightType = Light2D.LightType.Global;
            ownGlobalLight.color = Color.white;
            ownGlobalLight.intensity = 1f;
            ownGlobalLight.targetSortingLayers = layerIds;
        }

        private bool OtherGlobalLightExists()
        {
            foreach (var light in FindObjectsByType<Light2D>())
                if (light != ownGlobalLight && light.isActiveAndEnabled && light.lightType == Light2D.LightType.Global)
                    return true;
            return false;
        }

        private void CreateBloom()
        {
            var volumeObject = new GameObject("BreakthroughBloom");
            volumeObject.transform.SetParent(transform, false);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.weight = 1f;

            var profile = Track(ScriptableObject.CreateInstance<VolumeProfile>());
            bloom = Track(profile.Add<Bloom>());
            bloom.threshold.Override(.8f);
            bloom.scatter.Override(.7f);
            bloom.intensity.Override(0f);
            volume.sharedProfile = profile;
        }

        private void UpdateBreakthroughParts(Vector3 center, Vector3 feet, float t)
        {
            if (!playing)
            {
                if (partsShown) HideBreakthroughParts();
                return;
            }
            partsShown = true;

            RealmLevel r = effectRealm;
            var style = BreakthroughEffect.Style(r);
            var color = RealmAura.Color(r);
            var burstColor = RealmAura.BurstColor(r);

            dimRenderer.enabled = true;
            dimRenderer.transform.position = center;
            dimRenderer.color = new Color(0f, 0f, 0f, (float)BreakthroughEffect.DimAlpha(r, t));

            pointLight.enabled = true;
            pointLight.transform.position = center;
            pointLight.color = new Color((float)color.R, (float)color.G, (float)color.B, 1f);
            pointLight.intensity = (float)BreakthroughEffect.LightIntensity(r, t);

            bloom.intensity.value = (float)BreakthroughEffect.BloomIntensity(r, t);

            float fade = (float)BreakthroughEffect.BurstFade(t);
            float ringRadius = (float)BreakthroughEffect.RingRadius(r, t);
            UpdateSheetPart(ringRenderer, style.HasRing, ringFrames, ringFallbackSprite, feet,
                2f * ringRadius, .6f * ringRadius, false, fade, (t - .5f) / .8f, burstColor);

            UpdateSheetPart(pillarRenderer, style.HasPillar, pillarFrames, pillarFallbackSprite, feet,
                (float)BreakthroughEffect.PillarWidthAt(t), (float)style.PillarHeight, true, fade, (t - .5f) / .8f, burstColor);

            float shockRadius = (float)BreakthroughEffect.ShockwaveRadius(r, t);
            UpdateSheetPart(shockwaveRenderer, style.HasShockwave, shockwaveFrames, ringFallbackSprite, feet,
                2f * shockRadius, .6f * shockRadius, false, (float)BreakthroughEffect.ShockwaveAlpha(r, t),
                (t - .5f) / .4f, burstColor);

            UpdateBurstPetals(r, style.BurstPetals, center, color, t);

            var cam = Camera.main;
            flashRenderer.enabled = style.HasFlash && cam != null;
            if (flashRenderer.enabled)
            {
                var p = cam.transform.position;
                flashRenderer.transform.position = new Vector3(p.x, p.y, 0f);
                flashRenderer.color = new Color(1f, 1f, 1f, (float)BreakthroughEffect.FlashAlpha(r, t));
            }
        }

        // 고리·기둥·충격파. width 가 0 이면 숨긴다. 그림은 fitHeight 에 따라 세로 또는 가로를 맞추고 비율 유지, 흰색.
        private static void UpdateSheetPart(SpriteRenderer renderer, bool styleEnabled, Sprite[] sheet, Sprite fallback,
            Vector3 position, float width, float height, bool fitHeight, float alpha, float progress, AuraColor color)
        {
            if (!styleEnabled || width <= 0f || height <= 0f)
            {
                renderer.enabled = false;
                return;
            }

            renderer.enabled = true;
            renderer.transform.position = position;
            if (sheet != null)
            {
                int frame = Mathf.Clamp(Mathf.FloorToInt(progress * sheet.Length), 0, sheet.Length - 1);
                var sprite = sheet[frame];
                renderer.sprite = sprite;
                Vector2 size = sprite.rect.size / sprite.pixelsPerUnit;
                float s = fitHeight ? height / size.y : width / size.x;
                renderer.transform.localScale = new Vector3(s, s, 1f);
                renderer.color = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                renderer.sprite = fallback;
                Vector2 size = fallback.rect.size / fallback.pixelsPerUnit;
                renderer.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
                renderer.color = new Color((float)color.R, (float)color.G, (float)color.B, alpha);
            }
        }

        private void UpdateBurstPetals(RealmLevel r, int count, Vector3 center, AuraColor color, float t)
        {
            for (int i = 0; i < MaxBurstPetals; i++)
            {
                var renderer = burstPetalRenderers[i];
                if (i >= count)
                {
                    renderer.enabled = false;
                    continue;
                }

                var p = BreakthroughEffect.BurstPetal(r, i, t);
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

        private void HideBreakthroughParts()
        {
            partsShown = false;
            dimRenderer.enabled = false;
            ringRenderer.enabled = false;
            pillarRenderer.enabled = false;
            shockwaveRenderer.enabled = false;
            flashRenderer.enabled = false;
            foreach (var renderer in burstPetalRenderers) renderer.enabled = false;
            pointLight.intensity = 0f;
            pointLight.enabled = false;
            if (ownGlobalLight != null) ownGlobalLight.enabled = false;
            bloom.intensity.value = 0f;
            RestorePostProcessing();
        }

        // 처음 연출이 시작될 때의 값을 기억한다. 연출 중 다시 돌파해도 기억한 값은 그대로다.
        private void BeginPostProcessing()
        {
            if (!hasSavedPostProcessing)
            {
                var cam = Camera.main;
                if (cam == null || !cam.TryGetComponent(out UniversalAdditionalCameraData data)) return;
                savedCameraData = data;
                savedPostProcessing = data.renderPostProcessing;
                hasSavedPostProcessing = true;
            }
            if (savedCameraData != null) savedCameraData.renderPostProcessing = true;
        }

        private void RestorePostProcessing()
        {
            if (hasSavedPostProcessing && savedCameraData != null)
                savedCameraData.renderPostProcessing = savedPostProcessing;
            hasSavedPostProcessing = false;
            savedCameraData = null;
        }

        private void ApplyShake()
        {
            if (!playing) return;
            int pixels = BreakthroughEffect.ShakePixels(effectRealm, Time.time - effectStart);
            if (pixels <= 0) return;
            var cam = Camera.main;
            if (cam == null) return;
            float unit = pixels / 32f;
            cam.transform.position += new Vector3(UnityEngine.Random.Range(-1, 2) * unit, UnityEngine.Random.Range(-1, 2) * unit, 0f);
        }

        private void OnBrokeThrough(RealmLevel level)
        {
            effectStart = Time.time;
            effectRealm = level;
            previousRealm = (RealmLevel)Math.Max(0, (int)level - 1);
            playing = true;
            BeginPostProcessing();
            if (ownGlobalLight != null && !ownGlobalLight.enabled && !OtherGlobalLightExists())
                ownGlobalLight.enabled = true;
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
            RestorePostProcessing();
            ClearFrames();
            if (fallbackSprite != null) Destroy(fallbackSprite);
            if (fallbackTexture != null) Destroy(fallbackTexture);
            foreach (var obj in createdObjects)
                if (obj != null) Destroy(obj);
            createdObjects.Clear();
        }
    }
}
