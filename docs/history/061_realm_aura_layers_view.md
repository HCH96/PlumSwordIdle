# 061_realm_aura_layers_view — 상시 매화 오오라 층(기본 빛·기운·꽃잎)과 연출 시간 (Game)

## 목표
`PlumSword.Game.Realm.RealmAuraView` 를 고쳐 상시 오오라를 기본 빛 + 기운 + 떠오르는 꽃잎 세 층으로 그리고, 돌파 연출의 시간(`t`)·"보여 줄 경지" 전환을 1.8초 시간표로 바꾼다. 화면(Game) 작업이다. 돌파 연출 부품(어둡게·빛·블룸·고리·기둥·충격파·돌파 꽃잎·번쩍임·흔들림)은 062 가 한다.

## 묶음
realm_fx

## 선행 작업
- 058_breakthrough_style_timing
- 060_realm_aura_plum

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/realm.md ("동작"의 화면 줄, "연결 지점"의 `RealmAuraView`·`RealmAura`·`BreakthroughEffect`, "화면"의 "오오라와 돌파 연출" 앞부분과 "상시 오오라", "저장"의 연출 줄)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-20 — 매화 오오라와 단계별 돌파 연출` 항목만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. 이 작업은 그중 아래 범위만 맡는다.
- 고칠 파일: `Assets/Scripts/Game/Realm/RealmAuraView.cs` (이미 있음, 048 이 만듦). 필요하면 같은 폴더에 내부용(`internal`) 도우미 파일을 더해도 된다 (`.meta` 함께). `Initialize(RealmProgress realm, Battle battle)` 의 모양, 인자 검사(`null` → `ArgumentNullException`, 두 번 → `InvalidOperationException`), `BrokeThrough` 구독·`OnDestroy` 해제, `GameRoot` 조립은 그대로 둔다.
- 클래스에 `[DefaultExecutionOrder(100)]` 을 붙인다 (062 의 카메라 흔들림이 `CombatView.Update` 뒤에 오게 하려는 것).
- **위치**: 지금처럼 `LateUpdate` 에서 발 위치 `((float)battle.DisciplePosition − 2.25, −1.5)` 를 따라가고, 오오라 가운데는 발 위 1.25 유닛이다. Core 가 주는 `EffectPoint` 의 X, Y 는 이 가운데에 더한다.
- **연출 시간**: `BrokeThrough` 를 받으면 시작 시각(`Time.time`)과 직전 경지를 기억한다 (직전 경지 = 받은 새 경지 − 1. 연출 중에 또 돌파하면 그 시점에 보이던 경지가 아니라 받은 새 경지 − 1 을 쓴다). `t = Time.time − 시작 시각`. `BreakthroughEffect.IsPlaying(t)` 가 `false` 가 되면 연출을 끝낸다. 연출 중에 또 돌파하면 시작 시각을 새로 잡는다. 처음 시작(저장을 불러온 경우 포함)에는 연출하지 않는다. 062 가 이 `t` 와 새 경지를 쓰도록 비공개 필드로 둔다.
- **보여 줄 경지**: 연출 중이고 `BreakthroughEffect.ShowsNewAura(t)` 가 `false` 면 직전 경지, 그 밖에는 `realm.Current`. 보여 줄 경지가 삼류(`RealmAura.HasAura` 가 `false`)면 상시 오오라 세 층이 모두 보이지 않는다.
- **기본 빛** (`sortingOrder` 9990): 지금의 그림/대체 동작을 유지하되, 그림 경로와 색·알파를 "보여 줄 경지"로 계산한다: 그림 `Resources.Load<Texture2D>("Art/Effects/aura_" + RealmText.AssetId(보여 줄 경지))`, 대체 색은 `RealmAura.Color(보여 줄 경지)` RGB, 알파는 매 프레임 `RealmAura.PulseAlpha(보여 줄 경지, Time.time)`. 그림 애니메이션은 보여 줄 경지가 바뀔 때 첫 프레임부터 재생한다. 크기 배율: 연출 중 `t ≥ 0.5` 이면 `RealmAura.BurstScale(t − 0.5)` 를 곱하고, 그 밖에는 1 이다 (기존의 `BurstScale(t)` 즉시 적용을 이것으로 바꾼다).
- **기운** (`sortingOrder` 9991): `BreakthroughEffect.Style(보여 줄 경지).AuraWisps` 줄. 줄 `i` 마다 `RealmAura.Wisp(보여 줄 경지, i, Time.time)` 의 X, Y(가운데에 더함), Scale(세로 배율), Alpha.
  - 그림 `Resources.Load<Texture2D>("Art/Effects/aura_wisp")` 가 있으면 가로 한 줄 시트를 32×64 칸으로 잘라 10 fps 로 반복 재생한다 (임시값). 피벗 칸 가운데, 높이 3.0 유닛 × Scale, 비율 유지, 색 흰색에 알파만 곱한다. 가로가 32 로 나누어떨어지지 않거나 높이가 64 가 아니면 `Debug.LogWarning` 을 한 번 남기고 대체 그림을 쓴다.
  - 대체 그림: 실행 중에 한 번 만드는 8×32 텍스처(PPU 32, Point 필터). 픽셀 (x, y) 의 알파 = `(1 − |dx| / 4)² × sin(π × (y + 0.5) / 32)`, `dx = x + 0.5 − 4`, 결과를 0~1 로 잘라낸다. RGB 흰색. 가로 0.5 × 세로 2.0 × Scale 유닛으로 그린다. 색은 `RealmAura.Color(보여 줄 경지)` RGB, 알파는 `Wisp` 의 Alpha.
- **꽃잎** (`sortingOrder` 10002): `Style(보여 줄 경지).AuraPetals` 장. 장 `i` 마다 `RealmAura.FloatingPetal(보여 줄 경지, i, Time.time)` 의 위치와 Alpha.
  - 그림 `Resources.Load<Texture2D>("Art/Effects/petal")` 이 있으면 8×8 칸으로 자르고 `i % 칸 수` 번째 칸을 쓴다 (재생하지 않음). 가로가 8 로 나누어떨어지지 않거나 높이가 8 이 아니면 `Debug.LogWarning` 한 번, 대체 그림.
  - 대체 그림: 3×3 텍스처(PPU 32, Point 필터). 가운데와 상하좌우 다섯 픽셀 불투명 흰색, 모서리 네 픽셀 투명.
  - 화면에서 1.5배로 그린다. 색은 `RealmAura.Color(보여 줄 경지)` RGB, 알파는 `FloatingPetal` 의 Alpha.
  - 꽃잎 그림·대체 텍스처는 062 의 돌파 꽃잎도 쓰므로 한 번 만들어 필드에 둔다.
- 기운·꽃잎 `SpriteRenderer` 는 경지별 최대 개수(기운 4, 꽃잎 5)만큼 처음에 만들어 두고, 쓰지 않는 것은 끈다. 실행 중에 만든 텍스처와 스프라이트는 `OnDestroy` 에서 지운다.
- 그림이 없을 때 오류 로그(`Debug.LogError`, 예외)를 남기지 않는다. 지금 `Assets/Resources/Art/Effects/` 에 이 그림들이 없으면 대체 그림이다. 보고서의 가정에 이 작업이 읽는 리소스 경로와 없을 때 보여 주는 것을 적는다.
- 색·알파·위치·크기·개수는 `RealmAura`, `BreakthroughEffect` 의 값을 그대로 쓰고 화면 코드에서 다시 계산하지 않는다. 대체 텍스처의 픽셀 공식만 화면 코드에 둔다.
- 스테이지 전환 페이드, 패배, 구간 소개 중에도 그대로 보인다 (`Battle` 의 상태를 보지 않는다). 정렬 순서 참고: 바닥 −10, 나무 −5, 적 10 이상, 제자 10000, 투사체 20000, 피해 숫자 25000, 체력 바 30000. `Time.timeScale` 을 바꾸지 않는다.
- 이 작업의 완료 조건은 `[컴파일]` 과 `[사람 확인]` 뿐이다 (판단은 058·060 의 에딧 모드 테스트가 한다). 새 자동 테스트를 쓰지 않아도 된다.

## 완료 조건
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다 (플레이 모드 `GameBootTests` 포함: 게임을 켜서 `[Game]` 이 조립되는 동안 오류 로그가 없다).
- [컴파일] `RealmAuraView` 에 `[DefaultExecutionOrder(100)]` 이 붙어 있다.
- [컴파일] `RealmAuraView` 는 기운 그림을 `Resources.Load<Texture2D>("Art/Effects/aura_wisp")`, 꽃잎 그림을 `Resources.Load<Texture2D>("Art/Effects/petal")` 로 읽고, `null` 이면 실행 중에 만든 대체 텍스처를 쓴다.
- [컴파일] 기운과 꽃잎의 위치·배율·알파는 `RealmAura.Wisp`, `RealmAura.FloatingPetal` 의 결과를 그대로 쓰고, 개수는 `BreakthroughEffect.Style(...).AuraWisps`·`AuraPetals` 다.
- [사람 확인] 실행하고 10초 뒤 화면에서 제자 주변에 오오라·기운·꽃잎이 보이지 않는다 (삼류).
- [사람 확인] 에디터에서 금화를 200 이상 모아 돌파해 본다: 0.5초 뒤 연분홍 기본 빛과 세로 기운 2줄이 나타나고 기본 빛이 커졌다가 돌아오며, 제자가 기운 앞에 그려지고 걸어가도 발밑을 따라간다. 꽃잎은 아직 없다.
- [사람 확인] 에디터에서 일류·절정·초절정까지 돌파해 본다: 꽃잎이 2·3·5장 제자 앞으로 떠오르고, 기운이 2·3·4줄이며, 색이 점점 진한 매화 분홍이다.

## 하지 말 것
- 돌파 연출 부품(어둡게 가림막, `Light2D`, `Volume`·`Bloom`, 고리, 기둥, 충격파, 돌파 꽃잎, 번쩍임, 카메라 흔들림)은 만들지 않는다 (062 가 한다).
- 어셈블리 정의(`PlumSword.Game.asmdef`)를 고치지 않는다 (062 가 한다).
- `Assets/Scripts/Core/` 는 고치지 않는다 (058·059·060 이 한다).
- `RealmView`, `GameRoot`, `CombatView`, `StatUpgradeButtonEffect` 를 고치지 않는다. 제자 위치는 `Battle.DisciplePosition` 만 읽는다.
- `ParticleSystem`, 가산 혼합 재질·셰이더 에셋을 쓰지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다. 그림 파일을 만들거나 넣지 않는다.
- `docs/dev/assets.md` 를 고치지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/` 를 고치지 않는다.

## 보고서

### 한 일
- `RealmAuraView` 에 `[DefaultExecutionOrder(100)]` 을 붙였다.
- 상시 오오라를 세 층으로 나눴다: 기본 빛(9990), 기운(9991, 렌더러 4개), 꽃잎(10002, 렌더러 5개). 렌더러는 `Initialize` 때 최대 개수만큼 만들고, 쓰지 않는 것은 끈다.
- 위치: `LateUpdate` 에서 발 위치 `(DisciplePosition − 2.25, −1.5)` 위 1.25 유닛을 오오라 가운데로 두고, `RealmAura.Wisp`/`FloatingPetal` 의 X, Y 를 이 가운데에 더한다.
- 연출 시간: `BrokeThrough` 를 받으면 `effectStart = Time.time`, `effectRealm = 새 경지`, `previousRealm = 새 경지 − 1` 로 두고 `playing = true` 로 한다. `t = Time.time − effectStart`. `BreakthroughEffect.IsPlaying(t)` 가 `false` 가 되면 끝낸다. 연출 중에 또 돌파하면 시작 시각을 새로 잡는다. 처음 시작에는 연출하지 않는다. 이 필드는 비공개이고 062 가 쓴다.
- 보여 줄 경지: 연출 중이고 `ShowsNewAura(t)` 가 `false` 면 직전 경지, 그 밖에는 `realm.Current`. 보여 줄 경지가 바뀌면 기본 빛 그림을 다시 읽고 애니메이션을 첫 프레임부터 재생한다. 삼류면 세 층이 모두 꺼진다.
- 기본 빛의 크기 배율: 연출 중 `t ≥ 0.5` 이면 `RealmAura.BurstScale(t − 0.5)` 를 곱하고, 그 밖에는 1 이다.
- 기운: 그림 `Art/Effects/aura_wisp` 를 32×64 칸으로 잘라 10 fps 로 반복 재생한다 (높이 3.0 × Scale 유닛, 비율 유지, 흰색에 알파). 그림이 없으면 8×32 대체 텍스처를 0.5 × 2.0 × Scale 유닛으로 경지 색에 그린다.
- 꽃잎: 그림 `Art/Effects/petal` 을 8×8 칸으로 잘라 `i % 칸 수` 번째 칸을 쓴다. 그림이 없으면 3×3 십자 대체 텍스처를 쓴다. 1.5배로 그린다. 꽃잎 스프라이트는 062 도 쓰도록 필드(`petalSprites`/`petalFallbackSprite`, 도우미 `PetalSprite(i)`)에 둔다.
- 그림 크기가 맞지 않으면 그 그림마다 `Debug.LogWarning` 을 한 번 남기고 대체 그림을 쓴다. 실행 중에 만든 텍스처와 스프라이트는 모두 `OnDestroy` 에서 지운다.

### 바뀐 파일
- `Assets/Scripts/Game/Realm/RealmAuraView.cs`

### 가정
- 이 작업이 읽는 리소스 경로와 없을 때 보여 주는 것:
  - `Resources/Art/Effects/aura_<second_rate|first_rate|peak|transcendent>`: 없으면 64×64 원형 그러데이션을 2.4 × 3.0 유닛으로 그린다 (기존과 같음).
  - `Resources/Art/Effects/aura_wisp`: 없으면 8×32 세로 물방울.
  - `Resources/Art/Effects/petal`: 없으면 3×3 십자.
  - 지금 `Assets/Resources/Art/Effects/` 에는 이 그림들이 없으므로 셋 다 대체 그림이 보인다.
- 기본 빛은 "지금의 동작 유지"에 따라 기존대로 했다: 그림이 있으면 흰색에 알파 1, 1.5배로 그리고, 대체 텍스처는 Bilinear 필터를 쓴다. 사양 일반 규칙의 "대체 그림은 Point" 와는 다르다.
- 기운 그림 스프라이트는 PPU 64 로 만든다 (칸 높이 1 유닛). 이렇게 하면 배율 `3 × Scale` 로 높이 3.0 × Scale 유닛이 되고 비율도 유지된다. 기운 그림 애니메이션은 `Time.time × 10` 으로 프레임을 고르고, 줄마다 따로 시작하지 않는다.
- 꽃잎 크기는 `1.5 × EffectPoint.Scale` 로 그린다 (지금 Scale 은 항상 1). 꽃잎 그림 칸은 PPU 32 (8 픽셀 = 0.25 유닛).
- 그림이 있을 때 기운과 꽃잎은 흰색에 알파만 곱한다. 대체 그림일 때는 `RealmAura.Color` 의 RGB 에 알파를 곱한다.
- 직전 경지를 계산할 때 0 아래로 내려가지 않게 막았다 (삼류로 돌파하는 일은 없다).
- `effectRealm` 은 062 가 읽을 필드라 지금은 값을 넣기만 한다 (컴파일 경고 CS0414 가 날 수 있음).

### 사람이 확인할 것
- 실행하고 10초 뒤 화면에서 제자 주변에 오오라·기운·꽃잎이 보이지 않는다 (삼류).
- 에디터에서 금화를 200 이상 모아 돌파해 본다: 0.5초 뒤 연분홍 기본 빛과 세로 기운 2줄이 나타나고 기본 빛이 커졌다가 돌아오며, 제자가 기운 앞에 그려지고 걸어가도 발밑을 따라간다. 꽃잎은 아직 없다.
- 에디터에서 일류·절정·초절정까지 돌파해 본다: 꽃잎이 2·3·5장 제자 앞으로 떠오르고, 기운이 2·3·4줄이며, 색이 점점 진한 매화 분홍이다.
- 대체 기운(0.5 × 2.0 유닛)과 대체 꽃잎(약 0.14 유닛)의 크기가 화면에서 적당한지.

### 테스트 결과
- `python tools/loop/run_unity_tests.py`: EditMode 783/783 통과, PlayMode 80/80 통과 (`GameBootTests` 포함, 오류 로그 없음). 컴파일 오류 없음.
- 새 자동 테스트는 쓰지 않았다 (작업 문서의 완료 조건은 `[컴파일]` 과 `[사람 확인]` 뿐이다).


## 루프 기록 (2026-11-20 20:22)

- 작업 에이전트: claude(시도 1)
- 리뷰: claude (같은 에이전트) 통과
- 기능 브랜치: task/061_realm_aura_layers_view
- 루프 브랜치: loop/20261120-175740
