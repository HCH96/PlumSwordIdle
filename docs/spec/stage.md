# 스테이지 사양

- 기능 이름: stage (네임스페이스 `PlumSword.Core.Stage`)
- 작성일: 2026-11-16
- 고친 날: 2026-11-17

## 목표

스테이지는 일반 몬스터 10마리와 보스 하나로 된 한 판이다. 종남파부터 천마까지 다섯 테마를 순서대로 진행하고, 천마를 이기면 성장을 유지하며 다음 회차의 1스테이지부터 반복한다. 이 기능은 전체 진행 번호·회차·테마, 몬스터 10마리의 무리 구성과 좌표, 처치 수와 진행도, 보스 등장, 성공·실패 판정과 자동 재도전, 처치 금화의 계산과 지급, 스테이지 전환(페이드, 전투 멈춤, 강화 잠금), 1회차에 처음 들어간 스테이지의 소개(이름과 문구를 3초 보여 주는 동안 전투 멈춤), 그리고 스테이지 화면(스테이지 글자, 진행 바와 보스 체력 바, 페이드 막, 소개 패널)을 맡는다. 이동·공격·체력과 몬스터 능력치, 캐릭터 표시, 카메라, 배경은 맡지 않는다 (전투 기능의 것).

출처: `docs/design/milestone_1.md` "1단계: 스테이지와 필드", "1단계: 금화", "공격 타이밍과 동시 처치", "UI와 전환", "기능 간 연결과 작업 분할 시 확인할 점". 2단계 변경은 `docs/design/milestone_2.md` "보스와 스테이지 진행". 저장 대비는 "저장과 불러오기", "오프라인 보상".

## 동작

- **상태**: 전체 진행 번호(`StageNumber`, 처음 1), 화면상 스테이지 번호(`DisplayStageNumber`, 1~5), 회차(`RoundNumber`, 처음 1), 단계(`Phase`: `Playing` / `FadingOut` / `FadingIn` / `Intro`), 일반 몬스터 처치 수(`DefeatedNormalCount`, 0~10), 이번 스테이지에서 낸 보스(`Boss`, 없으면 `null`). 회차와 화면상 번호는 전체 진행 번호에서 계산한다. 기존 알림의 인자도 전체 진행 번호다. 본 소개 번호(`SeenIntroStage`, 0~5. 1회차의 몇 번째 스테이지까지 소개를 보여 주기로 했는지).
- **시작** `Begin()`: `GameRoot`가 화면을 모두 조립한 뒤 한 번 부른다. "스테이지 배치"와 "소개 판단"(아래)을 하고, 소개를 보여 주기로 했으면 `Phase = Intro`, 소개 시간 0, `battle.SetPaused(true)`를 한다. 그다음 `HasBegun`을 `true`로 만든 뒤 `StageStarted(StageNumber)`, `Changed`를 순서대로 한 번씩 보낸다. 페이드는 없고 강화 잠금은 건드리지 않는다. 소개를 보여 주지 않으면 전투 멈춤도 건드리지 않는다 (기존과 같다). 두 번째부터는 아무 일도 없다.
- **스테이지 배치**: 아래를 순서대로 한다.
  1. `battle.Reset()`.
  2. `StageLayout.RollGroups(random)`으로 무리 구성을 새로 정한다.
  3. `StageLayout.Positions(무리 구성)`의 좌표마다 순서대로 `battle.AddEnemy(EnemyKind.Normal, StageNumber, 좌표)`를 부른다 (모두 10번).
  4. `DefeatedNormalCount = 0`, `Boss = null`.
- **소개 판단**: `RoundNumber`가 1이고 `DisplayStageNumber`가 `SeenIntroStage`보다 크면 소개를 보여 주기로 하고 `SeenIntroStage = DisplayStageNumber`로 올린다. 아니면 보여 주지 않고 `SeenIntroStage`는 그대로다. 그래서 실패 후 재도전(같은 번호)과 2회차부터는 소개가 없다. `StageStarted`를 받는 시점에 `SeenIntroStage`는 이미 올라가 있다.
- **적을 가리는 법**: 스테이지는 쓰러진 적을 `Enemy.Kind`로만 가린다. 그 적을 누가 추가했는지는 가리지 않는다.
- **`HasBegun`이 `false`이거나 `Phase`가 `Playing`이 아닌 동안**에는 전투의 알림(`EnemyDefeated`, `DiscipleDefeated`, `EnemyDamaged`)을 모두 무시한다. (`Intro` 동안도 무시한다. 그동안 전투도 멈춰 있다.)
- **일반 몬스터가 쓰러짐** (`battle.EnemyDefeated`, `Kind == Normal`): 아래를 순서대로 한다.
  1. `wallet.Add(StageRewards.NormalGold(StageNumber))`. 제자가 같은 칸에 쓰러졌어도(`battle.IsDiscipleDefeated`가 `true`) 준다.
  2. `DefeatedNormalCount`가 10보다 작으면 1 올린다. 이미 10이면 그대로 둔다.
  3. 2번에서 9가 10이 되었고 `battle.IsDiscipleDefeated`가 `false`면 보스를 낸다: `Boss = battle.AddEnemy(EnemyKind.Boss, StageNumber, battle.DisciplePosition + StageLayout.BossDistance)`.
  4. `Changed`를 한 번 보낸다.
- **보스가 쓰러짐** (`battle.EnemyDefeated`, `Kind == Boss`):
  - `battle.IsDiscipleDefeated`가 `true`면(동시에 쓰러짐) 아무것도 하지 않는다. 금화도 알림도 없다. 실패는 곧 오는 `DiscipleDefeated`에서 처리된다.
  - 아니면 성공이다: `wallet.Add(StageRewards.BossGold(StageNumber))` → "전환 시작"(다음 전체 번호 = `StageNumber + 1`, 단 `int.MaxValue`면 그대로) → `StageCleared(StageNumber)` → `Changed`.
- **제자가 쓰러짐** (`battle.DiscipleDefeated`): 실패다. "전환 시작"(다음 전체 번호 = `StageNumber`) → `StageFailed(StageNumber)` → `Changed`.
- **전환 시작**: 다음 번호를 기억해 두고, `Phase = FadingOut`, 페이드 시간 0, `battle.SetPaused(true)`, `upgrades.SetLocked(true)`. `StageCleared`·`StageFailed`를 받는 시점에 `Phase`는 이미 `FadingOut`이고 `StageNumber`는 아직 끝난 스테이지의 번호다.
- **시간** `Tick(deltaSeconds)`: `HasBegun`이 `false`이거나 `Phase`가 `Playing`이면 아무것도 하지 않는다. 아니면 지금 단계의 시간(페이드 시간 또는 소개 시간)에 `deltaSeconds`를 더하고 아래를 순서대로 본다.
  1. `Phase`가 `FadingOut`이고 페이드 시간이 `0.5 − 1e-9` 이상이면 **가려진 순간의 교체**를 한다: `StageNumber = 기억해 둔 다음 전체 번호`, "스테이지 배치", "소개 판단", 페이드 시간에서 0.5를 뺀다 (0보다 작아지면 0), `Phase = FadingIn`, `StageStarted(StageNumber)`, `Changed`. 이때 회차와 화면상 번호도 새 전체 번호에 맞게 바뀐다.
  2. `Phase`가 `FadingIn`이고 페이드 시간이 `0.5 − 1e-9` 이상이면 **페이드인을 끝낸다**: 페이드 시간 0, `upgrades.SetLocked(false)`. 1번의 소개 판단에서 소개를 보여 주기로 했으면 `Phase = Intro`, 소개 시간 0이고 전투는 멈춘 채 둔다. 아니면 `Phase = Playing`, `battle.SetPaused(false)`. 어느 쪽이든 `Changed`를 한 번 보낸다. 페이드인 뒤 남은 시간은 소개로 넘기지 않고 버린다. 그래서 소개는 들어간 `Tick`에서 끝나지 않고 최소 한 프레임 그려지며, 그 뒤 3초를 온전히 센다.
  3. `Phase`가 `Intro`이고 소개 시간이 `3.0 − 1e-9` 이상이면 **소개를 끝낸다**: `Phase = Playing`, 소개 시간 0, `battle.SetPaused(false)`, `Changed`. 남은 시간은 버린다.
  - 한 번의 `Tick`에서 1번과 2번이 모두 일어날 수 있다 (예: 소개가 없는 전환에서 `Tick(1.0)`이면 `Playing`까지, 소개가 있는 전환에서 `Tick(4.0)`이면 소개 시간 0의 `Intro`까지). 3번은 소개로 들어간 뒤의 `Tick`에서만 일어난다.
- **페이드 막의 진하기** `FadeAlpha`: `Playing`·`Intro`이면 0. `FadingOut`이면 `Math.Min(1.0, 페이드 시간 / 0.5)`. `FadingIn`이면 `Math.Max(0.0, 1.0 − 페이드 시간 / 0.5)`.
- **진행 바 비율** `BarRatio`: `Boss`가 `null`이면 `DefeatedNormalCount / 10.0`. `null`이 아니면 `CombatLayout.HealthRatio(Boss.Health, Boss.MaxHealth)`. 보스가 쓰러진 뒤에도 가려진 순간의 교체 전까지는 `Boss`가 남아 있어 비율은 0.0이다.
- **보스 체력이 줄어듦** (`battle.EnemyDamaged`): 그 적이 `Boss`와 같은 객체면 `Changed`를 한 번 보낸다.
- **`Changed`가 가는 때** (이것이 전부다): `Begin`의 끝, 일반 몬스터가 쓰러질 때마다, 보스 체력이 줄 때마다, 전환 시작(`StageCleared`·`StageFailed` 뒤), 가려진 순간의 교체(`StageStarted` 뒤), 페이드인이 끝날 때, 소개가 끝날 때.
- 성공해도 실패해도 금화와 강화 단계는 그대로다. 재도전 중 다시 처치한 몬스터는 다시 금화를 준다.
- 테마가 바뀌어도 일반 상대와 보스는 기존 근거리 공격을 사용하며 보스 특수 공격 패턴은 추가하지 않는다. 천마 처치 뒤에도 클리어 상태에 머물지 않는다.
- 소개 중(`Intro`)에는 전투가 멈춰 있고 강화는 잠기지 않는다. 스테이지 글자와 진행 바는 그대로 보인다. 소개를 눌러 넘길 수 없다.
- 체력 전부 회복, 제자와 카메라의 시작 위치 이동, 공격 대기 초기화는 `battle.Reset()`이 한다 (`docs/spec/combat.md`).
- 화면(`StageView`)은 매 프레임 `Tick(Time.deltaTime)`을 부르고 `FadeAlpha`를 읽어 막을 그리며, `Changed`를 받을 때마다 글자와 바, 소개 패널을 다시 그린다.
- **프레임 갱신 순서**: `CombatView.Update`(전투 `Tick`)와 `StageView.Update`(스테이지 `Tick`)의 순서는 정하지 않는다. 그래서 멈춤이 풀리는 프레임(페이드인 끝, 소개 끝)의 전투 시간은 그 프레임에 쓰일 수도, 버려질 수도 있다. 이 한 프레임 차이는 허용하며 자동 테스트 대상이 아니다.

### 경계 상황

| 경우 | 결과 | 값의 변화 |
| --- | --- | --- |
| 생성자의 `wallet`, `upgrades`, `battle`, `random` 중 하나가 `null` | `ArgumentNullException` | |
| 생성자의 `startStage`가 1보다 작음 | 1로 만든다 (예외 없음) | `StageNumber` 1, 알림 없음 |
| `Begin()`을 두 번째로 부름 | 아무 일도 없다 | 변화 없음, 알림 없음 |
| `Begin()` 전에 전투에서 적이 쓰러짐 | 무시한다 | 금화·처치 수 그대로, 알림 없음 |
| `Begin()` 전의 `Tick` | 아무 일도 없다 | 변화 없음 |
| `Playing` 중의 `Tick` | 아무 일도 없다 | 변화 없음 |
| `Tick(0)` | 아무 일도 없다 | 변화 없음 |
| `Tick(음수)`, `Tick(NaN)`, `Tick(무한대)` | `ArgumentOutOfRangeException` (`Begin` 전이어도, `Playing`이어도) | 변화 없음 |
| 소개가 없는 전환의 `FadingOut` 시작에서 `Tick(1.0 이상)` | 교체와 전환 끝을 한 번에 한다 | `Phase`는 `Playing`, `StageStarted` 1번 |
| `FadingOut` 시작에서 `Tick(0.3)` 두 번 | 남은 0.1초를 페이드인으로 넘긴다 | `Phase`는 `FadingIn`, `FadeAlpha` 0.8 |
| 한 칸에 일반 몬스터 여러 마리가 쓰러짐 | 한 마리씩 차례로 처리한다 | 예: 3마리 → 금화 3번, 처치 수 +3, `Changed` 3번 |
| 제자와 일반 몬스터가 같은 칸에 쓰러짐 | 그 몬스터의 금화를 주고 실패 | 금화 +1마리분, 처치 수 +1, `StageFailed` 1번 |
| 제자와 보스가 같은 칸에 쓰러짐 | 실패. 보스 금화 없음 | 금화 그대로, `StageCleared` 없음, `StageFailed` 1번 |
| 열 번째 일반 몬스터와 제자가 같은 칸에 쓰러짐 | 금화를 주고, 보스를 내지 않고 실패 | 처치 수 10, `Boss`는 `null`, `StageFailed` 1번 |
| 처치 수가 이미 10인데 일반 몬스터가 또 쓰러짐 | 금화는 준다. 처치 수는 10 그대로, 보스는 다시 내지 않는다 | 금화 +1마리분, `Changed` 1번 |
| 처치 수가 10이 되기 전에 `Kind`가 `Boss`인 적이 쓰러짐 | 보스 처치와 똑같이 처리한다 (성공) | |
| 페이드 중(`FadingOut`, `FadingIn`)의 강화 시도 | 잠겨 있어 `TryUpgrade`가 `false` (`docs/spec/stat_upgrade.md`) | 금화·단계 그대로 |
| `StageNumber`가 `int.MaxValue`일 때 성공 | 전체 번호를 올리지 않는다 (예외 없음) | 교체 뒤에도 429,496,730회차 2스테이지 |
| `StageRewards`의 `stage`가 1보다 작음 | `ArgumentOutOfRangeException` | |
| `StageLayout.RollGroups(null)` | `ArgumentNullException` | |
| `StageLayout.Positions(null)` | `ArgumentNullException` | |
| `StageLayout.Positions`의 원소가 1보다 작거나 3보다 큼 | `ArgumentOutOfRangeException` | |
| `StageLayout.Positions(빈 목록)` | 빈 배열 | |
| 기존 `StageText.StageLabel`에 0 이하 전달 | 예외 없이 그대로 표시 | 예: `STAGE -3` |
| 새 `StageText` 표시 메서드에 1~5 밖의 화면상 번호 또는 1보다 작은 회차 전달 | `ArgumentOutOfRangeException` | 변화 없음 |
| 생성자의 `seenIntroStage`를 생략함 | 5로 본다. 소개를 보여 주지 않는다 (기존 동작) | |
| 생성자의 `seenIntroStage`가 0보다 작거나 5보다 큼 | 0 또는 5로 잘라낸다 (예외 없음) | 알림 없음 |
| 실패 후 같은 스테이지 재도전 | 소개 없음 | 페이드인이 끝나면 바로 `Playing`, `SeenIntroStage` 그대로 |
| 2회차 이후의 스테이지 진입 | 소개 없음 | `SeenIntroStage` 그대로 |
| 1회차에서 `SeenIntroStage` 이하 번호로 시작 (예: 시작 3, 본 번호 3) | 소개 없음 | `Begin` 뒤 바로 `Playing` |
| 소개 중(`Intro`)의 강화 시도 | 잠겨 있지 않아 금화가 충분하면 `true` | 금화 감소, 단계 +1 |
| 소개 중에 전투를 `Step` | 전투가 멈춰 있어 아무 일도 없다 | 변화 없음, 알림 없음 |
| 소개가 있는 전환의 `FadingOut` 시작에서 `Tick(1.0 이상)` (예: 4.0) | 교체와 페이드인 끝까지 한다. 남은 시간은 버리고 소개는 0초부터 센다 | `Phase`는 `Intro`, `StageStarted` 1번. 다음 `Tick(3.0)`에서 `Playing` |
| `StageText.IntroLine`에 1~5 밖의 화면상 번호 | `ArgumentOutOfRangeException` | |

## 수치

| 항목 | 값 | 비고 |
| --- | --- | --- |
| 전체 진행 번호 `N` | `int` | 처음 1, 최소 1, 최대 `int.MaxValue`. 최대에서 성공해도 유지 |
| 회차와 스테이지 | `int` | 회차 = `(N - 1) / 5 + 1`, 스테이지 = `(N - 1) % 5 + 1`. 정수 나눗셈. 1~6 → 1회차 1~5, 2회차 1 |
| 금화의 타입 | `long` | 정수만 쓴다. 반올림 없음. `stage`를 `long`으로 바꾼 뒤 곱한다 |
| 시간·좌표·비율의 타입 | `double` (초, 유닛) | 반올림하지 않는다. 테스트는 오차 1e-6 안에서 비교한다 |
| 일반 몬스터 금화 `StageRewards.NormalGold(stage)` | `10 + 2 × (stage − 1)` (임시) | 1·2·3스테이지 → 10·12·14 |
| 보스 금화 `StageRewards.BossGold(stage)` | `50 + 10 × (stage − 1)` (임시) | 1·2·3스테이지 → 50·60·70 |
| 한 스테이지의 일반 몬스터 수 `StageLayout.NormalCount` | 10 | |
| 무리 한 개의 최대 마리 수 `StageLayout.MaxGroupSize` | 3 | 최소 1 |
| 무리 마리 수 고르기 | 남은 수가 0이 될 때까지 `random.Next(1, Math.Min(3, 남은 수) + 1)`을 고르고 남은 수에서 뺀다 | 합은 정확히 10. 무리 수는 4~10개. 예: `2·1·3·1·3`, `1·2·2·3·2` |
| 첫 무리 중심 `StageLayout.FirstGroupPosition` | 6.0 유닛 (임시) | 제자의 시작 좌표는 0 |
| 무리 중심 간격 `StageLayout.GroupSpacing` | 3.0 유닛 (임시) | `g`번째(0부터) 무리 중심 = `6.0 + 3.0 × g` |
| 무리 안 간격 `StageLayout.InGroupSpacing` | 0.3 유닛 (임시) | `n`마리 무리의 `i`번째(0부터) = `중심 + (i − (n − 1) / 2.0) × 0.3` |
| 좌표 예 | `2·1·3·1·3` → 5.85, 6.15, 9.0, 11.7, 12.0, 12.3, 15.0, 17.7, 18.0, 18.3 | 무리 순서, 무리 안에서는 작은 좌표부터 |
| 보스 등장 거리 `StageLayout.BossDistance` | 18.0 유닛 (임시) | 열 번째 처치 순간의 `battle.DisciplePosition + 18.0`. 가로로 긴 화면(20:9)의 오른쪽 끝(제자 앞 16.5)보다 밖이다 |
| 페이드 시간 `StageProgress.FadeSeconds` | 0.5초 (임시) | 페이드아웃 0.5초, 페이드인 0.5초 |
| 페이드 비교의 허용 오차 | 1e-9 | 페이드 시간 `≥ 0.5 − 1e-9`이면 끝난 것으로 본다 |
| 진행도 | `DefeatedNormalCount / 10.0` | 한 마리당 10%. 3마리 → 0.3 |
| 스테이지 글자 `StageText.RoundStageLabel(round, stage)` | `ROUND {round} · STAGE {stage} · {테마}` | 숫자는 `CultureInfo.InvariantCulture`. 예: `ROUND 2 · STAGE 1 · JONGNAM` |
| 테마 이름 | `JONGNAM`, `BEGGAR CLAN`, `NAMGUNG`, `DEMON CULT`, `HEAVENLY DEMON` | 순서대로 1~5스테이지 |
| 보스 이름 (임시) | `JONGNAM MASTER`, `BEGGAR CLAN MASTER`, `NAMGUNG SWORDSMAN`, `DEMON CULT COMMANDER`, `HEAVENLY DEMON` | 순서대로 1~5스테이지 |
| 소개 시간 `StageProgress.IntroSeconds` | 3.0초 (임시) | 페이드인이 끝난 뒤(처음 시작은 `Begin` 뒤)부터 센다 |
| 소개 비교의 허용 오차 | 1e-9 | 소개 시간 `≥ 3.0 − 1e-9`이면 끝난 것으로 본다 |
| 본 소개 번호 `SeenIntroStage` | `int`, 0~5 | 새 게임 0. 생성자 기본값 5 (소개 없음) |
| 소개 제목 | `StageText.ThemeName(stage)` | 예: `JONGNAM` |
| 소개 문구 `StageText.IntroLine(stage)` (임시 영문) | 아래 표 | 한글 글꼴이 준비되면 한글 원문으로 바꾼다 |

| 화면상 번호 | `IntroLine` | 한글 원문 |
| --- | --- | --- |
| 1 | `A Mount Hua disciple faces a first sparring match at Jongnam.` | 화산의 제자, 종남에서 첫 대련에 나선다. |
| 2 | `Pass the Beggar Clan's trial and hear of the Demon Cult's movements.` | 개방의 시험을 통과하고 마교의 움직임을 듣는다. |
| 3 | `Cross swords with Namgung to prepare for the battle to come.` | 다가올 싸움에 대비해 남궁의 검과 겨룬다. |
| 4 | `The battle to hold back the Demon Cult's army begins.` | 마교의 군세를 막기 위한 전투가 시작된다. |
| 5 | `At the end of the war, the Heavenly Demon awaits.` | 전쟁의 끝에서 천마와 마주한다. |

`StageRewards`와 전투의 `EnemyStats`에 넘기는 `stage`는 전체 진행 번호다. 보스와 일반 상대의 능력치·보상은 기존 임시 공식을 유지한다.

몬스터·보스의 체력·공격력·공격 간격은 이 기능의 수치가 아니다 (`docs/spec/combat.md`의 `EnemyStats`).

## 연결 지점

### 제공하는 것

```csharp
namespace PlumSword.Core.Stage
{
    public enum StagePhase
    {
        Playing = 0,     // 전투 중
        FadingOut = 1,   // 성공·실패 뒤 화면을 가리는 중
        FadingIn = 2,    // 새 배치를 마치고 화면을 여는 중
        Intro = 3,       // 1회차 첫 진입의 소개를 보여 주는 중. 전투는 멈추고 강화는 열려 있다
    }

    public static class StageRewards
    {
        // 일반 몬스터 한 마리의 금화. 10 + 2 * (stage - 1). long 으로 계산.
        // stage 가 1 보다 작으면 ArgumentOutOfRangeException.
        public static long NormalGold(int stage);

        // 보스의 금화. 50 + 10 * (stage - 1). long 으로 계산.
        // stage 가 1 보다 작으면 ArgumentOutOfRangeException.
        public static long BossGold(int stage);
    }

    public static class StageLayout
    {
        public const int NormalCount = 10;
        public const int MaxGroupSize = 3;
        public const double FirstGroupPosition = 6.0;
        public const double GroupSpacing = 3.0;
        public const double InGroupSpacing = 0.3;
        public const double BossDistance = 18.0;

        // 무리별 마리 수를 정한다. 남은 수가 0 이 될 때까지
        // random.Next(1, Math.Min(3, 남은 수) + 1) 을 고른다. 합은 정확히 10.
        // random 이 null 이면 ArgumentNullException.
        public static int[] RollGroups(System.Random random);

        // 무리 구성을 좌표로 바꾼다. g 번째 무리 중심 = 6.0 + 3.0 * g,
        // n 마리 무리의 i 번째 = 중심 + (i - (n - 1) / 2.0) * 0.3. 무리 순서, 무리 안은 작은 좌표부터.
        // groups 가 null 이면 ArgumentNullException. 원소가 1 보다 작거나 3 보다 크면 ArgumentOutOfRangeException.
        // 빈 목록이면 빈 배열. 합이 10 인지는 검사하지 않는다.
        public static double[] Positions(System.Collections.Generic.IReadOnlyList<int> groups);
    }

    public sealed class StageProgress
    {
        public const double FadeSeconds = 0.5;
        public const double IntroSeconds = 3.0;

        // wallet: 처치 금화를 줄 지갑. upgrades: 페이드 동안 잠글 강화. battle: 적을 놓고 알림을 받을 전투.
        // random: 무리 구성에 쓸 난수. 넷 중 하나라도 null 이면 ArgumentNullException.
        // startStage: 저장된 전체 진행 번호. 1 보다 작으면 1 로 만든다 (예외 없음).
        // seenIntroStage: 저장된 본 소개 번호. 0 보다 작으면 0, 5 보다 크면 5 로 자른다 (예외 없음).
        // 기본값 5 는 "다섯 개 모두 봄"이라 소개를 보여 주지 않는다 (기존 호출·테스트와 같은 동작).
        // battle.EnemyDefeated, battle.DiscipleDefeated, battle.EnemyDamaged 를 구독한다.
        // 생성자는 전투·지갑·강화를 건드리지 않고 알림을 보내지 않는다.
        // 시작 상태: Phase 는 Playing, HasBegun 은 false, 처치 수 0, Boss 는 null.
        public StageProgress(
            PlumSword.Core.Currency.Wallet wallet,
            PlumSword.Core.StatUpgrade.StatUpgrades upgrades,
            PlumSword.Core.Combat.Battle battle,
            System.Random random,
            int startStage = 1,
            int seenIntroStage = 5);

        public int StageNumber { get; }           // 전체 진행 번호, 1 이상 int.MaxValue 이하. 기존 계약 유지
        public int DisplayStageNumber { get; }    // (StageNumber - 1) % 5 + 1
        public int RoundNumber { get; }           // (StageNumber - 1) / 5 + 1
        public StagePhase Phase { get; }
        public bool HasBegun { get; }             // Begin 이 불린 뒤 true
        public int DefeatedNormalCount { get; }   // 0 이상 10 이하
        public PlumSword.Core.Combat.Enemy Boss { get; }   // 이번 스테이지에서 낸 보스. 없으면 null
        public bool IsBossActive { get; }         // Boss != null
        public double BarRatio { get; }           // Boss 가 null 이면 처치 수 / 10.0, 아니면 보스 체력 비율
        public double FadeAlpha { get; }          // 페이드 막의 진하기. 0.0 ~ 1.0. Playing·Intro 는 0
        public int SeenIntroStage { get; }        // 0 이상 5 이하. 소개 판단에서 올라간다
        public bool IsIntroShowing { get; }       // Phase == StagePhase.Intro

        // 첫 스테이지를 배치한다 (battle.Reset 뒤 일반 몬스터 10 마리).
        // StageStarted, Changed 를 순서대로 보낸다. 페이드·잠금은 없다.
        // 소개를 보여 주기로 했으면 Phase = Intro, battle.SetPaused(true). 아니면 멈춤도 없다 (기존과 같다).
        // 두 번째부터는 아무 일도 없다.
        public void Begin();

        // 페이드·소개 시간을 센다. Begin 전이거나 Playing 이면 아무것도 하지 않는다.
        // 0: 아무 일도 없다. 음수·NaN·무한대: ArgumentOutOfRangeException (상태와 상관없이).
        // 페이드아웃 → 교체 → 페이드인 → (소개) → Playing. 페이드아웃의 남은 시간은 페이드인으로 넘기고,
        // 페이드인 뒤와 소개 뒤의 남은 시간은 버린다. 소개는 언제나 0 초부터 3 초를 센다
        // (소개가 없는 전환의 FadingOut 시작에서 Tick(1.0) 이면 Playing, 소개가 있으면 Tick(4.0) 이어도 Intro 에서 멈춘다).
        public void Tick(double deltaSeconds);

        // 스테이지가 배치된 뒤에 전체 진행 번호를 담아 보낸다 (Begin, 그리고 가려진 순간의 교체).
        public event System.Action<int> StageStarted;

        // 보스를 처치해 성공했을 때 끝난 스테이지의 전체 진행 번호를 담아 보낸다. 이때 Phase 는 이미 FadingOut.
        public event System.Action<int> StageCleared;

        // 제자가 쓰러져 실패했을 때 그 스테이지의 전체 진행 번호를 담아 보낸다. 이때 Phase 는 이미 FadingOut.
        public event System.Action<int> StageFailed;

        // 화면이 다시 그려야 할 때 보낸다. 가는 때는 "동작"의 목록이 전부다.
        public event System.Action Changed;
    }

    public static class StageText
    {
        // 기존 계약: 모든 int 를 예외 없이 그대로 표시한다. 예: -3 -> "STAGE -3".
        public static string StageLabel(int stage);
        public const string BossName = "GATEKEEPER";

        // round < 1 또는 stage 가 1~5 밖이면 ArgumentOutOfRangeException.
        // 예: (2, 1) -> "ROUND 2 · STAGE 1 · JONGNAM". 숫자는 InvariantCulture.
        public static string RoundStageLabel(int round, int stage);
        // stage 가 1~5 밖이면 ArgumentOutOfRangeException. 이름은 위 수치 표를 따른다.
        public static string ThemeName(int stage);
        public static string ThemedBossName(int stage);

        // stage 가 1~5 밖이면 ArgumentOutOfRangeException. 문구는 수치 표를 따른다.
        public static string IntroLine(int stage);
    }
}
```

```csharp
namespace PlumSword.Game
{
    public sealed class GameRoot : MonoBehaviour   // 이미 있는 클래스에 아래 하나를 더한다
    {
        // Awake 에서 Battle 을 만든 뒤
        // new StageProgress(Wallet, StatUpgrades, Battle, new System.Random(), 1, 0) 으로 하나 만든다.
        // (저장 기능이 생기면 저장된 두 값을 넣는다)
        public PlumSword.Core.Stage.StageProgress StageProgress { get; }
    }
}

namespace PlumSword.Game.Stage
{
    public sealed class StageView : MonoBehaviour
    {
        // canvas 아래에 페이드 막, 스테이지 글자, 진행 바, 보스 이름, 소개 패널을 만들고 지금 상태로 바로 표시한다.
        // stage.Changed 를 구독하고 OnDestroy 에서 끊는다.
        // Update 에서 stage.Tick(Time.deltaTime) 을 부른 뒤 페이드 막의 진하기를 맞춘다.
        public void Initialize(PlumSword.Core.Stage.StageProgress stage, UnityEngine.Canvas canvas);
    }
}
```

- 누가 만들고 누가 들고 있는지: `GameRoot.Awake`가 `Battle`을 만든 뒤 `StageProgress`를 하나 만들어 `GameRoot.StageProgress`로 들고 있는다. 게임에 `StageProgress`는 하나뿐이다. 난수는 시드 없는 `new System.Random()`이다 (테스트는 직접 만든 난수를 넣는다).
- `Tick`을 부르는 것은 `StageView` 하나뿐이다. `Begin`을 부르는 것은 `GameRoot` 하나뿐이다.
- 2단계 저장 기능이 쓰는 방법: 저장할 값은 `StageNumber`와 `SeenIntroStage` 둘이다. `StageStarted`를 구독해 두 속성을 저장한다 (그 시점에 `SeenIntroStage`는 이미 올라가 있다). 새 게임의 첫 소개 기록(`SeenIntroStage` 0 → 1)을 놓치지 않으려면 저장 기능은 `Begin()`보다 먼저 `StageStarted`를 구독해야 한다 (`GameRoot` 조립 순서는 저장 사양이 정한다). 되살릴 때는 생성자의 `startStage`와 `seenIntroStage`에 저장된 값을 넣고 `Begin()`을 부른다.
- 2단계 오프라인 보상 기능이 쓰는 방법: `StageRewards.NormalGold(StageProgress.StageNumber)`가 일반 몬스터 한 마리분 금화다.

- 기존 `StageNumber`, 세 알림의 인자, `StageLabel(int)`, `BossName`의 공개 계약과 기존 테스트를 유지한다. 새 화면은 `DisplayStageNumber`, `RoundNumber`, `RoundStageLabel`, `ThemedBossName`을 쓴다. 기존 테스트를 삭제·이름 변경하거나 검사를 줄이지 않고 새 테스트를 추가한다. 생성자의 새 인자 `seenIntroStage`는 기본값이 5라 기존 호출과 테스트는 소개 없이 그대로 동작한다. `StagePhase`에는 `Intro`를 끝에 더하고 기존 값의 번호는 바꾸지 않는다.

### 필요한 것

- `PlumSword.Core.Currency.Wallet` (`docs/spec/currency.md`): `Add(long)`.
- `PlumSword.Core.StatUpgrade.StatUpgrades` (`docs/spec/stat_upgrade.md`): `SetLocked(bool)`.
- `PlumSword.Core.Combat.Battle` (`docs/spec/combat.md`): `Reset()`, `AddEnemy(EnemyKind, int, double)`, `SetPaused(bool)`, `DisciplePosition`, `IsDiscipleDefeated`, `EnemyDefeated`, `DiscipleDefeated`, `EnemyDamaged`.
- `PlumSword.Core.Combat.Enemy` (`Kind`, `Health`, `MaxHealth`), `PlumSword.Core.Combat.EnemyKind`, `PlumSword.Core.Combat.CombatLayout.HealthRatio(long, long)` (`docs/spec/combat.md`).
- `PlumSword.Game.GameRoot.Wallet`, `UiCanvas` (`docs/spec/currency.md`), `GameRoot.StatUpgrades` (`docs/spec/stat_upgrade.md`), `GameRoot.Battle` (`docs/spec/combat.md`). 금화·강화·전투 기능이 먼저 구현되어 있어야 한다.
- 전투의 임시 적 배치 `PlumSword.Game.Combat.TempEncounter`는 이 기능을 구현할 때 지운다 (`docs/spec/combat.md`가 그렇게 정했다). 클래스 파일과 그 `.meta`, `GameRoot`의 `[TempEncounter]` 조립 줄을 함께 지운다.

## 화면

- **조립**: `GameRoot.Awake`에서 `[CombatView]`를 만든 다음, `[TempEncounter]`를 만드는 줄을 지우고 그 자리에서 아래를 순서대로 한다.
  1. `StageProgress`를 `startStage: 1`, `seenIntroStage: 0`으로 만든다. 저장 기능이 생기기 전에는 실행할 때마다 1스테이지 소개가 나온다.
  2. `[Game]`의 자식 `[StageView]`를 만들어 `StageView`를 붙이고 `Initialize(StageProgress, UiCanvas)`를 부른다.
  3. `StageProgress.Begin()`을 부른다. 화면들이 전투의 `EnemyAdded`를 이미 구독한 뒤여야 몬스터가 보이기 때문에 맨 마지막이다.
- **페이드 막**: 캔버스를 꽉 채우는 검은색 `Image`(앵커 (0, 0) ~ (1, 1), 여백 0). 캔버스의 **첫 번째 자식**으로 둔다 (`SetAsFirstSibling`). 그래서 전투 화면만 가리고 보유 금화, 강화 버튼, 스테이지 글자, 진행 바는 막 위에 그대로 보인다. `raycastTarget = false`. 색의 알파는 매 프레임 `stage.FadeAlpha`다.
- **스테이지 글자**: 화면 위쪽 가운데. 앵커 (0.5, 1), 피벗 (0.5, 1), 위치 (0, −8), 크기 600×16 (임시). 글자는 `StageText.RoundStageLabel(stage.RoundNumber, stage.DisplayStageNumber)` 그대로다. `TextMeshProUGUI`, 글자 크기 16 (임시), 흰색, 가운데 정렬, TMP 기본 글꼴, `raycastTarget = false`.
- **보스 이름**: 스테이지 글자 아래, 바 위. 앵커 (0.5, 1), 피벗 (0.5, 1), 위치 (0, −26), 크기 300×16 (임시). 글자는 `StageText.ThemedBossName(stage.DisplayStageNumber)`, 글자 크기 14 (임시), 흰색, 가운데 정렬. `stage.IsBossActive`가 `true`일 때만 보인다.
- **진행 바**: 보스 이름 아래. 앵커 (0.5, 1), 피벗 (0.5, 1), 위치 (0, −44), 크기 300×12 (임시). 바탕은 `Image` RGB (0.15, 0.15, 0.15). 채움은 바탕의 자식 `Image`로 왼쪽 끝이 고정되고 가로 길이가 `300 × stage.BarRatio`다 (앵커 (0, 0) ~ (`BarRatio`, 1), 여백 0). 둘 다 `raycastTarget = false`.
  - `stage.IsBossActive`가 `false`: 일반 몬스터 진행 바. 채움 색 노랑 RGB (0.95, 0.80, 0.25) (임시). 한 마리당 10%씩 찬다.
  - `stage.IsBossActive`가 `true`: 보스 체력 바. 채움 색 빨강 RGB (0.85, 0.20, 0.20) (임시). 보스가 등장한 순간 가득 찬 빨간 바로 바뀐다.
  - 보스를 처치했거나 실패한 뒤에도 가려진 순간의 교체까지는 보스 체력 바 그대로다. 교체 때 0%의 일반 진행 바로 돌아간다.
- **소개 패널**: 캔버스 가운데. 앵커 (0.5, 0.5), 피벗 (0.5, 0.5), 위치 (0, 0), 크기 640×72 (임시). 바탕은 검은색 알파 0.6 `Image` (임시). 그 안 위쪽 절반에 제목 `StageText.ThemeName(stage.DisplayStageNumber)` (글자 크기 22 (임시)), 아래쪽 절반에 문구 `StageText.IntroLine(stage.DisplayStageNumber)` (글자 크기 14 (임시)). 둘 다 `TextMeshProUGUI`, 흰색, 가운데 정렬, TMP 기본 글꼴. 모두 `raycastTarget = false`. `stage.IsIntroShowing`이 `true`일 때만 보인다 (`SetActive`). 스테이지 글자·진행 바보다 나중에 만들어 그 위에 그린다 (페이드 막보다도 위).
- **바뀌는 때**: 글자·보스 이름·바·소개 패널은 `Initialize`에서 한 번, 그 뒤로 `stage.Changed`가 올 때마다 다시 그린다. 페이드 막의 진하기만 매 프레임 `Update`에서 `Tick`을 부른 뒤 맞춘다.
- **누르는 것**: 없다. 이 화면은 입력을 받지 않는다. 페이드 중 강화 버튼이 눌리지 않는 것은 강화 잠금(`SetLocked`)이 한다. 버튼은 잠긴 동안 어둡게 보인다 (`docs/spec/stat_upgrade.md`). 소개 패널도 누를 수 없다.
- **연출**: 페이드와 스테이지 소개 말고는 없다. 소개 패널은 애니메이션 없이 나타났다 사라진다. 성공·실패 글자, 보스 등장 연출, 소리는 없다.
- **사람이 먼저 해 둘 것**: 금화 사양과 같다. TextMeshPro 기본 리소스(TMP Essentials)가 있어야 글자가 보인다. 없어도 예외로 게임이 멈추지 않아야 한다.
- 표시 글자, 바 비율, 페이드 진하기의 판단은 모두 Core(`StageText`(`IntroLine` 포함), `StageProgress.BarRatio`, `FadeAlpha`, `IsBossActive`, `IsIntroShowing`)에 있고 에딧 모드 테스트로 판정한다. `StageView`와 `GameRoot`의 조립은 자동 테스트 대상이 아니며 사람이 실행해 확인한다.

## 저장

| 값 | 타입 | 처음 값 | 비고 |
| --- | --- | --- | --- |
| 전체 진행 번호 (`StageProgress.StageNumber`) | `int` | 1 | 회차와 화면상 스테이지는 이 값으로 계산한다 |
| 본 소개 번호 (`StageProgress.SeenIntroStage`) | `int` | 0 | 1회차에서 몇 번째 스테이지까지 소개를 보여 주기로 했는지 |

- 저장과 불러오기는 2단계의 저장 기능이 만든다. 이 기능은 그때 필요한 것만 미리 제공한다.
  - 저장할 값: `StageNumber`와 `SeenIntroStage` 둘. 소개 시간, 처치 수, 보스, 단계(`Phase`), 무리 구성은 저장하지 않는다. 다시 시작하면 저장된 스테이지의 처음부터 새 무리 구성으로 시작하고, 본 소개는 다시 보여 주지 않는다. "본 소개"는 보여 주기로 정한 소개다: 처음 시작의 `Begin()` 또는 가려진 순간의 교체에서 정해지므로, 그 뒤 페이드인 중(소개가 나타나기 전)이나 소개 도중에 꺼져도 그 소개는 다시 나오지 않는다.
  - 되살리기: `new StageProgress(wallet, upgrades, battle, random, 저장된 번호, 저장된 본 소개 번호)` 뒤 `Begin()`. 1보다 작은 번호는 1, 0~5 밖의 본 소개 번호는 잘라낸다. 저장 전체의 손상 처리는 저장 기능이 정한다.
  - 저장할 때: 저장 기능이 `StageStarted`를 구독해 스테이지가 바뀔 때 저장한다.
- 오프라인 보상: 꺼져 있는 동안 스테이지는 진행되지 않고 번호도 바뀌지 않는다. 오프라인 보상 기능(2단계)은 `StageRewards.NormalGold(StageNumber)`를 한 마리분 금화로 쓴다. 꺼져 있는 동안 `SeenIntroStage`도 바뀌지 않는다.

## 범위 밖

- 제자의 이동·공격·체력, 몬스터의 접근과 공격, 몬스터·보스 능력치, 캐릭터 대체 표시와 머리 위 체력 바, 카메라, 임시 배경: 전투 기능(`docs/spec/combat.md`)이 정한다.
- 보유 금화 표시: 금화 기능(`docs/spec/currency.md`). 강화 버튼과 잠겼을 때의 모양: 강화 기능(`docs/spec/stat_upgrade.md`).
- 저장·불러오기, 오프라인 보상 계산과 보상 창: 2단계에서 한다.
- 테마별 배경·일반 상대·보스 외형과 에셋 적용은 별도로 다룬다. 이번에는 기존 임시 배경·대체 표시를 쓴다.
- 소개 넘기기(누르면 닫기), 소개의 등장·퇴장 애니메이션과 소리, 2회차 이후의 소개나 회차 시작 알림: 하지 않는다.
- 한글 소개 문구: 한글 글꼴이 준비되면 수치 표의 한글 원문으로 바꾼다. 이번에는 영문이다.
- 2단계의 "테마의 배경과 상대를 구분할 수 있다" 달성 기준은 이번 사양만으로 충족되지 않는다. 테마별 배경과 일반 상대·보스 외형을 별도로 기획하고 구현한 뒤 확인한다.
- 후속 문서 변경 제안: `docs/spec/combat.md`의 동작 및 연결 지점에서 `Battle.AddEnemy`와 `EnemyStats`의 `stage`가 화면상 번호가 아닌 회차를 포함한 전체 진행 번호임을 명시한다. 전투의 기존 능력치 공식은 바꾸지 않는다.
- 보스 등장 연출, 성공·실패 글자나 결과 창, 소리: 하지 않는다. 보스 등장 연출은 4단계 후보다.
- 보스전 제한 시간, 스테이지 선택, 이전 스테이지로 돌아가기, 수동 재도전 버튼: 없다.
- 한글 표기와 한글 글꼴: 이번에는 하지 않는다. 글꼴이 준비되면 별도로 다룬다.
- UI까지 가리는 화면 전체 페이드: 하지 않는다 (전투 화면만 가린다).

## 완료 조건 초안

아래에서 따로 적지 않으면 지갑은 `new Wallet(0)`, 강화는 항목마다 적힌 `new StatUpgrades(wallet, 공격력, 최대 체력, 1.0)`, 전투는 `new Battle(upgrades)`, 스테이지는 `new StageProgress(wallet, upgrades, battle, 최소 난수)`로 만든 것이다.

- **최소 난수**: `System.Random`을 상속해 `Next(int minValue, int maxValue)`가 항상 `minValue`를 돌려주게 한 테스트용 클래스. 무리 구성은 `1`이 10개, 좌표는 6.0, 9.0, …, 33.0이다.
- **최대 난수**: 같은 방식으로 항상 `maxValue − 1`을 돌려주고, 받은 `maxValue`를 순서대로 기록하는 테스트용 클래스. 무리 구성은 `3·3·3·1`이다.
- "시작한 뒤"는 `Begin()`을 부른 뒤다. "n칸"은 `battle.Step()`을 n번 부르는 것이다. "적을 직접 넣는다"는 테스트가 `battle.AddEnemy`를 부르는 것이다. 좌표·비율은 오차 1e-6 안에서 비교한다.

이미 구현되고 바뀌지 않은 완료 조건(1~30, 32, 38, 39)은 [records/stage.md](records/stage.md)의 "구현된 완료 조건"으로 옮겼다. 그 조건의 테스트는 코드에 있다. 아래 번호는 그대로 두고, 새 조건은 54번부터 붙인다.

진행 (`StageProgressTests`)

31. `new StatUpgrades(wallet, 100000000000, 100000000000, 1.0)`과 `startStage`가 `int.MaxValue`인 스테이지로, 시작한 뒤 `Boss` 적 하나를 `int.MaxValue`스테이지, 좌표 1.0에 직접 넣고 1칸 진행하면, 금화는 21474836510, `StageCleared`가 `int.MaxValue`를 담아 1번 온다. `Tick(0.5)` 뒤 `StageNumber`는 `int.MaxValue`, `RoundNumber`는 429496730, `DisplayStageNumber`는 2이고 예외가 없다.

표시 글자 (`StageTextTests`)

33. 기존 `StageText.StageLabel`은 1 → `"STAGE 1"`, 12 → `"STAGE 12"`, 0 → `"STAGE 0"`, -3 → `"STAGE -3"`이고 `BossName`은 `"GATEKEEPER"`다. `StageText.RoundStageLabel(2, 1)`은 `"ROUND 2 · STAGE 1 · JONGNAM"`이다. `de-DE`에서도 같은 형식이며 숫자는 InvariantCulture다. 다섯 스테이지의 `ThemeName`과 `ThemedBossName`은 수치 표와 같다. 새 세 메서드에 스테이지 0 또는 6을 넣거나 `RoundStageLabel`의 회차에 0을 넣으면 `ArgumentOutOfRangeException`이다.

화면 (자동 테스트 없음. 사람이 실행해 확인)

34. 게임을 실행하면 화면 위쪽 가운데에 `ROUND 1 · STAGE 1 · JONGNAM`과 빈 노란 진행 바가 보이고, 제자 앞에 몬스터 무리가 보인다. 화면 가운데에 `JONGNAM`과 `A Mount Hua disciple faces a first sparring match at Jongnam.`이 약 3초 보이는 동안 제자와 몬스터는 움직이지 않고, 소개가 사라지면 전투가 시작된다. 몬스터를 처치할 때마다 금화가 10씩 늘고 바가 10%씩 찬다. 다시 실행하면 무리 구성이 달라진다.
35. 10마리를 모두 처치하면 바가 가득 찬 빨간 바로 바뀌고 그 위에 `JONGNAM MASTER`가 보인다. 제자가 달려가 화면 밖에 있던 보스와 싸우고, 보스가 맞을 때마다 빨간 바가 줄어든다.
36. 보스를 처치하면 금화가 50 늘고 전투 화면이 검게 가려졌다가 열리며 `ROUND 1 · STAGE 2 · BEGGAR CLAN`이 된다. 가려진 동안에도 보유 금화와 강화 버튼은 보이고, 버튼은 어둡고 눌리지 않는다. 열린 뒤 바는 빈 노란 바이고 제자의 체력은 가득하다. 열린 뒤 화면 가운데에 `BEGGAR CLAN`과 소개 문구가 약 3초 보이고, 그동안 전투가 멈춰 있다가 소개가 사라지면 시작된다.
37. 제자의 체력이 0이 되면 같은 방식으로 가려졌다가 같은 스테이지 번호로 처음부터 다시 시작한다. 금화와 강화 단계는 그대로다. 소개는 다시 나오지 않는다.

회차 순환 (에딧 모드 테스트)

40. 각각 새 지갑·강화 `(10, 100)`·전투·최소 난수로 `startStage` 1~6을 넣어 스테이지를 만들고 `Begin()` 전에 읽는다. `StageNumber`는 각각 1~6, `RoundNumber`는 1, 1, 1, 1, 1, 2, `DisplayStageNumber`는 1, 2, 3, 4, 5, 1이다. 별도로 `startStage` 0과 -3은 세 속성 모두 1이다.
41. `(1000, 100)`과 `startStage: 5`로 만들고 알림을 기록한 뒤 `Begin()`을 부른다. `battle.AddEnemy(Boss, 5, 1.0)`을 부르고 `battle.Step()`을 한 번 호출한다. 금화는 90, `StageCleared(5)`는 1번이며 `Phase`는 `FadingOut`, `StageNumber`는 5다. `Tick(0.5)` 뒤 `StageNumber` 6, `RoundNumber` 2, `DisplayStageNumber` 1이고 `StageStarted(6)`이 한 번 온다. 금화는 90이고 모든 강화 단계는 시작 전과 같다.
42. `(10, 30)`과 `startStage: 6`으로 만들어 `Begin()`을 호출하면 배치된 일반 상대 10명의 최대 체력은 80, 공격력은 15다. `battle.AddEnemy(Boss, 6, 1.0)`으로 보스를 직접 넣으면 그 보스의 최대 체력은 250, 공격력은 30이다. `StageRewards.NormalGold(6)`은 20, `BossGold(6)`은 100이다. 알림을 기록하고 `battle.Step()`을 한 번 호출하면 보스는 살아 있고 제자는 쓰러져 `StageFailed(6)`이 한 번 온다. `Tick(0.5)` 뒤 `StageNumber` 6, `RoundNumber` 2, `DisplayStageNumber` 1이고 새 일반 상대 10명, 제자 체력 30, 처치 수 0, `Boss` null이다.
43. `(10, 100)`과 `startStage: 6`으로 만든 객체에서 `StageNumber`를 읽어 저장값으로 삼는다. 새 지갑·강화·전투·최소 난수와 그 저장값으로 다른 스테이지 객체를 만든 뒤 `Begin()`을 부르면 `StageNumber` 6, 회차 2, 화면상 번호 1, 처치 수 0, 보스 null이며 일반 상대 10명의 최대 체력은 80이다. `StageRewards.NormalGold(저장값)`은 20이다.

스테이지 소개 (에딧 모드 테스트, `StageProgressIntroTests`, `StageTextTests`)

44. `seenIntroStage`를 생략해 만들면 `SeenIntroStage`는 5다. 0·3을 넣으면 0·3, −2를 넣으면 0, 9를 넣으면 5이고 예외가 없다. 어느 경우든 `Begin()` 전의 `IsIntroShowing`은 `false`, `Phase`는 `Playing`이다.
45. (10, 100) `seenIntroStage: 0`으로 만들어 `Begin()`을 부르면 `SeenIntroStage`는 1, `Phase`는 `Intro`, `IsIntroShowing`은 `true`, `battle.Paused`는 `true`, `upgrades.Locked`는 `false`, `FadeAlpha`는 0.0, `battle.Enemies.Count`는 10이다. `StageStarted`가 1을 담아 1번, 그 뒤에 `Changed`가 1번 오고, `StageStarted`를 받는 시점에 읽은 `SeenIntroStage`는 1, `Phase`는 `Intro`다.
46. 45번에 이어 `Normal` 적 하나를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행하면 금화 0, `DefeatedNormalCount` 0, `battle.DisciplePosition` 0이고 `Changed`는 오지 않는다. `Tick(2.9)` 뒤 `Phase`는 `Intro`, `battle.Paused`는 `true`다. 이어서 `Tick(0.1)`을 부르면 `Phase`는 `Playing`, `IsIntroShowing`은 `false`, `battle.Paused`는 `false`이고 `Changed`가 1번 온다. 이어서 1칸 진행하면 금화는 10이다.
47. 지갑을 `new Wallet(20)`, (10, 100), `seenIntroStage: 0`으로 만들어 `Begin()`을 부른 뒤 (`Intro`) `upgrades.TryUpgrade(StatType.Attack)`은 `true`이고 금화는 0, `Phase`는 `Intro` 그대로다.
48. (1000, 100) `seenIntroStage: 0`으로 만들어 시작하고 `Tick(3.0)` 뒤 `Phase`는 `Playing`이다. `Boss` 적 하나를 1스테이지, 좌표 1.0에 직접 넣고 1칸 진행하면 `StageCleared`가 1을 담아 1번 온다. `Tick(0.5)` 뒤 `StageNumber` 2, `Phase` `FadingIn`, `SeenIntroStage` 2이고 `StageStarted`가 2를 담아 1번 오며, 그 시점에 읽은 `SeenIntroStage`는 2다. 다시 `Tick(0.5)` 뒤 `Phase`는 `Intro`, `battle.Paused`는 `true`, `upgrades.Locked`는 `false`, `FadeAlpha`는 0.0이다. 다시 `Tick(3.0)` 뒤 `Phase`는 `Playing`, `battle.Paused`는 `false`다.
49. 48번과 같이 `StageCleared`까지 간 뒤 `Tick(1.5)`를 부르면 `Phase`는 `Intro`, `StageNumber`는 2다. 이어서 `Tick(2.9)`를 부르면 `Phase`는 `Intro` 그대로이고 (남은 시간을 넘기지 않으므로), 다시 `Tick(0.1)`을 부르면 `Phase`는 `Playing`이다. 따로, `StageCleared`까지 간 뒤 `Tick(4.0)` 한 번이면 `Phase`는 `Intro`, `IsIntroShowing`은 `true`, `battle.Paused`는 `true`이고 `StageStarted`가 2를 담아 1번 온다. 이어서 `Tick(3.0)`을 부르면 `Phase`는 `Playing`, `battle.Paused`는 `false`다.
50. (10, 5) `seenIntroStage: 0`으로 만들어 시작하고 `Tick(3.0)` 뒤 `Normal` 적 하나를 1스테이지, 좌표 0.8에 직접 넣고 1칸 진행하면 `StageFailed`가 1을 담아 1번 온다. `Tick(1.0)` 뒤 `StageNumber` 1, `Phase` `Playing`, `battle.Paused`와 `upgrades.Locked`는 `false`, `SeenIntroStage` 1이고 `StageStarted`가 1을 담아 한 번 더 온다.
51. (1000, 100) `startStage: 5`, `seenIntroStage: 4`로 만들어 `Begin()`을 부르면 `Phase` `Intro`, `SeenIntroStage` 5다. `Tick(3.0)` 뒤 `Boss` 적 하나를 5스테이지, 좌표 1.0에 직접 넣고 1칸 진행하면 `StageCleared`가 5를 담아 1번 온다. `Tick(1.0)` 뒤 `StageNumber` 6, `RoundNumber` 2, `Phase` `Playing`, `battle.Paused` `false`, `SeenIntroStage` 5다.
52. (10, 100)으로 각각 새로 만들어 `Begin()`을 부른다. `startStage: 3, seenIntroStage: 3` → `Phase` `Playing`, `battle.Paused` `false`, `SeenIntroStage` 3. `startStage: 3, seenIntroStage: 2` → `Phase` `Intro`, `SeenIntroStage` 3. `startStage: 7, seenIntroStage: 0` → `Phase` `Playing`, `SeenIntroStage` 0. `seenIntroStage` 생략 → `Phase` `Playing`, `SeenIntroStage` 5. 또 48번의 첫 번째 `Tick(0.5)` 직후(페이드인 중, 소개가 나타나기 전)에 읽은 `StageNumber`(2)와 `SeenIntroStage`(2)로 새 지갑·강화·전투·최소 난수와 새 스테이지를 만들어 `Begin()`을 부르면 `Phase`는 `Playing`이고 `SeenIntroStage`는 2다. 48번의 두 번째 `Tick(0.5)` 뒤 `Tick(1.0)`을 더 부른 때(소개 도중) 읽은 값도 `StageNumber` 2, `SeenIntroStage` 2이고, 그 값으로 같은 방법으로 만들어 `Begin()`을 부르면 `Phase`는 `Playing`이다.
53. `StageText.IntroLine(1)`~`IntroLine(5)`는 수치 표의 영문 문구와 같다. `IntroLine(0)`과 `IntroLine(6)`은 `ArgumentOutOfRangeException`이다.

## 결정 기록

결정 기록은 [records/stage.md](records/stage.md)의 "결정 기록"에 있다. 구현에 필요한 규칙은 위 절들에 모두 있다.

## 변경 기록

### 2026-11-17 — 다섯 테마와 회차 순환 (기준 커밋: c98cc92)

| 절 | 바뀌기 전 | 바뀐 뒤 |
| --- | --- | --- |
| 목표·동작 | 끝없이 증가하는 스테이지 번호로 진행 | 전체 진행 번호로 다섯 테마와 회차를 계산하고 천마 뒤 다음 회차의 종남파부터 진행 |
| 경계 상황·수치 | 번호는 `int.MaxValue`에서 유지. 표시 글자는 `STAGE`와 번호, 보스 이름은 `GATEKEEPER` | 상한은 유지하고 화면상 번호는 1~5, 회차는 전체 번호로 계산. 새 표시 메서드에 잘못된 회차·화면상 번호를 넣으면 예외 |
| 연결 지점 | `StageNumber`와 알림은 진행 번호. `StageLabel(int)`과 `BossName`으로 화면 표시 | 기존 계약은 유지. 회차·화면상 번호 속성과 테마별 글자 메서드를 추가하고 화면에서 사용 |
| 화면 | 스테이지 번호와 공통 보스 이름 표시 | 회차·스테이지·테마와 테마별 보스 이름을 영문으로 표시. 기존 임시 외형 사용 |
| 저장 | 스테이지 번호 하나를 저장하도록 대비 | 같은 전체 진행 번호 하나를 저장하고 회차와 화면상 번호를 계산하도록 명시 |
| 범위 밖 | 다섯 테마·회차·소개를 2단계로 미룸 | 테마와 회차는 이번 범위. 소개, 테마별 배경·적 외형과 에셋은 별도 기획 |

- 완료 조건 초안: 새로 40~43 / 바뀜 31, 33~36 / 나머지 그대로 (이미 구현됨). 31의 기존 상한 검사는 유지하고 회차·화면상 번호 검사를 추가한다.
- 바뀌는 연결 지점: `StageProgress.DisplayStageNumber`, `StageProgress.RoundNumber`, `StageText.RoundStageLabel(int, int)`, `ThemeName(int)`, `ThemedBossName(int)`을 추가한다. 기존 `StageNumber`, 알림 인자, `StageLabel(int)`, `BossName`은 그대로 둔다.
- 고쳐야 할 기존 코드: `Assets/Scripts/Core/Stage/StageProgress.cs`에 회차와 화면상 번호 속성을 추가하고, `Assets/Scripts/Core/Stage/StageText.cs`에 새 표시 메서드를 추가한다. `Assets/Scripts/Game/Stage/StageView.cs`는 새 속성과 메서드로 스테이지 글자·보스 이름을 표시한다.
- 기존 테스트: 아래 테스트는 새 규칙에서도 기존 API의 계약이 유지되므로 이름·본문·기대값을 그대로 둔다. 새 화면 표시와 회차 순환 검사는 별도 테스트로 추가한다. 기존 테스트를 삭제하거나 이름을 바꾸거나 검사를 줄이지 않는다.

| 테스트 | 지금 | 바뀐 뒤 | 처리 |
| --- | --- | --- | --- |
| `StageTextTests.StageLabel_Stage_PrefixesStageAndSpace` | 1·12·0을 `STAGE`와 함께 표시 | 기존 메서드의 결과 유지 | 그대로 유지 |
| `StageTextTests.StageLabel_Negative_WritesAsIsWithoutException` | -3을 예외 없이 `STAGE -3`으로 표시 | 기존 메서드의 결과 유지 | 그대로 유지 |
| `StageTextTests.StageLabel_CurrentCultureIsGerman_HasNoDigitSeparator` | 독일 문화권에서도 `STAGE 1234` | 기존 메서드의 결과 유지 | 그대로 유지 |
| `StageTextTests.BossName_Always_IsGatekeeper` | `BossName`은 `GATEKEEPER` | 기존 상수 유지, 게임 화면은 새 메서드 사용 | 그대로 유지 |
| `StageProgressFadeTests.MaxStageCleared_StaysAtMaxWithoutException` | 최대 전체 번호에서 전환해도 `StageNumber` 유지 | 같은 전체 번호 계약 유지 | 그대로 유지, 새 회차·화면상 번호 검사는 별도 추가 |
| `StageProgressOutcomeTests.BossDefeatedOnMaxStage_NoOverflowAndCleared` | 최대 전체 번호에서 성공 알림과 `StageNumber`가 최대값 | 알림과 전체 번호 계약 유지 | 그대로 유지 |

### 2026-11-17 — 스테이지 소개 (기준 커밋: bd371a4)

| 절 | 바뀌기 전 | 바뀐 뒤 |
| --- | --- | --- |
| 목표·동작 | 소개는 범위 밖. 페이드인이 끝나면 바로 전투 | 1회차 첫 진입 때 페이드인 뒤(처음 시작은 `Begin` 뒤) 이름과 문구를 3초 보여 주고 그동안 전투를 멈춘다. 재도전·2회차부터는 없다. 소개는 언제나 0초부터 센다(페이드인 뒤 남은 시간은 버림). 전투와 스테이지의 프레임 갱신 순서는 정하지 않는다 |
| 경계 상황 | 소개 관련 없음 | 본 소개 번호 잘라내기, 재도전·2회차·이미 본 번호 시작의 소개 없음, 소개 중 강화 가능, 긴 `Tick`에서도 소개는 0초부터 3초, `IntroLine` 범위 밖 예외 |
| 수치 | 소개 수치 없음 | 소개 3.0초 (임시), 허용 오차 1e-9, 본 소개 번호 0~5, 영문 소개 문구 다섯 줄 |
| 연결 지점 | `StagePhase` 세 값, 생성자 인자 다섯 개 | `StagePhase.Intro` 추가, 생성자 `seenIntroStage = 5` 추가, `IntroSeconds`, `SeenIntroStage`, `IsIntroShowing`, `StageText.IntroLine(int)` 추가. `GameRoot`는 `seenIntroStage: 0` |
| 화면 | 연출은 페이드뿐 | 가운데 소개 패널(제목·문구) 추가 |
| 저장 | `StageNumber` 하나 | `StageNumber`와 `SeenIntroStage` 둘. 페이드인 중·소개 중에 꺼져도 그 소개는 다시 나오지 않는다. 저장 기능은 `Begin()` 전에 `StageStarted`를 구독한다 |
| 범위 밖 | 스테이지 소개는 별도 기획 | 소개 넘기기·애니메이션·소리·한글 문구는 하지 않음 |

- 완료 조건 초안: 새로 44~53 / 바뀜 34, 36, 37 / 나머지 그대로. 단, 바로 앞 변경 기록(다섯 테마와 회차 순환)의 새로 40~43·바뀜 31, 33~36도 아직 구현되지 않았다. 이 변경은 그 변경의 `RoundNumber`, `DisplayStageNumber`, `ThemeName`을 쓰므로 그 작업들이 먼저 끝나야 한다.
- 바뀌는 연결 지점: `StagePhase.Intro = 3`, `StageProgress` 생성자에 `int seenIntroStage = 5` 추가, `StageProgress.IntroSeconds`, `SeenIntroStage`, `IsIntroShowing`, `StageText.IntroLine(int)` 추가. 기존 값과 인자는 그대로다.
- 고쳐야 할 기존 코드: `Assets/Scripts/Core/Stage/StagePhase.cs`(값 추가), `Assets/Scripts/Core/Stage/StageProgress.cs`(생성자 인자, 소개 판단, `Begin`과 `Tick`의 소개 단계), `Assets/Scripts/Core/Stage/StageText.cs`(`IntroLine`), `Assets/Scripts/Game/Stage/StageView.cs`(소개 패널), `GameRoot`의 `StageProgress` 생성 줄(`seenIntroStage: 0`).
- 기존 테스트: 없음. 생성자의 기본값이 소개를 보여 주지 않아 기존 테스트(`Begin` 직후·페이드인 직후 `Paused`가 `false`, 한 판 전체 진행 등)는 이름·본문·기대값을 그대로 두고 통과해야 한다. 기존 테스트를 지우거나 이름을 바꾸거나 검사를 줄이지 않는다.
