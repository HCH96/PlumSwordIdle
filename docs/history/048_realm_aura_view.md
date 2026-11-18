# 048_realm_aura_view — 제자 주변 경지 오오라와 돌파 연출 (Game)

## 목표
제자 발밑을 따라다니며 경지별 오오라(그림이 없으면 원형 그러데이션 대체 표시)를 그리고 돌파 때 크기 연출을 하는 `RealmAuraView` 를 `PlumSword.Game.Realm` 에 만들고 `GameRoot` 에서 조립한다. 화면(Game) 작업이다. 색·깜빡임·연출 크기는 045 의 `RealmAura` 가 판정하므로 이 작업에는 새 자동 테스트가 필요 없다.

## 묶음
realm

## 선행 작업
- 045_realm_rules_text
- 046_realm_progress
- 047_realm_view

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/realm.md ("연결 지점"의 `RealmAuraView`·`RealmAura`·`RealmProgress.BrokeThrough`, "화면"의 "조립"·"오오라", "완료 조건 초안" 31~32)
- docs/spec/combat.md ("연결 지점" 절의 `Battle.DisciplePosition`, "화면"의 제자 표시 위치)

## 요구사항
- 047 이 `GameRoot` 에 만든 `public PlumSword.Core.Realm.RealmProgress Realm { get; private set; }` 를 쓴다 (`Awake` 에서 `Wallet` 직후에 만들어진다).
- **GameRoot** (`Assets/Scripts/Game/GameRoot.cs`) 에 자기 기능의 줄만 더한다: `CreateCombatView()` 호출 바로 뒤에 `CreateRealmAuraView()` 를 부른다. `CreateRealmAuraView` 는 `[Game]` 의 자식 `[RealmAuraView]` 오브젝트를 만들어 `RealmAuraView` 를 붙이고 `Initialize(Realm, Battle)` 를 부른다.
- **RealmAuraView** (`Assets/Scripts/Game/Realm/RealmAuraView.cs`, 네임스페이스 `PlumSword.Game.Realm`, `.meta` 함께):
  - `public void Initialize(PlumSword.Core.Realm.RealmProgress realm, PlumSword.Core.Combat.Battle battle)`. 인자 중 `null` 이 있으면 `ArgumentNullException`, 두 번 부르면 `InvalidOperationException`.
  - 월드 공간의 `SpriteRenderer` 하나로 오오라를 그린다 (오브젝트 이름 `RealmAura`). `sortingOrder` 는 9990 (제자 10000 뒤).
  - `realm.BrokeThrough` 를 구독하고 `OnDestroy` 에서 끊는다.
- **위치**: 매 프레임 `LateUpdate` 에서 발 위치 `((float)battle.DisciplePosition − 2.25, −1.5)` 를 따라가고, 오오라의 가운데는 발 위 1.25 유닛(즉 y = −0.25)이다. 이 값은 전투 사양 "화면"의 제자 표시 위치와 같다.
- **삼류**(`RealmAura.HasAura(realm.Current)` 가 `false`)에서는 아무것도 보이지 않는다 (렌더러를 끈다).
- **그림 모드**: `Resources.Load<Texture2D>("Art/Effects/aura_" + RealmText.AssetId(realm.Current))` 가 있으면 가로 한 줄 시트를 정사각형 칸(칸 한 변 = 시트 높이, 칸 수 = 가로 ÷ 높이)으로 잘라 `Sprite.Create` 로 만들고 10 fps(임시값)로 반복 재생한다. 피벗은 칸 가운데, PPU 32, 화면에서 1.5배(제자와 같다), 색은 흰색(그림 그대로). 가로가 높이로 나누어떨어지지 않으면 `Debug.LogWarning` 을 한 번 남기고 대체 모드를 쓴다.
- **대체 모드** (그림이 없을 때): 실행 중에 만든 64×64 텍스처 하나를 쓴다. 픽셀마다 알파 = `(1 − r)²` (r = 텍스처 가운데에서 잰 거리 ÷ 32, r ≥ 1 이면 0), RGB 는 흰색, `filterMode = Bilinear`. 이 텍스처로 만든 스프라이트를 가로 2.4 × 세로 3.0 유닛으로 늘려 그린다 (임시값). 색은 `RealmAura.Color(realm.Current)` 의 R, G, B, 알파는 매 프레임 `RealmAura.PulseAlpha(realm.Current, Time.time)` 이다. 텍스처는 처음 한 번만 만들고 `OnDestroy` 에서 지운다.
- 그림은 경지마다 따로 확인한다 (경지가 바뀔 때마다 새 경지의 그림을 찾는다). 지금 `Assets/Resources/Art/Effects/` 에는 오오라 그림이 하나도 없으므로 실행하면 대체 모드다. 그림이 없을 때 오류 로그(`Debug.LogError`, 예외)를 남기지 않는다 (경고도 시트 크기가 틀린 경우에만 남긴다). 보고서의 가정에 이 작업이 읽는 리소스 경로를 적고, 없을 때 무엇을 보여 주는지 적는다.
- **돌파 연출**: `BrokeThrough` 를 받으면 새 경지의 오오라로 바꾸고, 그때부터 흐른 시간 `t`(초, `Time.time` 차이)로 위의 크기에 `RealmAura.BurstScale(t)` 를 곱한다 (0.4초 동안 1.5배 → 1배). 그림·대체 모드 모두 같다. 연출 중에 또 돌파하면 `t` 를 0 부터 다시 센다. 처음 시작할 때는 연출하지 않는다 (배율 1).
- 스테이지 전환 페이드, 패배, 구간 소개 중에도 오오라는 그대로 보인다 (`Battle` 의 상태를 보지 않는다).
- 색·깜빡임·연출 크기는 `RealmAura` 의 값을 그대로 쓰고 화면 코드에서 다시 계산하지 않는다.
- 이 작업의 완료 조건은 `[컴파일]` 과 `[사람 확인]` 뿐이다. 새 자동 테스트(에딧 모드·플레이 모드)를 쓰지 않아도 된다.

## 완료 조건
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다 (플레이 모드 `GameBootTests` 포함: 게임을 켜서 `[Game]` 이 조립되는 동안 오류 로그가 없다).
- [컴파일] `GameRoot.Awake` 가 `CreateCombatView()` 바로 뒤에 `[RealmAuraView]` 를 만들어 `RealmAuraView.Initialize(Realm, Battle)` 를 부른다.
- [컴파일] `RealmAuraView` 는 오오라 그림을 `Resources.Load<Texture2D>("Art/Effects/aura_" + RealmText.AssetId(...))` 로 읽고, `null` 이면 실행 중에 만든 원형 그러데이션 대체 표시를 쓴다.
- [사람 확인] 실행하고 10초 뒤 화면에서 제자 주변에 오오라가 보이지 않는다 (삼류).
- [사람 확인] 에디터에서 금화를 200 이상 모아 돌파해 본다: 제자 주변에 하늘색 오오라가 0.4초 동안 커졌다가 돌아온 뒤 은은하게 깜빡이고, 제자가 오오라 앞에 그려지며 제자가 걸어가도 발밑을 따라간다.
- [사람 확인] 에디터에서 초절정까지 돌파해 본다: 오오라가 매화 분홍이다.
- [사람 확인] 에디터에서 스테이지 전환 페이드와 패배 동안에도 오오라가 그대로 보이는지 본다.

## 하지 말 것
- `RealmView`(경지 표시·돌파 버튼)는 고치지 않는다 (047 이 만든다).
- `GameRoot` 에는 자기 기능의 줄(`CreateRealmAuraView` 와 그 호출)만 추가하고, 다른 기능의 줄과 047 이 넣은 `Realm` 생성·잠금 구독은 고치지 않는다.
- `Assets/Scripts/Core/` 는 고치지 않는다 (045·046 의 `PlumSword.Core.Realm` 포함).
- `StatUpgrades`·`Battle`·`SaveData`·`SaveCodec`·`AutoSave` 를 고치지 않는다 (보너스 적용은 능력치 강화, 돌파 때 현재 체력 증가는 전투, 경지 저장은 저장 기능 사양의 일이다).
- `CombatView`, `StatUpgradeButtonEffect` 를 고치지 않는다. 제자 위치는 `Battle.DisciplePosition` 만 읽는다.
- Unity 파티클 같은 이펙트로 오오라를 만들지 않는다.
- `docs/dev/assets.md` 를 고치지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)과 어셈블리 정의를 고치지 않는다. 그림 파일을 만들거나 넣지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/` 를 고치지 않는다.

## 보고서

### 한 일
- 경지별 그림 시트와 원형 그러데이션 대체 표시, 위치 추적 및 돌파 크기 연출을 구현하고 GameRoot에 조립했다.
- 경지 변경 시 그림을 다시 찾고 생성한 스프라이트와 텍스처 및 이벤트 구독을 정리한다.

### 바뀐 파일
- Assets/Scripts/Game/GameRoot.cs
- Assets/Scripts/Game/Realm/RealmAuraView.cs 및 RealmAuraView.cs.meta
- docs/spec/realm.md
- tasks/doing/048_realm_aura_view.md, .loop/result.json

### 가정
- 그림은 Resources 경로 `Art/Effects/aura_` 뒤에 경지별 AssetId를 붙여 읽는다. 없으면 경지 색의 원형 그러데이션을 표시한다.
- 짝수 크기 텍스처의 거리는 픽셀 중심을 기준으로 측정하며, 그림 애니메이션은 새 경지에서 첫 프레임부터 시작한다.

### 사람이 확인할 것
- 실행 10초 뒤 삼류에서 제자 주변에 오오라가 보이지 않는지 확인한다.
- 금화 200 이상으로 돌파해 하늘색 오오라의 0.4초 크기 연출, 깜빡임, 제자 뒤 정렬과 이동 추적을 확인한다.
- 초절정 오오라가 매화 분홍인지 확인한다.
- 스테이지 전환 페이드와 패배 동안에도 오오라가 보이는지 확인한다.

### 테스트 결과
- 새 자동 테스트는 작업 문서에 따라 작성하지 않았다. Unity 컴파일과 기존 테스트는 직접 실행하지 않았으며 루프 스크립트의 판정을 기다린다.


## 루프 기록 (2026-11-18 12:25)

- 작업 에이전트: codex(시도 1)
- 리뷰: claude (교차) 통과
- 기능 브랜치: task/048_realm_aura_view
- 루프 브랜치: loop/20261118-121225
