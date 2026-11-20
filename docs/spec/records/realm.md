# 경지 돌파 기록

[realm.md](../realm.md) 사양의 이력이다. 구현에 필요한 규칙은 사양 문서에 모두 있으므로 **구현하는 에이전트는 읽지 않아도 된다.** 기획 스킬(`plan-feature`)이 이미 정한 것을 다시 묻지 않으려고 읽는다.

## 결정 기록

| 질문 | 답 | 방식 |
| --- | --- | --- |
| 이 사양이 맡는 범위 | 경지 단계·비용·배율·버튼·경지 표시·오오라. 보너스 적용은 능력치 강화, 현재 체력 증가는 전투, 저장 형식은 저장 기능 | 사용자 선택 (요청에서 미리 정함) |
| 2단계 저장 데이터와의 호환 | 하지 않는다. 지우고 새로 시작 | 사용자 선택 (요청에서 미리 정함) |
| 에셋이 없을 때 | 대체 표시로 동작하고, Resources 경로에 그림을 넣으면 바뀐다 | 사용자 선택 (요청에서 미리 정함) |
| 페이드 중 돌파 | 강화와 함께 잠근다. 경지에 `SetLocked`를 두고 `GameRoot`가 `StatUpgrades.LockedChanged`를 따라 건다 | 추천 수락 |
| 돌파 성공 연출 | 강화 버튼 연출(`StatUpgradeButtonEffect`)을 재사용하고, 오오라를 0.4초 동안 1.5배로 키운다 | 추천 수락 |
| 경계 상황 (금화 부족·최대 경지·잠김, 잘못된 저장 값) | 강화와 같다: `false`, 값 그대로, 알림 없음. 저장 값은 0~4로 잘라낸다 | 직접 정함 (기존 강화 규칙을 따름) |
| 보너스 배율의 타입 | 정수 퍼센트 `int` (0/10/20/30/40). 적용은 `long` 정수 나눗셈 | 추천 수락 |
| 능력치 강화·전투와의 연결 | 강화가 `RealmProgress`를 받아 `BonusPercent`를 읽는다. 전투는 `BrokeThrough`를 받아 최대 체력 차이만큼 현재 체력을 늘린다 | 추천 수락 |
| 한글 글꼴 없을 때 경지 이름 | 영어 번역: THIRD RATE / SECOND RATE / FIRST RATE / PEAK / TRANSCENDENT | 추천 수락 |
| 현재 경지 표시 위치 | 화면 왼쪽 위 구석 (8, −8) | 추천 수락 |
| 돌파 버튼 위치 | 아래쪽 가운데, HP 버튼 위 (220, 62) 200×48 | 직접 정함 / 임시값 |
| 삼류 오오라 | 없음. 이류부터 보인다 | 추천 수락 |
| 수치 | 비용 200/500/1,000/2,000, 보너스 0/10/20/30/40 % | 기획 문서 그대로 (임시) |
| 오오라 대체 표시 | 원형 그러데이션, 경지별 색, 1.6초 깜빡임 | 직접 정함 / 임시값 |
| 저장 값 | `realmLevel` `int` 0~4, 처음 0 | 직접 정함 |
| 2026-11-20 고치기: 범위 | 돌파 연출과 상시 오오라를 함께 개편한다 (고치기 모드, 이 사양 안에서) | 추천 수락 |
| 2026-11-20 고치기: 에셋이 없을 때 | 코드로 만든 임시 그림으로 전체 연출을 먼저 구현하고, `Art/Effects/` 그림을 넣으면 바뀐다 | 추천 수락 |
| 2026-11-20 고치기: 경지별 차이 | 단계마다 층이 하나씩 늘어난다: 이류 기운·꽃잎, 일류 +바닥 고리, 절정 +빛기둥, 초절정 +충격파·흰 번쩍임. 크기·밝기·꽃잎 수도 커진다 | 추천 수락 |
| 2026-11-20 고치기: 연출 중 게임 진행 | 전투는 계속, 히트스톱 없음 (`Time.timeScale` 그대로) | 추천 수락 |
| 2026-11-20 고치기: 연출 중 또 돌파 | 새 경지로 처음부터 | 추천 수락 |
| 2026-11-20 고치기: 연출 중 버튼 | 평소처럼 누를 수 있음 (잠금 규칙 그대로) | 추천 수락 |
| 2026-11-20 고치기: 경지 색 | 매화 분홍으로 통일, 경지가 오를수록 진하게, 초절정 돌파 부품은 흰 중심광. 이전의 하늘·파랑·보라·분홍 → 바뀜: 2026-11-20, 시안과 매화 테마에 맞춤 | 추천 수락 |
| 2026-11-20 고치기: 단계별 세기 | "보통" 표 (꽃잎 12/18/24/36, 어둡게 25~60%, 흔들림 0~3px, 빛 1~3, 고리 1.5~2.5, 기둥 5~7, 상시 기운 2~4줄·꽃잎 0~5장) (임시) | 추천 수락 |
| 2026-11-20 고치기: 빛 표현 | Light2D 점광원 + 돌파 때만 블룸, 모두 코드로 만든다. 전역 광원이 없으면 하나 만든다 | 추천 수락 |
| 2026-11-20 고치기: 어둡게 하는 범위 | 배경과 적까지. 제자·이펙트·체력 바·피해 숫자는 밝게 | 추천 수락 |
| 2026-11-20 고치기: 기존 오오라 0.4초 크기 연출 | `BurstScale`을 그대로 두고 터지는 때(0.5초)부터 적용 → 바뀜: 2026-11-20, 돌파 연출 시간표에 맞춤 | 직접 정함 |
| 2026-11-20 고치기: 흔들림·블룸 세기·정렬 순서·대체 그림 모양 | 흔들림 픽셀 단위, 블룸 = 빛 × 0.5, 정렬 9980~10003·29990, 대체 그림은 실행 중 생성 | 직접 정함 / 임시값 |

## 구현된 완료 조건

### 2026-11-20 옮김 — 처음 사양(2026-11-18)의 조건 (기록 045~057)

번호는 사양의 원래 번호다. 아래 말은 그때의 사양 기준이다. 26번과 31번은 2026-11-20 고치기에서 바뀌어 사양에 남았다.

아래에서 따로 적지 않으면 `new RealmProgress(wallet)`으로 만든 것이다. 실수는 오차 1e-9 안에서 비교한다.

경지 로직 (`RealmProgressTests`)

1. 새로 만들면 `Current`는 `ThirdRate`, `IsMax`는 `false`, `BonusPercent`는 0, `NextCost`는 200, `Locked`는 `false`다.
2. 금화 3700에서 `TryBreakthrough`를 4번 부르면 모두 `true`이고, 부르기 전의 `NextCost`는 순서대로 200, 500, 1000, 2000이며, 끝난 뒤 `Current`는 `Transcendent`, `IsMax`는 `true`, `BonusPercent`는 40, `NextCost`는 0, 금화는 0이다.
3. 금화 1000에서 `TryBreakthrough`를 1번 부르면 `Current`는 `SecondRate`, `BonusPercent`는 10, `NextCost`는 500, 금화는 800이다.
4. 금화 199에서 `TryBreakthrough`는 `false`이고 `Current`는 `ThirdRate`, 금화는 199이며 `BrokeThrough`와 `BalanceChanged`는 0번 온다.
5. 금화 200에서 `TryBreakthrough`는 `true`이고 `Current`는 `SecondRate`, 금화는 0이다.
6. 금화 1000에서 `TryBreakthrough`를 3번 부르면 결과는 순서대로 `true`, `true`, `false`이고 `Current`는 `FirstRate`, 금화는 300이다.
7. `new RealmProgress(wallet, 4)`에 금화 10000이 있어도 `TryBreakthrough`는 `false`, `CanBreakthrough`는 `false`이고 금화는 10000, `Current`는 `Transcendent`이며 `BrokeThrough`와 `BalanceChanged`는 0번 온다.
8. 금화 1000에서 `TryBreakthrough`를 부르면 `BrokeThrough`가 `SecondRate`를 담아 1번 오고, 받는 쪽에서 읽은 `Current`는 `SecondRate`, `BonusPercent`는 10, `NextCost`는 500이다.
9. 금화 1000에서 `TryBreakthrough`를 부르면 `Wallet.BalanceChanged`가 `BrokeThrough`보다 먼저 오고, `BalanceChanged`를 받는 시점에 읽은 `Current`는 `ThirdRate`다.
10. 금화 200에서 `CanBreakthrough`는 `true`, 금화 199에서는 `false`다. 부른 뒤 금화와 경지는 그대로이고 알림은 0번 온다.
11. `SetLocked(true)`를 부르면 `Locked`는 `true`이고 `LockedChanged`가 `true`를 담아 1번 온다. 한 번 더 `SetLocked(true)`를 부르면 `LockedChanged`는 더 오지 않는다.
12. 금화 1000에서 `SetLocked(true)` 뒤 `CanBreakthrough`는 `false`, `TryBreakthrough`는 `false`이고 금화는 1000, `Current`는 `ThirdRate`이며 `BrokeThrough`는 0번 온다.
13. 12번 상태에서 `SetLocked(false)`를 부르면 `LockedChanged`가 `false`를 담아 1번 오고, `TryBreakthrough`는 `true`다.
14. 새로 만든 직후 `SetLocked(false)`를 부르면 `LockedChanged`는 0번 온다.
15. `new RealmProgress(wallet, 2)`의 `Current`는 `FirstRate`, `BonusPercent`는 20, `NextCost`는 1000이고, 만드는 동안 금화는 바뀌지 않고 알림은 0번 온다.
16. `new RealmProgress(wallet, -1)`의 `Current`는 `ThirdRate`, `new RealmProgress(wallet, 7)`의 `Current`는 `Transcendent`이고 둘 다 예외가 없다.
17. `new RealmProgress(null)`은 `ArgumentNullException`을 던진다.

규칙 (`RealmRulesTests`)

18. `RealmRules.BonusPercent`는 `ThirdRate`부터 순서대로 0, 10, 20, 30, 40을 돌려주고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다.
19. `RealmRules.BreakthroughCost`는 `ThirdRate`부터 순서대로 200, 500, 1000, 2000, 0을 돌려주고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다. `RealmRules.LevelCount`는 5다.

표시 글자 (`RealmTextTests`)

20. `RealmText.Name`은 `ThirdRate`부터 순서대로 `"THIRD RATE"`, `"SECOND RATE"`, `"FIRST RATE"`, `"PEAK"`, `"TRANSCENDENT"`를 돌려주고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다.
21. `RealmText.Label(SecondRate)`는 `"REALM: SECOND RATE"`, `RealmText.Label(ThirdRate)`는 `"REALM: THIRD RATE"`이고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다.
22. `RealmText.AssetId`는 `ThirdRate`부터 순서대로 `"third_rate"`, `"second_rate"`, `"first_rate"`, `"peak"`, `"transcendent"`를 돌려주고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다.
23. `RealmText.Cost`는 (500, `false`) → `"G 500"`, (1000, `false`) → `"G 1,000"`, (0, `true`) → `"MAX"`, (2000, `true`) → `"MAX"`, (-5, `false`) → `"G -5"`를 돌려준다.
24. `RealmText.ButtonLabel`은 (`SecondRate`, 500, `false`) → `"BREAKTHROUGH\nSECOND RATE\nG 500"`, (`Transcendent`, 0, `true`) → `"BREAKTHROUGH\nTRANSCENDENT\nMAX"`를 돌려주고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다. `RealmText.ButtonTitle`은 `"BREAKTHROUGH"`다.

오오라 규칙 (`RealmAuraTests`)

25. `RealmAura.HasAura`는 `ThirdRate`에 `false`, 나머지 넷에 `true`를 돌려주고, `(RealmLevel)99`는 `ArgumentOutOfRangeException`을 던진다.
27. `RealmAura.PulseAlpha(SecondRate, t)`는 t = 0 → 0.2625, 0.4 → 0.35, 1.2 → 0.175, 1.6 → 0.2625이고, `PulseAlpha(ThirdRate, 0.4)`는 0이다.
28. `RealmAura.PulseAlpha(SecondRate, double.NaN)`과 `PulseAlpha(SecondRate, double.PositiveInfinity)`는 `ArgumentOutOfRangeException`을, `PulseAlpha((RealmLevel)99, 0)`도 `ArgumentOutOfRangeException`을 던진다.
29. `RealmAura.BurstScale`은 0 → 1.5, 0.2 → 1.25, 0.4 → 1.0, 1.0 → 1.0, -1 → 1.5, `double.PositiveInfinity` → 1.0을 돌려주고, `double.NaN`은 `ArgumentOutOfRangeException`을 던진다.

화면 (자동 테스트 없음. 사람이 실행해 확인)

30. 게임을 실행하면 화면 왼쪽 위에 `REALM: THIRD RATE`가 보이고, 아래쪽 HP 버튼 위에 돌파 버튼이 `BREAKTHROUGH` / `THIRD RATE` / 금화 아이콘 옆 `200`으로 보인다 (그림이 없으면 `G 200`). 금화가 200보다 적으면 어둡고 눌리지 않는다. 제자 주변에 오오라가 없다.
32. 초절정까지 돌파하면 버튼 비용 자리에 `MAX`가 보이고 눌리지 않으며, 오오라는 매화 분홍이다. 스테이지 전환 페이드 동안에는 돌파 버튼이 강화 버튼과 함께 어두워진다.
33. 기존 테스트 `CoreAssemblyTests.Core_DoesNotReferenceUnity`가 계속 통과한다.

## 구현된 변경 기록

(아직 없음)
