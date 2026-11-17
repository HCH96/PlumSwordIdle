# 사양 색인

기능별 사양 문서 목록이다. 기획 스킬(`plan-feature`)이 사양을 쓸 때 한 줄씩 추가한다. 각 사양은 동작, 수치, 연결 지점, 화면, 저장, 범위 밖, 완료 조건 초안을 담는다.

- 다른 기능이 부르는 공개 타입과 메서드(연결 지점)의 원본은 각 사양 문서의 "연결 지점" 절이다.
- 작업에 필요한 사양만 읽는다. 작업 문서의 "참고 문서"에 적힌 사양은 반드시 읽는다.

| 문서 | 기능 | 연결 지점 타입 |
| --- | --- | --- |
| [currency.md](currency.md) | 금화. 지급·소비 동작, 수치, 보유 금화 표시, 저장 값 | `PlumSword.Core.Currency.Wallet`, `PlumSword.Core.Currency.GoldText` |
| [stat_upgrade.md](stat_upgrade.md) | 능력치 강화. 공격력·체력·공격 속도의 단계·비용·구매, 최종 능력치 계산, 강화 버튼 화면, 저장 값 | `PlumSword.Core.StatUpgrade.StatUpgrades`, `PlumSword.Core.StatUpgrade.StatType`, `PlumSword.Core.StatUpgrade.StatUpgradeText` |
| [combat.md](combat.md) | 전투. 제자 전진, 몬스터 접근, 공격·피해, 쓰러짐 알림, 몬스터 능력치, 필드 화면(대체 표시·체력 바·카메라·임시 배경) | `PlumSword.Core.Combat.Battle`, `PlumSword.Core.Combat.Enemy`, `PlumSword.Core.Combat.EnemyKind`, `PlumSword.Core.Combat.EnemyStats`, `PlumSword.Core.Combat.CombatLayout` |
| [stage.md](stage.md) | 스테이지. 다섯 테마와 회차 순환, 몬스터 배치, 보스, 성공·실패와 재도전, 처치 금화, 페이드 전환, 스테이지 소개, 화면과 저장 값 | `PlumSword.Core.Stage.StageProgress`, `PlumSword.Core.Stage.StagePhase`, `PlumSword.Core.Stage.StageRewards`, `PlumSword.Core.Stage.StageLayout`, `PlumSword.Core.Stage.StageText` |
