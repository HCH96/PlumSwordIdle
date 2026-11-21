# 개방 보스 원본

2026-11-21 사용자가 [일러스트 시안](illustration_concept.png)을 승인했다. 붉은 머리띠, 검은 상투와 수염, 넓은 체형, 기운 회갈색 도복과 굵은 대나무 방망이를 특징으로 한다. 무기는 별도 장착물이 아니라 캐릭터 스프라이트에 포함한다.

- 일러스트는 사용자가 제공한 참고 이미지, 개방 일반 상대, 종남 장로와 제자·장로의 크기 비교를 참고하여 Codex 내장 이미지 생성으로 제작했다.
- PixelLab 기본 캐릭터 `48af9664-a759-4146-a0e0-076ea1c0e80c`: Pro, 96×96, 8방향, low top-down. 일러스트를 디자인 참고로, 제자 `6c24a0f7-bff6-4d33-88cc-a7839592c1dd`를 스타일 참고로 사용했다. 전투 방향은 [남서쪽](export/Idle/rotations/south-west.png)이다.
- 남서쪽 기본 그림의 불투명 픽셀 높이는 64px이다. 같은 기준의 제자 53px, 종남 장로 70px과 비교하면 각각 약 1.21배, 0.91배다. 무기를 포함한 외곽 높이이며 게임 표시 크기는 아직 확정하지 않았다.
- [PixelLab 초기 원본 ZIP](pixellab_original.zip)과 [풀어 놓은 원본](export/)을 보관한다.

| 동작 | 재생 미리보기 | 프레임 | PixelLab 그룹 |
| --- | --- | --- | --- |
| 대기 | [GIF](idle_preview.gif) | 9 | `b3d31a7d-fddf-41cd-97f4-e7a8e8f33919` |
| 이동 | [GIF](walk_preview.gif) | 25 | `0adda1f0-0d29-4688-8779-00263efba625` |
| 일반 공격 | [GIF](attack_preview.gif) | 25 | `0c1d112f-611e-425d-b978-d8419f1e7e0f` |
| 쓰러짐 | [GIF](down_preview.gif) | 17 | `06aa646c-e5e6-4b72-ba42-95c2c5390828` |

대기는 V3, 나머지는 PixMiniMax로 생성했다. 프레임 수는 기준 그림을 포함하며 GIF 재생 속도는 검토용이다. 방망이는 모든 동작에서 캐릭터와 한 프레임에 함께 그려진다.

## 쓰러짐 방향 수정 시안

사용자가 기존 쓰러짐의 반대 방향을 요청했다. [수정 미리보기](down_right_v2_preview.gif)는 머리가 화면 오른쪽, 발이 왼쪽을 향한 채 쓰러져 누워 있다. PixMiniMax, 남서 방향, 기준 그림 포함 17프레임, 그룹 `dc72a18f-44c6-40ef-8375-7c6cbc278318`이다. [수정 원본 ZIP](pixellab_down_revision.zip)에 프레임을 보관한다. 사용자 확인 전이다.

기존 수정본이 뻣뻣하다는 의견을 받아 다시 만들었다. [자연스러운 쓰러짐 시안](down_natural_v5_preview.gif)은 무릎과 허리가 접히며 화면 오른쪽으로 내려앉고 방망이를 몸에 둔 채 기대어 끝난다. V3, 남서 방향, 기준 그림 포함 17프레임, 그룹 `e43dc0e9-dfe3-4a06-852f-b7202ba6dd5b`이다. [원본 ZIP](pixellab_down_natural.zip)에 보관한다. 사용자 확인 전이다.

방망이를 놓치는 동작을 요청받아 [확정 시안](down_drop_v7_preview.gif)을 만들었다. 넘어지는 도중 손을 풀고 방망이가 몸 앞쪽에 떨어져 남는다. PixMiniMax, 남서 방향, 기준 그림 포함 25프레임, 그룹 `a46e223b-ed9b-4fac-a503-462a837d4560`이다. 무기는 캐릭터와 같은 프레임에 그렸으며 [원본 ZIP](pixellab_down_drop.zip)에 보관한다.

## 게임 적용

2026-11-21 사용자가 방망이를 놓치는 쓰러짐과 앞서 확인한 대기·이동·공격을 확정했다. 선택한 개별 프레임은 [원본 폴더](approved_export/Idle/animations/)에, 가로 시트는 [대기](idle_approved_sheet.png), [이동](walk_approved_sheet.png), [공격](attack_approved_sheet.png), [쓰러짐](down_approved_sheet.png)에 보관한다. 같은 시트를 `Assets/Resources/Art/Enemies/gaebang_boss_*.png`로 복사해 2스테이지에 적용했다. 크기·피벗·재생 속도는 [에셋 목록](../../../docs/dev/assets.md)을 따른다.

## 보스 기술 애니메이션

사용자가 방망이를 들어 크게 휘두르고 자세를 회복하는 [미리보기](skill_spin_draft_preview.gif)를 승인했다. PixMiniMax, 남서쪽, 96×96, 기준 그림 포함 33프레임, 그룹 `69a6cfa2-be4e-47e5-99e1-6964d2d1c132`이다. [PixelLab 원본 ZIP](pixellab_skill_spin_draft.zip), [개별 프레임](skill_approved_export/Idle/animations/Gaebang_boss_staff_spin_skill_draft/south-west/)과 [승인 시트](skill_approved_sheet.png)를 보관한다. 게임용 사본은 `Assets/Resources/Art/Enemies/gaebang_boss_skill.png`다. 세 번째 공격마다 재생하고 일반 공격과 같은 피해를 한 번 준다. 주기와 피해량은 임시값이다.
