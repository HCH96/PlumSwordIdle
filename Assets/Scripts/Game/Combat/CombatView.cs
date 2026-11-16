using System;
using System.Collections.Generic;
using PlumSword.Core.Combat;
using UnityEngine;

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
        private static readonly Color FlashColor = Color.white;

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
            _battle.Tick(deltaTime);
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
            actor.Body.color = FlashColor;
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
