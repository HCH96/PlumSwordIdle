# 능력치 강화 기록

[stat_upgrade.md](../stat_upgrade.md) 사양의 이력이다. 구현에 필요한 규칙은 사양 문서에 모두 있으므로 **구현하는 에이전트는 읽지 않아도 된다.** 기획 스킬(`plan-feature`)이 이미 정한 것을 다시 묻지 않으려고 읽는다.

## 결정 기록

| 질문 | 답 | 방식 |
| --- | --- | --- |
| 최종 능력치(공격력, 최대 체력, 공격 간격)는 어느 기능이 계산하나 | 강화가 시작값을 생성자 인자로 받아 최종값을 계산해 내준다 → 바뀜: 2026-11-18, 공격 간격은 전투(`Battle.DiscipleBaseAttackInterval`)로 옮기고 공격력·최대 체력만 계산한다 | 추천 수락 |
| 강화 버튼 화면을 이 사양에 넣나 | 넣는다 (표시 글자 규칙 + 버튼 UI) | 추천 수락 |
| 체력 강화 때 현재 체력 +20은 누가 하나 | 강화는 알림(`Upgraded`)과 새 최대 체력만 내주고, 전투가 현재 체력을 늘린다 | 추천 수락 |
| 페이드 중 잠금을 어떻게 만드나 | 로직에 잠금 스위치 `SetLocked(bool)`. 스테이지 기능이 부른다 | 추천 수락 |
| 강화할 수 없을 때(금화 부족, 최대 단계, 잠김) 강화를 시도하면 | `false`, 금화·단계 그대로, 알림 없음 | 추천 수락 |
| 한글 글꼴이 없을 때 버튼 글자 | 영문 약자: `ATK` / `HP` / `SPD`, `Lv.3`, `G 35`, `MAX` | 추천 수락 |
| 버튼에 현재 능력치 값도 보여 주나 | 기획대로 이름·단계·비용 세 가지만 | 추천 수락 |
| 버튼 배치 | 금화 글자 아래 가로 한 줄 (`ATK`, `HP`, `SPD`). 좌표와 크기는 임시값 | 추천 수락 / 임시값 |
| 잘못된 저장 값(음수 단계, 공격 속도 11 이상) | 범위 안으로 잘라낸다 (0, 10). 예외 없음 → 바뀜: 2026-11-18, 공격 속도가 없어져 음수 → 0 만 남는다 | 추천 수락 |
| 숫자 타입 | 단계 `int`, 비용·공격력·최대 체력 `long`, 공격 간격 `double` → 바뀜: 2026-11-18, 공격 간격(`double`)은 없어졌다 | 직접 정함 |
| 공격 간격 공식 | `Math.Max(0.5, 시작 간격 - 0.05 × 단계)`, 최대 10단계 → 바뀜: 2026-11-18, 공격 속도 강화를 없앴다 | 직접 정함 (기획의 "최소 0.5초, 최대 10번"을 식으로 옮김) |
| 공격력·체력의 기술적 최대 단계 | `int.MaxValue`. 기획상 상한은 없고 넘침을 막는 용도 | 직접 정함 |
| 시작 능력치를 누가 넣나 | 전투 사양이 생길 때까지 `GameRoot`가 10 / 100 / 1.0을 직접 넣는다 → 바뀜: 전투 사양이 생겨 `GameRoot`가 `Battle` 상수를 넣는다 | 직접 정함 / 임시값 |
| 알림 모양과 순서 | `Upgraded(StatType)`, `LockedChanged(bool)`. `BalanceChanged` → 단계 증가 → `Upgraded` | 직접 정함 |
| 잘못된 인자(없는 `StatType`, `null` 지갑, 잘못된 시작값) | 예외 (`ArgumentOutOfRangeException`, `ArgumentNullException`) | 직접 정함 |
| 타입·메서드 이름, 버튼 색과 글자 구성 | `StatType`, `StatUpgrades`, `StatUpgradeText`, `StatUpgradeView`. 버튼 글자는 세 줄 한 덩어리 | 직접 정함 |
| 3단계 고치기 범위 | 공격 속도 제거, 내공 강화(단계·비용·버튼), 공격력·최대 체력에 경지 보너스, 체력 강화 때 최종 최대 체력 차이 | 사용자 선택 (요청에서 미리 정함) |
| 공격 속도 제거를 누가 하나 | 사람이 대화로 main에서 직접 한다(루프 관문 때문). 사양에는 제거된 뒤의 상태를 쓰고, 제거 목록은 범위 밖에 둔다 | 사용자 선택 (요청에서 미리 정함) |
| 기본 공격 간격 1초를 어디에 두나 | 전투의 `Battle.DiscipleBaseAttackInterval`. `StatUpgrades`에서 공격 간격 관련은 모두 없앤다 | 추천 수락 |
| 경지를 받는 모양 | 생성자 맨 끝 선택 인자 `realm = null`. `null`이면 0% | 추천 수락 |
| 내공 아이콘이 없을 때 | 그림 모드는 그대로 두고 아이콘 칸만 숨긴다. 경로 `Art/UI/StatUpgrade/inner_power` | 추천 수락 |
| 제거 때 저장 쪽 | `SaveData` 칸·코덱은 그대로. `Capture`는 공격 속도 칸에 0, `GameRoot`는 읽지 않는다. 칸 교체는 save.md에서 | 추천 수락 |
| 내공 단계를 읽는 연결 지점 | `GetLevel(StatType.InnerPower)` + `Upgraded(InnerPower)`. 배율 계산은 전투가 한다 | 사용자 선택 (요청) / 직접 정함 |
| 내공의 타입·이름·상한 | `StatType.InnerPower = 2`, `INNER`, `int.MaxValue` | 직접 정함 |
| 저장되기 전 내공 | `GameRoot`가 0을 넣는다 (다시 켜면 0) | 직접 정함 |
| 공격 속도 그림과 MAX 테스트 대체 | `speed.png`를 지운다. 글자 테스트는 `HP\nLv.5\nMAX`로 | 직접 정함 |

## 구현된 완료 조건

### 2026-11-18 옮김 — 처음 사양(c02cb2a)의 조건 중 구현되고 바뀌지 않은 것 (기록 004~006)

번호는 사양의 원래 번호다. 아래 말은 그때의 사양 기준이다.

아래에서 따로 적지 않으면 `new StatUpgrades(wallet, 10, 100, 1.0)`으로 만든 것이다. 공격 간격은 오차 1e-9 안에서 비교한다.

강화 로직 (`StatUpgradesTests`)

3. 금화 1000에서 `TryUpgrade(Attack)`을 3번 부르면 모두 `true`이고, 부르기 전의 `GetCost(Attack)`은 순서대로 20, 25, 30이며, 끝난 뒤 `GetLevel(Attack)`은 3, `GetCost(Attack)`은 35, `Attack`은 16, 금화는 925다.
5. 금화 100에서 `TryUpgrade(Health)`를 1번 부르면 `MaxHealth`는 120이고 금화는 80이다. 한 번 더 부르면 `MaxHealth`는 140이고 금화는 55다.
9. 금화 19에서 `TryUpgrade(Attack)`은 `false`이고 `GetLevel(Attack)`은 0, 금화는 19이며 `Upgraded`와 `BalanceChanged`는 0번 온다.
10. 금화 20에서 `TryUpgrade(Attack)`은 `true`이고 `GetLevel(Attack)`은 1, 금화는 0이다.
11. 금화 50에서 `TryUpgrade(Attack)`을 3번 부르면 결과는 순서대로 `true`, `true`, `false`이고 `GetLevel(Attack)`은 2, `Attack`은 14, 금화는 5다.
12. 금화 1000에서 `TryUpgrade(Health)`를 부르면 `Upgraded`가 `StatType.Health`를 담아 1번 오고, 받는 쪽에서 읽은 `GetLevel(Health)`는 1, `MaxHealth`는 120, `GetCost(Health)`는 25다.
13. 금화 1000에서 `TryUpgrade(Attack)`을 부르면 `Wallet.BalanceChanged`가 `Upgraded`보다 먼저 오고, `BalanceChanged`를 받는 시점에 읽은 `GetLevel(Attack)`은 0이다.
15. `SetLocked(true)`를 부르면 `Locked`는 `true`이고 `LockedChanged`가 `true`를 담아 1번 온다. 한 번 더 `SetLocked(true)`를 부르면 `LockedChanged`는 더 오지 않는다.
16. 금화 1000에서 `SetLocked(true)` 뒤 `CanUpgrade(Attack)`은 `false`, `TryUpgrade(Attack)`은 `false`이고 금화는 1000, 단계는 0이며 `Upgraded`는 0번 온다.
17. 16번 상태에서 `SetLocked(false)`를 부르면 `LockedChanged`가 `false`를 담아 1번 오고, `TryUpgrade(Attack)`은 `true`다.
18. 새로 만든 직후(`Locked`가 `false`) `SetLocked(false)`를 부르면 `LockedChanged`는 0번 온다.
27. `(StatType)99`로 `GetLevel`, `GetMaxLevel`, `GetCost`, `IsMaxLevel`, `CanUpgrade`, `TryUpgrade`를 부르면 각각 `ArgumentOutOfRangeException`을 던지고 금화와 단계는 그대로다.

표시 글자 (`StatUpgradeTextTests`)

29. `StatUpgradeText.Level`은 0 → `"Lv.0"`, 3 → `"Lv.3"`, 10 → `"Lv.10"`, -1 → `"Lv.-1"`을 돌려준다.
30. `StatUpgradeText.Cost`는 (20, `false`) → `"G 20"`, (1234, `false`) → `"G 1,234"`, (70, `true`) → `"MAX"`, (-5, `false`) → `"G -5"`를 돌려준다.

화면 (자동 테스트 없음. 사람이 실행해 확인)

33. 기존 테스트 `CoreAssemblyTests.Core_DoesNotReferenceUnity`가 계속 통과한다.

## 구현된 변경 기록

없음.
