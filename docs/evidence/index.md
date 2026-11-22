# 루프 증거 색인

자동 개발 루프가 통합 브랜치에 합친 루프마다 한 줄씩 쌓인다. **루프 스크립트만 쓴다.** 사람이 main 에 올리기 전에 이 문서부터 본다.

- 줄마다 보고서(`<루프>/report.md`)에 판정 근거, 완료 조건별 테스트, 에이전트가 스스로 정한 가정, 화면 갈무리가 있다.
- 루프 하나는 통합 브랜치의 병합 커밋 하나이고, `loop-merged/<루프>` 태그가 그 커밋을 가리킨다.
- 확인하고 올리기, 되돌리기는 `docs/dev/loop.md` "사람이 확인하고 main 에 올리기" 절을 본다.

| 루프 | 묶음 | 합친 시각 | 작업 | 테스트 (통과/전체) | 리뷰 | 사람이 볼 것 | 보고서 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `20261117-113918` | 테마회차 | 2026-11-17 11:59 | 3건 | EditMode 304/304, PlayMode 2/2 | claude (교차) | 10건 | [report.md](20261117-113918/report.md) |
| `20261117-115917` | 스테이지소개 | 2026-11-17 12:07 | 1건 | EditMode 323/323, PlayMode 2/2 | claude (교차) | 0건 | [report.md](20261117-115917/report.md) |
| `20261117-123053` | 스테이지소개 | 2026-11-17 12:38 | 2건 | EditMode 333/333, PlayMode 2/2 | codex (교차) | 15건 | [report.md](20261117-123053/report.md) |
| `20261117-134243` | 저장 | 2026-11-17 14:23 | 6건 | EditMode 391/391, PlayMode 16/16 | claude (교차) | 9건 | [report.md](20261117-134243/report.md) |
| `20261117-143608` | 저장 | 2026-11-17 14:40 | 1건 | EditMode 391/391, PlayMode 22/22 | codex (교차) | 10건 | [report.md](20261117-143608/report.md) |
| `20261117-151529` | 오프라인보상 | 2026-11-17 15:29 | 4건 | EditMode 438/438, PlayMode 22/22 | claude (교차) | 3건 | [report.md](20261117-151529/report.md) |
| `20261117-161017` | 오프라인보상 | 2026-11-17 16:13 | 1건 | EditMode 438/438, PlayMode 25/25 | claude (같은 에이전트) | 4건 | [report.md](20261117-161017/report.md) |
| `20261117-182336` | 시작화면 | 2026-11-17 18:48 | 7건 | EditMode 499/499, PlayMode 45/45 | claude (교차) | 11건 | [report.md](20261117-182336/report.md) |
| `20261118-121225` | realm | 2026-11-18 12:26 | 4건 | EditMode 627/627, PlayMode 77/77 | claude (교차) | 21건 | [report.md](20261118-121225/report.md) |
| `20261118-141612` | stat_upgrade_m3 | 2026-11-18 14:26 | 3건 | EditMode 634/634, PlayMode 78/78 | claude (교차) | 4건 | [report.md](20261118-141612/report.md) |
| `20261119-181417` | combat_m3 | 2026-11-19 18:21 | 2건 | EditMode 654/654, PlayMode 78/78 | claude (교차) | 0건 | [report.md](20261119-181417/report.md) |
| `20261119-183146` | combat_m3 | 2026-11-19 18:40 | 2건 | EditMode 674/674, PlayMode 80/80 | claude (교차) | 3건 | [report.md](20261119-183146/report.md) |
| `20261119-190813` | save_m3 | 2026-11-19 19:16 | 2건 | EditMode 684/684, PlayMode 80/80 | claude (교차) | 4건 | [report.md](20261119-190813/report.md) |
| `20261120-175740` | realm_fx | 2026-11-20 20:32 | 5건 | EditMode 783/783, PlayMode 80/80 | claude (같은 에이전트) | 14건 | [report.md](20261120-175740/report.md) |
| `20261122-035241` | stage45_final | 2026-11-22 04:18 | 5건 | EditMode 793/793, PlayMode 142/142 | claude (교차) | 3건 | [report.md](20261122-035241/report.md) |
| `20261122-050216` | audio | 2026-11-22 05:48 | 5건 | EditMode 884/884, PlayMode 142/142 | claude (교차) | 0건 | [report.md](20261122-050216/report.md) |
| `20261122-065407` | audio | 2026-11-22 06:59 | 1건 | EditMode 884/884, PlayMode 148/148 | claude (교차) | 2건 | [report.md](20261122-065407/report.md) |
| `20261122-091339` | flow_test | 2026-11-22 09:36 | 3건 | EditMode 884/884, PlayMode 154/154 | claude (교차) | 0건 | [report.md](20261122-091339/report.md) |
| `20261122-094424` | flow_test | 2026-11-22 09:50 | 1건 | EditMode 884/884, PlayMode 155/155 | claude (교차) | 1건 | [report.md](20261122-094424/report.md) |
| `20261122-131718` | skill_hits | 2026-11-22 13:39 | 3건 | EditMode 897/897, PlayMode 160/160 | claude (교차) | 4건 | [report.md](20261122-131718/report.md) |
