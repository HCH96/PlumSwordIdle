# 사양 색인

기능별 사양 문서 목록이다. 기획 스킬(`plan-feature`)이 사양을 쓸 때 한 줄씩 추가한다. 각 사양은 동작, 수치, 연결 지점, 화면, 저장, 범위 밖, 완료 조건 초안을 담는다.

- 다른 기능이 부르는 공개 타입과 메서드(연결 지점)의 원본은 각 사양 문서의 "연결 지점" 절이다.
- 작업에 필요한 사양만 읽는다. 작업 문서의 "참고 문서"에 적힌 사양은 반드시 읽는다.
- 사양 문서에는 지금의 규칙과 아직 구현하지 않은 변경 기록만 둔다. 결정 기록, 구현된 완료 조건, 구현된 변경 기록은 [records/](records/index.md)의 같은 이름 파일에 있다. 구현하는 에이전트는 읽지 않아도 된다. 아직 기록 파일이 없는 사양은 결정 기록이 사양 안에 있고, 기획 스킬이 다음에 고칠 때 옮긴다.

| 문서 | 기능 | 연결 지점 타입 |
| --- | --- | --- |
| [currency.md](currency.md) | 금화. 지급·소비 동작, 수치, 보유 금화 표시, 저장 값 | `PlumSword.Core.Currency.Wallet`, `PlumSword.Core.Currency.GoldText` |
| [stat_upgrade.md](stat_upgrade.md) | 능력치 강화. 공격력·체력·공격 속도의 단계·비용·구매, 최종 능력치 계산, 강화 버튼 화면, 저장 값 | `PlumSword.Core.StatUpgrade.StatUpgrades`, `PlumSword.Core.StatUpgrade.StatType`, `PlumSword.Core.StatUpgrade.StatUpgradeText` |
| [combat.md](combat.md) | 전투. 제자 전진, 몬스터 접근, 공격·피해, 쓰러짐 알림, 몬스터 능력치, 필드 화면(제자 스프라이트와 애니메이션, 대체 표시·체력 바·카메라·임시 배경) | `PlumSword.Core.Combat.Battle`, `PlumSword.Core.Combat.Enemy`, `PlumSword.Core.Combat.EnemyKind`, `PlumSword.Core.Combat.EnemyStats`, `PlumSword.Core.Combat.CombatLayout`, `PlumSword.Core.Combat.DiscipleAnimator`, `PlumSword.Core.Combat.DiscipleMotion` |
| [stage.md](stage.md) | 스테이지. 다섯 테마와 회차 순환, 몬스터 배치, 보스, 성공·실패와 재도전, 처치 금화, 페이드 전환, 스테이지 소개, 화면과 저장 값 | `PlumSword.Core.Stage.StageProgress`, `PlumSword.Core.Stage.StagePhase`, `PlumSword.Core.Stage.StageRewards`, `PlumSword.Core.Stage.StageLayout`, `PlumSword.Core.Stage.StageText` |
| [save.md](save.md) | 저장과 불러오기. 저장 값과 파일 형식, 자동 저장 시점, 저장 실패 재시도, 손상·판 번호 처리, 손상 오류 창, 저장 데이터 유무 | `PlumSword.Core.Save.SaveData`, `PlumSword.Core.Save.SaveCodec`, `PlumSword.Core.Save.SaveLoadResult`, `PlumSword.Core.Save.AutoSave`, `PlumSword.Core.Save.ISaveStore` |
| [offline_reward.md](offline_reward.md) | 오프라인 보상. 경과 시간 금화 계산(10초·8시간 임시), 보상 창과 받기·중복 지급 방지, 백그라운드 복귀 시 다시 시작 | `PlumSword.Core.OfflineReward.OfflineRewardCalculator`, `PlumSword.Core.OfflineReward.OfflineRewardResult`, `PlumSword.Core.OfflineReward.OfflineRewardClaim`, `PlumSword.Core.OfflineReward.OfflineRewardText` |
| [start_menu.md](start_menu.md) | 시작 화면과 기본 메뉴. 게임 시작·이어하기, 설정 창과 음량(PlayerPrefs), 시작 화면 복귀·게임 종료와 저장 실패, 뒤로 가기 키, GameRoot 시작 순서 | `PlumSword.Core.StartMenu.StartMenuRules`, `PlumSword.Core.StartMenu.VolumeSettings`, `PlumSword.Core.StartMenu.LeaveGame`, `PlumSword.Core.StartMenu.StartMenuText` |
