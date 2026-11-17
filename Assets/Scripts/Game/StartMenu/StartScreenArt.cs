using PlumSword.Core.StartMenu;
using UnityEngine;

namespace PlumSword.Game.StartMenu
{
    /// <summary>
    /// 시작 화면 그림 (docs/dev/assets.md "시작 화면"). 제목이 들어 있는 배경과 글자가 그려진 버튼 세 장.
    /// 하나라도 없으면 Load 가 null 을 돌려주고, 시작 화면은 단색 바탕과 영문 글자로 대신 그린다.
    /// </summary>
    public sealed class StartScreenArt
    {
        public const string BackgroundPath = "Art/StartMenu/title_background";
        public const string StartButtonPath = "Art/StartMenu/button_start";
        public const string ContinueButtonPath = "Art/StartMenu/button_continue";
        public const string SettingsButtonPath = "Art/StartMenu/button_settings";

        public StartScreenArt(Sprite background, Sprite startButton, Sprite continueButton, Sprite settingsButton)
        {
            Background = background;
            StartButton = startButton;
            ContinueButton = continueButton;
            SettingsButton = settingsButton;
        }

        public Sprite Background { get; }
        public Sprite StartButton { get; }
        public Sprite ContinueButton { get; }
        public Sprite SettingsButton { get; }

        // 그림 네 장을 Resources 에서 읽는다. 하나라도 없으면 null.
        public static StartScreenArt Load()
        {
            var background = Resources.Load<Sprite>(BackgroundPath);
            var start = Resources.Load<Sprite>(StartButtonPath);
            var resume = Resources.Load<Sprite>(ContinueButtonPath);
            var settings = Resources.Load<Sprite>(SettingsButtonPath);
            if (background == null || start == null || resume == null || settings == null) return null;
            return new StartScreenArt(background, start, resume, settings);
        }

        // Continue → 이어하기 그림, 나머지(정의되지 않은 값 포함) → 게임 시작 그림. StartMenuText.StartButtonLabel 과 같은 규칙.
        public Sprite StartButtonSprite(StartButtonKind kind) => kind == StartButtonKind.Continue ? ContinueButton : StartButton;
    }
}
