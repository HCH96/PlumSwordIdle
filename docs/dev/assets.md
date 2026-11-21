# 에셋 목록

게임에 들어가는(또는 들어갈) 그림 에셋의 목록이다. 경로, 용도, 크기, 프레임, 기준점, 상태를 적는다. 크기·가져오기 설정은 [art.md](art.md), 제자 기준 그림체와 제작 절차는 [art_style.md](art_style.md)를 따른다. 적용된 에셋은 실제 값을, 계획 에셋은 계획값을 적는다.

경지 표시 D안은 [원본 색인](../../art/source/realm_status/index.md)에 보관한다. 매화 붓터치 1장과 경지 이름 그림 5장은 각각 투명 PNG 2172×724, 정지 이미지다. 게임 리소스 `Art/UI/RealmStatus/`에 적용한다. 왼쪽 위 기준, Trilinear·밉맵·최대 크기 4096을 사용하며 원본 투명 여백을 유지한다.

> **초안 (2026-11-17).** 지금 있는 사양(`docs/spec/`)의 대체 표시와, 사양이 2단계로 미뤄 둔 항목에서 뽑았다. 제자 대기·달리기·공격은 실제 적용한 값으로 고쳤다. 2단계 사양(테마 스테이지, 오프라인 보상 등)이 확정되면 그 사양을 쓰면서 이 표를 고친다. 상태가 `계획`인 줄은 아직 파일이 없다.

## 쓰는 법

- **누가 고치는가**: 에셋을 만들어 넣는 사람(또는 사람과 대화하는 에이전트)이 고친다. 루프 안의 에이전트는 이 표를 읽기만 하고, 표에 없는 에셋을 만들지 않는다. 필요한 에셋이 없으면 대체 표시로 구현하고 보고서의 가정에 적는다.
- **불러오는 법**: 화면 코드는 `Resources.Load<Sprite>("<리소스 경로>")`로 읽는다. 리소스 경로는 `Assets/Resources/` 다음부터 확장자 없이 쓴다 (예: `Art/Characters/disciple_idle`). 파일이 없으면 지금처럼 단색 사각형 대체 표시를 쓴다 (`architecture.md`). 그래서 에셋이 들어오기 전에도 루프 작업은 막히지 않는다.
- **상태**: `계획`(파일 없음) → `임시`(들어왔지만 다시 그릴 예정) → `최종`.
- **유닛 환산**: PPU 32. 48px = 1.5 유닛, 96px = 3 유닛, 128px = 4 유닛.
- 애니메이션은 가로 한 줄 스프라이트 시트로 넣고, 칸 크기는 캔버스 크기와 같게 한다. 재생 속도는 초당 프레임(fps)이다.

## 작은 스테이지 UI (2026-11-18)

`Art/UI/Substage/`의 정지 이미지. 1프레임, 가운데 기준점이며 방향과 fps는 해당 없다. 사용자가 시안을 승인했으며 1스테이지 진행 UI에 적용했다. `header` 1536×505, `completed` 905×910, `current` 828×857, `empty` 547×557, `boss` 921×1024px이다. [시안과 원본](../../art/source/substage_ui/index.md)을 보관한다. 금빛 박동은 코드로 그리며 각 이미지 자체는 정지 그림이다.

## 오프라인 보상 UI (2026-11-20 적용)

`Art/UI/OfflineReward/`의 정지 이미지. 가운데 기준점, 1프레임이며 방향과 fps는 해당 없다. `window`는 1536×1024, `reward_plaque`와 `claim_button`은 각각 2172×724px이다. 원본 캔버스의 투명 여백을 포함해 배치하고 Trilinear·밉맵을 사용한다. 금화는 `Art/UI/StatUpgrade/coin`을 재사용한다. `title_label`, `elapsed_label`, `applied_label`, `maximum_label`, `claim_label`, `retry_label`, `save_failed_label`은 한글 그림 글자이며, 시간은 글자로 표시하고 금화 숫자는 `Art/UI/damage_numbers` 그림에서 잘라 표시한다. [시안과 원본](../../art/source/offline_reward/index.md).

## 설정 UI (2026-11-18 적용)

`Art/UI/Settings/` 아래의 정지 이미지다. 모두 1프레임이며 방향과 fps는 해당 없다. 기준점은 가운데, 상태는 사용자가 선택한 시안 적용이다. 원본은 [설정 UI 원본](../../art/source/settings/index.md)에 둔다.

| 리소스 이름 | 용도 | 크기(px) |
| --- | --- | --- |
| `window` | 조작 요소가 없는 설정 창 바탕 | 1536×1024 |
| `button_settings` | 게임 중 설정 열기 | 2172×724 |
| `button_title`, `button_quit`, `button_close` | 시작 화면, 게임 종료, 닫기 버튼 | 1536×353, 1536×344, 1536×347 |
| `slider_track`, `slider_handle` | 음량 바탕과 움직이는 손잡이 | 1536×121, 325×347 |
| `label_bgm`, `label_sfx` | 배경 음악·효과음 글자 | 1536×425, 1364×484 |

큰 시안을 축소해서 표시하므로 이 폴더는 Trilinear·밉맵을 적용한다. 값과 채워지는 부분은 화면에서 따로 그린다.

## 능력치 강화 UI (2026-11-18 적용)

`Art/UI/StatUpgrade/` 아래 정지 이미지이며 각 1프레임, 가운데 기준점이다. 방향과 fps는 해당 없다. 상태는 사용자 선택 시안 적용. [원본과 색인](../../art/source/stat_upgrade/index.md). 버튼 바탕은 1536×334, 공격력은 350×345, 체력은 347×306, 공격 속도는 395×325, 금화는 501×511px이다. 각각 `button`, `attack`, `health`, `speed`, `coin`으로 불러오며 Trilinear·밉맵을 사용한다. 보유 금화 표시용 `currency_plaque`는 1536×280이며 [원본](../../art/source/currency/index.md)을 따로 보관한다.

- 내공 아이콘 `inner_power`는 1254×1254px 투명 PNG이며, 매화와 회오리치는 기운을 표현한다. 2026-11-21 Codex 내장 이미지 생성으로 제작했고 사용자가 최종 확정해 게임에 적용했다. 다른 강화 아이콘과 같은 가져오기 설정을 사용한다.

## 캐릭터

| ID | 리소스 경로 | 용도 | 캔버스(px) | 프레임 | fps | 기준점 | 방향 | 상태 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| disciple_idle | `Art/Characters/disciple_idle` | 제자 서 있기 (적 앞에서 공격 대기 중 포함) | 76×76 | 24 | 10 | (38, 12)px = 발 아래 가운데 | 남동쪽(오른쪽 앞) | 임시 |
| disciple_run | `Art/Characters/disciple_run` | 제자 전진 (달리기, 이동 속도 초당 6.48 유닛) | 76×80 | 24 | 20 | (38, 14)px = 발 아래 가운데 | 남동쪽(오른쪽 앞) | 임시 |
| disciple_attack | `Art/Characters/disciple_attack` | 제자 검 휘두르기 (공격 1회 = 1번 재생, 7번 프레임 = 피해 시점) | 76×76 | 17 | 17 ÷ 공격 간격 | (38, 12)px = 발 아래 가운데 | 남동쪽(오른쪽 앞) | 임시 |
| disciple_down | `Art/Characters/disciple_down` | 제자 패배 시 즉시 뒤로 쓰러짐 | 76×76 | 17 | 13.33 (프레임당 0.075초) | (38, 12)px | 남동쪽 | 최종 |

- **제자 대기·달리기·공격은 2026-11-17에 적용했다 (상태 `임시`).** 원본은 [제자 에셋 색인](../../art/source/disciple/index.md)의 선택된 시트를 그대로(확대·축소 없이) `Assets/Resources/Art/Characters/`에 복사했다. 원본 이름: `idle_south-east_sheet.png` → `disciple_idle.png`, `run_south-east_sheet.png` → `disciple_run.png`, `attack_south-east_sheet.png` → `disciple_attack.png`.
  - 플레이 확인을 위해 화면에서는 발 피벗을 유지한 채 제자와 적 표시를 1.5배로 키운다. 시트 원본과 PPU는 바꾸지 않는다. 비정수 확대이므로 픽셀의 균일함과 배경과의 차이는 실제 화면에서 다시 확인한다.
  - 계획의 48×48 대신 원본 칸 크기를 쓴다. 칸이 동작마다 달라(76×76, 76×80) 위 표의 기준점(칸 왼쪽 아래에서 잰 px)으로 발을 맞춘다. 몸 키는 약 53px(1.66 유닛)라 일반 몬스터 대체 표시(32px)보다 크다.
  - 피벗과 프레임 자르기는 Unity 가져오기 설정이 아니라 화면 코드(`DiscipleSprites`)가 한다. 시트는 Single 스프라이트로 가져오고, 가져오기 설정은 `Assets/Editor/PixelArtTextureImporter.cs`가 `Art/` 아래 그림에 [art.md](art.md)의 값(Sprite, PPU 32, Point, 압축 없음, 밉맵 끔)으로 맞춘다.
  - 원본 방향은 남동쪽(오른쪽 앞)이다. 반전하지 않고 오른쪽으로 진행하는 제자로 쓴다.
  - 재생 속도와 공격 시점 맞추기는 [전투 사양](../spec/combat.md) "화면"의 "제자 애니메이션"을 본다. 시트를 바꾸면 칸 수·칸 크기가 같아야 하고, 다르면 `DiscipleSprites`의 값과 이 표를 함께 고친다. 크기가 맞지 않으면 게임은 대체 표시(파란 사각형)로 돌아간다.
- 별도 피격 애니메이션은 사용하지 않는다. 쓰러짐은 사용자가 확정한 [원본·프레임·미리보기](../../art/source/disciple/index.md)를 적용했고 마지막 프레임을 유지한다.

- 스킬 `Art/Characters/disciple_skill`: 사용자 확정 제자와 매화 꽃잎 합성 시트, 8열 × 4행, 칸 99×105px, 32프레임, 24fps, 발 피벗 (58, 6)px, 남동쪽, 최종. [원본과 제작 정보](../../art/source/disciple/index.md)를 보관한다.

## 몬스터와 보스

- **5스테이지 천마신교 최종 보스**: [원본·미리보기·제작 기록](../../art/source/tianmo_final_boss/index.md). `Art/Enemies/tianmo_final_boss_idle`, `walk`, `attack`, `down`은 각각 104×104px × 25프레임의 가로 한 줄 시트이고 기준점은 (52, 16)px이다. `Art/Enemies/tianmo_final_boss_skill`은 캐릭터와 검격이 함께 들어 있는 5열×5행 시트로, 칸 260×80px, 25프레임, 기준점 (222, 5)px이며 맨 위 왼쪽부터 가로 순서다. PPU 32, Point, 무압축, 밉맵 끔, 최대 4096, 표시 배율 1.5다. 대기·이동은 10fps, 공격과 매 세 번째 기술은 공격 간격에 맞춰 재생하며 피해 프레임은 각각 11·15번이다. 쓰러짐은 0.075초/프레임이다.
- 천마신교 4스테이지 보스는 사용자가 승인한 [원본과 미리보기](../../art/source/tianmo_boss/index.md)를 사용한다. `Art/Enemies/tianmo_boss_` 아래 `idle`, `walk`, `attack`, `down`, `skill`은 각 칸 104×104px, 각각 17·25·25·17·37프레임이며 발 피벗은 (52, 18)px이다. 기술은 보정된 피색 검격을 포함한다. PPU 32, Point, 무압축, 최대 4096, 표시 배율 1.5다.
- 남궁 장로 보스는 사용자가 승인한 [원본과 재생 미리보기](../../art/source/namgung_boss/index.md)를 사용한다. `Art/Enemies/namgung_boss_idle`, `walk`, `attack`, `down`은 100×100px, 각각 17·25·25·17프레임, 피벗 (50, 15)px이다. `namgung_boss_skill`은 장로와 빛기둥을 합성한 5열×5행 시트, 칸 246×138px, 25프레임, 피벗 (209, 15)px이다. PPU 32, Point, 무압축, 최대 4096, 표시 배율 1.5. 대기·이동 10fps, 공격·기술은 25 ÷ 공격 간격 fps, 쓰러짐은 0.075초/프레임이다. 기술 시트는 원본 게임 팩의 잘린 칸과 발 기준점을 그대로 사용한다.

- 남궁 일반 상대는 사용자가 승인한 청의 검객과 머리를 풀어 내린 여성 검객을 3스테이지에 사용한다. [원본·생성 정보·미리보기](../../art/source/namgung/index.md). `Art/Enemies/namgung_male_` 및 `namgung_female_` 아래 `idle`, `walk`, `attack`, `down`을 둔다. 모두 76×76px, 각각 17·25·25·17프레임, 발 기준점은 남자 (38, 12)·여자 (38, 13)px이다. PPU 32, Point, 무압축, 최대 4096, 화면 배율 1.5. 대기·이동 10fps, 공격은 25 ÷ 공격 간격 fps, 쓰러짐은 0.075초/프레임이다. 검기는 공격 프레임에 포함된다.

- 천마신교 4스테이지 일반 상대는 사용자가 승인한 마검사와 흑갑 호위다. 5스테이지에는 일반 상대 없이 최종 보스만 등장한다. [원본·생성 정보·미리보기](../../art/source/tianmo_enemies/index.md). `Art/Enemies/tianmo_swordsman_`과 `tianmo_guard_` 아래 `idle`, `walk`, `attack`, `down`을 둔다. 각 칸 64×64px, 17·25·25·17프레임이고 발 기준점은 각각 (32, 6), (32, 5)px이다. PPU 32, Point, 무압축, 화면 배율 1.5, 대기·이동 10fps, 공격 25 ÷ 공격 간격 fps, 쓰러짐 0.075초/프레임이다. 공격은 승인한 마검사의 왼쪽 수평 찌르기와 호위의 수정 베기다.

일반 몬스터와 보스는 테마별로 하나씩 둔다 (테마 목록은 아래 "배경"). `<테마>` 자리에 테마 ID가 들어간다. 테마가 확정되기 전에는 `default` 한 벌만 만든다.

- 종남 장로 보스의 선택된 원본은 [원본 색인](../../art/source/jongnam_boss/index.md)에 보관하고 1스테이지에 적용했다. PPU 32, Point, 무압축, 최대 텍스처 크기 4096이며 제자와 같이 화면에서 1.5배로 표시한다. 접근 중 걷기를 반복하고 세 번째 공격마다 스킬 동작과 눈꽃 투사체를 사용한다.

| 리소스 경로 | 칸(px) | 프레임 | 기준점(px) | 방향 | 상태 |
| --- | --- | --- | --- | --- | --- |
| `Art/Enemies/jongnam_boss_idle` | 96×100 | 24 | (48, 15) | 남서 | 최종 |
| `Art/Enemies/jongnam_boss_walk` | 96×104 | 24 | (48, 18) | 남서 | 최종 |
| `Art/Enemies/jongnam_boss_attack` | 100×100 | 24 | (50, 15) | 남서 | 최종 |
| `Art/Enemies/jongnam_boss_skill` | 100×100 | 24 | (50, 15) | 남서 | 최종 |
| `Art/Enemies/jongnam_boss_down` | 100×100 | 17 | (50, 15) | 남서, 화면 오른쪽으로 쓰러짐 | 최종 |

- 종남파 일반 대련 상대의 남녀 일러스트·기본 그림·대기/걷기/공격은 사용자가 선택해 [원본 색인](../../art/source/jongnam/index.md)에 보관하고 1스테이지에 적용했다. 아래 공통 계획값과 달리 게임용 시트는 다음과 같다 (프레임 기준점은 칸 왼쪽 아래에서 잰 px).

| ID | 리소스 경로 | 칸(px) | 프레임 | fps | 기준점(px) | 방향 | 상태 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| jongnam_male_idle | `Art/Enemies/jongnam_male_idle` | 68×76 | 16 | 10 | (34, 11) | 남서 | 최종 |
| jongnam_male_walk | `Art/Enemies/jongnam_male_walk` | 72×84 | 24 | 12 | (36, 15) | 남서 | 최종 |
| jongnam_male_attack | `Art/Enemies/jongnam_male_attack` | 92×96 | 24 | 24 ÷ 공격 간격 | (46, 21) | 남서 | 최종 |
| jongnam_female_idle | `Art/Enemies/jongnam_female_idle` | 76×76 | 9 | 10 | (38, 12) | 남서 | 최종 |
| jongnam_female_walk | `Art/Enemies/jongnam_female_walk` | 72×84 | 24 | 12 | (36, 16) | 남서 | 최종 |
| jongnam_female_attack | `Art/Enemies/jongnam_female_attack` | 96×92 | 24 | 24 ÷ 공격 간격 | (48, 20) | 남서 | 최종 |

- 시트는 원본 크기로 PPU 32, Point, 무압축으로 가져오고 공격 시트의 가로가 2048px을 넘으므로 최대 크기를 4096으로 둔다. 화면에서는 제자와 같이 1.5배로 표시한다. 에셋 누락·크기 오류 시 기존 주황색 대체 표시를 쓴다.

- 개방 보스는 [승인한 원본과 미리보기](../../art/source/gaebang_boss/index.md)를 2스테이지에 적용한다. `Art/Enemies/gaebang_boss_idle`, `walk`, `attack`, `down`, `skill` 시트는 각 칸 96×96, 각각 9·25·25·25·33프레임이다. 모든 동작의 발 피벗은 (48, 16)px, PPU 32, Point, 무압축, 최대 4096, 표시 배율 1.5다. 대기·걷기 10fps, 공격과 기술은 각각 프레임 수 ÷ 공격 간격 fps, 쓰러짐은 0.075초/프레임이다. 방망이와 기술 궤적은 프레임에 포함된다.

- 개방 일반 상대는 [승인한 원본과 미리보기](../../art/source/gaebang/index.md)를 2스테이지에 적용한다. 아래 리소스 경로는 `Art/Enemies/` 아래이며, 기준점은 칸 왼쪽 아래부터 잰다. 대기·걷기는 10fps, 공격은 24 ÷ 공격 간격 fps, 쓰러짐은 0.075초/프레임이다. PPU 32, Point, 무압축, 최대 4096, 표시 배율 1.5다.

| 리소스 | 칸(px) | 프레임 | 기준점(px) |
| --- | --- | --- | --- |
| `gaebang_staff_idle` | 96×96 | 17 | (48, 24) |
| `gaebang_staff_walk` | 96×96 | 25 | (48, 24) |
| `gaebang_staff_attack` | 68×68 | 24 | (34, 10) |
| `gaebang_staff_down` | 96×96 | 16 | (48, 24) |
| `gaebang_unarmed_idle` | 64×84 | 17 | (32, 13) |
| `gaebang_unarmed_walk` | 84×84 | 24 | (42, 13) |
| `gaebang_unarmed_attack` | 84×84 | 24 | (42, 13) |
| `gaebang_unarmed_down` | 104×104 | 16 | (52, 23) |

| ID | 리소스 경로 | 용도 | 캔버스(px) | 프레임 | fps | 기준점 | 방향 | 상태 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| enemy_<테마>_walk | `Art/Enemies/<테마>_walk` | 일반 몬스터 접근 | 48×48 | 6 | 10 | 발 아래 가운데 | 왼쪽 | 계획 |
| enemy_<테마>_attack | `Art/Enemies/<테마>_attack` | 일반 몬스터 공격 | 48×48 | 5 | 12 | 발 아래 가운데 | 왼쪽 | 계획 |
| enemy_<테마>_hit | `Art/Enemies/<테마>_hit` | 일반 몬스터 피격 | 48×48 | 2 | 10 | 발 아래 가운데 | 왼쪽 | 계획 |
| boss_<테마>_idle | `Art/Bosses/<테마>_idle` | 보스 서 있기 (보스는 움직이지 않는다) | 128×128 | 4 | 6 | 발 아래 가운데 | 왼쪽 | 계획 |
| boss_<테마>_attack | `Art/Bosses/<테마>_attack` | 보스 공격 | 128×128 | 6 | 10 | 발 아래 가운데 | 왼쪽 | 계획 |
| boss_<테마>_hit | `Art/Bosses/<테마>_hit` | 보스 피격 | 128×128 | 2 | 10 | 발 아래 가운데 | 왼쪽 | 계획 |

- 종남 남녀 제자와 장로는 [선택한 쓰러짐 원본](../../art/source/jongnam_fall/index.md)을 사용한다. 남녀 시트는 `Art/Enemies/jongnam_male_down`, `Art/Enemies/jongnam_female_down`, 각 76×76px 17프레임이며 피벗은 각각 (38, 11), (38, 12)px이다. 세 동작 모두 0.075초/프레임이다. 쓰러짐 그림이 없는 상대는 즉시 없앤다.
- 보스 대체 표시(2.0×3.0 유닛 = 64×96px)보다 크다. 128×128에서 몸 높이를 96px 안팎으로 그리면 지금 배치와 크게 다르지 않다.

## 배경

테마당 시차 스크롤용 레이어 세 장. 높이 360, 폭 800 이상이고 가로로 이어 붙여도 이음매가 안 보이게 그린다.

- 4스테이지는 사용자가 선택한 [검은 성문 안뜰 A](../../art/source/stage4_magyo/index.md), 5스테이지는 [어두운 대전 B](../../art/source/stage5_tianmo/index.md)를 적용한다. 1~3스테이지와 같은 위치·높이·좌우 반전 반복이며 별도 시차 레이어는 없다.
- 3스테이지 남궁세가 도심 배경은 2026-11-21 사용자가 좌우 반전 반복을 확인하고 승인했다. [원본과 생성 문구](../../art/source/stage3_namgung/index.md)를 보관하며 1스테이지와 같은 위치·높이·좌우 반전 반복을 쓴다.
- 2스테이지 개방 강가 야영지 배경은 2026-11-21 사용자가 시안을 승인했다. 원본과 생성 문구는 [원본 색인](../../art/source/stage2_gaebang/index.md)에 보관한다. 1스테이지와 같은 위치·높이·좌우 반전 반복을 쓴다.
- 1스테이지 화산파 연무장 배경은 사용자가 v3를 확인하고 확정했다. `Assets/Resources/Art/Backgrounds/stage1_preview.png`를 좌우로 번갈아 반전해 반복하며 별도 시차 레이어는 없다. [원본과 참고 자료](../../art/source/stage1_courtyard_revision/index.md)를 보관한다.

| ID | 리소스 경로 | 용도 | 크기(px) | 기준점 | 상태 |
| --- | --- | --- | --- | --- | --- |
| stage1_preview | `Art/Backgrounds/stage1_preview` | 1스테이지 화산파 연무장 | 2172×724 | 가운데 | 최종 |
| stage2_gaebang | `Art/Backgrounds/stage2_gaebang` | 2스테이지 개방 강가 야영지 | 2172×724 | 가운데 | 시안 승인·적용 |
| stage3_namgung | `Art/Backgrounds/stage3_namgung` | 3스테이지 남궁세가 도심 | 2172×724 | 가운데 | 시안 승인·적용 |
| stage4_magyo | `Art/Backgrounds/stage4_magyo` | 4스테이지 마교 성문 안뜰 | 2172×724 | 가운데 | 시안 승인·적용 |
| stage5_tianmo | `Art/Backgrounds/stage5_tianmo` | 5스테이지 천마 대전 | 2172×724 | 가운데 | 시안 승인·적용 |
| bg_<테마>_far | `Art/Backgrounds/<테마>_far` | 먼 산·하늘 (가장 느림) | 800×360 | 가운데 | 계획 |
| bg_<테마>_mid | `Art/Backgrounds/<테마>_mid` | 중간 (나무, 건물) | 800×360 | 가운데 | 계획 |
| bg_<테마>_ground | `Art/Backgrounds/<테마>_ground` | 땅·길 (카메라와 같이 움직임) | 800×192 | 위 가운데 (윗변이 땅 높이) | 계획 |

테마 ID (2단계 사양에서 확정한다):

| 테마 ID | 이름 | 상태 |
| --- | --- | --- |
| default | 1단계 임시 필드 (화산 산길) | 테마 확정 전까지 이것만 만든다 |
| zhongnan | 종남파 | 2단계 사양에 이름이 나온다 |
| (미정) ×3 | 2~4번째 테마 | 2단계 사양에서 정한다 |
| tianmo | 천마 | 2단계 사양에 이름이 나온다 |

## UI

- 피해 숫자: `Art/UI/damage_numbers`는 [아이보리 숫자 원본과 확정된 매화색 원본](../../art/source/damage_numbers/index.md) 중 아이보리 원본을 사용한다. 원본 2172×724, PPU 32, Point, 무압축, 최대 4096이며 화면 코드가 0~9와 쉼표를 잘라 조합한다. 매화색은 추후 스킬 피해용이며 아직 게임에 적용하지 않는다.

UI 스프라이트는 작게 그리고 9-slice로 늘린다. 테두리 폭(9-slice 경계)은 가져오기 설정의 Sprite Border로 사람이 에디터에서 넣는다.

- 설정 버튼과 설정 창은 사용자가 선택해 [설정 UI 원본 색인](../../art/source/settings/index.md)에 보관했고 게임에도 적용했다. 분리된 게임용 리소스 경로와 크기는 위 "설정 UI" 표를 따른다. 모두 1프레임 정지 이미지로 방향과 fps는 해당 없다.

| ID | 리소스 경로 | 용도 | 원본 크기 | 화면 크기 | 상태 |
| --- | --- | --- | --- | --- | --- |
| jongnam_stage_hud | `Art/UI/jongnam_stage_hud` | 종남파 스테이지 제목·진행도 틀 | 2172×724px | 260×86.7 기준 픽셀 | 최종 (선택된 [원본](../../art/source/jongnam_ui/index.md)) |
| boss_health_frame_1~5 | `Art/UI/boss_health_frame_{1..5}` | 1~5스테이지(종남·개방·남궁·마교·천마) 보스 체력 틀. 게이지 칸은 투명, 채움은 코드의 색 바 | 2172×724px, 최대 2048·밉맵·필터 2 | 300×100 기준 픽셀, 게이지 칸은 [스테이지 사양](../spec/stage.md) 화면 절 | 원본 그대로 복사, 코드 연결 전 ([원본](../../art/source/boss_health_ui/index.md)) |
| jongnam_title_plum_duel | `Art/UI/jongnam_title_plum_duel` | 종남 일반 전투 제목 `매화 아래 비무` | 1917×462px | 최대 75.3×12.3 기준 픽셀, 비율 유지 | 최종 (선택 원본의 투명 여백 제거) |
| substage_intro_plaque | `Art/UI/Substage/intro_plaque` | 작은 스테이지 시작 번호 패 | 2172×724px | 360×120 기준 픽셀 | 최종 ([원본](../../art/source/substage_intro/index.md)) |

- 위 틀과 제목은 정지 이미지다. 시작 화면 그림처럼 원본 해상도를 화면에서 축소하므로 Trilinear·밉맵으로 가져온다. 진행 바는 별도 UI로 겹쳐 표시하며 처치 수 숫자는 표시하지 않는다. 틀 리소스가 없거나 종남이 아닌 스테이지는 기존 도형 틀을 쓰고, 제목 이미지가 없으면 기존 영문 제목을 쓴다.

| ID | 리소스 경로 | 용도 | 크기(px) | 9-slice 테두리 | 상태 |
| --- | --- | --- | --- | --- | --- |
| ui_button | `Art/UI/button` | 강화 버튼 (누를 수 있음) | 24×24 | 8 | 계획 |
| ui_button_disabled | `Art/UI/button_disabled` | 강화 버튼 (누를 수 없음) | 24×24 | 8 | 계획 |
| ui_button_pressed | `Art/UI/button_pressed` | 강화 버튼 (누르는 중) | 24×24 | 8 | 계획 |
| ui_panel | `Art/UI/panel` | 아래 강화 영역 바탕, 오프라인 보상 창 | 24×24 | 8 | 계획 |
| ui_bar_frame | `Art/UI/bar_frame` | 진행 바·보스 체력 바 테두리 | 16×8 | 3 | 계획 |
| ui_bar_fill | `Art/UI/bar_fill` | 바 채움 (색은 코드에서 곱한다) | 4×4 | 1 | 계획 |
| ui_hpbar_disciple | `Art/UI/health_frame_disciple` | 제자 머리 위 체력 바 금색 프레임과 매화, 투명 가운데 | 2172×724 원본 | 없음 | 임시 |
| ui_hpbar_enemy | `Art/UI/health_frame_enemy` | 일반 몬스터 머리 위 체력 바 은색 프레임, 투명 가운데 | 2172×724 원본 | 없음 | 임시 |

## 시작 화면

2026-11-17 사용자가 고른 시안을 적용했다. 원본과 만드는 법은 [시작 화면 원본 색인](../../art/source/start_menu/index.md). 화면 배치는 [시작 화면 사양](../spec/start_menu.md) "화면", 불러오기는 `StartScreenArt`(`Assets/Scripts/Game/StartMenu/`)가 한다. 배경·버튼 네 장 중 하나라도 없으면 시작 화면은 단색 바탕과 영문 글자(대체 표시)로 돌아간다.

| ID | 리소스 경로 | 용도 | 크기(px) | 화면 크기 (640×360 기준) | 상태 |
| --- | --- | --- | --- | --- | --- |
| start_title_background | `Art/StartMenu/title_background` | 시작 화면 배경. 제목 `매화검 키우기`, 제자, 화산 산길 포함 | 1672×941 | 화면을 덮음 (16:9에서 640×360, 비율 유지, 넘치면 가운데 기준으로 잘림) | 최종 (선택 원본 그대로) |
| start_button_start | `Art/StartMenu/button_start` | 시작 버튼, 저장 없음 (`게임 시작` 글자 포함) | 1436×262 | 230×42 | 최종 (선택 시안에서 잘라냄) |
| start_button_continue | `Art/StartMenu/button_continue` | 시작 버튼, 저장 있음 (`이어하기` 글자 포함) | 1436×262 | 230×42 | 임시 (글자를 붓글씨가 아닌 글꼴로 넣음) |
| start_button_settings | `Art/StartMenu/button_settings` | 시작 화면 설정 버튼 (`설정` 글자 포함) | 1436×262 | 230×42 | 최종 (선택 시안에서 잘라냄) |
| start_petal_open | `Art/StartMenu/petal_open` | 흩날리는 매화 꽃잎, 펼쳐진 모양 (`Variant` 0) | 64×62 | 긴 변 7~14 | 최종 (선택 원본을 줄임) |
| start_petal_curled | `Art/StartMenu/petal_curled` | 흩날리는 매화 꽃잎, 휘어진 모양 (`Variant` 1) | 64×29 | 긴 변 7~14 | 최종 (선택 원본을 줄임) |
| start_petal_folded | `Art/StartMenu/petal_folded` | 흩날리는 매화 꽃잎, 접힌 모양 (`Variant` 2) | 64×59 | 긴 변 7~14 | 최종 (선택 원본을 줄임) |

- 정지 이미지라 프레임·재생 속도·방향은 해당 없다. 기준점은 가운데(UI `Image`).
- **꽃잎** (2026-11-17 적용): 사용자가 고른 원본 세 장(1536×1024·1774×887·1536×1024 캔버스, 투명 배경)을 꽃잎 테두리로 자르고 긴 변 64px(여백 2px 포함)로 줄였다 (`prepare_petals.py`, BOX). 원본 꽃잎의 픽셀 한 칸이 약 8px이라 줄인 뒤 거의 1:1이다. 시작 화면에서는 긴 변 7~14 UI 단위로, 회전·뒤집기와 함께 움직인다 ([시작 화면 사양](../spec/start_menu.md) "매화 꽃잎 흩날림"). 1스테이지 전투 화면에서도 같은 그림을 긴 변 4.2~8.4 기준 픽셀로 작게 표시한다. 꽃잎이 하나도 없으면 꽃잎 없이 그린다. 처치 등 전투 이펙트 `fx_plum`(아래 "이펙트")은 따로 계획 상태다.
- **픽셀 크기 규칙의 예외.** 배경·버튼 네 장은 AI 시안의 원본 해상도를 그대로(확대·축소 없이) 넣었다. 화면에서는 약 0.16~0.38배(배경)로 정수배가 아니게 줄어 보이므로, 가져오기 설정을 Point 대신 Trilinear와 밉맵으로 둔다 ([art.md](art.md) "가져오기 설정"). 다른 픽셀아트 에셋과 픽셀 크기가 같지 않다.
- 버튼은 배경을 투명하게 했고, 판 아래 그림자는 반투명 검정으로 남겼다. 세 장 모두 1436×262로 맞췄다 (원본 판 1434×262·1436×261에 투명 여백 1px 안팎).
- `이어하기`는 시안에 없어서, 설정 판의 글자를 지운 빈 판에 Noto Serif KR Black(SIL OFL 1.1, 상업 사용 가능)으로 글자를 그려 만들었다. 시안의 붓글씨와 모양이 다르므로 붓글씨 `이어하기`가 생기면 같은 크기로 바꾼다.
- **한글 글꼴**: 프로젝트 안에는 TMP 기본 글꼴(LiberationSans, 한글 없음)뿐이다. 그래서 시작 화면의 한글은 모두 그림이고, 설정 창 등 나머지 글자는 영문이다.

## 아이콘

| ID | 리소스 경로 | 용도 | 크기(px) | 상태 |
| --- | --- | --- | --- | --- |
| icon_gold | `Art/Icons/gold` | 금화 (보유 금화, 비용, 오프라인 보상) | 16×16 | 계획 |
| icon_atk | `Art/Icons/atk` | 공격력 강화 | 16×16 | 계획 |
| icon_hp | `Art/Icons/hp` | 체력 강화 | 16×16 | 계획 |
| icon_spd | `Art/Icons/spd` | 공격 속도 강화 | 16×16 | 계획 |

- 지금 강화 사양은 아이콘을 범위 밖(4단계)으로 둔다. 만들어 두기만 하고, 쓰는 것은 사양을 고칠 때 정한다.

## 이펙트

| ID | 리소스 경로 | 용도 | 캔버스(px) | 프레임 | fps | 기준점 | 상태 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| fx_slash | `Art/Effects/slash` | 제자 검기 (공격 때 앞쪽) | 64×64 | 5 | 16 | 가운데 | 계획 |
| fx_hit | `Art/Effects/hit` | 맞은 자리 번쩍임 | 32×32 | 4 | 16 | 가운데 | 계획 |
| fx_plum | `Art/Effects/plum` | 매화 꽃잎 흩날림 (처치·보스 등장 등) | 64×64 | 8 | 12 | 가운데 | 계획 |

- 시작 화면의 매화 꽃잎 세 장은 위 "시작 화면"에 적용했다. `fx_plum`과는 별개다.
- 4스테이지 재: `Art/Effects/Stage4Ash/ash_01`~`ash_05`, 1536×1024 투명 PNG 다섯 장. [생성 원본](../../art/source/stage4_ash_particles/)을 그대로 사용하고 화면 코드에서 조각 주변만 스프라이트로 지정한다. Point·무압축·PPU 32. 4스테이지 배경 위에 표시하며 화면 크기와 노출 조건은 [전투 사양](../spec/combat.md)의 "4스테이지 재"를 따른다.

## 그림이 아닌 것 (메모)

- **한글 글꼴**: 사양들이 "한글 글꼴이 준비되면 글자만 바꾼다"고 미뤄 두었다. 2026-11-17 확인: 프로젝트에는 아직 한글 TMP 글꼴이 없다 (시작 화면은 그림 글자로 해결). 픽셀 글꼴(비트맵, 12px 또는 16px)과 TMP 글꼴 에셋이 필요하다. 글꼴은 생성하지 않고 상업적으로 쓸 수 있는 무료 글꼴을 고른다.
- **소리**: 준비된 배경음악·효과음과 연결 상태는 [오디오 에셋 목록](audio.md)을 따른다.
