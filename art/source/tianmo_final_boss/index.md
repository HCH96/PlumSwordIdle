# 5스테이지 천마신교 최종 보스

5스테이지 최종 보스 자료다. 사용자가 공중 부유·광기 표현의 [전신 시안](concept_approved.png)과 기본 스프라이트, 아래의 넓게 휘어진 검격 기술을 승인했다. 원본을 이 폴더에 보관하고 게임용 시트는 `Assets/Resources/Art/Enemies/`에 준비했다. 전투에는 아직 연결하지 않았다.

## 게임용 파일

| 동작 | 게임용 시트 | 배열 | 칸 기준점 |
| --- | --- | --- | --- |
| 부유 대기 | [tianmo_final_boss_idle.png](../../../Assets/Resources/Art/Enemies/tianmo_final_boss_idle.png) | 104×104px, 가로 25칸 | (52, 16)px |
| 부유 이동 | [tianmo_final_boss_walk.png](../../../Assets/Resources/Art/Enemies/tianmo_final_boss_walk.png) | 104×104px, 가로 25칸 | (52, 16)px |
| 일반 공격 | [tianmo_final_boss_attack.png](../../../Assets/Resources/Art/Enemies/tianmo_final_boss_attack.png) | 104×104px, 가로 25칸 | (52, 16)px |
| 쓰러짐 | [tianmo_final_boss_down.png](../../../Assets/Resources/Art/Enemies/tianmo_final_boss_down.png) | 104×104px, 가로 25칸 | (52, 16)px |
| 승인된 검격 기술 | [tianmo_final_boss_skill.png](../../../Assets/Resources/Art/Enemies/tianmo_final_boss_skill.png) | 260×80px, 5열×5행, 25칸 | (222, 5)px |

기준점은 각 칸의 왼쪽 아래부터 잰 픽셀이다. 일반 동작 시트는 남서쪽 원본 프레임을 원래 크기와 순서대로 가로로 배치했고, 기준점은 첫 프레임의 발 위치를 기준으로 잡은 적용 전 값이다. 기술은 승인된 게임 팩의 `whole-with-sprite.png`를 그대로 사용한다. 기술 칸의 기준점은 게임 팩의 자르기 오프셋 `(-222, -75)`와 칸 높이 80px에서 환산했다. 기술은 캐릭터까지 포함하므로 기본 캐릭터와 겹쳐 그리지 않는다. 각 PNG에 Unity `.meta`가 있으며 PPU 32·Point·무압축·밉맵 끔·최대 4096 설정을 따른다. 부유 높이, 화면 배치, 속도와 피해 시점은 전투 연결 시 확인한다. [에셋 목록](../../../docs/dev/assets.md)의 몬스터와 보스 항목에도 기록했다.

| 자료 | 파일 | 내용 |
| --- | --- | --- |
| 전투 방향 | [남서쪽](export/Idle/rotations/south-west.png) | 96×96px, 제자와 마주 보는 방향 |
| 크기 비교 | [comparison.png](comparison.png) | 제자와 외곽 아래를 맞추고 5배 최근접 확대 |
| 원본 | [ZIP](pixellab_original.zip) / [8방향](export/Idle/rotations/) | PixelLab 내보내기와 원본 그림 |

- PixelLab Pro, 캐릭터 `3a4951b2-dd2b-4803-817a-1ec37549b4d1`, 8방향, low top-down. 디자인은 승인 시안, 화풍은 화산 제자 `6c24a0f7-bff6-4d33-88cc-a7839592c1dd`를 참고했다.
- 생성 요청: 긴 흑발, 창백한 얼굴과 붉은 눈, 광기 어린 미소, 검정·붉은 겉옷과 상아색 도복, 붉은 선이 있는 검은 직검, 공중에 살짝 떠 있는 SD 최종 보스.
- 남서쪽 원본의 무기와 장식을 포함한 불투명 픽셀 높이는 73px이다. 같은 기준의 제자는 53px이다. 비교 그림은 부유 높이나 게임 표시 크기를 확정하지 않는다.

## 애니메이션 시안

PixelLab PixMiniMax, 남서쪽, 각 프레임 104×104px. 미리보기 재생 속도는 검토용이며 게임 적용 속도는 미정이다. [원본 ZIP](pixellab_animations.zip)과 [프레임](animations_export/Idle/animations/)을 보관한다. 기본 그림을 첫 프레임으로 포함한다.

| 동작 | 미리보기 | 프레임 | PixelLab 그룹 |
| --- | --- | --- | --- |
| 부유 대기 | [GIF](idle_preview.gif) | 25 | `9ed554b9-a4eb-4737-aaa0-6ca74f4859ef` |
| 부유 이동 | [GIF](move_preview.gif) | 25 | `400429eb-4b6d-41c6-b0e0-b90bf482cd04` |
| 일반 공격 | [GIF](attack_preview.gif) | 25 | `25d6adbb-4ee0-4b4c-8ddb-167bed40f247` |
| 쓰러짐 | [GIF](down_preview.gif) | 25 | `4aff01fd-bc21-4171-8f81-371df5283446` |
| 보스 기술 | [GIF](skill_preview.gif) | 33 | `7cff6dc3-09ed-435b-9bd8-3e00b8b2fb0f` |

생성 요청: 대기는 제자리에서 가볍게 떠오르며 흔들리고, 이동은 발을 걷지 않고 떠서 전진하는 반복 동작, 공격은 검을 한 번 베고 복귀, 쓰러짐은 오른쪽으로 떨어져 누운 채 유지, 기술은 검에 붉고 검은 기운을 모아 왼쪽으로 검기를 내보내고 복귀한다. 검토용 [주요 프레임 비교](contact.png)도 보관한다. 부유는 게임에서 지면과 그림자 배치까지 함께 확인해야 한다.

## 붉은 기둥 기술 시안

사용자 요청으로 더 크고 화려한 기술을 별도로 제작했다. 기존 일반 공격 동작에 기운을 모으는 빛, 보스 전방으로 순차 발생하는 붉은 기둥 세 개와 교차 검기를 합성했다. [미리보기](crimson_pillars_preview.gif), [레이어별 게임 팩](crimson_pillars_gamepack.zip). PixelLab VFX 효과 `11fccf4b-9757-47ff-817f-650efe361f05`, 312×214px, 25프레임, 10fps, 발 기준점 (271, 188)px. 재생 속도는 검토용이며 사용자 확인 및 게임 적용 전이다.

## 붉은 검격 기술 수정 시안

기둥 대신 검격이 날아가도록 사용자가 요청해 별도로 만들었다. 일반 공격 동작에 기운을 모은 뒤 커다란 붉은 초승달 검격이 화면 왼쪽으로 날아간다. [미리보기](crimson_wave_preview.gif), [레이어별 게임 팩](crimson_wave_gamepack.zip). PixelLab VFX 효과 `d64c9519-dd22-4c62-a09d-8dc4a672a609`, 260×111px, 25프레임, 10fps, 발 기준점 (219, 93)px. 첫 프레임은 발동 전 여유이며 속도는 검토용이다. 사용자 확인 및 게임 적용 전이다.

사용자가 초승달 모양을 바꾸고 검격을 사선으로 기울여 입체감을 더해 달라고 요청했다. 날카로운 사선 형태와 밝은 날·붉은 면을 겹친 [수정 미리보기](diagonal_wave_preview.gif), [게임 팩](diagonal_wave_gamepack.zip)을 만들었다. PixelLab VFX 효과 `69ac0282-2853-4d73-914a-bdcf54626641`, 264×81px, 25프레임, 10fps, 발 기준점 (223, 78)px. 첫 프레임은 발동 전 여유이며 사용자 확인과 게임 적용 전이다.

이후 사용자의 그림을 참고해 보스 쪽으로 양 끝이 향하는 넓은 가로 곡선과 붉은 가장자리, 밝은 중심, 선과 불꽃을 겹친 [승인된 기술 미리보기](sweeping_wave_preview.gif)와 [게임 팩](sweeping_wave_gamepack.zip)을 만들었다. PixelLab VFX 효과 `e147037e-8942-4b04-bbcc-1c5a1eff61a1`, 266×86px, 25프레임, 10fps, 발 기준점 (225, 78)px. 첫 프레임은 발동 전 여유이며 재생 속도는 검토용이다. 사용자가 5스테이지 최종 보스 기술로 승인했으며 게임 적용은 하지 않았다.
