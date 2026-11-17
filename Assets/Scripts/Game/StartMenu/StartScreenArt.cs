using System;
using System.Collections.Generic;
using PlumSword.Core.StartMenu;
using UnityEngine;

namespace PlumSword.Game.StartMenu
{
    /// <summary>
    /// 시작 화면 그림 (docs/dev/assets.md "시작 화면"). 제목이 들어 있는 배경과 글자가 그려진 버튼 세 장.
    /// 하나라도 없으면 Load 가 null 을 돌려주고, 시작 화면은 단색 바탕과 영문 글자로 대신 그린다.
    /// 꽃잎 그림(PetalPaths)은 따로 다룬다. 찾은 것만 Petals 에 담고, 하나도 없으면 꽃잎 없이 그린다.
    /// </summary>
    public sealed class StartScreenArt
    {
        public const string BackgroundPath = "Art/StartMenu/title_background";
        public const string StartButtonPath = "Art/StartMenu/button_start";
        public const string ContinueButtonPath = "Art/StartMenu/button_continue";
        public const string SettingsButtonPath = "Art/StartMenu/button_settings";

        // 흩날리는 매화 꽃잎 (펼쳐진, 휘어진, 접힌).
        public static readonly string[] PetalPaths =
        {
            "Art/StartMenu/petal_open",
            "Art/StartMenu/petal_curled",
            "Art/StartMenu/petal_folded",
        };

        // petals 가 null 이면 꽃잎 없음. null 인 항목은 뺀다.
        public StartScreenArt(Sprite background, Sprite startButton, Sprite continueButton, Sprite settingsButton, IReadOnlyList<Sprite> petals = null)
        {
            var found = new List<Sprite>();
            if (petals != null)
                foreach (var petal in petals)
                    if (petal != null) found.Add(petal);
            Petals = found;
            Background = background;
            StartButton = startButton;
            ContinueButton = continueButton;
            SettingsButton = settingsButton;
        }

        public Sprite Background { get; }
        public Sprite StartButton { get; }
        public Sprite ContinueButton { get; }
        public Sprite SettingsButton { get; }
        public IReadOnlyList<Sprite> Petals { get; }

        // 그림 네 장을 Resources 에서 읽는다. 하나라도 없으면 null. 꽃잎은 찾은 것만 담는다.
        public static StartScreenArt Load()
        {
            var background = Resources.Load<Sprite>(BackgroundPath);
            var start = Resources.Load<Sprite>(StartButtonPath);
            var resume = Resources.Load<Sprite>(ContinueButtonPath);
            var settings = Resources.Load<Sprite>(SettingsButtonPath);
            if (background == null || start == null || resume == null || settings == null) return null;
            var petals = Array.ConvertAll(PetalPaths, path => Resources.Load<Sprite>(path));
            return new StartScreenArt(background, start, resume, settings, petals);
        }

        // Continue → 이어하기 그림, 나머지(정의되지 않은 값 포함) → 게임 시작 그림. StartMenuText.StartButtonLabel 과 같은 규칙.
        public Sprite StartButtonSprite(StartButtonKind kind) => kind == StartButtonKind.Continue ? ContinueButton : StartButton;
    }
}
