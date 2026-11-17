# 제자 에셋 색인

2026-11-17 보관. PixelLab의 **Plum Sword Disciple - SD**와 기존 제자 일러스트 시안이다. 게임에 넣기 위한 원본 보관이며 `Assets/Resources/`에 적용한 상태는 아니다.

다른 에셋에 적용할 그림체와 제작 절차는 [제자 기준 아트 스타일](../../../docs/dev/art_style.md)을 본다.

| 자료 | 파일 | 내용 |
| --- | --- | --- |
| 일러스트 시안 | [illustration_concept.png](illustration_concept.png) | 기존 제자 콘셉트의 보관 사본. 원래 파일은 [protagonist-disciple-v2.png](../../concepts/characters/protagonist-disciple-v2.png) |
| 기본 스프라이트 | [base_south-east.png](base_south-east.png) | PixelLab 원본 1장, 64×64px, 남동쪽 방향 |
| 대기 애니메이션 | [시트](idle_south-east_sheet.png) / [프레임](idle_frames/) / [미리보기](idle_preview.gif) | 사용자가 선택한 24프레임. 각 칸 76×76px, 남동쪽. 미리보기 10fps |
| 달리기 애니메이션 | [시트](run_south-east_sheet.png) / [프레임](run_frames/) / [미리보기](run_preview.gif) | 사용자가 선택한 24프레임. 각 칸 76×80px, 남동쪽. 빠른 미리보기 20fps |
| 공격 스프라이트 시트 | [attack_south-east_sheet.png](attack_south-east_sheet.png) | **선택한 공격**. 기존 PixelLab 원본 17프레임을 순서대로 배치한 가로 한 줄. 각 칸 76×76px, 전체 1292×76px |
| 공격 개별 프레임 | [attack_frames/](attack_frames/) | `frame_000.png`부터 `frame_016.png`까지 PixelLab 원본. 순서대로 재생 |
| PixelLab 원본 묶음 | [pixellab_original_export.zip](pixellab_original_export.zip) | 방향별 기본 그림, 두 공격 실험의 개별 프레임과 메타데이터가 든 원본 내보내기 |
| 내보내기 정보 | [pixellab_metadata.json](pixellab_metadata.json) | 캐릭터 생성 문구, 크기, 방향과 프레임 파일 목록 |
| 공격 미리보기 | [일반](../../experiments/disciple-motion/pixellab-attack-original.gif) / [빠르게](../../experiments/disciple-motion/pixellab-attack-fast.gif) | 기존 미리보기. 속도는 확정값이 아니다 |

## PixelLab 기록

- 캐릭터: `Plum Sword Disciple - SD`, ID `6c24a0f7-bff6-4d33-88cc-a7839592c1dd`.
- 공격: `wide plum sword slash 16f`, 그룹 ID `c183e73d-ffb2-4a3a-b5c8-32175b13c9cb`. 이름과 달리 내보낸 결과는 17프레임이다.
- 2026-11-17에 새 공격 시안들 대신 위 기존 17프레임 공격을 사용자가 선택했다. 새로 생성한 공격 시안들은 선택하지 않았다.
- 대기: `idle natural breathing 24f`, 그룹 ID `fe7528ca-8a5a-48dc-8ed5-43df4ffd40d5`.
- 달리기: `sprint loop pose 24f`, 그룹 ID `8fd88f3e-635a-41fc-90d5-aeaca1b3ebf9`. 이전 질주 시안의 달리는 자세를 시작과 종료 목표로 사용했다.
- 기본 그림과 공격은 모두 남동쪽 방향이다. 캐릭터 원본의 시점은 low top-down이다.
- 재생 속도는 아직 확정하지 않았다. 게임의 공격 주기와 함께 정한다.
- 대기와 달리기의 미리보기 속도도 임시이며 게임에 넣을 때 조절한다. 시트는 원본 프레임을 가로로 순서대로 배열한 것이다.

## 게임에 넣을 때

- [에셋 목록](../../../docs/dev/assets.md)의 제자 계획은 오른쪽을 보는 48×48 캔버스다. 이 원본의 기본 그림은 64×64, 공격 프레임은 76×76이므로 크기·시점·피벗과 프레임 수를 결정한 뒤 적용한다. 원본을 덮어쓰지 않는다.
- 목록의 리소스 경로와 가져오기 설정을 확인하고, 실제 적용한 파일과 상태를 목록에 기록한다.
