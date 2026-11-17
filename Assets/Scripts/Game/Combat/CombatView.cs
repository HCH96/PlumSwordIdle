using System;
using System.Collections.Generic;
using PlumSword.Core.Combat;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PlumSword.Game.Combat
{
    /// <summary>
    /// 화면 위쪽의 자동 전투 필드. 월드 공간에 SpriteRenderer 로 대체 표시(단색 사각형)를 그린다.
    /// 매 프레임 Battle.Tick(Time.deltaTime) 을 부르는 유일한 곳이고, 그 뒤 Battle 의 상태를 읽어 그리기만 한다.
    /// 사양: docs/spec/combat.md "화면".
    /// </summary>
    public sealed class CombatView : MonoBehaviour
    {
        // 아래 숫자는 모두 임시값 (docs/spec/combat.md "화면").
        private const float CameraSize = 5.625f;                        // docs/dev/art.md "기본 숫자"
        private const int ReferenceWidth = 640;                          // docs/dev/art.md "기본 숫자"
        private const int ReferenceHeight = 360;
        private static readonly Color SkyColor = new Color(0.55f, 0.75f, 0.90f);
        private const float CameraOffsetX = 4.0f;
        private const float CameraZ = -10f;

        private static readonly Vector2 DiscipleSize = new Vector2(1.0f, 1.0f);
        private static readonly Color DiscipleColor = new Color(0.25f, 0.50f, 0.90f);
        private const int DiscipleOrder = 1;

        private static readonly Vector2 NormalSize = new Vector2(1.0f, 1.0f);
        private static readonly Color NormalColor = new Color(0.80f, 0.45f, 0.20f);
        private static readonly Vector2 BossSize = new Vector2(2.0f, 3.0f);
        private static readonly Color BossColor = new Color(0.55f, 0.20f, 0.60f);
        private const int EnemyOrder = 0;

        private const float HealthBarWidth = 1.0f;
        private const float HealthBarHeight = 0.12f;
        private const float HealthBarGap = 0.2f;                         // 머리 위 간격
        private static readonly Color HealthBarBackColor = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color DiscipleHealthColor = new Color(0.20f, 0.80f, 0.30f);
        private static readonly Color EnemyHealthColor = new Color(0.85f, 0.20f, 0.20f);
        private const int HealthBarBackOrder = 5;
        private const int HealthBarFillOrder = 6;

        private const float FlashSeconds = 0.1f;

        private static readonly Vector2 GroundSize = new Vector2(40f, 6f);
        private static readonly Color GroundColor = new Color(0.35f, 0.30f, 0.25f);
        private const int GroundOrder = -10;

        private const int TreeCount = 8;
        private const double TreeSpacing = 5.0;
        private const int TreeFirstOffset = -3;
        private static readonly Vector2 TreeSize = new Vector2(1.0f, 2.5f);
        private static readonly Color TreeColor = new Color(0.20f, 0.45f, 0.25f);
        private const int TreeOrder = -5;

        private Battle _battle;
        private Camera _camera;
        private Texture2D _texture;
        private Sprite _sprite;

        private CombatActor _disciple;
        private readonly Dictionary<Enemy, CombatActor> _enemies = new Dictionary<Enemy, CombatActor>();
        private Transform _ground;
        private Transform[] _trees;

        // 제자 스프라이트. 불러오지 못하면 null 이고 단색 사각형을 쓴다.
        private static readonly Color DiscipleSpriteFlashColor = new Color(1.0f, 0.45f, 0.45f);
        private DiscipleSprites _discipleSprites;
        private bool _discipleAttackedThisTick;
        private readonly DiscipleAnimator _discipleAnimator = new DiscipleAnimator();

        /// <summary>
        /// 카메라를 설정하고 제자·배경 오브젝트를 만든다. 이미 battle.Enemies 에 있는 적의 오브젝트도 만든다.
        /// battle 의 알림을 구독하고 OnDestroy 에서 끊는다. 두 번 부르면 InvalidOperationException.
        /// </summary>
        public void Initialize(Battle battle, Camera camera)
        {
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            if (_battle != null) throw new InvalidOperationException("CombatView 는 한 번만 Initialize 할 수 있다.");

            _battle = battle;
            _camera = camera;

            _camera.orthographic = true;
            _camera.orthographicSize = CameraSize;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = SkyColor;
            ApplyPixelPerfect(_camera);

            _texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "CombatWhite",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            _texture.SetPixel(0, 0, Color.white);
            _texture.Apply();
            _sprite = Sprite.Create(_texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            _sprite.name = "CombatWhite";

            CreateBackground();
            _disciple = CreateActor("[Disciple]", DiscipleSize, DiscipleColor, DiscipleOrder, DiscipleHealthColor);
            ApplyDiscipleSprites();

            foreach (var enemy in _battle.Enemies)
            {
                OnEnemyAdded(enemy);
            }

            _battle.EnemyAdded += OnEnemyAdded;
            _battle.EnemyDamaged += OnEnemyDamaged;
            _battle.DiscipleDamaged += OnDiscipleDamaged;
            _battle.EnemyDefeated += OnEnemyDefeated;
            _battle.ResetCompleted += OnResetCompleted;

            Refresh(0f);
        }

        private void Update()
        {
            if (_battle == null) return;

            float deltaTime = Time.deltaTime;
            double previousPosition = _battle.DisciplePosition;
            _discipleAttackedThisTick = false;
            _battle.Tick(deltaTime);
            AnimateDisciple(deltaTime, previousPosition);
            Refresh(deltaTime);
        }

        private void OnDestroy()
        {
            if (_battle != null)
            {
                _battle.EnemyAdded -= OnEnemyAdded;
                _battle.EnemyDamaged -= OnEnemyDamaged;
                _battle.DiscipleDamaged -= OnDiscipleDamaged;
                _battle.EnemyDefeated -= OnEnemyDefeated;
                _battle.ResetCompleted -= OnResetCompleted;
            }

            if (_sprite != null) Destroy(_sprite);
            if (_texture != null) Destroy(_texture);
            _sprite = null;
            _texture = null;

            _discipleSprites?.Dispose();
            _discipleSprites = null;
        }

        // ---- 제자 스프라이트 (없으면 단색 사각형 대체 표시 그대로) ----

        /// <summary>제자 몸을 스프라이트로 바꿨으면 true. 테스트와 사람 확인용.</summary>
        internal bool UsesDiscipleSprites => _discipleSprites != null;

        private void ApplyDiscipleSprites()
        {
            _discipleSprites = DiscipleSprites.TryLoad();
            if (_discipleSprites == null) return;

            // 스프라이트 피벗이 발 아래라 몸은 Root 자리에 크기 1 로 둔다.
            var body = _disciple.Body;
            body.sprite = _discipleSprites.Get(DiscipleMotion.Idle, 0);
            body.transform.localScale = Vector3.one;
            body.transform.localPosition = Vector3.zero;
            _disciple.BodyColor = Color.white;
            _disciple.FlashColor = DiscipleSpriteFlashColor;
            body.color = Color.white;

            // 체력 바는 동작마다 움직이지 않게 대기 그림의 머리 높이 기준으로 고정한다.
            _disciple.HealthBarY = DiscipleSprites.HeadHeight + HealthBarGap;
            _disciple.HealthBack.localPosition = new Vector3(0f, _disciple.HealthBarY, 0f);
        }

        private void AnimateDisciple(float deltaTime, double previousPosition)
        {
            if (_discipleSprites == null) return;

            bool moved = _battle.DisciplePosition - previousPosition > DiscipleAnimator.MoveThreshold;
            // EnemyDamaged 는 제자의 공격만 보낸다. (대기 시간이 늘어난 것으로는 가릴 수 없다: 달려와 멈춘 칸에도 앞 동작 대기로 늘어난다.)
            bool attacked = _discipleAttackedThisTick;
            bool engaged = !_battle.Paused && !_battle.IsDiscipleDefeated && HasEnemyInDiscipleRange();

            _discipleAnimator.Update(deltaTime, moved, attacked, engaged,
                _battle.DiscipleAttackCooldown, _battle.DiscipleAttackInterval);
            _disciple.Body.sprite = _discipleSprites.Get(_discipleAnimator.Motion, _discipleAnimator.Frame);
        }

        private bool HasEnemyInDiscipleRange()
        {
            foreach (var enemy in _battle.Enemies)
            {
                if (enemy.Position - _battle.DisciplePosition <= Battle.DiscipleAttackRange + Battle.Epsilon)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// URP 2D 의 Pixel Perfect Camera 를 붙인다 (docs/dev/art.md "기본 숫자"): 기준 640×360, PPU 32.
        /// 화면 높이에 맞는 정수배로만 확대하고(카메라 크기는 이 컴포넌트가 정한다), 카메라와 스프라이트를
        /// 픽셀 격자에 맞춰 움직여 소수 좌표 때문에 픽셀이 떨려 보이지 않게 한다. 남는 폭·높이는 잘라내지 않고 더 보여 준다.
        /// </summary>
        private static void ApplyPixelPerfect(Camera camera)
        {
            var pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
            if (pixelPerfect == null) pixelPerfect = camera.gameObject.AddComponent<PixelPerfectCamera>();

            pixelPerfect.assetsPPU = (int)DiscipleSprites.PixelsPerUnit;
            pixelPerfect.refResolutionX = ReferenceWidth;
            pixelPerfect.refResolutionY = ReferenceHeight;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.None;
        }

        // ---- 알림 ----

        private void OnEnemyAdded(Enemy enemy)
        {
            if (enemy == null || _enemies.ContainsKey(enemy)) return;

            CombatActor actor = enemy.Kind == EnemyKind.Boss
                ? CreateActor("[Boss]", BossSize, BossColor, EnemyOrder, null)
                : CreateActor("[Enemy]", NormalSize, NormalColor, EnemyOrder, EnemyHealthColor);
            _enemies.Add(enemy, actor);
            UpdateEnemy(enemy, actor);
        }

        private void OnEnemyDamaged(Enemy enemy)
        {
            _discipleAttackedThisTick = true;
            if (enemy != null && _enemies.TryGetValue(enemy, out var actor))
            {
                StartFlash(actor);
            }
        }

        private void OnDiscipleDamaged()
        {
            StartFlash(_disciple);
        }

        private void OnEnemyDefeated(Enemy enemy)
        {
            if (enemy == null || !_enemies.TryGetValue(enemy, out var actor)) return;

            _enemies.Remove(enemy);
            Destroy(actor.Root);
        }

        private void OnResetCompleted()
        {
            foreach (var actor in _enemies.Values)
            {
                Destroy(actor.Root);
            }
            _enemies.Clear();

            _disciple.FlashRemaining = 0f;
            _disciple.Body.color = _disciple.BodyColor;

            if (_discipleSprites != null)
            {
                _discipleAnimator.Reset();
                _disciple.Body.sprite = _discipleSprites.Get(_discipleAnimator.Motion, _discipleAnimator.Frame);
            }
        }

        // ---- 매 프레임 맞추기 ----

        private void Refresh(float deltaTime)
        {
            // 제자
            _disciple.Root.transform.localPosition = new Vector3((float)_battle.DisciplePosition, 0f, 0f);
            SetHealth(_disciple, _battle.DiscipleHealth, _battle.DiscipleMaxHealth);
            UpdateFlash(_disciple, deltaTime);

            // 적
            foreach (var pair in _enemies)
            {
                UpdateEnemy(pair.Key, pair.Value);
                UpdateFlash(pair.Value, deltaTime);
            }

            // 카메라
            double cameraX = _battle.DisciplePosition + CameraOffsetX;
            _camera.transform.position = new Vector3((float)cameraX, 0f, CameraZ);

            // 배경
            _ground.localPosition = new Vector3((float)cameraX, -GroundSize.y * 0.5f, 0f);
            double firstSlot = Math.Floor(cameraX / TreeSpacing) + TreeFirstOffset;
            for (int i = 0; i < _trees.Length; i++)
            {
                float x = (float)((firstSlot + i) * TreeSpacing);
                _trees[i].localPosition = new Vector3(x, TreeSize.y * 0.5f, 0f);
            }
        }

        private void UpdateEnemy(Enemy enemy, CombatActor actor)
        {
            float y = enemy.Kind == EnemyKind.Boss ? 0f : (float)CombatLayout.EnemyOffsetY(enemy.Index);
            actor.Root.transform.localPosition = new Vector3((float)enemy.Position, y, 0f);
            SetHealth(actor, enemy.Health, enemy.MaxHealth);
        }

        private static void SetHealth(CombatActor actor, long health, long maxHealth)
        {
            if (actor.HealthFill == null) return;

            float width = HealthBarWidth * (float)CombatLayout.HealthRatio(health, maxHealth);
            actor.HealthFill.localScale = new Vector3(width, HealthBarHeight, 1f);
            // 왼쪽 끝을 바탕의 왼쪽 끝에 고정한다.
            actor.HealthFill.localPosition = new Vector3(-HealthBarWidth * 0.5f + width * 0.5f, actor.HealthBarY, 0f);
        }

        private static void StartFlash(CombatActor actor)
        {
            actor.FlashRemaining = FlashSeconds;
            actor.Body.color = actor.FlashColor;
        }

        private static void UpdateFlash(CombatActor actor, float deltaTime)
        {
            if (actor.FlashRemaining <= 0f) return;

            actor.FlashRemaining -= deltaTime;
            if (actor.FlashRemaining <= 0f)
            {
                actor.FlashRemaining = 0f;
                actor.Body.color = actor.BodyColor;
            }
        }

        // ---- 오브젝트 만들기 ----

        private void CreateBackground()
        {
            _ground = CreateRect("[Ground]", transform, GroundSize, GroundColor, GroundOrder).transform;

            _trees = new Transform[TreeCount];
            for (int i = 0; i < TreeCount; i++)
            {
                _trees[i] = CreateRect("[Tree]", transform, TreeSize, TreeColor, TreeOrder).transform;
            }
        }

        /// <summary>
        /// 크기 없는 Root(발 아래 가운데) 아래에 몸 사각형과, healthColor 가 있으면 체력 바(바탕·채움)를 형제로 만든다.
        /// </summary>
        private CombatActor CreateActor(string name, Vector2 size, Color color, int order, Color? healthColor)
        {
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);

            var body = CreateRect("Body", root.transform, size, color, order);
            body.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);

            var actor = new CombatActor
            {
                Root = root,
                Body = body,
                BodyColor = color,
                HealthBarY = size.y + HealthBarGap,
            };

            if (healthColor.HasValue)
            {
                var back = CreateRect("HealthBack", root.transform,
                    new Vector2(HealthBarWidth, HealthBarHeight), HealthBarBackColor, HealthBarBackOrder);
                back.transform.localPosition = new Vector3(0f, actor.HealthBarY, 0f);
                actor.HealthBack = back.transform;

                var fill = CreateRect("HealthFill", root.transform,
                    new Vector2(HealthBarWidth, HealthBarHeight), healthColor.Value, HealthBarFillOrder);
                actor.HealthFill = fill.transform;
            }

            return actor;
        }

        private SpriteRenderer CreateRect(string name, Transform parent, Vector2 size, Color color, int order)
        {
            var rectObject = new GameObject(name);
            rectObject.transform.SetParent(parent, false);
            rectObject.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = rectObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
