using PlumSword.Core.Currency;
using PlumSword.Game.Currency;
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

        /// <summary>게임에 하나뿐인 지갑. 금화를 주거나 쓰는 기능은 조립할 때 이것을 넘겨받는다.</summary>
        public Wallet Wallet { get; private set; }

        /// <summary>공용 UI 캔버스. 화면 기능은 이 캔버스 아래에 UI 를 만든다.</summary>
        public Canvas UiCanvas { get; private set; }

        /// <summary>게임에 하나뿐인 능력치 강화. 전투·스테이지 기능은 조립할 때 이것을 넘겨받는다.</summary>
        public PlumSword.Core.StatUpgrade.StatUpgrades StatUpgrades { get; private set; }

        /// <summary>게임에 하나뿐인 전투. Tick 은 CombatView 만 부른다. 스테이지 기능은 이것으로 적을 놓는다.</summary>
        public PlumSword.Core.Combat.Battle Battle { get; private set; }

        private void Awake()
        {
            // Core 객체 (게임에 하나뿐인 지갑. 1단계는 저장이 없어 0 에서 시작한다)
            Wallet = new Wallet();
            StatUpgrades = new PlumSword.Core.StatUpgrade.StatUpgrades(
                Wallet,
                PlumSword.Core.Combat.Battle.DiscipleBaseAttack,
                PlumSword.Core.Combat.Battle.DiscipleBaseMaxHealth,
                PlumSword.Core.Combat.Battle.DiscipleBaseAttackInterval);
            Battle = new PlumSword.Core.Combat.Battle(StatUpgrades);

            // 공용 UI 뿌리
            UiCanvas = CreateUiCanvas();
            EnsureEventSystem();

            // 기능별 화면 (기능이 늘면 여기에 한 줄씩 추가한다)
            CreateCurrencyView();
            CreateStatUpgradeView();
            CreateCombatView();
            CreateTempEncounter(); // 임시. 스테이지 기능을 구현할 때 지운다
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

        // 임시. 스테이지 기능을 구현할 때 지운다 (이 메서드와 Awake 의 호출 줄).
        private void CreateTempEncounter()
        {
            var encounterObject = new GameObject("[TempEncounter]");
            encounterObject.transform.SetParent(transform, false);
            encounterObject.AddComponent<PlumSword.Game.Combat.TempEncounter>().Initialize(Battle);
        }
    }
}
