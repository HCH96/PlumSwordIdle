# 사양 색인

기능별 사양 문서 목록이다. 기획 스킬(`plan-feature`)이 사양을 쓸 때 한 줄씩 추가한다. 각 사양은 동작, 수치, 연결 지점, 화면, 저장, 범위 밖, 완료 조건 초안을 담는다.

- 다른 기능이 부르는 공개 타입과 메서드(연결 지점)의 원본은 각 사양 문서의 "연결 지점" 절이다.
- 작업에 필요한 사양만 읽는다. 작업 문서의 "참고 문서"에 적힌 사양은 반드시 읽는다.
- 사양 문서에는 지금의 규칙과 아직 구현하지 않은 변경 기록만 둔다. 결정 기록, 구현된 완료 조건, 구현된 변경 기록은 [records/](records/index.md)의 같은 이름 파일에 있다. 구현하는 에이전트는 읽지 않아도 된다. 아직 기록 파일이 없는 사양은 결정 기록이 사양 안에 있고, 기획 스킬이 다음에 고칠 때 옮긴다.

| 문서 | 기능 | 연결 지점 타입 |
| --- | --- | --- |
| [currency.md](currency.md) | 금화. 지급·소비 동작, 수치, 보유 금화 표시, 저장 값 | `PlumSword.Core.Currency.Wallet`, `PlumSword.Core.Currency.GoldText` |
| [stat_upgrade.md](stat_upgrade.md) | 능력치 강화. 공격력·체력·내공의 단계·비용·구매, 경지 보너스가 반영된 최종 능력치, 강화 버튼 화면, 저장 값 | `PlumSword.Core.StatUpgrade.StatUpgrades`, `PlumSword.Core.StatUpgrade.StatType`, `PlumSword.Core.StatUpgrade.StatUpgradeText` |
| [combat.md](combat.md) | 전투. 제자 전진, 몬스터 접근, 공격·피해, 자동 발동 스킬 이십사수매화검법, 체력 강화·돌파 때 현재 체력, 쓰러짐 알림, 몬스터 능력치, 필드 화면(제자 스프라이트와 애니메이션, 대체 표시·체력 바·카메라·임시 배경) | `PlumSword.Core.Combat.Battle`, `PlumSword.Core.Combat.Enemy`, `PlumSword.Core.Combat.EnemyKind`, `PlumSword.Core.Combat.EnemyStats`, `PlumSword.Core.Combat.CombatLayout`, `PlumSword.Core.Combat.DiscipleAnimator`, `PlumSword.Core.Combat.DiscipleMotion` |
| [stage.md](stage.md) | 스테이지. 다섯 테마·회차, 일반 6구간과 보스 구간(5스테이지는 최종 보스만, FINAL), 구간별 2~3명 배치·번호 소개·진행 UI, 보상·전환·재도전·저장 | `PlumSword.Core.Stage.StageProgress`, `PlumSword.Core.Stage.StagePhase`, `PlumSword.Core.Stage.StageRewards`, `PlumSword.Core.Stage.StageLayout`, `PlumSword.Core.Stage.StageText`, `PlumSword.Core.Stage.StageLabelArt` |
| [save.md](save.md) | 저장과 불러오기. 저장 값과 파일 형식, 자동 저장 시점, 저장 실패 재시도, 손상·판 번호 처리, 손상 오류 창, 저장 데이터 유무 | `PlumSword.Core.Save.SaveData`, `PlumSword.Core.Save.SaveCodec`, `PlumSword.Core.Save.SaveLoadResult`, `PlumSword.Core.Save.AutoSave`, `PlumSword.Core.Save.ISaveStore` |
| [offline_reward.md](offline_reward.md) | 오프라인 보상. 경과 시간 금화 계산(10초·8시간 임시), 보상 창과 받기·중복 지급 방지, 백그라운드 복귀 시 다시 시작 | `PlumSword.Core.OfflineReward.OfflineRewardCalculator`, `PlumSword.Core.OfflineReward.OfflineRewardResult`, `PlumSword.Core.OfflineReward.OfflineRewardClaim`, `PlumSword.Core.OfflineReward.OfflineRewardText` |
| [start_menu.md](start_menu.md) | 시작 화면과 기본 메뉴. 게임 시작·이어하기, 설정 창과 음량(PlayerPrefs), 시작 화면 복귀·게임 종료와 저장 실패, 뒤로 가기 키, GameRoot 시작 순서, 시작 화면 그림과 매화 꽃잎 흩날림 | `PlumSword.Core.StartMenu.StartMenuRules`, `PlumSword.Core.StartMenu.VolumeSettings`, `PlumSword.Core.StartMenu.LeaveGame`, `PlumSword.Core.StartMenu.StartMenuText`, `PlumSword.Core.StartMenu.PetalField`, `PlumSword.Core.StartMenu.Petal` |
| [realm.md](realm.md) | 경지 돌파. 5단계 경지·몬스터 처치 자동 돌파·공격력/최대 체력 보너스 %, 한글 경지 표시, 경지별 매화 오오라(빛·기운·꽃잎)와 단계별 돌파 연출(어둡게·빛·블룸·고리·기둥·충격파·번쩍임·흔들림), 저장 값 | `PlumSword.Core.Realm.RealmProgress`, `PlumSword.Core.Realm.RealmLevel`, `PlumSword.Core.Realm.RealmRules`, `PlumSword.Core.Realm.RealmText`, `PlumSword.Core.Realm.RealmAura`, `PlumSword.Core.Realm.BreakthroughEffect`, `PlumSword.Core.Realm.BreakthroughStyle`, `PlumSword.Core.Realm.EffectPoint` |
| [audio.md](audio.md) | 소리. 상태별 배경음악과 교차 페이드, 효과음 14종의 연결 시점·합치기·간격·동시 재생, 음량 계산, 일시정지·포커스, 파일 없음 처리 | `PlumSword.Core.Audio.AudioCatalog`, `PlumSword.Core.Audio.MusicRules`, `PlumSword.Core.Audio.MusicFader`, `PlumSword.Core.Audio.AudioMix`, `PlumSword.Core.Audio.SfxQueue`, `PlumSword.Core.Audio.SfxTriggers`, `PlumSword.Core.Audio.UiSoundWatch`, `PlumSword.Core.Audio.SfxVoicePool`, `PlumSword.Game.Audio.AudioView` |
| [balance.md](balance.md) | 수치 표. 강화 비용·레벨당 상승·시작 능력치, 스킬, 몬스터 능력치(구간별 성장), 처치 보상, 천마 고정값, 경지 수치를 한곳에 모으고 시뮬레이터가 바꿔 쓰게 함 | `PlumSword.Core.Balance.BalanceTable`, `PlumSword.Core.Balance.GrowthCurve` |
| [auto_upgrade.md](auto_upgrade.md) | 자동 강화. 가장 싼 강화부터 자동 구매, 켜고 끄는 버튼, 저장 값 | `PlumSword.Core.StatUpgrade.AutoUpgrade`, `PlumSword.Core.StatUpgrade.AutoUpgradeText` |
| [regression.md](regression.md) | 회귀와 엔딩. 천마 패배 시 1-1 회귀, 천마 첫 처치 시 엔딩 화면과 저장 삭제, 총 플레이 시간·회귀 횟수 저장 | `PlumSword.Core.Stage.PlayTime`, `PlumSword.Core.Stage.EndingText`, `PlumSword.Core.Stage.StageProgress` (회귀 규칙) |
