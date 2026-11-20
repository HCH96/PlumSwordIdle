# 062_breakthrough_effect_view — 단계별 돌파 연출 부품과 빛·블룸·흔들림 (Game)

## 목표
061 이 고친 `PlumSword.Game.Realm.RealmAuraView` 에 돌파 연출 부품(어둡게 가림막, 점광원과 전역 광원, 블룸, 바닥 고리, 빛기둥, 충격파, 돌파 꽃잎, 흰 번쩍임, 카메라 흔들림)을 더한다. 블룸을 쓰려고 `PlumSword.Game.asmdef` 에 URP 참조 두 줄을 더한다. 화면(Game) 작업이다.

## 묶음
realm_fx

## 선행 작업
- 059_breakthrough_parts
- 060_realm_aura_plum
- 061_realm_aura_layers_view

## 참고 문서
- docs/index.md
- docs/dev/architecture.md
- docs/spec/realm.md ("연결 지점"의 `BreakthroughEffect`·`RealmAura.BurstColor`·"필요한 것", "화면"의 "오오라와 돌파 연출" 앞부분과 "돌파 연출", "범위 밖", "완료 조건 초안" 31·49~51)

## 요구사항
- 이 사양의 `## 변경 기록` `2026-11-20 — 매화 오오라와 단계별 돌파 연출` 항목만 구현한다. 그 밖의 규칙은 이미 구현되어 있다. 이 작업은 그중 아래 범위만 맡는다.
- **어셈블리 정의** (관문 예외 `asmdef`): `Assets/Scripts/Game/PlumSword.Game.asmdef` 의 `references` 배열 끝에 `"Unity.RenderPipelines.Core.Runtime"` 과 `"Unity.RenderPipelines.Universal.Runtime"` 두 줄만 더한다 (`Volume`·`VolumeProfile` 과 `Bloom`·`UniversalAdditionalCameraData` 가 그 어셈블리에 있다. `Light2D` 는 이미 참조된 `Unity.RenderPipelines.Universal.2D.Runtime` 에 있다). 다른 칸과 다른 어셈블리 정의는 바꾸지 않는다.
- 고칠 파일: `Assets/Scripts/Game/Realm/RealmAuraView.cs` (061 이 고침). 필요하면 같은 폴더에 내부용(`internal`) 도우미 파일을 더해도 된다 (`.meta` 함께). 061 이 만든 연출 시간 `t`, 새 경지 `r`(받은 `BrokeThrough` 값), 보여 줄 경지, 상시 오오라 세 층, 꽃잎 그림·대체 텍스처를 그대로 쓴다. `Initialize` 의 모양과 `GameRoot` 조립은 바꾸지 않는다.
- 연출 중(`BreakthroughEffect.IsPlaying(t)`)에만 아래 부품을 보여 준다. 끝나면 부품을 모두 숨기고, 점광원 세기 0, 블룸 세기 0, 카메라 후처리를 시작 전 값으로 돌려놓는다. 연출 중에 또 돌파하면 061 이 시작 시각을 새로 잡으므로 부품도 새 경지 기준으로 0초부터 다시 계산한다 (카메라 후처리의 "시작 전 값"은 처음 연출이 시작될 때 기억한 값을 유지한다).
- 오오라 가운데 = 발 위 1.25 유닛 (061 과 같다). "발 위치"는 발밑 그 자체다.
- **어둡게** (`sortingOrder` 9980): 실행 중에 한 번 만드는 128×64 텍스처(PPU 2, 즉 1 픽셀 = 0.5 유닛, Bilinear 필터). 픽셀 (x, y) 의 알파 = `BreakthroughEffect.DimMask(d)`, `d` = 픽셀 중심 `((x + 0.5 − 64) × 0.5, (y + 0.5 − 32) × 0.5)` 의 원점 거리(유닛). RGB 검정. 64×32 유닛으로 오오라 가운데에 맞춘다. `SpriteRenderer.color` 의 알파 = `BreakthroughEffect.DimAlpha(r, t)`.
- **빛**: `[RealmAuraView]` 아래에 점 `Light2D`(`lightType = Point`, `pointLightInnerRadius = 0.5`, `pointLightOuterRadius = 5.0`, 색 `RealmAura.Color(r)` 의 RGB 에 A 1, 세기 `BreakthroughEffect.LightIntensity(r, t)`, 모든 정렬 레이어에 적용)를 오오라 가운데에 둔다. 연출이 아닐 때 세기 0. `Initialize` 때 장면에 `lightType == Global` 인 `Light2D` 가 없으면 `[RealmAuraView]` 아래에 세기 1, 흰색, 모든 정렬 레이어에 적용되는 전역 `Light2D` 를 하나 만든다 (점광원만 있으면 나머지 화면이 어두워지기 때문). 정렬 레이어 목록은 `SortingLayer.layers` 의 id 로 채운다 (`Light2D` 의 대상 정렬 레이어를 정하는 공개 API 가 없으면 리플렉션 대신 그 버전에서 쓸 수 있는 방법을 쓰고, 보고서의 가정에 적는다).
- **블룸**: `Initialize` 때 `[RealmAuraView]` 아래 오브젝트에 `Volume`(`isGlobal = true`, `priority = 100`, `weight = 1`)을 붙이고, `ScriptableObject.CreateInstance<VolumeProfile>()` 로 만든 프로필에 `Bloom` 을 더한다: `threshold` 0.8, `scatter` 0.7, `intensity` 0 (모두 `overrideState = true`, 임시값). 연출 중 세기 = `BreakthroughEffect.BloomIntensity(r, t)`. 연출이 시작될 때 `Camera.main` 의 `UniversalAdditionalCameraData.renderPostProcessing` 을 기억했다가 `true` 로 바꾸고, 연출이 끝나면 기억한 값으로 돌려놓는다. `Camera.main` 이 `null` 이거나 `UniversalAdditionalCameraData` 가 없으면 후처리 전환만 건너뛴다 (예외·오류 로그 없음). 프로필은 `OnDestroy` 에서 지운다. 설정 에셋(렌더러, 볼륨 프로필 에셋)은 바꾸지 않는다.
- **고리** (`sortingOrder` 9992, `Style(r).HasRing` 일 때만): 그림 `Resources.Load<Texture2D>("Art/Effects/breakthrough_ring")` (칸 160×48) 이 있으면 그것을, 없으면 실행 중에 만든 64×16 텍스처(PPU 32, Point): `u = (x + 0.5 − 32) / 32`, `v = (y + 0.5 − 8) / 8`, `d = √(u² + v²)`, 알파 = `max(0, 1 − |d − 0.85| / 0.15)`, RGB 흰색. 가운데를 발 위치에 맞춘다. 대체 그림은 가로 `2 × RingRadius(r, t)` × 세로 `0.6 × RingRadius(r, t)` 유닛, 그림은 가로를 `2 × RingRadius(r, t)` 에 맞추고 비율 유지. 알파 `BurstFade(t)`, 대체 그림의 색 `RealmAura.BurstColor(r)` RGB, 그림은 흰색. 반지름이 0 이면 숨긴다.
- **기둥** (`sortingOrder` 9993, `HasPillar` 일 때만): 그림 `Art/Effects/breakthrough_pillar` (칸 64×192) 이 있으면 그것을, 없으면 16×64 텍스처(PPU 32, Point): `dx = x + 0.5 − 8`, 알파 = `(1 − |dx| / 8)² × min(1, (63.5 − y) / 16)`, RGB 흰색. 아래 끝(피벗 아래 가운데)을 발 위치에 맞춘다. 대체 그림은 가로 `PillarWidthAt(t)` × 세로 `Style(r).PillarHeight` 유닛, 그림은 세로를 `PillarHeight` 에 맞추고 비율 유지. 알파 `BurstFade(t)`, 대체 그림의 색 `BurstColor(r)` RGB. 폭이 0 이면 숨긴다.
- **충격파** (`sortingOrder` 9994, `HasShockwave` 일 때만): 그림 `Art/Effects/breakthrough_shockwave` (칸 192×64) 가 있으면 그것을, 없으면 고리의 대체 텍스처를 쓴다. 가운데를 발 위치에 맞춘다. 대체 그림은 가로 `2 × ShockwaveRadius(r, t)` × 세로 `0.6 × ShockwaveRadius(r, t)`, 그림은 가로를 `2 × ShockwaveRadius(r, t)` 에 맞추고 비율 유지. 알파 `ShockwaveAlpha(r, t)`, 대체 그림의 색 `BurstColor(r)` RGB. 반지름이 0 이면 숨긴다.
- **부품 그림 재생**: 고리·기둥·충격파 그림은 가로 한 줄 시트를 위 칸 크기로 잘라 진행도에 맞춰 한 번 재생한다. 프레임 = `⌊진행도 × 칸 수⌋` 를 0 ~ 칸 수 − 1 로 잘라낸 값. 진행도는 고리·기둥 `(t − 0.5) / 0.8`, 충격파 `(t − 0.5) / 0.4`. 시트 가로가 칸 너비로 나누어떨어지지 않거나 높이가 칸 높이가 아니면 `Debug.LogWarning` 을 한 번 남기고 대체 그림을 쓴다. 그림은 색을 입히지 않는다 (흰색).
- **돌파 꽃잎** (`sortingOrder` 10003): `Style(r).BurstPetals` 장 (최대 36 장을 처음에 만들어 두고 나머지는 끈다). 장 `i` 마다 `BreakthroughEffect.BurstPetal(r, i, t)` 의 X, Y(오오라 가운데에 더함), Scale, Alpha. 그림은 061 의 꽃잎 그림·대체 텍스처를 그대로 쓰고 화면에서 1.5배 × Scale. 색 `RealmAura.Color(r)` RGB, 알파는 `BurstPetal` 의 Alpha.
- **번쩍임** (`sortingOrder` 29990, `HasFlash` 일 때만): 1×1 흰 텍스처를 30×12 유닛으로 늘려 `Camera.main` 위치의 (x, y) 에 둔다. 알파 `FlashAlpha(r, t)`. `Camera.main` 이 없으면 숨긴다.
- **흔들림**: `LateUpdate` 에서 `ShakePixels(r, t)` 가 0 보다 크면 `Camera.main` 위치의 x, y 에 각각 `UnityEngine.Random.Range(-1, 2)` × 픽셀 수 ÷ 32 유닛을 더한다. 원래 위치는 `CombatView.Update` 가 매 프레임 다시 정하므로 따로 되돌리지 않는다 (061 이 붙인 `[DefaultExecutionOrder(100)]` 덕에 이 더하기가 그 뒤에 온다). 번쩍임 위치는 흔들기 전 카메라 위치로 정한다.
- 연출은 그림만 덮는다. 전투·스테이지 진행·페이드·버튼·저장 타이머에 영향을 주지 않고 `Time.timeScale` 을 바꾸지 않는다.
- 색·알파·크기·시간·개수는 `BreakthroughEffect`, `RealmAura` 의 값을 그대로 쓰고 화면 코드에서 다시 계산하지 않는다. 대체 텍스처의 픽셀 공식만 화면 코드에 둔다.
- 실행 중에 만든 텍스처·스프라이트·`VolumeProfile` 은 `OnDestroy` 에서 지운다. 그림이 없을 때 오류 로그(`Debug.LogError`, 예외)를 남기지 않는다. 보고서의 가정에 읽는 리소스 경로와 없을 때 보여 주는 것, 전역 광원을 만들었는지 여부를 적는다.
- 이 작업의 완료 조건은 `[컴파일]` 과 `[사람 확인]` 뿐이다 (판단은 058·059·060 의 에딧 모드 테스트가 한다). 새 자동 테스트를 쓰지 않아도 된다.

## 완료 조건
- [컴파일] 화면 코드가 컴파일되고 기존 테스트가 모두 통과한다 (플레이 모드 `GameBootTests` 포함: 게임을 켜서 `[Game]` 이 조립되는 동안 오류 로그가 없다).
- [컴파일] `PlumSword.Game.asmdef` 의 `references` 에 `Unity.RenderPipelines.Core.Runtime` 과 `Unity.RenderPipelines.Universal.Runtime` 이 더해졌고, 그 밖의 칸은 바뀌지 않았다.
- [컴파일] `RealmAuraView` 는 고리·기둥·충격파 그림을 `Resources.Load<Texture2D>("Art/Effects/breakthrough_ring")`, `"Art/Effects/breakthrough_pillar"`, `"Art/Effects/breakthrough_shockwave"` 로 읽고, `null` 이면 실행 중에 만든 대체 텍스처를 쓴다.
- [컴파일] 부품의 크기·알파·세기와 흔들림 픽셀은 `BreakthroughEffect` 의 `DimAlpha`, `DimMask`, `LightIntensity`, `BloomIntensity`, `FlashAlpha`, `ShakePixels`, `BurstFade`, `RingRadius`, `PillarWidthAt`, `ShockwaveRadius`, `ShockwaveAlpha`, `BurstPetal` 결과를 그대로 쓴다.
- [사람 확인] 실행하고 10초 뒤 화면의 밝기와 색이 이번 변경 전 갈무리와 같다 (돌파 전 평소 화면. 전역 광원·블룸이 평소 화면을 바꾸지 않는다).
- [사람 확인] 에디터에서 금화 200 이상으로 첫 돌파를 해 본다: 금화가 200 줄고 왼쪽 위가 `REALM: SECOND RATE` 가 된다. 0.5초 동안 배경과 적이 조금 어두워지며 분홍 꽃잎 12장이 제자에게 모인 뒤 바깥으로 흩어지고, 연분홍 오오라와 기운 2줄이 나타난다. 고리·기둥·번쩍임은 없다. 전투는 멈추지 않는다.
- [사람 확인] 에디터에서 일류 → 절정 돌파를 해 본다: 바닥 고리가 퍼지고 빛기둥이 솟으며, 화면이 2 픽셀 흔들리고 제자 주변의 바닥·배경이 분홍으로 밝아지며 기둥이 번져 보인다. 1.8초 뒤 블룸과 어둡게가 사라지고 화면이 원래 밝기로 돌아온다.
- [사람 확인] 에디터에서 절정 → 초절정 돌파를 해 본다: 흰 번쩍임과 충격파가 더해지고 꽃잎 36장이 터지며 3 픽셀 흔들린다. 그 뒤 상시 오오라는 기운 4줄과 떠오르는 꽃잎 5장이다.
- [사람 확인] 에디터에서 연출 중에 다시 돌파해 본다: 새 경지의 연출이 처음부터 나온다. 저장된 절정으로 이어하면 연출 없이 절정 오오라만 보인다.

## 관문 예외
- asmdef

## 하지 말 것
- `PlumSword.Game.asmdef` 의 위 두 참조 말고는 어셈블리 정의를 바꾸지 않는다. 다른 `.asmdef` 는 고치지 않는다.
- `ProjectSettings/`, `Packages/`, URP 렌더러·파이프라인 설정 에셋(`Assets/Settings/`)을 고치지 않는다. 볼륨 프로필 에셋을 만들지 않는다.
- `Assets/Scripts/Core/` 는 고치지 않는다 (058·059·060 이 한다).
- `RealmView`, `GameRoot`, `CombatView`, `StatUpgradeButtonEffect` 를 고치지 않는다. 카메라 위치는 `CombatView` 를 고치지 않고 `LateUpdate` 에서 더하기만 한다.
- 061 이 만든 상시 오오라 세 층의 동작을 바꾸지 않는다.
- `ParticleSystem`, 가산 혼합 재질·셰이더 에셋, 히트스톱(`Time.timeScale`), 연출 중 버튼 잠금을 쓰지 않는다. 평소(연출이 아닐 때) 블룸·점광원을 켜지 않는다.
- 씬·프리팹·에셋 파일(`.unity`, `.prefab`, `.asset`)을 고치지 않는다. 그림 파일을 만들거나 넣지 않는다.
- `docs/dev/assets.md` 를 고치지 않는다.
- 루프의 증거용 갈무리 `Assets/Tests/PlayMode/Capture/` 를 고치지 않는다.

## 보고서

### 한 일
- `PlumSword.Game.asmdef` 의 `references` 끝에 `Unity.RenderPipelines.Core.Runtime`, `Unity.RenderPipelines.Universal.Runtime` 두 줄만 더했다.
- `RealmAuraView` 에 돌파 연출 부품을 더했다: 어둡게(9980, 128×64 PPU 2 Bilinear, 알파 `DimMask`, 색 알파 `DimAlpha`), 점 `Light2D`(안 0.5 / 바깥 5.0, 색 `RealmAura.Color(r)`, 세기 `LightIntensity`), 전역 `Volume`(우선순위 100, 가중치 1) + 실행 중 만든 `VolumeProfile` 의 `Bloom`(문턱 0.8, 흩어짐 0.7, 세기 0 → 연출 중 `BloomIntensity`), 고리(9992)·기둥(9993)·충격파(9994)(그림 시트 또는 대체 텍스처, 진행도 프레임 재생), 돌파 꽃잎(10003, 36장 미리 생성), 번쩍임(29990, 1×1 흰색을 30×12 유닛), 흔들림(`LateUpdate` 에서 `ShakePixels` × {−1,0,1} ÷ 32 유닛).
- 연출 시작 때 `Camera.main` 의 `UniversalAdditionalCameraData.renderPostProcessing` 을 기억하고 `true` 로, 끝날 때(그리고 `OnDestroy`) 기억한 값으로 되돌린다. 연출 중 재돌파 때는 처음 기억한 값을 유지한다.
- 연출이 끝나면 부품 렌더러를 모두 끄고 점광원 세기 0(컴포넌트도 끔), 블룸 세기 0, 후처리 복원.
- 실행 중 만든 텍스처·스프라이트·`VolumeProfile`·`Bloom` 은 061 의 `createdObjects` 목록으로 `OnDestroy` 에서 지운다.
- `docs/spec/realm.md` "화면 › 돌파 연출 › 빛" 에 전역 광원을 연출 중에만 켜는 결정을 적었다.

### 바뀐 파일
- `Assets/Scripts/Game/PlumSword.Game.asmdef`
- `Assets/Scripts/Game/Realm/RealmAuraView.cs`
- `docs/spec/realm.md`
- `tasks/doing/062_breakthrough_effect_view.md` (보고서 추가)

### 가정
- 읽는 리소스 경로: `Art/Effects/breakthrough_ring`(칸 160×48), `Art/Effects/breakthrough_pillar`(칸 64×192), `Art/Effects/breakthrough_shockwave`(칸 192×64). 없으면 대체 텍스처(고리·충격파: 64×16 타원 테두리, 기둥: 16×64 세로 기둥, PPU 32 Point)를 `RealmAura.BurstColor(r)` 색으로 그린다. 크기가 틀리면 `Debug.LogWarning` 한 번(`Initialize` 때 한 번만 읽음) 후 대체 그림. 그림 시트의 PPU 는 32 로 잘랐고, 크기 맞춤은 스프라이트 크기(유닛)로 나눠 배율을 정했다.
- 꽃잎은 061 의 꽃잎 그림·대체 텍스처를 그대로 쓴다. 061 상시 꽃잎과 같게, 그림이 있으면 흰색(색 입히지 않음, "그림은 색을 입히지 않는다" 공통 규칙), 대체 그림일 때만 `RealmAura.Color(r)` RGB.
- **전역 광원**: 실제 게임 씬(`SampleScene`)에는 이미 전역 `Light2D` 가 있어 만들지 않는다. 장면에 켜진 전역 광원이 없으면 `[RealmAuraView]/GlobalLight2D` 를 만들지만 **꺼 둔 채로** 두고, 연출이 시작될 때 다른 켜진 전역 광원이 없을 때만 켜며 연출이 끝나면 끈다. 이유: `[Game]` 은 `DontDestroyOnLoad` 라서, 플레이 모드 테스트처럼 빈 씬에서 조립된 뒤 `SampleScene` 을 불러오면 늘 켜 둔 전역 광원이 씬의 전역 광원과 겹쳐 URP 가 `More than one global light` 오류 로그를 남겼다(처음 구현에서 플레이 모드 테스트 다수가 이것으로 실패). 점광원도 연출이 아닐 때는 꺼 두므로 평소 화면에는 영향이 없다.
- `Light2D` 의 대상 정렬 레이어는 URP 17.6 의 공개 속성 `targetSortingLayers` 에 `SortingLayer.layers` 의 id 를 넣어 정했다 (리플렉션 없음).
- 장면의 전역 광원 찾기는 `FindObjectsByType<Light2D>()`(Unity 6.6 의 인자 없는 형태)에서 `isActiveAndEnabled && lightType == Global` 인 것을 찾는다.
- 점광원은 연출이 아닐 때 세기 0 에 더해 컴포넌트도 끈다 (같은 결과, 그리는 비용만 줄임).
- 번쩍임은 `HasFlash` 이고 `Camera.main` 이 있으면 연출 내내 켜 두고 알파 `FlashAlpha(r, t)`(대부분 0)로 그린다. 위치 z 는 0.
- 고리·기둥·충격파는 폭(가로) 또는 높이가 0 이하이면 숨긴다 (기둥 높이 0 인 경지는 `HasPillar` 가 거짓이라 실제로는 폭 조건만 쓰인다).
- `Camera.main` 에 `UniversalAdditionalCameraData` 가 없으면 `TryGetComponent` 로 건너뛴다 (`GetUniversalAdditionalCameraData()` 는 컴포넌트를 새로 붙이므로 쓰지 않았다). 연출 중에 `RealmAuraView` 가 지워지면 `OnDestroy` 에서 후처리 값을 되돌린다.
- 블룸 수치(문턱 0.8, 흩어짐 0.7)는 작업 문서의 임시값 그대로다.
- 발 위치는 061 의 오오라 가운데 계산과 같은 `(DisciplePosition − 2.25, −1.5)`.

### 사람이 확인할 것
- 실행하고 10초 뒤 화면의 밝기와 색이 이번 변경 전 갈무리와 같다 (돌파 전 평소 화면. 전역 광원·블룸이 평소 화면을 바꾸지 않는다).
- 에디터에서 금화 200 이상으로 첫 돌파를 해 본다: 금화가 200 줄고 왼쪽 위가 `REALM: SECOND RATE` 가 된다. 0.5초 동안 배경과 적이 조금 어두워지며 분홍 꽃잎 12장이 제자에게 모인 뒤 바깥으로 흩어지고, 연분홍 오오라와 기운 2줄이 나타난다. 고리·기둥·번쩍임은 없다. 전투는 멈추지 않는다.
- 에디터에서 일류 → 절정 돌파를 해 본다: 바닥 고리가 퍼지고 빛기둥이 솟으며, 화면이 2 픽셀 흔들리고 제자 주변의 바닥·배경이 분홍으로 밝아지며 기둥이 번져 보인다. 1.8초 뒤 블룸과 어둡게가 사라지고 화면이 원래 밝기로 돌아온다.
- 에디터에서 절정 → 초절정 돌파를 해 본다: 흰 번쩍임과 충격파가 더해지고 꽃잎 36장이 터지며 3 픽셀 흔들린다. 그 뒤 상시 오오라는 기운 4줄과 떠오르는 꽃잎 5장이다.
- 에디터에서 연출 중에 다시 돌파해 본다: 새 경지의 연출이 처음부터 나온다. 저장된 절정으로 이어하면 연출 없이 절정 오오라만 보인다.
- 추가: 블룸이 실제로 보이는지 (프로젝트의 URP 2D 렌더러·카메라 설정에서 후처리가 동작하는지는 실행해야 안다). 어둡게 가림막·점광원이 씬의 기존 전역 광원과 섞여 의도한 밝기인지.
- 추가: 일류 돌파(고리만, 기둥 없음)도 한 번 보면 좋다.

### 테스트 결과
- `python tools/loop/run_unity_tests.py --project .`: EditMode 783개 중 783개 통과, PlayMode 80개 중 80개 통과 (`GameBootTests` 포함, 오류 로그 없음). 결과: 통과.
- `RealmAuraView.cs` 관련 컴파일 경고 없음 (유니티 로그 확인).
- 이 작업은 `[컴파일]`·`[사람 확인]` 조건뿐이라 새 자동 테스트는 쓰지 않았다.


## 루프 기록 (2026-11-20 20:30)

- 작업 에이전트: claude(시도 1)
- 리뷰: claude (같은 에이전트) 통과
- 기능 브랜치: task/062_breakthrough_effect_view
- 루프 브랜치: loop/20261120-175740
