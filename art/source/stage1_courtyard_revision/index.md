# 1스테이지 연무장 개선 시안

- 상태: 사용자가 플레이 확인 후 v3를 최종 배경으로 확정함. 기존 배경은 `references/original_preview.png`에 보관한다.
- `concept_v1.png`: 내장 imagegen으로 생성한 원본. 기존 시안의 산·매화·보랏빛 분위기를 유지하면서 연무 도구와 넓은 바닥을 강조했다.
- `concept_v2.png`: v1을 바탕으로 반전 반복의 경계를 단순하게 만든 후속 시안. `mirror_check_v2.png`는 반전·원본·반전 순서로 잇는 검토 그림이다. 연결부의 거울 무늬는 줄었으나 정자와 폭포가 주기적으로 반복된다.
- `concept_v3.png`: 사용자의 피드백에 따라 전각·정자·매화나무·무구·목인장·석등을 풍성하게 되살린 시안. `mirror_check_v3.png`는 좌우 반전 반복, `combat_mockup_v3.png`는 대기 스프라이트와 체력 바를 합성한 검토 그림이다. 실제 UI와 게임 실행은 포함하지 않는다.
- `combat_mockup.png`: 게임용 대기 시트의 첫 프레임을 배경에 합성한 참고 그림. 640×360 화면, 배경 높이 18유닛·중심 y=2.9, 발 y=-1.5, 캐릭터 1.5배를 근사하고 2배로 표시한다. UI는 합성하지 않았으며 실제 게임 화면이 아니다.
- `mirror_check.png`: 원본과 좌우 반전본을 번갈아 붙인 검토 그림. 이음부 색 단절은 적지만 산과 매화나무가 대칭으로 반복된다.
- `make_review.py`: 위 두 검토 그림을 만드는 스크립트. Pillow 필요.
- `references/`: 생성과 비교에 사용한 기존 배경, 제자와 종남 일반 상대 기본 그림 및 실제 대기 시트의 복사본. 기본 그림은 생성 참고, 대기 시트는 화면 합성에 사용했다.

## 생성 문구

내장 imagegen 편집 모드. 기존 stage1_preview를 장면 참고로, 제자와 종남 남녀 기본 스프라이트를 그림체 참고로 제공했다.

> Create an improved pixel-art concept for a 2D idle game combat background. Preserve the existing dusky lavender mountain mist, plum blossoms, traditional martial-arts architecture and stone courtyard mood, but simplify busy details. Huashan sect training courtyard, restrained training racks and practice posts, tiled eaves, distant mountains and plum trees. Slight elevated 3/4 view compatible with the characters. Wide panorama, continuous broad stone fighting ground in the lower third; calm moderately light low-contrast muted fighting band for character and red/green health-bar readability. Reserve the lower section for UI. Layered atmospheric depth, limited pink accents, consistent game-sprite pixel density. Keep edges quiet for alternating horizontal-mirror tiling and avoid conspicuous symmetric repetition. Background only, no characters, UI, text or watermark.

v2는 내장 imagegen에 v1을 편집 대상으로 제공하고 다음을 요청했다: 양쪽 가장자리에서 산·하늘·난간·바닥이 반전 연결에 부드럽게 이어지도록 가장자리를 단순화하고, 경계에 눈에 띄는 물체와 대각선 돌 틈을 없앤다. 랜드마크는 작게 배치하며 기존의 차분한 보랏빛 연무장과 3/4 픽셀아트 시점을 유지한다.

v3는 내장 imagegen에 기존 게임 배경과 v2를 참고로 제공했다. 반복되는 오브젝트는 허용하고, 원래 분위기의 전각과 정자, 핀 매화나무, 창과 검을 둔 무구대, 목인장, 석등을 되살리되 캐릭터가 서는 바닥과 체력 바가 보이도록 확보해 달라고 요청했다. 양쪽 끝은 좌우 반전 연결을 위해 잘린 오브젝트 없이 하늘·난간·바닥을 이어 달라고 했다.

## 검토와 남은 일

- 실제 대기 프레임 합성에서 캐릭터의 발이 바닥에 놓이고 어두운 제자와 밝은 종남 의상이 구분된다. 체력 바는 단순 사각형으로 근사했다.
- 좌우 반전 이음새에 뚜렷한 색 단절은 적지만 산과 나무의 거울 대칭 및 돌바닥의 V자 무늬가 보인다. 이동 중 반복은 Unity에서 아직 검증하지 않았다.
- 원본은 생성 시안으로 픽셀 격자의 균일성이나 게임용 크기를 확정하지 않았다. 실제 UI 가림, 화면 전체 구간의 가독성, 픽셀 밀도와 반복 패턴은 선택 후 게임 적용 전에 확인해야 한다.
