# 오프라인 보상 UI 원본

2026-11-20 사용자가 선택한 시안과 그에 맞춰 만든 분리 원본이다. Codex 내장 이미지 생성으로 제작했다. 모두 정지 이미지 1프레임이며 방향과 재생 속도는 해당 없다.

| 자료 | 파일 | 크기(px) | 용도 |
| --- | --- | --- | --- |
| 화면 시안 | [concept.png](concept.png) | 1672×941 | 화면 구성 참고. 시간과 금액은 예시 |
| 창 바탕 | [window_source.png](window_source.png) | 1536×1024 | 빈 제목판과 본문을 포함한 창 |
| 보상 표시판 | [reward_plaque_source.png](reward_plaque_source.png) | 2172×724 | 금화 아이콘과 금액 뒤에 배치 |
| 받기 버튼 | [claim_button_source.png](claim_button_source.png) | 2172×724 | 받기·재시도 글자를 위에 배치 |

창과 표시판 원본은 바깥이 투명하며 그림 주위에 여백이 있다. 숫자는 포함하지 않는다. 금화 아이콘은 기존 `Assets/Resources/Art/UI/StatUpgrade/coin.png`를 재사용할 수 있다. 경과 시간, 적용 시간과 금액의 숫자는 실시간 글자로 표시한다.

게임용 이미지는 `Assets/Resources/Art/UI/OfflineReward/`에 원본 캔버스 그대로 복사해 사용한다. 여백을 포함한 비율에 맞춰 화면에서 배치하며, 기존 대형 UI처럼 Trilinear와 밉맵으로 축소 표시한다.

## 한글 그림 글자

시안의 붓글씨에 맞춰 Codex 내장 이미지 생성으로 만든 정지 이미지다. `title_label_source.png`(오프라인 보상), `elapsed_label_source.png`(지난 시간), `applied_label_source.png`(보상 적용), `maximum_label_source.png`(최대), `claim_label_source.png`(받기), `retry_label_source.png`(재시도), `save_failed_label_source.png`(저장 실패 안내)를 게임의 같은 이름 리소스로 사용한다.
