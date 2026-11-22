# 스테이지 표시 글자 원본

2026-11-22. 작은 스테이지 제목판·번호 패에 올릴 그림 글자와 빈 제목판이다. 게임용 파일은 `Assets/Resources/Art/UI/Substage/Label/`에 두었다 (제목·이름은 투명 여백을 8px 남기고 자름). 화면 연결은 작업 084~086이 한다.

| 자료 | 파일 | 상태 |
| --- | --- | --- |
| 금빛 스테이지 번호 0~9, `-` | [stage_numbers_gold.png](stage_numbers_gold.png) | 사용자가 금빛 선택 (2026-11-22) |
| 아이보리 번호 (비교용) | [stage_numbers_ivory.png](stage_numbers_ivory.png) | 쓰지 않음 |
| 글자를 지운 제목판 | [header_blank.png](header_blank.png) | 사용자 승인 (2026-11-22), 게임용 복사본 `Assets/Resources/Art/UI/Substage/Label/` |
| 배치 시안 | [asset_layout_mockup.png](asset_layout_mockup.png), [stage_number_preview.png](stage_number_preview.png) | 참고 |
| 스테이지 제목 1~5 | `title_1.png` ~ `title_5.png` (매화 아래 비무, 개방 분타, 남궁세가 연무장, 천마신교 외성, 천마 교주전) | 사용자가 Codex로 제작 (2172×724) |
| 보스 이름 1~5 | `boss_name_1.png` ~ `boss_name_5.png` (장로 진무현, 방주 홍무진, 가주 남궁혁, 호법 사도진, 교주 이천상) | 사용자가 Codex로 제작 (2172×724) |
| 합성 시안 | [stage_label_composite.png](stage_label_composite.png), 스크립트 [compose.py](compose.py) | 사용자 승인 (2026-11-22) |

- 번호 시트: 2172×724 피해 숫자(`Assets/Resources/Art/UI/damage_numbers.png`)의 0~9를 그대로 잘라 쓰고, 같은 색·외곽선·그림자로 `-`를 그렸다. 금빛은 아이보리 글자를 위 크림색 → 아래 호박색 세로 그라데이션으로 다시 칠한 것이다. 제작 스크립트: [make_stage_digits.py](make_stage_digits.py).
- 시트는 2132×300px이고 글자 사이 간격은 20px이다. 글자 순서는 0~9, `-`이고 각 글자의 (왼쪽 x, 너비)는 (20,185) (225,152) (397,177) (594,170) (784,181) (985,185) (1190,172) (1382,172) (1574,172) (1766,176) (1962,150)이다. 높이는 0~300 전체이며, 글자가 보이는 줄은 약 17~290이다.
- 빈 제목판: `Assets/Resources/Art/UI/Substage/header.png`(1536×505)에서 위쪽 띠의 "매화 아래 비무" 영역(x 470~1079, y 124~231)을 띠 색으로 채웠다. 다른 픽셀은 원본과 같다.
- 안쪽 패의 세로 가운데: 제목판 y 363(안쪽 칸 265~461), 구간 시작 번호 패 y 395(안쪽 칸 272~519).
