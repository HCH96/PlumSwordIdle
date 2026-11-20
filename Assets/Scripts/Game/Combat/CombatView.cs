using System;
using System.Collections.Generic;
using System.Linq;
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

        private const float ActorDisplayScale = 1.5f;
        private static readonly Vector2 DiscipleSize = new Vector2(1.5f, 1.5f);
        private static readonly Color DiscipleColor = new Color(0.25f, 0.50f, 0.90f);
        private const int DiscipleOrder = 10000;
        // 연무장 시안 위에서 제자와 적을 함께 배치하는 화면 전용 오프셋.
        private static readonly Vector2 ActorDisplayOffset = new Vector2(-2.25f, -1.5f);
        private const float EnemyDisplaySeparation = 0.4f;

        private static readonly Vector2 NormalSize = new Vector2(1.5f, 1.5f);
        private static readonly Color NormalColor = new Color(0.80f, 0.45f, 0.20f);
        private static readonly Vector2 BossSize = new Vector2(3.0f, 4.5f);
        private static readonly Color BossColor = new Color(0.55f, 0.20f, 0.60f);
        private const int EnemyOrder = 10;

        private const float HealthBarWidth = 1f;
        private const float HealthBarHeight = 0.125f;
        private const float HealthBarFillCenterX = 0f;
        private const float HealthAnimationSeconds = 0.3f;
        private const float HealthBarGap = 0.2f;                         // 머리 위 간격
        private static readonly Color DiscipleHealthColor = new Color(0.43f, 0.78f, 0.25f);
        private static readonly Color EnemyHealthColor = new Color(0.88f, 0.20f, 0.20f);
        private const int HealthBarFillOrder = 30001;

        private const float FlashSeconds = 0.1f;
        private static readonly Color SkillSlashColor = new Color(1f, 0.55f, 0.75f, 0.6f);

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
        private Texture2D _discipleHealthFrameTexture;
        private Texture2D _enemyHealthFrameTexture;
        private Sprite _discipleHealthFrame;
        private Sprite _enemyHealthFrame;
        private Texture2D _bossProjectileTexture;
        private Sprite _bossProjectileSprite;
        private readonly Dictionary<BossProjectile, GameObject> _bossProjectiles = new Dictionary<BossProjectile, GameObject>();

        private CombatActor _disciple;
        private readonly Dictionary<Enemy, CombatActor> _enemies = new Dictionary<Enemy, CombatActor>();
        private readonly List<CombatActor> _defeatedEnemies = new List<CombatActor>();
        private Transform _ground;
        private Transform[] _trees;
        // 사용자가 플레이하며 검토하는 임시 1스테이지 배경. 파일이 없으면 기존 대체 표시를 쓴다.
        private Transform[] _previewBackground;
        private float _previewWidth;
        private GameRoot _gameRoot;
        private GameObject _petals;
        private SpriteRenderer _skillSlash;
        private double _skillSlashElapsed;
        private Sprite[] _damageDigits;
        private readonly List<(GameObject Root, float Elapsed)> _damageNumbers = new List<(GameObject, float)>();

        // 제자 스프라이트. 불러오지 못하면 null 이고 단색 사각형을 쓴다.
        private static readonly Color DiscipleSpriteFlashColor = new Color(1.0f, 0.45f, 0.45f);
        private DiscipleSprites _discipleSprites;
        private JongnamSprites _jongnamMale;
        private JongnamSprites _jongnamFemale;
        private JongnamSprites _jongnamBoss;
        private bool _discipleAttackedThisTick;
        private float _discipleDownElapsed;
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
            _gameRoot = GetComponentInParent<GameRoot>();

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
            _discipleHealthFrame = Resources.Load<Sprite>("Art/UI/health_frame_disciple");
            _enemyHealthFrame = Resources.Load<Sprite>("Art/UI/health_frame_enemy");
            if (_discipleHealthFrame == null) _discipleHealthFrame = CreateHealthFrame(true, out _discipleHealthFrameTexture);
            if (_enemyHealthFrame == null) _enemyHealthFrame = CreateHealthFrame(false, out _enemyHealthFrameTexture);
            CreateBossProjectileSprite();
            CreateDamageDigits();

            CreateBackground();
            CreatePetals();
            _jongnamMale = JongnamSprites.TryLoad(false);
            _jongnamFemale = JongnamSprites.TryLoad(true);
            _jongnamBoss = JongnamSprites.TryLoadBoss();
            _disciple = CreateActor("[Disciple]", DiscipleSize, DiscipleColor, DiscipleOrder, DiscipleHealthColor);
            _disciple.DisplayedHealth = _battle.DiscipleHealth;
            _disciple.HealthRatioInitialized = false;
            SetHealth(_disciple, _battle.DiscipleHealth, _battle.DiscipleMaxHealth, 0f);
            ApplyDiscipleSprites();
            _skillSlash = CreateRect("[SkillSlash]", transform, new Vector2(2f, 0.4f), SkillSlashColor, 20000);
            _skillSlash.enabled = false;

            foreach (var enemy in _battle.Enemies)
            {
                OnEnemyAdded(enemy);
            }

            _battle.EnemyAdded += OnEnemyAdded;
            _battle.EnemyDamaged += OnEnemyDamaged;
            _battle.DiscipleDamaged += OnDiscipleDamaged;
            _battle.EnemyDefeated += OnEnemyDefeated;
            _battle.ResetCompleted += OnResetCompleted;
            _battle.SkillUsed += OnSkillUsed;

            Refresh(0f);
        }

        private void Update()
        {
            if (_battle == null) return;

            float deltaTime = Time.deltaTime;
            if (!_battle.Paused) UpdateDamageNumbers(deltaTime);
            _disciple.DisplayedHealth = _battle.DiscipleHealth;
            foreach (var pair in _enemies) pair.Value.DisplayedHealth = pair.Key.Health;
            double previousPosition = _battle.DisciplePosition;
            _discipleAttackedThisTick = false;
            _battle.Tick(deltaTime);
            UpdateSkillSlash(deltaTime);
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
                _battle.SkillUsed -= OnSkillUsed;
            }

            if (_sprite != null) Destroy(_sprite);
            if (_texture != null) Destroy(_texture);
            if (_discipleHealthFrameTexture != null && _discipleHealthFrame != null) Destroy(_discipleHealthFrame);
            if (_enemyHealthFrameTexture != null && _enemyHealthFrame != null) Destroy(_enemyHealthFrame);
            if (_discipleHealthFrameTexture != null) Destroy(_discipleHealthFrameTexture);
            if (_enemyHealthFrameTexture != null) Destroy(_enemyHealthFrameTexture);
            if (_bossProjectileSprite != null) Destroy(_bossProjectileSprite);
            if (_bossProjectileTexture != null) Destroy(_bossProjectileTexture);
            if (_damageDigits != null) foreach (var digit in _damageDigits) if (digit != null) Destroy(digit);
            _sprite = null;
            _texture = null;

            _discipleSprites?.Dispose();
            _discipleSprites = null;
            _jongnamMale?.Dispose();
            _jongnamFemale?.Dispose();
            _jongnamBoss?.Dispose();
            _jongnamMale = null;
            _jongnamFemale = null;
            _jongnamBoss = null;
            if (_petals != null) Destroy(_petals);
        }

        // ---- 제자 스프라이트 (없으면 단색 사각형 대체 표시 그대로) ----

        /// <summary>제자 몸을 스프라이트로 바꿨으면 true. 테스트와 사람 확인용.</summary>
        internal bool UsesDiscipleSprites => _discipleSprites != null;

        private void ApplyDiscipleSprites()
        {
            _discipleSprites = DiscipleSprites.TryLoad();
            if (_discipleSprites == null) return;

            // 발 피벗은 유지하면서 표시 크기만 임시로 1.5배로 한다.
            var body = _disciple.Body;
            body.sprite = _discipleSprites.Get(DiscipleMotion.Idle, 0);
            body.transform.localScale = new Vector3(ActorDisplayScale, ActorDisplayScale, 1f);
            body.transform.localPosition = Vector3.zero;
            _disciple.BodyColor = Color.white;
            _disciple.FlashColor = DiscipleSpriteFlashColor;
            body.color = Color.white;

            // 체력 바는 동작마다 움직이지 않게 대기 그림의 머리 높이 기준으로 고정한다.
            _disciple.HealthBarY = DiscipleSprites.HeadHeight * ActorDisplayScale + HealthBarGap;
            _disciple.HealthBack.localPosition = new Vector3(0f, _disciple.HealthBarY, 0f);
        }

        private void AnimateDisciple(float deltaTime, double previousPosition)
        {
            if (_discipleSprites == null) return;

            if (_battle.IsDiscipleDefeated)
            {
                int frame = Mathf.Min((int)(_discipleDownElapsed / DiscipleSprites.DownFrameSeconds), DiscipleSprites.DownFrameCount - 1);
                _disciple.Body.sprite = _discipleSprites.GetDown(frame);
                _discipleDownElapsed += deltaTime;
                return;
            }

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
            // 프레임마다 스프라이트의 중심이 달라져도 적들의 앞뒤 순서는 바뀌지 않는다.
            actor.Body.sortingOrder = EnemyOrder + Math.Min(enemy.Index, 9989);
            bool jongnam = _gameRoot != null
                && _gameRoot.StageProgress != null && _gameRoot.StageProgress.DisplayStageNumber == 1;
            actor.Jongnam = !jongnam ? null : enemy.Kind == EnemyKind.Boss
                ? _jongnamBoss : (enemy.Index % 2 == 0 ? _jongnamMale : _jongnamFemale);
            actor.PreviousPosition = enemy.Position;
            actor.DisplayedHealth = enemy.Health;
            if (actor.Jongnam != null)
            {
                actor.Body.sprite = actor.Jongnam.Idle[0];
                actor.Body.transform.localPosition = Vector3.zero;
                actor.Body.transform.localScale = new Vector3(ActorDisplayScale, ActorDisplayScale, 1f);
                actor.BodyColor = Color.white;
                actor.FlashColor = DiscipleSpriteFlashColor;
                actor.Body.color = Color.white;
                if (actor.HealthBack != null)
                {
                    actor.HealthBarY = DiscipleSprites.HeadHeight * ActorDisplayScale + HealthBarGap;
                    actor.HealthBack.localPosition = new Vector3(0f, actor.HealthBarY, 0f);
                }
            }
            _enemies.Add(enemy, actor);
            UpdateEnemy(enemy, actor);
        }

        private void OnEnemyDamaged(Enemy enemy)
        {
            _discipleAttackedThisTick = true;
            if (enemy != null && _enemies.TryGetValue(enemy, out var actor))
            {
                StartFlash(actor);
                long damage = Math.Max(0, actor.DisplayedHealth - enemy.Health);
                actor.DisplayedHealth = enemy.Health;
                float y = enemy.Kind == EnemyKind.Boss ? 0f : (float)CombatLayout.EnemyOffsetY(enemy.Index);
                ShowDamageNumber(damage, new Vector3((float)enemy.Position + ActorDisplayOffset.x + EnemyDisplaySeparation,
                    ActorDisplayOffset.y + y + (enemy.Kind == EnemyKind.Boss ? 2.3f : 1.8f), 0f));
            }
        }

        private void OnDiscipleDamaged()
        {
            StartFlash(_disciple);
            long damage = Math.Max(0, _disciple.DisplayedHealth - _battle.DiscipleHealth);
            _disciple.DisplayedHealth = _battle.DiscipleHealth;
            ShowDamageNumber(damage, new Vector3((float)_battle.DisciplePosition + ActorDisplayOffset.x,
                ActorDisplayOffset.y + 1.8f, 0f));
        }

        private void OnEnemyDefeated(Enemy enemy)
        {
            if (enemy == null || !_enemies.TryGetValue(enemy, out var actor)) return;

            _enemies.Remove(enemy);
            if (actor.Jongnam == null || actor.Jongnam.Down == null)
            {
                Destroy(actor.Root);
                return;
            }
            actor.Body.color = Color.white;
            actor.Body.sprite = actor.Jongnam.Down[0];
            if (actor.HealthBack != null) actor.HealthBack.gameObject.SetActive(false);
            if (actor.HealthFill != null) actor.HealthFill.gameObject.SetActive(false);
            _defeatedEnemies.Add(actor);
        }

        private void OnResetCompleted()
        {
            _discipleDownElapsed = 0f;
            _skillSlash.enabled = false;
            foreach (var number in _damageNumbers) if (number.Root != null) Destroy(number.Root);
            _damageNumbers.Clear();
            _disciple.DisplayedHealth = _battle.DiscipleHealth;
            _disciple.HealthRatioInitialized = false;
            SetHealth(_disciple, _battle.DiscipleHealth, _battle.DiscipleMaxHealth, 0f);
            foreach (var projectile in _bossProjectiles.Values) Destroy(projectile);
            _bossProjectiles.Clear();
            foreach (var actor in _enemies.Values)
            {
                Destroy(actor.Root);
            }
            _enemies.Clear();
            foreach (var actor in _defeatedEnemies) Destroy(actor.Root);
            _defeatedEnemies.Clear();

            _disciple.FlashRemaining = 0f;
            _disciple.Body.color = _disciple.BodyColor;

            if (_discipleSprites != null)
            {
                _discipleAnimator.Reset();
                _disciple.Body.sprite = _discipleSprites.Get(_discipleAnimator.Motion, _discipleAnimator.Frame);
            }
        }

        // ---- 매 프레임 맞추기 ----

        private void OnSkillUsed()
        {
            _skillSlash.transform.localPosition = new Vector3(
                (float)_battle.DisciplePosition + ActorDisplayOffset.x + 1f, ActorDisplayOffset.y + 0.75f, 0f);
            _skillSlashElapsed = 0.0;
            _skillSlash.color = SkillSlashColor;
            _skillSlash.enabled = true;
        }

        private void UpdateSkillSlash(float deltaTime)
        {
            if (!_skillSlash.enabled) return;
            _skillSlashElapsed += deltaTime;
            var color = _skillSlash.color;
            color.a = (float)CombatLayout.SkillSlashAlpha(_skillSlashElapsed);
            _skillSlash.color = color;
            if (_skillSlashElapsed >= CombatLayout.SkillSlashSeconds) _skillSlash.enabled = false;
        }

        private void Refresh(float deltaTime)
        {
            // 제자
            _disciple.Root.transform.localPosition = new Vector3(
                (float)_battle.DisciplePosition + ActorDisplayOffset.x, ActorDisplayOffset.y, 0f);
            SetHealth(_disciple, _battle.DiscipleHealth, _battle.DiscipleMaxHealth, deltaTime);
            UpdateFlash(_disciple, deltaTime);

            // 적
            foreach (var pair in _enemies)
            {
                UpdateEnemy(pair.Key, pair.Value, deltaTime);
                AnimateEnemy(pair.Key, pair.Value, deltaTime);
                UpdateFlash(pair.Value, deltaTime);
            }
            for (int i = _defeatedEnemies.Count - 1; i >= 0; i--)
            {
                var actor = _defeatedEnemies[i];
                actor.DownElapsed += deltaTime;
                int frame = Mathf.Min((int)(actor.DownElapsed / JongnamSprites.DownFrameSeconds), JongnamSprites.DownFrameCount - 1);
                actor.Body.sprite = actor.Jongnam.Down[frame];
                if (actor.DownElapsed >= JongnamSprites.DownDuration + 0.5f)
                {
                    Destroy(actor.Root);
                    _defeatedEnemies.RemoveAt(i);
                }
            }
            RefreshBossProjectiles();

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

            bool showPreview = _previewBackground != null && _gameRoot != null
                && _gameRoot.StageProgress != null && _gameRoot.StageProgress.DisplayStageNumber == 1;
            _ground.gameObject.SetActive(!showPreview);
            foreach (var tree in _trees) tree.gameObject.SetActive(!showPreview);
            if (_previewBackground != null)
            {
                int centerTile = Mathf.FloorToInt((float)cameraX / _previewWidth);
                for (int i = 0; i < _previewBackground.Length; i++)
                {
                    int tileIndex = centerTile + i - 1;
                    _previewBackground[i].gameObject.SetActive(showPreview);
                    _previewBackground[i].localPosition = new Vector3(tileIndex * _previewWidth, 2.9f, 0f);
                    _previewBackground[i].GetComponent<SpriteRenderer>().flipX = (tileIndex & 1) != 0;
                }
            }
        }

        private void UpdateEnemy(Enemy enemy, CombatActor actor, float deltaTime = 0f)
        {
            float y = enemy.Kind == EnemyKind.Boss ? 0f : (float)CombatLayout.EnemyOffsetY(enemy.Index);
            actor.Root.transform.localPosition = new Vector3(
                (float)enemy.Position + ActorDisplayOffset.x + EnemyDisplaySeparation, y + ActorDisplayOffset.y, 0f);
            SetHealth(actor, enemy.Health, enemy.MaxHealth, deltaTime);
        }

        private void AnimateEnemy(Enemy enemy, CombatActor actor, float deltaTime)
        {
            if (actor.Jongnam == null) return;
            if (_battle.Paused) return;
            if (!_battle.Paused)
            {
                actor.AnimationTime += deltaTime;
                if (Math.Abs(enemy.Position - actor.PreviousPosition) > 0.0001) actor.WalkGrace = 0.12f;
                else actor.WalkGrace = Mathf.Max(0f, actor.WalkGrace - deltaTime);
            }
            actor.PreviousPosition = enemy.Position;

            // 피해가 이미 처리된 순간을 검격 프레임에 맞춘다. 다음 피해 전에는 앞 동작을 보여 준다.
            bool isBoss = actor.Jongnam.Skill != null;
            bool currentSkill = isBoss && enemy.AttackCount > 0 && enemy.AttackCount % 3 == 0;
            bool nextSkill = isBoss && (enemy.AttackCount + 1) % 3 == 0;
            int currentHitFrame = isBoss ? (currentSkill ? 14 : 13) : 10;
            int nextHitFrame = isBoss ? (nextSkill ? 14 : 13) : 10;
            const int attackFrames = 24;
            float fps = attackFrames / (float)enemy.AttackInterval;
            double cooldown = enemy.AttackCooldown;
            double sinceHit = enemy.AttackInterval - cooldown;
            if (cooldown > 0 && sinceHit * fps < attackFrames - currentHitFrame)
            {
                int frame = Mathf.Clamp(currentHitFrame + (int)(sinceHit * fps), 0, attackFrames - 1);
                actor.Body.sprite = (currentSkill ? actor.Jongnam.Skill : actor.Jongnam.Attack)[frame];
            }
            else if (cooldown > 0 && cooldown * fps <= nextHitFrame)
            {
                int frame = Mathf.Clamp(nextHitFrame - (int)Math.Ceiling(cooldown * fps), 0, nextHitFrame - 1);
                actor.Body.sprite = (nextSkill ? actor.Jongnam.Skill : actor.Jongnam.Attack)[frame];
            }
            else
            {
                var frames = actor.WalkGrace > 0f ? actor.Jongnam.Walk : actor.Jongnam.Idle;
                float rate = actor.WalkGrace > 0f ? 12f : 10f;
                actor.Body.sprite = frames[(int)(actor.AnimationTime * rate) % frames.Length];
            }
        }

        private void CreateBossProjectileSprite()
        {
            const int size = 16;
            _bossProjectileTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "JongnamSnowProjectile" };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - 7.5f, dy = y - 7.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                Color color = Color.clear;
                if (d < 6.8f && d > 4.5f) color = new Color(0.35f, 0.68f, 0.94f, 0.9f);
                if (d < 3.2f || (d < 6f && (Mathf.Abs(dx) < 0.8f || Mathf.Abs(dy) < 0.8f)))
                    color = new Color(0.88f, 0.98f, 1f, 1f);
                _bossProjectileTexture.SetPixel(x, y, color);
            }
            _bossProjectileTexture.Apply();
            _bossProjectileSprite = Sprite.Create(_bossProjectileTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }

        private void CreateDamageDigits()
        {
            var sheet = Resources.Load<Sprite>("Art/UI/damage_numbers");
            if (sheet == null || sheet.texture.width != 2172 || sheet.texture.height != 724) return;
            // 원본 시안의 글자별 수평 범위. 세로 기준선은 모든 글자에 공통이다.
            int[] left = { 33, 232, 403, 587, 770, 957, 1150, 1342, 1518, 1705, 1904 };
            int[] right = { 217, 383, 579, 756, 950, 1141, 1321, 1513, 1689, 1880, 1994 };
            _damageDigits = new Sprite[left.Length];
            for (int i = 0; i < left.Length; i++)
                _damageDigits[i] = Sprite.Create(sheet.texture, new Rect(left[i], 214, right[i] - left[i] + 1, 300),
                    new Vector2(0.5f, 0.5f), 32f);
        }

        private void ShowDamageNumber(long damage, Vector3 position)
        {
            if (damage <= 0 || _damageDigits == null) return;
            string text = damage.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            var root = new GameObject("[DamageNumber]");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = position + new Vector3((_damageNumbers.Count % 3 - 1) * 0.2f, 0f, 0f);
            const float scale = 0.05f;
            float width = 0f;
            foreach (char c in text) width += _damageDigits[c == ',' ? 10 : c - '0'].rect.width * scale / 32f;
            float x = -width * 0.5f;
            foreach (char c in text)
            {
                var sprite = _damageDigits[c == ',' ? 10 : c - '0'];
                float glyphWidth = sprite.rect.width * scale / 32f;
                var obj = new GameObject(c.ToString());
                obj.transform.SetParent(root.transform, false);
                obj.transform.localPosition = new Vector3(x + glyphWidth * 0.5f, 0f, 0f);
                obj.transform.localScale = Vector3.one * scale;
                var renderer = obj.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 25000;
                x += glyphWidth;
            }
            _damageNumbers.Add((root, 0f));
        }

        private void UpdateDamageNumbers(float deltaTime)
        {
            for (int i = _damageNumbers.Count - 1; i >= 0; i--)
            {
                var number = _damageNumbers[i];
                number.Elapsed += deltaTime;
                if (number.Root == null || number.Elapsed >= 0.8f)
                {
                    if (number.Root != null) Destroy(number.Root);
                    _damageNumbers.RemoveAt(i);
                    continue;
                }
                number.Root.transform.localPosition += Vector3.up * (deltaTime * 0.45f);
                float alpha = Mathf.Clamp01((0.8f - number.Elapsed) / 0.25f);
                foreach (var renderer in number.Root.GetComponentsInChildren<SpriteRenderer>()) renderer.color = new Color(1f, 1f, 1f, alpha);
                _damageNumbers[i] = number;
            }
        }

        private void RefreshBossProjectiles()
        {
            var removed = new List<BossProjectile>();
            foreach (var pair in _bossProjectiles)
                if (!_battle.BossProjectiles.Contains(pair.Key)) removed.Add(pair.Key);
            foreach (var key in removed)
            {
                Destroy(_bossProjectiles[key]);
                _bossProjectiles.Remove(key);
            }
            foreach (var projectile in _battle.BossProjectiles)
            {
                if (!_bossProjectiles.TryGetValue(projectile, out var obj))
                {
                    obj = new GameObject("[JongnamSnowProjectile]");
                    obj.transform.SetParent(transform, false);
                    obj.transform.localScale = new Vector3(ActorDisplayScale, ActorDisplayScale, 1f);
                    var renderer = obj.AddComponent<SpriteRenderer>();
                    renderer.sprite = _bossProjectileSprite;
                    renderer.sortingOrder = 20000;
                    _bossProjectiles.Add(projectile, obj);
                }
                obj.transform.localPosition = new Vector3((float)projectile.Position + ActorDisplayOffset.x + EnemyDisplaySeparation,
                    ActorDisplayOffset.y + 0.9f, 0f);
            }
        }

        private static void SetHealth(CombatActor actor, long health, long maxHealth, float deltaTime)
        {
            if (actor.HealthFill == null) return;

            float target = (float)CombatLayout.HealthRatio(health, maxHealth);
            if (!actor.HealthRatioInitialized)
            {
                actor.HealthRatioInitialized = true;
                actor.HealthRatioShown = actor.HealthRatioStart = actor.HealthRatioTarget = target;
                actor.HealthAnimationElapsed = HealthAnimationSeconds;
            }
            else if (target != actor.HealthRatioTarget)
            {
                actor.HealthRatioStart = actor.HealthRatioShown;
                actor.HealthRatioTarget = target;
                actor.HealthAnimationElapsed = 0f;
            }

            actor.HealthAnimationElapsed = Mathf.Min(HealthAnimationSeconds, actor.HealthAnimationElapsed + Mathf.Max(0f, deltaTime));
            float progress = actor.HealthAnimationElapsed / HealthAnimationSeconds;
            actor.HealthRatioShown = Mathf.Lerp(actor.HealthRatioStart, actor.HealthRatioTarget, Mathf.SmoothStep(0f, 1f, progress));
            float width = HealthBarWidth * actor.HealthRatioShown;
            actor.HealthFill.localScale = new Vector3(width, HealthBarHeight, 1f);
            // 왼쪽 끝을 바탕의 왼쪽 끝에 고정한다.
            actor.HealthFill.localPosition = new Vector3(HealthBarFillCenterX - HealthBarWidth * 0.5f + width * 0.5f, 0f, 0f);
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

        private void CreatePetals()
        {
            if (_gameRoot == null || _gameRoot.UiCanvas == null) return;
            _petals = new GameObject("[CombatPetals]", typeof(RectTransform));
            var rect = (RectTransform)_petals.transform;
            rect.SetParent(_gameRoot.UiCanvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();
            _petals.AddComponent<CombatPetals>().Initialize(_gameRoot);
        }

        private void CreateBackground()
        {
            _ground = CreateRect("[Ground]", transform, GroundSize, GroundColor, GroundOrder).transform;

            _trees = new Transform[TreeCount];
            for (int i = 0; i < TreeCount; i++)
            {
                _trees[i] = CreateRect("[Tree]", transform, TreeSize, TreeColor, TreeOrder).transform;
            }

            var preview = Resources.Load<Sprite>("Art/Backgrounds/stage1_preview");
            if (preview == null) return;
            // 아래 UI 뒤까지 채우면서 연무장 바닥 높이는 제자의 발에 맞춘다.
            float scale = 18f / preview.bounds.size.y;
            _previewWidth = preview.bounds.size.x * scale;
            _previewBackground = new Transform[3];
            for (int i = 0; i < _previewBackground.Length; i++)
            {
                var tile = new GameObject("[Stage1BackgroundPreview]");
                tile.transform.SetParent(transform, false);
                tile.transform.localScale = new Vector3(scale, scale, 1f);
                var renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = preview;
                renderer.sortingOrder = GroundOrder;
                _previewBackground[i] = tile.transform;
            }
        }

        /// <summary>
        /// 크기 없는 Root(발 아래 가운데) 아래에 몸과 체력 바 묶음을 만든다.
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
                bool disciple = name == "[Disciple]";
                var bar = new GameObject("HealthBack");
                bar.transform.SetParent(root.transform, false);
                bar.transform.localPosition = new Vector3(0f, actor.HealthBarY, 0f);
                actor.HealthBack = bar.transform;

                var fill = CreateRect("HealthFill", bar.transform,
                    new Vector2(HealthBarWidth, HealthBarHeight), healthColor.Value, HealthBarFillOrder);
                actor.HealthFill = fill.transform;
                var frameObject = new GameObject("HealthFrame");
                frameObject.transform.SetParent(bar.transform, false);
                var frame = frameObject.AddComponent<SpriteRenderer>();
                frame.sprite = disciple ? _discipleHealthFrame : _enemyHealthFrame;
                frame.sortingOrder = HealthBarFillOrder + 1;
                if ((disciple ? _discipleHealthFrameTexture : _enemyHealthFrameTexture) == null)
                    frameObject.transform.localScale = new Vector3(1.25f / frame.sprite.bounds.size.x, 0.55f / frame.sprite.bounds.size.y, 1f);
            }

            return actor;
        }

        private static Sprite CreateHealthFrame(bool disciple, out Texture2D texture)
        {
            // 투명한 내부와 바깥에 금속 프레임만 그린다. 제자는 왼쪽에 다섯 꽃잎 매화를 얹는다.
            const int width = 44, height = 12;
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = disciple ? "DiscipleHealthFrame" : "EnemyHealthFrame",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels(new Color[width * height]);
            Color shadow = disciple ? new Color32(100, 52, 31, 255) : new Color32(69, 55, 64, 255);
            Color metal = disciple ? new Color32(203, 144, 67, 255) : new Color32(148, 139, 151, 255);
            Color light = disciple ? new Color32(255, 211, 126, 255) : new Color32(219, 211, 218, 255);
            for (int x = 4; x <= 39; x++)
            {
                texture.SetPixel(x, 1, shadow);
                texture.SetPixel(x, 2, metal);
                texture.SetPixel(x, 3, shadow);
                texture.SetPixel(x, 8, shadow);
                texture.SetPixel(x, 9, metal);
                texture.SetPixel(x, 10, shadow);
                if (x > 5 && x < 38) texture.SetPixel(x, 9, light);
            }
            for (int y = 3; y <= 8; y++)
            {
                texture.SetPixel(3, y, shadow);
                texture.SetPixel(4, y, metal);
                texture.SetPixel(5, y, shadow);
                texture.SetPixel(38, y, shadow);
                texture.SetPixel(39, y, metal);
                texture.SetPixel(40, y, shadow);
            }
            // 꽃잎마다 외곽선과 밝은 면을 두어 작은 크기에서도 십자와 구분한다.
            if (disciple)
            {
                string[] flower = {
                    "...sss.....", "..sppls....", ".sphhpls...", ".sphhpls...", "ssppcppsss.",
                    "sphpccpphps", "sphhcpphhps", ".sppcpppps.", "..spplppss.", "...sppps...", "....sss...." };
                Color outline = new Color32(116, 42, 53, 255);
                Color petal = new Color32(238, 105, 132, 255);
                Color pinkLight = new Color32(255, 184, 188, 255);
                Color gold = new Color32(255, 203, 76, 255);
                for (int row = 0; row < flower.Length; row++)
                    for (int x = 0; x < flower[row].Length; x++)
                    {
                        char c = flower[row][x];
                        if (c == '.') continue;
                        texture.SetPixel(x, height - 1 - row, c == 's' ? outline : c == 'h' || c == 'l' ? pinkLight : c == 'c' ? gold : petal);
                    }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
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
