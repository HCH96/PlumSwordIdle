# 저장과 불러오기 기록

[save.md](../save.md) 사양의 이력이다. 구현에 필요한 규칙은 사양 문서에 모두 있으므로 **구현하는 에이전트는 읽지 않아도 된다.** 기획 스킬(`plan-feature`)이 이미 정한 것을 다시 묻지 않으려고 읽는다.

## 결정 기록

| 질문 | 답 | 방식 |
| --- | --- | --- |
| 마지막 저장 시각을 저장 값에 넣는가 | 넣는다. 저장할 때마다 Game 이 넘긴 UTC `DateTime.Ticks`(`long`). 쓰는 규칙은 오프라인 보상이 정한다 | 추천 수락, 2026-11-17 |
| 설정·음량의 자리를 진행 저장 파일에 열어 두는가 | 열지 않는다. 진행과 따로 저장하며 방법은 시작 화면 기획에서 정한다 | 추천 수락, 2026-11-17 |
| 시작 화면이 없는데 손상 알림과 `새로 시작`은 어디서 | 이번에 간단한 손상 오류 창(`NEW GAME` 버튼)을 만든다. 시작 화면 기획 때 옮길 수 있다 | 추천 수락, 2026-11-17 |
| 변화가 생길 때 언제 파일을 쓰는가 | 알림은 '저장할 것 있음'만 표시하고 그 프레임 끝(`LateUpdate`)에 한 번 쓴다. 끌 때·백그라운드는 바로 쓴다 | 추천 수락, 2026-11-17 |
| 구독·저장 여부·재시도 판단을 어디에 두는가 | Core `AutoSave`. 쓰기는 Core 의 `ISaveStore` 를 Game 이 파일로 구현 | 추천 수락, 2026-11-17 |
| 쓰기 실패 뒤 재시도 | 3초(임시)마다 자동. 그사이 새 변화도 기다림. 실패 동안 `SAVE FAILED` 표시, 성공하면 지움 | 추천 수락, 2026-11-17 |
| 형식은 멀쩡한데 값이 기능 범위 밖일 때 | 손상이 아니다. 값을 그대로 넘기고 각 기능의 생성자가 잘라낸다 | 추천 수락, 2026-11-17 |
| 판 번호가 1 이 아닐 때 | 오류 없이 처음 값으로 새로 시작하고 첫 저장에서 덮어쓴다 (`UnsupportedVersion`) | 사용자 선택, 2026-11-17 (추천은 손상처럼 오류 창) |
| `NEW GAME` 때 손상된 파일 | 따로 남기지 않고 첫 저장에서 덮어쓴다 | 사용자 선택, 2026-11-17 (추천은 `save.corrupt.dat` 로 남김) |
| 손상 창이 떠 있는 동안 끄면 | 아무것도 쓰지 않는다. 다시 켜면 같은 창 | 추천 수락, 2026-11-17 |
| 플레이 모드 테스트·화면 갈무리의 저장 | `Application.isBatchMode` 면 파일을 읽고 쓰지 않는 `MemorySaveStore`. 그 밖(에디터 플레이, 빌드)은 `persistentDataPath` | 추천 수락, 2026-11-17 |
| 파일 형식 | `이름=값` 여덟 줄, 불변 문화권, UTF-8. 모르는 이름은 무시, 같은 이름 두 번은 손상 | 직접 정함 |
| 안전한 쓰기 | `save.tmp` 에 쓰고 `File.Replace`(없으면 `File.Move`) | 직접 정함 (기획의 "안전하게 기록한 뒤 교체") |
| 파일 읽기 중 예외 | 손상과 같다 | 직접 정함 |
| 손상 때 뒤의 전투 | `Battle.SetPaused(true)`, `Begin` 을 미룸. `NEW GAME` 에서 풀고 `Begin` | 직접 정함 |
| 페이드아웃 중에 끄면 | 끝난 스테이지부터 다시 하고 보스 금화는 받은 채. 허용 | 직접 정함 |
| 판 2 에서 판 1 의 공격 속도 칸 | 내공 칸으로 교체하고 경지 칸을 더한다 (아홉 줄). 공격 속도 칸을 남기지 않는다 | 추천 수락, 2026-11-19 |
| 판 1(2단계) 저장을 판 2 로 옮기는가 | 옮기지 않는다. 판 1 은 `UnsupportedVersion` 으로 새로 시작 (3단계는 저장을 지우고 새로 시작한다는 기획, 판 번호 규칙 그대로) | 기획 문서, 2026-11-19 |
| `Capture`·`AutoSave` 에 경지를 넘기는 법 | 필수 인자 `realm`. `null` 이면 `ArgumentNullException` | 추천 수락, 2026-11-19 |
| 돌파 때 저장을 위해 `BrokeThrough` 를 구독하는가 | 구독하지 않는다. 돌파는 금화 소비의 `BalanceChanged` 로 저장된다 | 경지 사양, 2026-11-19 |
| 스킬 재사용 대기·공격 대기 저장 | 저장하지 않는다. 켤 때 준비된 상태 | 기획 문서, 2026-11-19 |
| 판 2 에서 `SaveDataTests.CurrentVersion_IsOne` 의 이름 | 이름은 두고 본문만 2 로 | 추천 수락, 2026-11-19 |
| 판 2 의 줄 순서 | `version, gold, attackLevel, healthLevel, innerPowerLevel, realmLevel, stageNumber, seenIntroStage, savedAtUtcTicks` | 직접 정함, 2026-11-19 |

## 구현된 완료 조건

### 2026-11-19 옮김 — 처음 사양(2026-11-17, 판 1)의 조건 (기록 026~032)

번호는 사양의 원래 번호다. 아래 말은 그때의 사양 기준이다.

테스트에서 "가짜 저장소"는 `ISaveStore` 를 구현한 테스트용 클래스다: 쓴 횟수와 마지막 글자를 세고, 실패하게(`false`) 하거나 예외를 던지게 바꿀 수 있다. "스테이지 객체"는 새 `Battle(upgrades)` 와 `new System.Random(0)` 으로 만든다. 시작 능력치는 `(10, 100, 1.0)` 이다.

3. `Decode(null)` 은 `Status` `Empty`, `Data` `null`, `HasSaveData` `false` 이다.
4. `Decode("")`, `Decode("hello")` 는 둘 다 `Corrupt`, `Data` `null`, `HasSaveData` `false` 이다.
8. 1번 글자의 금화 줄을 `gold=12a`, `gold= 12`, `gold=99999999999999999999` 로 바꾼 글자와 `attackLevel=3000000000` 으로 바꾼 글자는 모두 `Corrupt` 이다.
9. 1번 글자 끝에 `gold=5\n` 을 한 줄 더 붙인 글자(같은 이름 두 번)는 `Corrupt` 이다. 1번 글자 끝에 `=` 가 없는 줄 `oops\n` 을 붙인 글자도 `Corrupt` 이다.
10. 1번 글자의 `\n` 을 모두 `\r\n` 으로 바꾼 글자, 1번 글자 사이에 빈 줄과 `extra=1` 줄을 넣은 글자는 둘 다 `Loaded` 이고 값이 2번과 같다.
15. `new SaveLoadResult(Loaded, null)` 과 `new SaveLoadResult(Empty, 어떤 SaveData)` 는 `ArgumentException`. `new SaveLoadResult(Corrupt, null)` 은 `Status` `Corrupt`, `HasSaveData` `false` 이다.
16. `MemorySaveStore` 는 처음 `Text` 가 `null` 이고, `TryWrite("abc")` 가 `true` 이며 그 뒤 `Text` 가 `"abc"` 이다. `TryWrite(null)` 은 `ArgumentNullException` 이다.
18. 가짜 저장소로 `AutoSave` 를 만들면 쓴 횟수 0, `Enabled` `true`, `IsDirty` `false`, `HasFailed` `false` 이고, 그 상태에서 `Flush(100)` 은 `false`, 쓴 횟수 0 이다.
19. `new Wallet(0)` 으로 만든 `AutoSave` 에서 `wallet.Add(10)` 을 하면 `IsDirty` 가 `true` 이고 쓴 횟수는 아직 0 이다. `Flush(100)` 은 `true`, 쓴 횟수 1, `IsDirty` `false` 이고, 마지막 글자를 `Decode` 하면 `Gold` 10, `SavedAtUtcTicks` 100 이다. 바로 다시 `Flush(200)` 은 `false`, 쓴 횟수 1 이다.
20. `new Wallet(100)` 으로 만든 `AutoSave` 에서 `TryUpgrade(StatType.Attack)` (금화 80, 공격력 단계 1) 뒤 `wallet.Add(5)` 를 하고 `Flush(100)` 을 한 번 부르면 쓴 횟수 1 이고 마지막 글자의 `Gold` 85, `AttackLevel` 1 이다.
21. `new Wallet(0)` 으로 만든 `AutoSave` 에서 `wallet.Add(0)`, 실패하는 `TryUpgrade(StatType.Attack)`, `wallet.TrySpend(5)` 를 해도 `IsDirty` 는 `false` 이다.
22. `startStage: 1`, `seenIntroStage: 0` 인 스테이지 객체로 `AutoSave` 를 만든 뒤 `stage.Begin()` 을 부르면 `IsDirty` 가 `true` 이고, `Flush(100)` 의 마지막 글자를 `Decode` 하면 `StageNumber` 1, `SeenIntroStage` 1 이다.
23. 가짜 저장소를 실패하게 하고 `wallet.Add(10)` → `Flush(100)` 을 하면 `false`, 쓴 횟수(시도) 1, `HasFailed` `true`, `IsDirty` `true`, `FailedChanged(true)` 가 한 번 간다.
24. 23번에 이어 저장소를 성공하게 바꾸고 바로 `Flush(200)` 을 하면 `false` 이고 시도는 1 그대로다. `Tick(2.9)` 뒤 `Flush(250)` 도 `false`, 시도 1 이다. `Tick(0.1)` 뒤 `Flush(300)` 은 `true`, 시도 2, `HasFailed` `false`, `IsDirty` `false`, 마지막 글자의 `SavedAtUtcTicks` 300 이고, `FailedChanged` 는 모두 두 번(`true`, `false`) 간다.
25. 가짜 저장소를 계속 실패하게 하고 `wallet.Add(10)` → `Flush(100)` → `Tick(3.0)` → `Flush(200)` 을 하면 시도 2, `HasFailed` `true` 이고 `FailedChanged` 는 한 번만 간다. 그 뒤 `Tick(2.9)` → `Flush(300)` 은 시도 2 그대로다 (대기를 다시 3초 센다).
26. 가짜 저장소가 예외를 던지게 하고 `wallet.Add(10)` → `Flush(100)` 을 하면 예외 없이 `false`, `HasFailed` `true`, `IsDirty` `true` 이다.
27. 변화가 없는 `AutoSave` 에서 `SaveNow(500)` 은 `true`, 쓴 횟수 1, 마지막 글자의 `SavedAtUtcTicks` 500 이다. 23번처럼 실패해 대기 중일 때 저장소를 성공하게 바꾸고 `SaveNow(600)` 을 부르면 `true`, 바로 쓰고 `HasFailed` `false` 이다.
28. `enabled: false` 로 만든 `AutoSave` 에서 `wallet.Add(10)` 을 하면 `IsDirty` `false` 이고, `Flush(100)` 과 `SaveNow(100)` 은 `false`, 쓴 횟수 0 이다. `Enable()` 뒤 `Enabled` 는 `true` 이고, `wallet.Add(5)` → `Flush(200)` 은 `true`, 쓴 횟수 1, 마지막 글자의 `Gold` 15 이다. `Enable()` 을 한 번 더 불러도 상태가 그대로다.
29. `AutoSave.Tick` 에 -0.1, `double.NaN`, `double.PositiveInfinity` 를 넣으면 각각 `ArgumentOutOfRangeException` 이다. 대기가 없을 때 `Tick(0)`, `Tick(1.0)` 은 아무것도 바꾸지 않는다.
30. `SaveText` 의 네 값은 `"SAVE FAILED"`, `"SAVE DATA IS CORRUPTED"`, `"START A NEW GAME? THE OLD SAVE WILL BE REPLACED."`, `"NEW GAME"` 이다.
31. (플레이 모드) 비어 있는 임시 폴더의 `FileSaveStore.Load()` 는 `Empty` 이다. `TryWrite("abc")` 는 `true` 이고, 그 폴더에 `save.dat` 의 내용이 `abc` 이며 `save.tmp` 는 없다. 그 뒤 `Load()` 는 `Corrupt` 이다. 이어서 1번의 `SaveData` 를 `Encode` 해 `TryWrite` 하면 `Load()` 가 `Loaded` 이고 값이 2번과 같다.
32. (플레이 모드) 임시 폴더에 `save.dat` 을 `"old"` 로 써 두고 같은 폴더에 `save.tmp` 라는 이름의 **폴더**를 만들어 두면, `TryWrite("new")` 는 예외 없이 `false` 이고 `save.dat` 의 내용은 `"old"` 그대로다.
33. (플레이 모드) 배치 모드에서 게임이 켜지면 `GameRoot.LoadResult.Status` 가 `Empty`, `GameRoot.AutoSave.Enabled` 가 `true`, `GameRoot.StageProgress.StageNumber` 가 1, `GameRoot.StageProgress.HasBegun` 이 `true` 이다. 배치 모드가 아니면(에디터의 테스트 러너에서 직접 돌림) 이 테스트는 `Assert.Ignore` 로 건너뛴다.

## 구현된 변경 기록

없음.
