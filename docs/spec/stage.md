# 스테이지 사양

- 기능 이름: stage (`PlumSword.Core.Stage`)
- 갱신: 2026-11-18, 큰 스테이지 안의 작은 스테이지 진행
- 고친 날: 2026-11-22, 4·5스테이지 진행 구체화

## 목표

큰 스테이지 하나는 일반 상대 구간 여섯 개와 보스 구간 하나로 구성된다. 일반 구간마다 종전의 한 무리에 해당하는 적 2~3명이 무작위로 나오고, 각 구간 시작에는 `1-1`처럼 큰 스테이지 번호와 작은 구간 번호를 중앙에 잠깐 표시한다. 1스테이지는 화산파 연무장에서 종남파와 대련한다. 다섯 테마를 마치면 다음 회차의 1스테이지로 순환한다. 단, 5스테이지(천마)는 일반 구간 없이 최종 보스 구간만 있고 번호 대신 `FINAL`을 표시한다.

## 동작

- 천마신교는 4·5스테이지로 구성한다. 4스테이지는 다른 스테이지와 같이 일반 구간 1~6(천마신교 일반 상대 마검사·흑갑 호위)과 7구간 보스를 진행한다.
- 화면상 큰 스테이지가 `StageLayout.FinalDisplayStage`(5)이면 일반 구간이 없다. 큰 스테이지를 배치할 때(`Begin()`, 성공·실패 전환의 가려진 순간 모두) 바로 `SubstageNumber = 7`, 처치 수 0으로 두고 최종 보스 한 명만 제자 앞 24유닛에 배치한다. 일반 상대를 만들지 않고 무리 난수도 뽑지 않는다. 이후 순서(테마 소개 조건, 1.2초 구간 소개, 전투)는 다른 보스 구간과 같다.
- 최종 보스의 능력치와 보상은 기존 공식에 전체 진행 번호를 넣은 값이다. 1회차는 체력 220, 공격력 26, 보상 90금화이고, 2회차(번호 10)는 370, 46, 140이다. 따로 붙는 보너스는 없다.
- 5스테이지에서 패배하면 같은 5스테이지를 다시 배치한다. 보스는 체력이 가득한 새 보스이고, 테마 소개는 다시 보이지 않는다. 최종 보스를 혼자 이기면 다음 회차의 1스테이지 1구간으로 간다.
- 회귀 규칙(`StageProgress`의 `useRegression`)을 켜면 천마 패배는 1스테이지 1구간으로 회귀, 천마 처치는 엔딩이 된다. 게임(`GameRoot`)은 켠다. 자세한 규칙은 `docs/spec/regression.md`.
- 전체 진행 번호 `StageNumber`는 1 이상이다. 화면상 큰 스테이지는 `(StageNumber - 1) % 5 + 1`, 회차는 `(StageNumber - 1) / 5 + 1`이다. `int.MaxValue`에서 성공하면 번호는 유지한다.
- `Begin()`은 최초 한 번 큰 스테이지를 배치한다. 전투를 초기화하고, 작은 번호 `SubstageNumber = 1`, 처치 수 0, 보스 없음으로 둔 뒤 첫 무리를 만든다(5스테이지는 위 예외). `StageStarted`와 `Changed`를 보낸다. 다시 부르면 아무 일도 없다.
- 일반 구간 1~6은 매번 `Random.Next(2, 4)`로 상대 수를 독립적으로 고른다. 현재 제자 위치에서 앞쪽 24유닛을 무리 중심으로 두고 상대 중심 간격을 2유닛으로 배치한다. 예: 중심 24이면 2명은 23·25, 3명은 22·24·26이다. 한 번에 현재 구간의 무리만 배치한다.
- 현재 무리의 일반 상대를 모두 쓰러뜨리면 설정된 결판 뒤 대기 후 0.5초 페이드아웃하고 가려진 순간 다음 구간을 제자의 현재 위치를 기준으로 배치한 뒤 0.5초 페이드인한다. 여섯 번째 무리 뒤에는 7구간의 보스를 제자 앞 24유닛에 배치한다. 실제 현재 무리에 속한 적만 구간 완료 판단에 포함한다.
- 대기 시간이 양수인 성공 전환은 보상과 처치 수를 즉시 반영하고 `ClearWait`에서 강화를 잠그고 제자 전진만 막는다. 전투는 계속되지만 전투 알림은 무시하며 보스와 보스 활성 상태는 다음 배치까지 유지한다. 대기가 끝나면 전투를 멈추고 페이드아웃하며, 대기에서 남는 시간은 페이드에 넘긴다. 가려진 순간 제자리 유지를 푼다. 대기 시간이 0이면 바로 페이드아웃한다. 회귀 규칙을 켠 천마 처치는 대기 뒤(대기 0이면 즉시) 페이드 대신 `Cleared`로 간다. 실패는 대기 없이 전환한다. 페이드인이 끝나면 강화 잠금을 풀고 구간 소개를 보여 준다.
- 성공한 보스의 금화를 지급하고 다음 번호를 기억한 다음 전환 상태에서 `StageCleared`와 `Changed`를 순서대로 보낸다. 일반 구간 성공과 대기 종료 때도 `Changed`를 보낸다. 대기 시간은 설정값에서 1e-9초를 뺀 값 이상이면 끝난 것으로 판정한다.
- 각 일반 구간과 보스 구간이 시작하면 `SubstageIntro` 상태에서 번호를 1.2초 표시하며 전투를 멈춘다. 강화는 잠그지 않는다. 1회차에서 처음 방문한 큰 스테이지는 기존 테마 소개를 3초 표시한 뒤 구간 번호를 1.2초 표시한다. 테마 소개는 본 큰 스테이지에서는 다시 보이지 않지만, 구간 번호는 매 구간마다 표시한다.
- 적 처치 보상은 기존 그대로다. 일반 상대는 `10 + 2 × (StageNumber - 1)`, 보스는 `50 + 10 × (StageNumber - 1)` 금화다. 일반 적과 제자가 같은 전투 칸에서 쓰러지면 적의 보상은 지급하지만 구간은 진행하지 않고 실패한다. 보스와 제자가 동시에 쓰러지면 보스 보상 없이 실패한다.
- 보스를 혼자 이기면 다음 큰 스테이지로, 제자가 쓰러지면 같은 큰 스테이지로 전환한다. 전환 중에는 0.5초 페이드아웃과 0.5초 페이드인을 적용하고 전투·강화를 잠근다. 가려진 순간 전투를 초기화하고 새 큰 스테이지의 1구간(5스테이지는 보스 구간)을 배치한다. 페이드가 끝나면 강화를 풀고 필요한 테마 소개와 구간 소개를 순서대로 보여 준다.
- 실패와 게임 재시작 모두 현재 큰 스테이지의 **1구간부터**(5스테이지는 보스 구간부터) 시작한다. 금화와 강화 단계는 유지하며 재도전 중 처치한 적의 금화는 다시 받는다.
- 보스 체력이 변하면 `Changed`를 보낸다. `BarRatio`는 보스 전에는 완료한 일반 구간 수 ÷ 6, 보스가 나타난 뒤에는 보스 체력 비율이다. 5스테이지에서는 처음부터 보스 체력 비율이다. `FadeAlpha`는 페이드 상태에서만 0~1로 바뀐다.
- `Tick`은 음수·NaN·무한대에서 `ArgumentOutOfRangeException`을 던진다. 페이드아웃의 남는 시간은 페이드인에 넘기고, 페이드 또는 소개가 끝난 뒤 새 소개에 이전 단계의 남은 시간을 넘기지 않는다.

## 수치

| 항목 | 값 |
| --- | --- |
| 일반 구간 / 보스 구간 | 6 / 7번째 |
| 일반 구간당 적 수 | 2 또는 3, 무작위 |
| 무리 중심 거리 / 무리 안 간격 | 제자 앞 24 / 2 유닛 |
| 보스 등장 거리 | 제자 앞 24유닛 |
| 구간 번호 소개 / 큰 스테이지 이야기 소개 | 1.2초 / 3초 |
| 페이드아웃 / 페이드인 | 각각 0.5초 |
| 결판 뒤 대기 | 게임에서 사용할 기본값 1.5초 (임시), 생성자 기본값 0초 |
| 테마 순서 | 종남, 개방, 남궁, 마교, 천마 |
| 최종 스테이지(화면상) / 구간 | 5 / 보스 구간(7)만 |
| 보스 체력 틀 크기 / 중심 | 300×100 UI 단위 / (0, −112) (임시) |

## 연결 지점

- `StageProgress(Wallet, StatUpgrades, Battle, Random, int startStage = 1, int seenIntroStage = 5, double clearWaitSeconds = 0.0, bool useRegression = false, int regressionCount = 0)`: 필수 인자 `null`은 예외, 시작 번호는 최소 1, 본 소개 번호는 0~5로 자른다. 대기 시간이 음수·NaN·무한대면 `ArgumentOutOfRangeException`. 생성 시 전투를 시작하지 않는다.
- `useRegression`은 천마 패배 시 회귀와 처치 시 종료를 켠다. `regressionCount`는 음수를 0으로 보정한 초기 회귀 횟수다.
- `UseRegression`은 회귀 규칙 사용 여부를 읽는다.
- `RegressionCount`는 현재 회귀 횟수를 읽는다.
- `Regressed`는 회귀 시 새 회귀 횟수를 보낸다.
- `GameCleared`는 천마 처치 후 종료 상태가 되었을 때 보낸다.
- `StagePhase.Cleared = 6`은 종료 상태이며 Tick과 전투 알림을 무시한다.
- `ClearWaitSeconds`는 설정된 대기 시간을 읽는다. `DefaultClearWaitSeconds = 1.5`는 게임에서 사용할 임시 상수다.
- `StageNumber`, `DisplayStageNumber`, `RoundNumber`, `SubstageNumber`(1~7), `Phase`, `HasBegun`, `DefeatedNormalCount`, `Boss`, `IsBossActive`, `IsIntroShowing`, `IsSubstageIntroShowing`, `SeenIntroStage`, `BarRatio`, `FadeAlpha`를 읽는다.
- `Begin()`과 `Tick(double)`을 호출하고 `StageStarted`, `StageCleared`, `StageFailed`, `Changed`를 구독한다. 스테이지 이벤트의 숫자는 작은 번호가 아니라 전체 큰 스테이지 번호다.
- `StagePhase`는 `Playing = 0`, `FadingOut = 1`, `FadingIn = 2`, `Intro = 3`, `SubstageIntro = 4`, `ClearWait = 5`, `Cleared = 6`이다.
- `StageLayout.SubstagePositions(Random, double center)`는 무리 하나의 2~3개 좌표를 반환한다. `NormalSubstageCount = 6`, `MinSubstageEnemies = 2`, `MaxGroupSize = 3`, `EncounterDistance = 24`, `InGroupSpacing = 2`, `BossDistance = 24`. 기존 `FirstGroupPosition = 6`, `RollGroups`·`Positions`와 `NormalCount`는 이전 호출과 테스트 호환용이며 현재 스테이지 배치에 사용하지 않는다.
- `public const int StageLayout.FinalDisplayStage = 5`: 일반 구간 없이 최종 보스만 나오는 화면상 큰 스테이지 번호.
- `StageRewards.NormalGold(int)`와 `BossGold(int)`는 1 미만에 예외를 던진다. `StageText`는 기존 회차·테마·보스 이름과 영문 테마 소개를 제공한다. 1스테이지 소개는 `Mount Hua and Jongnam disciples spar in Mount Hua's training yard.`이다.
- `StageRewards.NormalGold(int stage, int substage)`와 `BossGold(int stage, int substage)`는 수치 표의 구간별 보상을 반환하며 1 미만의 스테이지나 1~7 밖의 구간에 예외를 던진다. 기존 메서드는 1구간을 쓴다.
- `public static string StageText.SubstageLabel(int stage, int substage)`: `stage`가 5면 `"FINAL"`, 아니면 `"{stage}-{substage}"`를 문화권과 무관하게 만든다. `stage`가 1~5 밖이거나 `substage`가 1~7 밖이면 `ArgumentOutOfRangeException`을 던진다.
- `StageLabelArt`는 제목판·번호 시트 경로 상수 `HeaderBlankPath`, `NumberSheetPath`와 스테이지 1~5의 그림 경로 `TitlePath(int stage)`, `BossNamePath(int stage)`를 제공한다. 경로의 번호는 문화권과 무관하며 범위 밖은 예외다.
- `UsesBossName(int stage, int substage)`는 5스테이지 또는 7구간에서 참이고, `NumberGlyphs(int stage, int substage)`는 일반 구간에 새 배열 `{stage, HyphenGlyph, substage}`를 반환한다. 스테이지는 1~5, 구간은 1~7이며 번호 배열은 스테이지 1~4와 구간 1~6만 허용한다. 잘못된 값은 `ArgumentOutOfRangeException`이다.
- `HyphenGlyph = 10`, `GlyphTop = 8`, `GlyphHeight = 290`, `GlyphOverlap = 18`. `GlyphLeft(int glyph)`와 `GlyphWidth(int glyph)`는 시트 왼쪽 위 기준 0~10 글자의 좌표와 너비를 반환하고 범위 밖은 예외다. `LabelWidth(int[] glyphs, double scale)`와 `GlyphCenterX(int[] glyphs, int index, double scale)`는 글자 폭과 겹침을 반영한 전체 너비와 가운데 정렬 좌표를 반환한다. 빈 배열의 너비는 0이고 null, 유효하지 않은 글자·인덱스, 양의 유한수가 아닌 배율은 예외다.
- 전투는 `Battle.AddEnemy`, `EnemyDefeated`, `EnemyDamaged`, `DiscipleDefeated`, `Reset`, `SetPaused`, `SetHoldPosition`으로 연결하고, 강화는 `SetLocked`, 저장은 `StageStarted`, `StageNumber`, `SeenIntroStage`로 연결한다.

## 화면

- 진행 표시에는 스테이지별 그림 제목과 금빛 그림 번호를 표시한다. 구간 시작 패에도 그림 번호를 쓰며, 보스 구간과 5스테이지는 두 곳 모두 번호 대신 해당 보스 이름 그림을 쓴다. 진행 표시의 번호 중심은 (0, −61), 소개 패는 (0, −5.5)이다. 기존 TMP 번호는 비활성 상태에서도 계속 갱신한다.
- 진행 표시는 빈 제목판·2132×300 번호 시트·제목 5장·보스 이름 5장이 모두 있을 때 그림 글자를 사용한다. 구간 시작 패는 번호 시트와 보스 이름 5장이 모두 필요하다. 필요한 그림이 하나라도 없으면 각 화면은 기존 글자 표시를 쓴다.
- 모든 스테이지와 회차에 1스테이지의 매화 제목판과 진행 표시를 공통으로 사용하고, 화면상 스테이지와 구간에 맞는 `1-1`~`4-7`, `FINAL` 등의 번호, 여섯 매화 표식과 보스 표식을 표시한다. 완료 표식은 분홍 매화, 현재 표식은 금빛으로 표시하고 박동한다. 보스 구간에서는 해당 스테이지의 보스 이름과 현재 체력을 기존 붉은 체력 바로 표시한다.
- 1~5스테이지 모두 `IsBossActive`인 동안에는 진행 표시의 매화 표식 6개(`Marker1~6`), 보스 표식(`BossMarker`), 진행 선(`Track`, `TrackHighlight`, `Progress`)을 비활성화한다. 진행 표시 패와 보스 이름 그림, 붉은 체력 바는 보인다. 일반 구간에서는 표식과 진행 선을 다시 보인다. 기존 `BossName` TMP 글자는 항상 비활성이지만 텍스트는 계속 갱신한다.
- **보스 체력 틀**: `IsBossActive`인 동안 화면상 스테이지 번호 n의 `Art/UI/boss_health_frame_{n}`(2172×724px 투명 틀, 1~5)이 있으면 `BossHealthFrame` 이미지를 위쪽 가운데 기준으로 300×100 UI 단위(임시), 중심 (0, −112)에 표시한다. 입력은 가로채지 않는다. 이때 `StageBar`(어두운 바탕과 `Fill`)를 틀의 게이지 칸에 맞춘다. 게이지 칸은 원본 픽셀(왼쪽 위 기준, 끝 포함)로 아래 표와 같고, `k = 300 / 2172`이다. 바의 너비 `(오른쪽 − 왼쪽 + 1)·k`, 높이 `(아래 − 위 + 1)·k`, 중심 x `((왼쪽 + 오른쪽 + 1)/2 − 1086)·k`, 중심 y `−112 − ((위 + 아래 + 1)/2 − 362)·k`이다. 채움 색(`BossFillColor`)과 `BarRatio` 동작은 그대로다. 틀은 보스가 사라지고 다음 큰 스테이지가 배치될 때 숨긴다. 해당 틀이 없으면 중심 (0, −112)의 180×7 대체 바를 쓴다. 일반 구간의 바 위치·크기는 바뀌지 않는다.
- 틀 스프라이트는 `StageView.Initialize`에서 다섯 장을 불러온다. 보스 틀과 바는 위쪽 가운데 앵커 및 가운데 피벗을 써서 지정 좌표가 실제 중심이다.

| 틀 | 게이지 왼쪽~오른쪽 | 게이지 위~아래 |
| --- | --- | --- |
| 1 종남 | 405~1965 | 342~401 |
| 2 개방 | 457~1933 | 324~385 |
| 3 남궁 | 448~1947 | 334~397 |
| 4 마교 | 518~1964 | 351~425 |
| 5 천마 | 533~1930 | 322~401 |

- 진행 표시와 구간 소개 패의 숨긴 TMP 번호 및 스테이지 글자(`… · 4-7`)는 `StageText.SubstageLabel`을 쓴다. 5스테이지에서는 `FINAL`이고, 보스 이름은 `HEAVENLY DEMON`이다.
- 에셋이 없을 때는 기존 진행 UI를 사용한다. 각 구간 시작에는 매화·검 장식 번호 패와 동적으로 생성한 번호를 화면 가운데 1.2초 표시한다. 0.24초 동안 왼쪽에서 이동하며 확대·페이드인하고 마지막 0.24초에 페이드아웃한다. UI는 입력을 가로채지 않으며 그림이 없을 때는 단색 패로 대체한다. 기존 테마 소개 패널은 처음 방문 시에만 따로 표시한다.
- 전환 페이드 막은 전투 화면만 가린다. UI는 입력을 받지 않는다. 에셋이 없으면 기존 도형 UI를 쓴다.
- 게임은 성공 판정 뒤 1.5초 기다린 뒤 페이드아웃한다. 성공 전환의 쓰러짐 대기는 이 시간을 뺀 나머지만 기다리며, 필요한 시트가 없으면 추가로 기다리지 않는다. 실패 시 제자 쓰러짐 대기는 그대로다.
- 제자가 패배하면 판정과 전투 정지는 즉시 처리하고, 화면에서는 쓰러짐 17프레임을 0.075초씩 한 번 보여 준 뒤 기존 0.5초 페이드아웃을 시작한다. 1스테이지에서 마지막 종남 상대 또는 장로를 이겼을 때도 같은 시간 기다린다. 2스테이지의 마지막 일반 상대는 쓰러짐 16프레임을 0.075초씩 보여 준 뒤 페이드아웃한다. 필요한 쓰러짐 그림이 없으면 기다리지 않고 기존 페이드를 쓴다.
- 3스테이지의 마지막 일반 상대도 쓰러짐 17프레임을 0.075초씩 보여 준 뒤 페이드아웃한다. 두 일반 상대의 쓰러짐 시트 중 하나라도 없으면 기다리지 않는다.
- 4스테이지의 마지막 일반 상대가 쓰러지면, 마검사·흑갑 호위 쓰러짐 시트가 모두 있을 때 17프레임을 0.075초씩 보여 준 뒤 페이드아웃한다. 둘 중 하나라도 없으면 기다리지 않는다.
- 5스테이지 최종 보스를 이기면, `Art/Enemies/tianmo_final_boss_down`이 104×104 프레임 25개의 시트(2600×104)일 때 25프레임을 0.075초씩(1.875초) 보여 준 뒤 페이드아웃한다. 시트가 없거나 크기가 다르면 기다리지 않는다.
- 3스테이지 보스를 이겼을 때도 쓰러짐 시트가 있으면 17프레임을 0.075초씩 보여 준 뒤 페이드아웃한다.

## 저장

저장 형식 판 1은 큰 스테이지 번호와 본 테마 소개 번호를 유지한다. 작은 스테이지 번호·남은 적·체력·현재 무리는 저장하지 않는다. 불러오기와 패배 후 재도전은 큰 스테이지의 1구간부터 새 무리를 고른다. 5스테이지를 불러오거나 재도전하면 일반 구간 없이 최종 보스 구간부터 시작한다. 오프라인 보상은 꺼져 있는 동안 스테이지나 구간을 진행하지 않는다.

## 범위 밖

- 나머지 테마의 실제 보스 외형·특수 기술, 나머지 테마의 새로운 UI 그림, 스테이지 선택과 수동 재도전은 별도 작업이다. 종남 보스의 접근과 종남·개방·남궁·천마신교 보스의 기술은 전투 사양에서 정한다.
- 최종 보스 처치 추가 보상, 회차 완료 보너스, 보스 등장 카메라 연출은 하지 않는다.
- 전투 좌표, 공격·피해·체력과 몬스터 능력치는 [전투 사양](combat.md), 저장 형식과 실패 처리는 [저장 사양](save.md), 보유 금화 표시와 강화는 각 사양을 따른다.

## 검증

- 여러 난수 시드에서 매 구간 2~3명과 2유닛 간격을 확인한다.
- 여섯 무리를 차례로 처치하면 번호가 1~7로 바뀌고, 매 구간 전투가 멈춘 상태에서 1.2초 소개가 끝나야 시작하며, 마지막에는 보스 한 명만 나온다.
- 일반·보스 보상과 동시 사망, 페이드·잠금, 실패와 재시작 시 1구간 복원, 5스테이지 다음 회차와 최대 번호를 확인한다.
- 모든 스테이지와 다음 회차의 번호·금빛 현재 표식·완료 표식, 해당 보스 이름과 체력 바, 중앙 소개 패널을 플레이 모드에서 확인한다.

### 완료 조건 (2026-11-22 추가)

이미 구현된 완료 조건 1~39는 기록 파일로 옮겼다. 새 조건은 40번부터 붙인다. 41~46은 지갑 `new Wallet(0)`, 조건마다 적은 공격력·최대 체력의 강화, 최소 난수(`Next(min, max)`가 항상 `min`을 돌려주는 `MinRandom`)로 만든 `StageProgress`를 쓴다. 좌표·비율은 오차 1e-6 안에서 비교한다.

40. `StageText.SubstageLabel(5, 7)` → `"FINAL"`, `(5, 1)` → `"FINAL"`, `(4, 7)` → `"4-7"`, `(1, 1)` → `"1-1"`. `stage` 0·6 또는 `substage` 0·8이면 `ArgumentOutOfRangeException`이다.
41. 시작 번호 5로 `Begin()` 하면 `SubstageNumber` 7, `IsBossActive` 참, 적은 보스 1명(체력 220, 공격력 26, 위치 = 제자 위치 + 24), `DefeatedNormalCount` 0, `BarRatio` 1.0, `Phase` `SubstageIntro`, `StageStarted` [5]이다.
42. 시작 번호 10이면 `Begin()` 뒤 보스 1명의 체력 370, 공격력 46이고, 이 보스를 이기면 금화가 140 는다.
43. 시작 번호 5에서 보스를 혼자 이기면 금화 +90, `StageCleared` [5]이다. 페이드 1.0초 뒤 `StageNumber` 6, `DisplayStageNumber` 1, `SubstageNumber` 1, 일반 상대 2명이다.
44. 시작 번호 5에서 제자가 쓰러지면 `StageFailed` [5]이다. 페이드 1.0초 뒤 `StageNumber` 5, `SubstageNumber` 7, 체력이 220인 보스 1명만 있고, `Phase`는 `SubstageIntro`(테마 소개 없음)이다.
45. 시작 번호 4는 기존과 같이 1구간에 일반 상대 2명으로 시작한다. 세 구간을 마치면 `BarRatio` 0.5, 여섯 구간 뒤에는 7구간 보스이다.
46. 시작 번호 5·본 소개 4로 `Begin()` 하면 `Intro` 3초 → `SubstageIntro` 1.2초 → `Playing` 순서이고, 적은 처음부터 보스 1명뿐이다.
47. (플레이 모드) 5스테이지 화면: 진행 표시 번호와 구간 소개 패가 `FINAL`, `Marker1~6`·`BossMarker`가 비활성, 진행 표시는 활성, 보스 이름 `HEAVENLY DEMON`, 체력 바 채움 `anchorMax.x` 1이다.
48. (플레이 모드) 1~4스테이지와 시작 번호 9: 일반 구간에는 표식이 활성이다. 여섯 구간 뒤 보스 구간에서는 `Marker1~6`·`BossMarker`가 비활성, 진행 표시는 활성, 번호 `"{n}-7"`, 보스 이름 TMP는 비활성이고 체력 바는 활성이다.
49. (플레이 모드) 시작 번호 4에서 마지막 일반 상대를 처치하면 0.5초 뒤 `FadeAlpha` 0이고, 1.5초 뒤 0보다 크다.
50. (플레이 모드) 시작 번호 5에서 최종 보스를 처치하면 쓰러짐 시트가 있을 때 1.5초 뒤 `FadeAlpha` 0이고, 2.5초 뒤 0보다 크다. 시트를 없앤 경우에는 0.5초 뒤 0보다 크다.
51. (플레이 모드) 시작 번호 1~5 각각에서 보스 구간이 되면 `BossHealthFrame`이 활성이고 그림이 `boss_health_frame_{n}`이며 `raycastTarget`은 거짓이다. 시작 번호 1이면 `StageBar`의 크기가 약 215.6×8.3, 중심이 약 (13.7, −113.4)이다(오차 0.1). 시작 번호 5이면 약 193.1×11.0이다.
52. (플레이 모드) 일반 구간에서는 `BossHealthFrame`이 비활성이고, `StageBar`의 위치·크기는 기존과 같다.
53. (플레이 모드) 틀 그림을 불러오지 못하게 하면 보스 구간에서 `BossHealthFrame` 없이 `StageBar`가 180×7, 중심 (0, −112)이다.

## 결정 기록

결정 기록은 [records/stage.md](records/stage.md)에 있다.

## 변경 기록

### 2026-11-22 — 4·5스테이지 진행 구체화: 4스테이지 천마신교 일반 상대, 5스테이지 최종 보스만 FINAL, 보스 구간 진행 표식 숨김 (기준 커밋: 346b9e3)

| 절 | 바뀌기 전 | 바뀐 뒤 |
| --- | --- | --- |
| 목표 | 모든 큰 스테이지가 일반 6구간 + 보스 | 5스테이지는 최종 보스 구간만, `FINAL` 표시 |
| 동작 | "변경 예정" 줄. 5스테이지도 일반 6구간 + 보스 | 4스테이지는 일반 6 + 보스, 5스테이지는 배치 때 바로 7구간 최종 보스 1명. 보상·능력치는 기존 공식, 패배 시 5스테이지 보스부터 재도전 |
| 수치 | — | 최종 스테이지 5 / 보스 구간만 |
| 연결 지점 | — | `StageLayout.FinalDisplayStage`, `StageText.SubstageLabel(int, int)` 추가 |
| 화면 | 모든 스테이지 표식 표시, `5-7` 번호, 5스테이지 마지막 천마 일반 상대 쓰러짐 대기 | 보스 등장 시 1~5스테이지 모두 표식 숨김, 5스테이지 `FINAL`, 4스테이지 마지막 일반 상대 17프레임 대기, 5스테이지 최종 보스 25프레임 대기 |
| 화면 | 보스 체력은 180×7 단색 붉은 바 | 보스 구간에 스테이지별 보스 체력 틀(`Art/UI/boss_health_frame_1~5`, 300×100) 표시, 바를 틀의 게이지 칸에 맞춤. 틀이 없으면 기존 바 |
| 저장 | — | 5스테이지 불러오기·재도전은 최종 보스 구간부터 |
| 범위 밖 | — | 최종 보스 추가 보상·회차 보너스·카메라 연출 제외 |

- 완료 조건 초안: 새로 40~53 / 바뀜 없음 / 1~39는 이미 기록 파일에 있음.
- 바뀌는 연결 지점: 추가만 있다. `StageLayout.FinalDisplayStage`, `StageText.SubstageLabel(int stage, int substage)`. 기존 연결 지점은 그대로다.
- 고쳐야 할 기존 코드: `Assets/Scripts/Core/Stage/StageProgress.cs`(5스테이지 배치), `StageText.cs`, `StageLayout.cs`, `Assets/Scripts/Game/Stage/StageView.cs`(스테이지 글자·쓰러짐 대기 조건), `SubstagePreview.cs`(번호·표식 숨김), `SubstageIntroBanner.cs`(번호 글자). 보스 체력 틀 그림 `Assets/Resources/Art/UI/boss_health_frame_1~5.png`와 `.meta`는 사람이 미리 넣었다(코드에서 불러오기만 한다). 전투 화면 쪽은 [전투 사양](combat.md)의 같은 날 변경 기록.
- 기존 테스트:

| 테스트 | 지금 | 바뀐 뒤 | 처리 |
| --- | --- | --- | --- |
| `StageHudTests.AllThemes_ShareTheStageOneProgressHud` | `TestCase(5, "5-1")` (5스테이지 1구간 표식) | `TestCase(4, "4-1")` | 이름 유지, 케이스 데이터만 교체 |
| `StageHudTests.ProgressHud_ChangesFromProgressToBossHealth` | `TestCase(5, 5)`, 보스 때 진행 표식 검사 없음 | `TestCase(9, 4)`로 교체, 보스 구간에서 `Marker1~6`·`BossMarker` 비활성 검사 추가 | 이름 유지, 검사 수는 줄이지 않음 |
| `TianmoSpriteTests.LastNormalOpponent_StageFourWaitsForDownBeforeFade` (2026-11-22 사람이 이름을 `…StageFive…`에서 바꿈) | 시작 번호 5의 마지막 일반 상대 | 시작 번호 4 (`new StageProgress(…, 4, 4)`) | 이름은 이미 바뀜, 본문의 번호만 교체 |
| `StageProgressRoundTests.FifthStageCleared_AdvancesToSecondRoundAtCoveredTransition`, `StageProgressIntroTickTests.FifthStageCleared_SecondRoundHasNoIntro` | 시작 번호 5에서 보스를 직접 넣어 처치 | 같음 (배치된 최종 보스는 24유닛 앞이라 닿지 않음) | 고치지 않고 계속 통과해야 함 |
