# 스테이지 기록

[stage.md](../stage.md) 사양의 이력이다. 구현에 필요한 규칙은 사양 문서에 모두 있으므로 **구현하는 에이전트는 읽지 않아도 된다.** 기획 스킬(`plan-feature`)이 이미 정한 것을 다시 묻지 않으려고 읽는다.

## 결정 기록

| 질문 | 답 | 방식 |
| --- | --- | --- |
| 스테이지 사양이 맡는 범위 | 배치, 보스 등장, 성공·실패와 재도전, 처치 금화 계산·지급, 스테이지 UI, 페이드. 이동·공격·체력·몬스터 능력치·카메라·배경은 전투 | 이미 정해짐 (`docs/spec/combat.md`) |
| 보스 등장 거리 | 열 번째 처치 순간의 제자 좌표 + 18.0 유닛 | 사용자 선택 (추천안) / 임시값 |
| 페이드가 가리는 범위 | 전투 화면만. 금화, 강화 버튼, 스테이지 글자, 진행 바는 그대로 보인다 | 사용자 선택 (추천안) |
| 페이드 단계와 시간을 누가 세나 | Core의 `StageProgress.Tick`. 남은 시간은 다음 단계로 넘긴다 | 직접 정함 |
| 성공·실패 뒤 전환이 시작되는 때 | 기다림 없이 바로 페이드아웃 | 직접 정함 (기획 그대로) |
| 게임을 처음 켰을 때 | 페이드 없이 `Begin()`으로 바로 1스테이지 시작 | 직접 정함 |
| 열 번째 일반 몬스터와 제자가 함께 쓰러질 때 | 금화는 주고, 보스를 내지 않고 실패 | 직접 정함 |
| 처치 수가 10을 넘거나, 10 전에 보스 종류가 쓰러질 때 | 적을 `Kind`로만 가린다. 금화는 주고 처치 수는 10에서 멈춘다. 보스 종류가 쓰러지면 성공 | 직접 정함 |
| 무리 마리 수를 고르는 난수 | `random.Next(1, Math.Min(3, 남은 수) + 1)`을 남은 수가 0이 될 때까지 | 직접 정함 (기획의 규칙을 식으로 옮김) |
| 무리의 좌표 | 첫 무리 중심 6.0, 중심 간격 3.0, 무리 안은 중심 기준 대칭으로 0.3 간격 | 직접 정함 / 임시값 |
| 숫자 타입 | 스테이지 번호 `int`, 금화 `long`, 시간·좌표·비율 `double` | 직접 정함 |
| 스테이지 번호의 상한 | `int.MaxValue`에서 더 오르지 않는다 | 직접 정함 → 바뀜: 2026-11-17, 전체 진행 번호의 상한으로 유지하고 화면상 번호는 1~5 순환 |
| 잘못된 저장 값(1보다 작은 번호) | 1로 시작한다. 예외 없음 | 직접 정함 (기존 사양과 같은 방식) |
| 잘못된 인자(`null`, 음수 시간, 스테이지 0) | 예외 (`ArgumentNullException`, `ArgumentOutOfRangeException`) | 직접 정함 |
| 한글 글꼴이 없을 때 글자 | `STAGE 1`, 보스 이름 `GATEKEEPER` | 직접 정함 (기존 사양과 같은 방식) / 임시값 → 바뀜: 2026-11-17, 회차·테마와 테마별 영문 보스 이름 표시 |
| 진행 바와 글자의 위치·색 | 위쪽 가운데. 일반 노랑, 보스 빨강. 좌표와 크기는 임시값 | 직접 정함 / 임시값 |
| 보스가 쓰러진 뒤의 바 | 가려진 순간의 교체까지 보스 체력 바(0%) 그대로 | 직접 정함 |
| 타입·메서드 이름과 알림 | `StageProgress`, `StagePhase`, `StageRewards`, `StageLayout`, `StageText`, `StageView`. 알림은 `StageStarted`, `StageCleared`, `StageFailed`, `Changed` | 직접 정함 |
| 테마 외형의 범위 | 테마와 이름만 정하고 기존 임시 배경·대체 표시 사용. 에셋 적용은 별도 | 추천 수락, 2026-11-17 |
| 회차별 적과 보상 증가 | 전체 진행 번호를 기존 전투·보상 공식에 넘긴다 | 추천 수락, 2026-11-17 |
| 저장할 진행 값 | 전체 진행 번호 하나를 저장해 회차와 화면상 번호를 계산 | 추천 수락, 2026-11-17 |
| 회차와 테마 표시 | 영문 `ROUND 2 · STAGE 1 · JONGNAM` 형식 | 추천 수락, 2026-11-17 |
| 전체 진행 번호 상한 | `int.MaxValue`에서 같은 스테이지 반복 | 추천 수락, 2026-11-17 |
| 보스 이름 | 테마별 영문 임시 이름 | 추천 수락, 2026-11-17 |
| 스테이지 소개 | 별도 기획으로 범위 밖 | 사용자 지정, 2026-11-17 → 바뀜: 2026-11-17, 이번 고치기에서 기획 |
| 기존 연결 지점과 테스트 | `StageNumber`와 기존 알림은 전체 진행 번호, 기존 표시 API는 유지하고 화면용 속성과 메서드를 추가한다 | 사용자 요청에 따른 수정, 2026-11-17. 앞서 화면상 번호로 바꾼 결정은 기존 테스트와 충돌하여 변경 |
| 소개와 페이드의 순서 | 페이드인이 끝난 뒤 소개 3초, 그다음 전투 | 추천 수락, 2026-11-17 |
| 소개 중 강화 | 잠그지 않는다. 전투와 이동만 멈춘다 | 추천 수락, 2026-11-17 |
| 소개 넘기기 | 없다. 3초 뒤 자동으로만 사라진다 | 추천 수락, 2026-11-17 |
| 재실행·불러오기 뒤 본 소개 | 본 소개 번호 `SeenIntroStage`를 저장해 다시 보여 주지 않는다 | 추천 수락, 2026-11-17 |
| 소개 글자 | 임시 영문. 한글 원문은 사양에 함께 둔다 | 추천 수락, 2026-11-17 |
| 기존 테스트와의 충돌 | 생성자 인자 `seenIntroStage` 기본값 5(소개 없음). `GameRoot`가 0을 넣는다 | 추천 수락, 2026-11-17 |
| 본 소개 번호를 올리는 때 | 소개를 보여 주기로 정한 순간(배치 직후, `StageStarted` 전) | 직접 정함 |
| 남은 시간 | 페이드인 뒤 남은 시간은 소개로 넘기고, 소개 뒤 남은 시간은 버린다 | 직접 정함 → 바뀜: 2026-11-17, 긴 프레임에서 소개가 한 번도 그려지지 않을 수 있어 페이드인 뒤 남은 시간도 버리고 소개는 0초부터 센다 (교차 평가 반영) |
| 전투와 스테이지의 프레임 갱신 순서 | 정하지 않는다. 멈춤이 풀리는 프레임의 한 프레임 차이는 허용 | 사용자 승인, 2026-11-17 (교차 평가 반영) |
| 페이드인 중·소개 중에 꺼졌을 때 | 그 소개는 이미 본 것으로 다시 보여 주지 않는다. 저장 기능은 `Begin()` 전에 `StageStarted`를 구독한다 | 사용자 승인, 2026-11-17 (교차 평가 반영) |
| 5스테이지 구간 번호와 `SubstageNumber` | 화면은 번호 대신 `FINAL`, Core 값은 보스 구간 7 | 사용자 선택, 2026-11-22 |
| 보스 구간의 진행 표식 | 1~5스테이지 모두 보스가 등장하면 매화 표식 6개와 보스 표식을 숨기고 보스 이름·체력 바만 남긴다 | 사용자 지정, 2026-11-22 |
| 보스 구간의 매화 제목판·번호 글자 | 남기고 표식만 숨긴다 | 추천 수락, 2026-11-22 |
| 5스테이지 보스 체력 바와 `BarRatio` | 기존 붉은 바, 처음부터 보스 체력 비율 | 추천 수락, 2026-11-22 → 바뀜: 2026-11-22, 사용자가 만든 스테이지별 보스 체력 틀을 씌운다(채움 색·`BarRatio`는 그대로) |
| 5스테이지 진입 연출 | 기존 순서(페이드인 → 1회차 첫 방문만 테마 소개 3초 → 구간 소개 1.2초)에 번호 패만 `FINAL` | 추천 수락, 2026-11-22 |
| 5스테이지 패배·저장·다음 회차 | 기존 규칙 그대로: 같은 5스테이지 보스부터 재도전, 전체 진행 번호만 저장, 승리 시 다음 회차 1-1 | 이미 정해짐 (이 사양의 동작·저장) |
| 최종 보스 처치 보상 | 기존 `BossGold(StageNumber)` 그대로. 추가 보상·회차 보너스 없음 | 사용자 선택, 2026-11-22 |
| 천마 일반 상대 이름이 들어간 기존 테스트 | 사람이 루프 전에 대화에서 `StageFive…` → `StageFour…`로 이름만 바꾸고 main 에 커밋한다. 본문은 루프가 바꾼다 | 사용자 지정, 2026-11-22 |
| 새 표시 API 이름 | `StageText.SubstageLabel(int, int)`, `StageLayout.FinalDisplayStage` | 직접 정함, 2026-11-22 |
| 보스 체력 바 그림 | 사용자가 만든 스테이지별 틀 5개를 적용. 채움·바탕 그림은 없어 기존 색 바를 틀의 게이지 칸에 맞춘다 | 사용자 지정, 2026-11-22 |
| 보스 체력 틀 위치 | 위쪽 기존 보스 바 자리, 더 크게 (300×100, 중심 (0, −126), 임시) | 추천 수락, 2026-11-22 |
| 게임용 틀 사본 | 원본 그대로 `Assets/Resources/Art/UI/boss_health_frame_1~5.png`로 복사, `.meta`는 `jongnam_stage_hud`처럼 최대 2048·밉맵·필터 2. 이 대화에서 미리 만든다 | 추천 수락, 2026-11-22 |
| 게이지 칸 좌표 | 틀마다 투명 게이지 영역을 원본 픽셀로 재서 표로 둔다 | 직접 정함, 2026-11-22 |
| 표식 숨김 기준 | `IsBossActive`. `Marker1~6`·`BossMarker`의 `gameObject` 비활성화, 스프라이트 값은 그대로 | 직접 정함, 2026-11-22 |

## 구현된 완료 조건

### 2026-11-17 옮김 — 처음 사양(c98cc92)의 조건 중 구현되고 바뀌지 않은 것 (기록 013~019)

번호는 사양의 원래 번호다. 아래 말은 그때의 사양 기준이다.

아래에서 따로 적지 않으면 지갑은 `new Wallet(0)`, 강화는 항목마다 적힌 `new StatUpgrades(wallet, 공격력, 최대 체력, 1.0)`, 전투는 `new Battle(upgrades)`, 스테이지는 `new StageProgress(wallet, upgrades, battle, 최소 난수)`로 만든 것이다.

- **최소 난수**: `System.Random`을 상속해 `Next(int minValue, int maxValue)`가 항상 `minValue`를 돌려주게 한 테스트용 클래스. 무리 구성은 `1`이 10개, 좌표는 6.0, 9.0, …, 33.0이다.
- **최대 난수**: 같은 방식으로 항상 `maxValue − 1`을 돌려주고, 받은 `maxValue`를 순서대로 기록하는 테스트용 클래스. 무리 구성은 `3·3·3·1`이다.
- "시작한 뒤"는 `Begin()`을 부른 뒤다. "n칸"은 `battle.Step()`을 n번 부르는 것이다. "적을 직접 넣는다"는 테스트가 `battle.AddEnemy`를 부르는 것이다. 좌표·비율은 오차 1e-6 안에서 비교한다.

보상 (`StageRewardsTests`)

1. `StageRewards.NormalGold`는 1·2·3스테이지에서 10·12·14, `StageRewards.BossGold`는 50·60·70이다.
2. `StageRewards.NormalGold(int.MaxValue)`는 4294967302, `StageRewards.BossGold(int.MaxValue)`는 21474836510이다.
3. `StageRewards.NormalGold(0)`과 `StageRewards.BossGold(0)`은 각각 `ArgumentOutOfRangeException`을 던진다.

배치 (`StageLayoutTests`)

4. `StageLayout.RollGroups(최대 난수)`는 `3, 3, 3, 1`이고, 난수가 받은 `maxValue`는 순서대로 4, 4, 4, 2다.
5. `StageLayout.RollGroups(최소 난수)`는 `1`이 10개다.
6. 시드 0부터 999까지의 `new System.Random(시드)`마다 `StageLayout.RollGroups`의 결과는 합이 10이고, 모든 원소가 1 이상 3 이하이며, 길이가 4 이상 10 이하다.
7. `StageLayout.Positions(new[] { 2, 1, 3, 1, 3 })`은 5.85, 6.15, 9.0, 11.7, 12.0, 12.3, 15.0, 17.7, 18.0, 18.3이다. `Positions(new[] { 3, 3, 3, 1 })`은 5.7, 6.0, 6.3, 8.7, 9.0, 9.3, 11.7, 12.0, 12.3, 15.0이다.
8. `StageLayout.Positions(빈 배열)`은 빈 배열이다. `Positions(null)`과 `RollGroups(null)`은 `ArgumentNullException`을, `Positions(new[] { 0 })`과 `Positions(new[] { 4 })`는 `ArgumentOutOfRangeException`을 던진다.

진행 (`StageProgressTests`)

9. (10, 100) 새로 만들면 `StageNumber`는 1, `HasBegun`은 `false`, `Phase`는 `Playing`, `DefeatedNormalCount`는 0, `Boss`는 `null`, `IsBossActive`는 `false`, `BarRatio`는 0.0, `FadeAlpha`는 0.0이고, `battle.Enemies.Count`는 0이다. `startStage`에 5를 넣으면 `StageNumber`는 5, 0이나 −3을 넣으면 1이고 예외가 없다.
10. 생성자의 `wallet`, `upgrades`, `battle`, `random`에 각각 `null`을 넣으면 `ArgumentNullException`을 던진다.
11. (10, 100) `Begin()`을 부르면 `HasBegun`은 `true`, `battle.Enemies.Count`는 10이고, 적의 `Position`은 순서대로 6.0, 9.0, 12.0, 15.0, 18.0, 21.0, 24.0, 27.0, 30.0, 33.0, 모두 `Kind`가 `Normal`이고 `MaxHealth`가 30이다. `StageStarted`가 1을 담아 1번, 그 뒤에 `Changed`가 1번 온다. `battle.Paused`와 `upgrades.Locked`는 `false`다. `Begin()`을 한 번 더 부르면 `battle.Enemies.Count`는 10 그대로이고 알림은 더 오지 않는다.
12. (10, 100) `startStage`를 3으로 만들고 시작하면 적 10마리의 `MaxHealth`는 모두 50이다.
13. (10, 100) 최대 난수로 만들고 시작하면 적의 `Position`은 순서대로 5.7, 6.0, 6.3, 8.7, 9.0, 9.3, 11.7, 12.0, 12.3, 15.0이다.
14. (1000, 100) `Begin()`을 부르지 않고 `Normal` 적 하나를 1스테이지, 좌표 0.8에 직접 넣어 1칸 진행하면, 금화는 0, `DefeatedNormalCount`는 0이고 `Changed`는 0번 온다. `Tick(1.0)`을 불러도 `Phase`는 `Playing`이고 `battle.Enemies.Count`는 0이다.
15. (1000, 100) 시작한 뒤 `Normal` 적 3마리를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행하면, 금화는 30, `Wallet.BalanceChanged`의 값은 순서대로 10, 20, 30, `DefeatedNormalCount`는 3, `BarRatio`는 0.3, `Changed`는 (시작 때의 1번 뒤로) 3번 오고, `Phase`는 `Playing`이다.
16. (1000, 100) 시작한 뒤 `Normal` 적 10마리를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행하면, 금화는 100, `DefeatedNormalCount`는 10, `IsBossActive`는 `true`, `Boss.Kind`는 `Boss`, `Boss.Position`은 18.0, `Boss.MaxHealth`는 100, `BarRatio`는 1.0, `battle.Enemies.Count`는 11, `battle.DiscipleHealth`는 50, `Phase`는 `Playing`이다.
17. (1000, 100) 시작한 뒤 `Normal` 적 11마리를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행하면, 금화는 110, `DefeatedNormalCount`는 10, `battle.Enemies.Count`는 11 (보스는 하나뿐), `battle.DiscipleHealth`는 45다.
18. (30, 100000) 시작한 뒤 `Normal` 적 10마리를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행한 다음, `Boss.Health`가 100보다 작아질 때까지 최대 5000칸 진행하면, `Boss.Health`는 70, `BarRatio`는 0.7이고, 마지막 칸에서 `Changed`가 1번 이상 온다.
19. (1000, 100) 시작한 뒤 `Boss` 적 하나를 1스테이지, 좌표 1.0에 직접 넣고 1칸 진행하면, 금화는 50, `StageCleared`가 1을 담아 1번, `StageFailed`는 0번 온다. `Phase`는 `FadingOut`, `StageNumber`는 1, `FadeAlpha`는 0.0, `battle.Paused`는 `true`, `upgrades.Locked`는 `true`, `battle.DiscipleHealth`는 90이다. `StageCleared`를 받는 시점에 읽은 `Phase`는 `FadingOut`이고, `Changed`는 `StageCleared` 뒤에 온다.
20. (10, 5) 시작한 뒤 `Normal` 적 하나를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행하면, 금화는 0, `DefeatedNormalCount`는 0, `StageFailed`가 1을 담아 1번, `StageCleared`는 0번 온다. `Phase`는 `FadingOut`, `battle.Paused`는 `true`, `upgrades.Locked`는 `true`다.
21. (30, 5) 시작한 뒤 `Normal` 적 하나를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행하면 (제자와 일반 몬스터가 함께 쓰러짐), 금화는 10, `DefeatedNormalCount`는 1, `StageFailed`가 1번, `StageCleared`는 0번 온다.
22. (100, 10) 시작한 뒤 `Boss` 적 하나를 1스테이지, 좌표 1.0에 직접 넣고 1칸 진행하면 (제자와 보스가 함께 쓰러짐), 금화는 0, `StageCleared`는 0번, `StageFailed`가 1을 담아 1번 오고 `Phase`는 `FadingOut`이다.
23. (1000, 50) 시작한 뒤 `Normal` 적 10마리를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행하면 (열 번째 몬스터와 제자가 함께 쓰러짐), 금화는 100, `DefeatedNormalCount`는 10, `Boss`는 `null`, `battle.Enemies.Count`는 10, `StageFailed`가 1번 온다.
24. 19번에 이어 `Tick(0.25)`를 부르면 `FadeAlpha`는 0.5, `Phase`는 `FadingOut`, `StageNumber`는 1이다. 한 번 더 `Tick(0.25)`를 부르면 `StageNumber`는 2, `Phase`는 `FadingIn`, `FadeAlpha`는 1.0, `DefeatedNormalCount`는 0, `Boss`는 `null`, `BarRatio`는 0.0, `battle.Enemies.Count`는 10, 적의 `MaxHealth`는 모두 40, `Position`은 6.0부터 3.0씩 33.0까지, `battle.DisciplePosition`은 0, `battle.DiscipleHealth`는 100, `battle.Paused`와 `upgrades.Locked`는 `true`, 금화는 50이고, `StageStarted`가 2를 담아 1번 온다.
25. 24번에 이어 `Tick(0.25)`를 부르면 `FadeAlpha`는 0.5, `Phase`는 `FadingIn`이다. 한 번 더 `Tick(0.25)`를 부르면 `Phase`는 `Playing`, `FadeAlpha`는 0.0, `battle.Paused`와 `upgrades.Locked`는 `false`이고, `StageStarted`는 더 오지 않는다.
26. 20번에 이어 `Tick(0.5)`를 부르면 `StageNumber`는 1, `Phase`는 `FadingIn`, `battle.Enemies.Count`는 10, `battle.DiscipleHealth`는 5, `battle.IsDiscipleDefeated`는 `false`이고, `StageStarted`가 1을 담아 한 번 더 온다. 이어서 `Tick(0.5)`를 부르면 `Phase`는 `Playing`이다.
27. 19번에 이어 `Tick(0.3)`을 2번 부르면 `Phase`는 `FadingIn`, `StageNumber`는 2, `FadeAlpha`는 0.8이다.
28. 19번에 이어 `Tick(2.0)`을 부르면 `Phase`는 `Playing`, `StageNumber`는 2, `battle.Paused`와 `upgrades.Locked`는 `false`이고, `StageStarted`가 2를 담아 1번 온다.
29. 19번에 이어 (금화 50, `FadingOut`) `upgrades.TryUpgrade(StatType.Attack)`은 `false`이고 금화는 50 그대로다. `Tick(1.0)` 뒤에는 `true`이고 금화는 30이다.
30. (10, 100) 시작한 뒤 (`Playing`) `Tick(1.0)`과 `Tick(0)`을 불러도 `Phase`는 `Playing`, `StageNumber`는 1, `battle.Enemies.Count`는 10이고 알림은 더 오지 않는다. `Tick(-0.1)`, `Tick(double.NaN)`, `Tick(double.PositiveInfinity)`는 `Begin()` 전에도 뒤에도 각각 `ArgumentOutOfRangeException`을 던진다.

진행 (`StageProgressTests`, 이어서)

32. (1000, 100000) 시작한 뒤 아무것도 직접 넣지 않고 `Phase`가 `FadingOut`이 될 때까지 최대 20000칸 진행하면 (스테이지 한 판 전체), 금화는 150, `DefeatedNormalCount`는 10, `StageCleared`가 1을 담아 1번, `StageFailed`는 0번 오고, `IsBossActive`는 `true`, `BarRatio`는 0.0이다.

화면 (자동 테스트 없음. 사람이 실행해 확인)

38. `TempEncounter` 클래스와 `GameRoot`의 `[TempEncounter]` 조립이 없다.
39. 기존 테스트 `CoreAssemblyTests.Core_DoesNotReferenceUnity`가 계속 통과한다.

## 구현된 변경 기록

없음.
