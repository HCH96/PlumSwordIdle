# 천마신교 4스테이지 보스 기본 스프라이트 시안

사용자가 여성 보스의 [일러스트 시안](concept_approved_before_eyepatch.png)을 승인하고 한쪽 눈에 안대를 추가해 달라고 요청했다. 일러스트 파일은 안대 추가 전이다.

- [남서쪽 기본 그림](export/Idle/rotations/south-west.png): 96×96px, 투명 배경. 왼쪽 앞을 보는 전투 방향이다.
- [제자·1~3스테이지 보스와 크기 비교](height_comparison.png): 왼쪽부터 화산 제자, 종남 장로, 개방 보스, 남궁 장로, 천마신교 보스. 원본 픽셀을 4배로 확대했다.
- [PixelLab 원본 ZIP](pixellab_original.zip)과 [8방향 원본](export/Idle/rotations/). 사용자가 기본 그림을 승인했다.
- PixelLab 캐릭터 `f3395d21-8dd6-4a21-9861-54f1378e1b3e`, Pro, 96×96px, 8방향, low top-down. 스타일 참고: 종남 장로 `0db24a81-6d08-4887-8eaf-504bc24d0817`. 승인 시안을 디자인 참고로 사용하고 검은 안대를 추가하도록 지시했다.
- 생성 지시: 긴 흑발과 붉은 묶음, 한쪽 검은 안대와 반대쪽 붉은 눈, 광기 어린 미소, 짙은 도복과 붉은 안감·긴 붉은 천, 은빛 날이 있는 넓은 곡도를 든 여성 무인. 큰 머리의 SD 체형과 분명한 실루엣.
- 남서쪽 그림의 불투명 픽셀 높이(무기 포함)는 69px. 동일 기준으로 제자 53px, 종남 장로 70px, 개방 보스 64px, 남궁 장로 70px이다. 실제 게임 표시 크기와 피벗은 아직 정하지 않았다.

## 애니메이션 시안

PixMiniMax, 남서쪽. 원본은 [ZIP](pixellab_animations.zip)과 [개별 프레임](animations_export/Idle/animations/)에 보관한다. 각 프레임은 104×104px 투명 배경이며 기준 그림을 포함한다. 검토 GIF는 어두운 배경에 최근접 3배 확대, 10fps로 재생하며 쓰러짐 마지막 프레임은 0.9초 유지한다. 게임용 속도와 피벗은 미정이다.

| 동작 | 검토용 | 프레임 | PixelLab 그룹 |
| --- | --- | --- | --- |
| 대기 | [보기](idle_preview.gif) | 17 | `9d948056-cd7f-4777-bafc-fbfa3378e2f8` |
| 이동 | [보기](walk_preview.gif) | 25 | `35814804-22a7-44d0-923b-43c15d9a49bc` |
| 공격 | [보기](attack_preview.gif) | 25 | `b1c35d2a-1202-42c1-87a5-84421f6cef09` |
| 쓰러짐 | [보기](down_preview.gif) | 17 | `92143f45-2ade-4755-86db-dd7b01378d08` |

대기는 제자리 호흡, 이동은 왼쪽 앞을 향한 제자리 걷기, 공격은 곡도를 한 번 베고 돌아오기, 쓰러짐은 오른쪽 뒤로 넘어져 멈추도록 요청했다. 생성된 공격에는 흰 검격이 두 차례 나타난다. 사용자가 대기·이동·쓰러짐을 승인하고 공격 수정을 요청했다.

## 정면 공격 수정 시안

- [반복 미리보기](attack_forward_preview.gif), [개별 프레임](animations_export/Idle/animations/Tianmo_boss_attack_forward/south-west/), [PixelLab 원본 ZIP](pixellab_attack_forward.zip). 기존 공격은 그대로 보관한다.
- PixMiniMax 남서쪽, 104×104px, 기준 그림 포함 25프레임. 그룹 `6bf28871-d4ce-47f6-aa56-98f8647c9f9a`. 검토용 GIF는 어두운 배경에 최근접 3배 확대, 10fps다.
- 생성 지시: 적이 있는 화면 왼쪽을 향한 상태에서 곡도를 들어 가슴 높이로 정면을 한 번 수평으로 베고 원래 자세로 돌아온다. 검과 검격이 아래나 화면 앞이 아닌 왼쪽을 향하고 안대·붉은 눈·복장을 유지한다.
- 사용자가 수정 공격을 승인했다.

## 피색 연속 베기 기술 시안

- [검토용 재생](blood_skill_preview.gif), [색 보정 프레임](blood_skill_frames/), [프레임 전체 보기](blood_skill_contact.png). 프레임은 104×104px, 37장(기준 그림 포함), 남서쪽이다. 미리보기는 어두운 배경에 최근접 3배, 프레임당 65ms로 반복한다. 실제 게임 재생 속도와 기술 피해·발동 조건은 미정이다.
- PixMiniMax 그룹 `82b458a3-d5e2-4ef7-baf1-3a7e5664a0d8`. [생성 원본 ZIP](pixellab_skill_original.zip)과 [원본 프레임](animations_export/Idle/animations/Tianmo_boss_blood_multislash_skill/south-west/)을 보관한다.
- 왼쪽 상대를 향해 곡도를 빠르게 여러 번 베고, 휘두르는 방향에 맞춰 짙은 적색의 검격이 생기도록 요청했다. 생성 결과의 밝은 빨강·분홍 검격 픽셀은 검토용 프레임에서 짙은 피색으로 보정했다. 본래 캐릭터의 기준 그림 색은 유지했다.
- 사용자가 기술 시안을 승인했다. 색 보정 프레임을 게임용 시트에 사용하고 4스테이지 보스로 적용한다. 기존 보스처럼 매 세 번째 공격에 기술을 표시하며 근접 피해는 한 번 적용한다(임시).
