# 설정 UI 원본 색인

2026-11-18 사용자가 선택한 설정 버튼과 설정 창의 이미지 시안이다. 정지 이미지이며 방향과 재생 속도는 해당 없다.

| 자료 | 파일 | 크기(px) | 프레임 | 상태 |
| --- | --- | --- | --- | --- |
| 설정 버튼 시안 | [settings_button_concept.png](settings_button_concept.png) | 2172×724 | 1 | 선택됨, 게임 적용 |
| 설정 창 시안 | [settings_window_concept.png](settings_window_concept.png) | 1536×1024 | 1 | 선택됨, 분리 요소로 게임 적용 |

- Codex 내장 이미지 생성으로 제작했다. [시작 화면 일러스트](../start_menu/title_concept.png), [시작 화면 버튼 시안](../start_menu/buttons_concept.png)과 기존 설정 화면을 참고했다.
- 버튼 생성 지시 요약: 짙은 자주색·검정 바탕, 청동색 테두리, 매화 장식과 `설정` 글자가 있는 무협풍 버튼.
- 설정 창 생성 지시 요약: 매화 장식과 짙은 옻칠풍 테두리, `설정`, `배경 음악`, `효과음`, `시작 화면`, `게임 종료`, `닫기`가 있는 화면 배치.
- 설정 창의 80과 슬라이더·버튼은 시안의 일부다. 실제 조작에는 아래의 독립된 요소를 쓴다. PixelLab은 사용하지 않았다.

## 분리된 조작 요소 (선택 및 적용)

2026-11-18 확인한 분리 시안 원본은 `window_source.png`, `slider_track_source.png`, `slider_handle_source.png`, `button_title_source.png`, `button_quit_source.png`, `button_close_source.png`, `label_bgm_source.png`, `label_sfx_source.png`다. 정지 이미지, 각 1프레임이며 방향·재생 속도는 해당 없다. 게임용 그림은 `Assets/Resources/Art/UI/Settings/`에 별도로 둔다. 여백을 잘라 배치하며 원본은 보존한다.
