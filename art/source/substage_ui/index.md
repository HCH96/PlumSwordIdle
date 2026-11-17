# 작은 스테이지 진행 UI 원본

사용자가 승인한 [시안](concept.png)을 바탕으로 Codex 내장 이미지 생성으로 분리했다. PixelLab은 사용하지 않았다. 모두 1프레임 정지 이미지이며 방향과 fps는 해당 없다.

| 자료 | 파일 | 상태 |
| --- | --- | --- |
| 번호를 올리는 제목판 | [header_source.png](header_source.png) | 게임 적용 |
| 지난 표식 | [completed_source.png](completed_source.png) | 게임 적용 |
| 금빛 현재 표식 | [current_source.png](current_source.png) | 게임 적용 |
| 남은 표식 | [empty_source.png](empty_source.png) | 게임 적용 |
| 보스 표식 | [boss_source.png](boss_source.png) | 게임 적용 |

게임용은 `Assets/Resources/Art/UI/Substage/`에 있다. 일반 구간 6개와 보스 구간에 실제 진행을 연결했으며 현재 위치 표식의 금빛은 코드로 박동한다.
