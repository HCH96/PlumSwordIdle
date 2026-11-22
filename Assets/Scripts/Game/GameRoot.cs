using PlumSword.Core.Currency;
using PlumSword.Core.Save;
using PlumSword.Core.OfflineReward;
using PlumSword.Core.StartMenu;
using PlumSword.Game.Currency;
using PlumSword.Game.OfflineReward;
using PlumSword.Game.Save;
using PlumSword.Game.StartMenu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PlumSword.Game
{
    /// <summary>
    /// 화면 조립의 뿌리. 각 기능은 Awake 에서 순수 C# 로직(PlumSword.Core)을 만들고,
    /// 자기 화면(오브젝트, UI)을 이 오브젝트의 자식으로 만들어 붙인다.
    /// 조립 순서와 규칙은 docs/dev/architecture.md 참고.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        // UI 기준 해상도 (docs/dev/art.md "기본 숫자").
        private static readonly Vector2 UiReferenceResolution = new Vector2(640f, 360f);
        private ISaveStore saveStore;
        internal static StartEntry NextEntry = StartEntry.Launch;
        internal static MemorySaveStore BatchSaveStore;
        private bool restarting;

        public OfflineRewardResult OfflineReward { get; private set; }
        public OfflineRewardClaim OfflineRewardClaim { get; private set; }
        public StartEntry Entry { get; private set; }
        public bool IsResumeRestart => Entry == StartEntry.ResumeInGame || Entry == StartEntry.ResumeOnTitle;
        public bool IsStartScreenShowing { get; private set; }
        public VolumeSettings Volume { get; private set; }
        public LeaveGame LeaveGame { get; private set; }
        public SettingsView Settings { get; private set; }
        public PlumSword.Game.Audio.AudioView Audio { get; private set; }

        /// <summary>시작할 때 읽은 저장 결과. 이어하기 여부를 판단할 때 쓴다.</summary>
        public SaveLoadResult LoadResult { get; private set; }

        /// <summary>스테이지 시작 전에 연결하는 자동 저장.</summary>
        public AutoSave AutoSave { get; private set; }

        /// <summary>게임에 하나뿐인 지갑. 금화를 주거나 쓰는 기능은 조립할 때 이것을 넘겨받는다.</summary>
        public Wallet Wallet { get; private set; }

        public PlumSword.Core.Realm.RealmProgress Realm { get; private set; }

        /// <summary>공용 UI 캔버스. 화면 기능은 이 캔버스 아래에 UI 를 만든다.</summary>
        public Canvas UiCanvas { get; private set; }

        /// <summary>게임에 하나뿐인 능력치 강화. 전투·스테이지 기능은 조립할 때 이것을 넘겨받는다.</summary>
        public PlumSword.Core.StatUpgrade.StatUpgrades StatUpgrades { get; private set; }

        public PlumSword.Core.StatUpgrade.AutoUpgrade AutoUpgrade { get; private set; }

        /// <summary>게임에 하나뿐인 전투. Tick 은 CombatView 만 부른다. 스테이지 기능은 이것으로 적을 놓는다.</summary>
        public PlumSword.Core.Combat.Battle Battle { get; private set; }

        /// <summary>게임에 하나뿐인 스테이지 진행. Tick 은 StageView 만, Begin 은 GameRoot 만 부른다.</summary>
        public PlumSword.Core.Stage.StageProgress StageProgress { get; private set; }

        private void Awake()
        {
            Entry = NextEntry;
            NextEntry = StartEntry.Launch;
            if (Application.isBatchMode)
            {
                saveStore = BatchSaveStore ?? new MemorySaveStore();
                LoadResult = SaveCodec.Decode(BatchSaveStore != null ? BatchSaveStore.Text : null);
            }
            else
            {
                var fileStore = new FileSaveStore(Application.persistentDataPath);
                saveStore = fileStore;
                LoadResult = fileStore.Load();
            }

            // 저장된 값이 있으면 복원하고, 없으면 처음 값으로 Core 객체를 만든다.
            var data = LoadResult.Data;
            Wallet = new Wallet(data != null ? data.Gold : 0);
            Realm = new PlumSword.Core.Realm.RealmProgress(Wallet, data != null ? data.RealmLevel : 0, data != null ? data.RealmKills : 0);
            StatUpgrades = new PlumSword.Core.StatUpgrade.StatUpgrades(
                Wallet,
                PlumSword.Core.Balance.BalanceTable.Current.DiscipleBaseAttack,
                PlumSword.Core.Balance.BalanceTable.Current.DiscipleBaseMaxHealth,
                data != null ? data.AttackLevel : 0,
                data != null ? data.HealthLevel : 0, data != null ? data.InnerPowerLevel : 0, Realm);
            AutoUpgrade = new PlumSword.Core.StatUpgrade.AutoUpgrade(StatUpgrades, Wallet, true);
            Battle = new PlumSword.Core.Combat.Battle(StatUpgrades, Realm, true);
            Battle.EnemyDefeated += enemy => Realm.RecordKill();

            // 공용 UI 뿌리
            UiCanvas = CreateUiCanvas();
            EnsureEventSystem();

            // 기능별 화면 (기능이 늘면 여기에 한 줄씩 추가한다)
            CreateCurrencyView();
            CreateStatUpgradeView();
            CreateAutoUpgradeView();
            CreateRealmView();
            CreateCombatView();
            CreateRealmAuraView();
            CreateStage(data != null ? data.StageNumber : 1, data != null ? data.SeenIntroStage : 0);

            if (LoadResult.Status == SaveLoadStatus.Loaded && StartMenuRules.CalculatesOfflineReward(Entry))
                OfflineReward = OfflineRewardCalculator.Calculate(data.SavedAtUtcTicks, System.DateTime.UtcNow.Ticks, StageProgress.StageNumber);

            AutoSave = new AutoSave(saveStore, Wallet, StatUpgrades, Realm, StageProgress,
                enabled: StartMenuRules.AutoSaveStartsEnabled(LoadResult.Status, OfflineReward != null && OfflineReward.HasReward));
            var saveViewObject = new GameObject("[SaveView]");
            saveViewObject.transform.SetParent(transform, false);
            saveViewObject.AddComponent<SaveView>().Initialize(AutoSave, UiCanvas);

            Volume = Application.isBatchMode ? new VolumeSettings() : VolumePrefs.Load();
            LeaveGame = new LeaveGame(AutoSave);
            var settingsObject = new GameObject("[SettingsView]");
            settingsObject.transform.SetParent(transform, false);
            Settings = settingsObject.AddComponent<SettingsView>();
            Settings.Initialize(UiCanvas, Volume, LeaveGame, ReturnToTitle, Quit, SaveVolume);
            var audioObject = new GameObject("[AudioView]");
            audioObject.transform.SetParent(transform, false);
            Audio = audioObject.AddComponent<PlumSword.Game.Audio.AudioView>();
            Audio.Initialize(this);

            if (StartMenuRules.ShowsStartScreen(Entry, Application.isBatchMode))
            {
                IsStartScreenShowing = true;
                Battle.SetPaused(true);
                var startScreenObject = new GameObject("[StartScreenView]");
                startScreenObject.transform.SetParent(transform, false);
                startScreenObject.AddComponent<StartScreenView>().Initialize(UiCanvas,
                    StartMenuRules.StartButton(LoadResult.Status), StartFlow, () => Settings.Open(SettingsMode.Title));
            }
            else
            {
                StartFlow();
            }

            if (!Application.isBatchMode)
            {
                var watcherObject = new GameObject("[OfflineResumeWatcher]");
                watcherObject.transform.SetParent(transform, false);
                watcherObject.AddComponent<OfflineResumeWatcher>().Initialize(this, AutoSave);
            }
            var backKeyObject = new GameObject("[BackKeyWatcher]");
            backKeyObject.transform.SetParent(transform, false);
            backKeyObject.AddComponent<BackKeyWatcher>().Initialize(this, Settings);
        }

        private void StartFlow()
        {
            IsStartScreenShowing = false;
            if (LoadResult.Status == SaveLoadStatus.Corrupt)
            {
                Battle.SetPaused(true);
                var errorViewObject = new GameObject("[SaveErrorView]");
                errorViewObject.transform.SetParent(transform, false);
                errorViewObject.AddComponent<SaveErrorView>().Initialize(UiCanvas, StartNewGame);
            }
            else if (OfflineReward != null && OfflineReward.HasReward)
            {
                Battle.SetPaused(true);
                OfflineRewardClaim = new OfflineRewardClaim(Wallet, AutoSave, OfflineReward);
                var rewardViewObject = new GameObject("[OfflineRewardView]");
                rewardViewObject.transform.SetParent(transform, false);
                rewardViewObject.AddComponent<OfflineRewardView>().Initialize(OfflineRewardClaim, UiCanvas, StartAfterClaim);
            }
            else
            {
                AutoSave.Enable();
                Battle.SetPaused(false);
                StageProgress.Begin();
            }
        }

        public void Restart()
        {
            if (restarting) return;
            restarting = true;
            NextEntry = StartMenuRules.ResumeEntry(IsStartScreenShowing);
            Recreate();
        }

        public void ReturnToTitle()
        {
            if (restarting) return;
            restarting = true;
            NextEntry = StartEntry.TitleReturn;
            Recreate();
        }

        internal void RecreateAs(StartEntry entry)
        {
            if (restarting) return;
            restarting = true;
            NextEntry = entry;
            Recreate();
        }

        private void Recreate()
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            var restarter = new GameObject("[GameRestarter]");
            DontDestroyOnLoad(restarter);
            restarter.AddComponent<GameRestarter>();
        }

        private void SaveVolume()
        {
            if (!Application.isBatchMode) VolumePrefs.Save(Volume);
        }

        private void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void StartAfterClaim()
        {
            Battle.SetPaused(false);
            StageProgress.Begin();
        }

        private Canvas CreateUiCanvas()
        {
            var canvasObject = new GameObject("[Canvas]");
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UiReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null) return;

            var eventSystemObject = new GameObject("[EventSystem]");
            eventSystemObject.transform.SetParent(transform, false);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private void CreateCurrencyView()
        {
            var viewObject = new GameObject("[CurrencyView]");
            viewObject.transform.SetParent(transform, false);
            viewObject.AddComponent<CurrencyView>().Initialize(Wallet, UiCanvas);
        }

        private void CreateStatUpgradeView()
        {
            var viewObject = new GameObject("[StatUpgradeView]");
            viewObject.transform.SetParent(transform, false);
            viewObject.AddComponent<PlumSword.Game.StatUpgrade.StatUpgradeView>()
                .Initialize(StatUpgrades, Wallet, UiCanvas);
        }

        private void CreateAutoUpgradeView()
        {
            var viewObject = new GameObject("[AutoUpgradeView]");
            viewObject.transform.SetParent(transform, false);
            viewObject.AddComponent<PlumSword.Game.StatUpgrade.AutoUpgradeView>().Initialize(AutoUpgrade, UiCanvas);
        }

        private void OnDestroy()
        {
            AutoUpgrade?.Dispose();
        }

        private void CreateRealmView()
        {
            var viewObject = new GameObject("[RealmView]");
            viewObject.transform.SetParent(transform, false);
            viewObject.AddComponent<PlumSword.Game.Realm.RealmView>().Initialize(Realm, Wallet, UiCanvas);
        }

        private void CreateCombatView()
        {
            Camera worldCamera = Camera.main;
            if (worldCamera == null)
            {
                var cameraObject = new GameObject("[Camera]");
                cameraObject.transform.SetParent(transform, false);
                cameraObject.tag = "MainCamera";
                worldCamera = cameraObject.AddComponent<Camera>();
            }

            var viewObject = new GameObject("[CombatView]");
            viewObject.transform.SetParent(transform, false);
            viewObject.AddComponent<PlumSword.Game.Combat.CombatView>().Initialize(Battle, worldCamera);
        }

        private void CreateRealmAuraView()
        {
            var viewObject = new GameObject("[RealmAuraView]");
            viewObject.transform.SetParent(transform, false);
            viewObject.AddComponent<PlumSword.Game.Realm.RealmAuraView>().Initialize(Realm, Battle);
        }

        // 스테이지 로직을 만들고 화면을 붙인다. 시작은 저장 기능을 연결한 뒤에 한다.
        private void CreateStage(int startStage, int seenIntroStage)
        {
            StageProgress = new PlumSword.Core.Stage.StageProgress(
                Wallet, StatUpgrades, Battle, new System.Random(), startStage, seenIntroStage,
                PlumSword.Core.Stage.StageProgress.DefaultClearWaitSeconds);

            var viewObject = new GameObject("[StageView]");
            viewObject.transform.SetParent(transform, false);
            viewObject.AddComponent<PlumSword.Game.Stage.StageView>().Initialize(StageProgress, UiCanvas);
        }

        private void StartNewGame()
        {
            AutoSave.Enable();
            Battle.SetPaused(false);
            StageProgress.Begin();
        }
    }
}
