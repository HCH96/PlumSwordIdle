# 남궁 일반 상대 시안

2026-11-21 사용자가 [두 검객 시안](concept_approved.png)을 승인했다. PixelLab 기본 그림과 애니메이션은 당시 검토 대기였다.

시안은 사용자 참고 이미지의 청백색 무협 복장과 직검, 기존 화산 제자와 종남·개방 상대의 SD 픽셀 화풍을 참고해 내장 이미지 생성으로 만들었다. 남자 청의 검객과 여자 남색 검객 두 종류를 제안했다.

PixelLab Pro에서 각 64×64, 8방향, low top-down으로 생성했고 화산 제자 `6c24a0f7-bff6-4d33-88cc-a7839592c1dd`를 스타일 참고로 썼다. 시안 이미지 업로드가 자동 승인 검토에서 거부되어 외형은 이미지 대신 글로 설명했다. 따라서 시안과 세부 외형이 다를 수 있다.

| 대상 | 캐릭터 ID | PixelLab 원본 | 대기 | 이동 | 공격 | 쓰러짐 |
| --- | --- | --- | --- | --- | --- | --- |
| 청의 검객 | `fae80179-515f-483c-b669-771a0d03dd72` | [ZIP](male_pixellab_export.zip) | [보기](male_idle_preview.gif) | [보기](male_walk_preview.gif) | [보기](male_attack_preview.gif) | [보기](male_down_preview.gif) |
| 남색 검객 | `3f8c40b3-f87b-4ecd-baa5-c96bff3ff241` | [ZIP](female_pixellab_export.zip) | [보기](female_idle_preview.gif) | [보기](female_walk_preview.gif) | [보기](female_attack_preview.gif) | [보기](female_down_preview.gif) |

남서 방향 PixMiniMax. 대기·쓰러짐은 17프레임, 이동·공격은 25프레임이며 모두 76×76이다. 검토 GIF는 원본 프레임을 어두운 배경에 합성하고 최근접으로 3배 확대해 10fps로 반복한다. 쓰러짐도 검토를 위해 반복한다. 게임 재생 속도와 피벗은 아직 정하지 않았다.

그룹 ID 순서(대기·이동·공격·쓰러짐): 청의 `5887aeb0-cc69-4627-8314-51ca936edf4d`, `50710b55-e9a7-4a2f-a0fc-d4b29e5af6be`, `bbdba9c0-23a0-4dcc-8ef9-1f8bf3e57e6a`, `22f8126d-6898-4dfd-ab2d-25efdf706e08`; 남색 `679a17bc-6cf9-43d5-bbd5-6c47299ea02c`, `79695487-ffb7-46bf-8e4b-b44260fbd301`, `822667a1-85ba-4fab-829b-6a23ab83b3cf`, `15b077be-0288-41c3-8a51-e302192e2f19`.

생성 지시: 대기는 발과 검을 유지하며 미세하게 호흡하고 처음 자세로 복귀, 이동은 제자리에서 왼쪽 앞을 향해 번갈아 걷고 처음 자세로 복귀, 공격은 왼쪽으로 검을 한 번 휘두른 뒤 복귀, 쓰러짐은 화면 오른쪽으로 넘어져 누운 상태로 끝낸다. 재생을 확인하고 수정 여부를 결정한다.

## 복장과 검기 수정 시안

사용자 요청에 따라 여성 검객의 도복을 청의 검객처럼 푸른 겉옷과 흰 속옷, 짙은 띠로 바꾸고 네 동작을 다시 만들었다. 수정 캐릭터 ID `f0f25637-0fc9-4963-b3ff-9dbc705bdc61`, [원본 ZIP](female_pixellab_v2.zip). 대기·이동·공격·쓰러짐 그룹은 각각 `dcd9d5a5-822f-4882-a09c-70c527fc92d5`, `0cea54ea-8b63-443d-beac-64317dc60b18`, `101fafed-8f40-46f7-8f5b-b7462cb0b638`, `ff1bd4b8-36be-4c46-bbc2-d5c47c091080`이다. [대기](female_idle_v2_preview.gif), [이동](female_walk_v2_preview.gif), [공격](female_attack_v2_preview.gif), [쓰러짐](female_down_v2_preview.gif). 여성 공격에는 생성 과정에서 짧은 흰 궤적이 나타난다.

청의 검객은 기존 대기·이동·쓰러짐을 유지하고, 검 궤적을 따라 푸른 흰빛 검기가 한 번 나타나는 [공격 시안](male_attack_sword_energy_v2_preview.gif)을 새로 만들었다. 그룹 `49f38eba-57b4-46c9-9d78-bcac087fa78a`, [원본 ZIP](male_pixellab_v2.zip). 두 수정본은 남서 방향, 칸 76×76, 대기·쓰러짐 17프레임, 이동·공격 25프레임이고 미리보기는 10fps 반복이다. 당시 확인 대기였다.

## 여성 검객 풀어 내린 머리 시안

사용자 요청으로 청백색 도복은 유지하고 머리끈 없이 긴 머리를 풀었다. PixelLab 상태 ID `7d81832b-fee6-42fa-b901-7b3c5df8adfd`, [원본 ZIP](female_pixellab_v3.zip). 남서쪽 PixMiniMax 동작: [대기](female_idle_v3_preview.gif), [이동](female_walk_v3_preview.gif), [공격](female_attack_v3_preview.gif), [쓰러짐](female_down_v3_preview.gif). 그룹은 각각 `bd1b9e2a-3b10-4f2d-b39d-8c517d25dd94`, `fb9df52c-b980-4106-ba3a-5c4c5baed30b`, `f14bfa60-2247-4360-8dda-73b5f47b4dbd`, `e4cbf24e-7f67-46e4-8a54-5f64f20d80c9`. 칸은 76×76, 대기·쓰러짐 17프레임, 이동·공격 25프레임. 미리보기는 3배·10fps 반복. 공격에 흰 궤적이 두 번 보일 수 있어 확인이 필요하다. 아직 적용하지 않았다.

## 여성 검객 네 번째 시안

사용자 의견을 반영해 풀어 내린 검은 머리와 청백색 도복을 유지하면서 눈·얼굴을 더 여성스럽게 표현하도록 새 캐릭터를 생성했다. PixelLab Pro 64×64, 8방향, low top-down, 화산 제자 스타일 참조. ID `f4997b06-748b-4b0e-ac51-73bba597fac8`, [원본 ZIP](female_pixellab_v4.zip). 남서쪽 PixMiniMax [대기](female_idle_v4_preview.gif), [이동](female_walk_v4_preview.gif), [공격](female_attack_v4_preview.gif), [쓰러짐](female_down_v4_preview.gif). 그룹 순서는 `9f42f2a8-b673-4169-b8d0-8b837639a323`, `42f0b0ec-261c-4f59-aaec-1807cabee275`, `a65af10e-4c7c-4b8b-9ce9-e0c46accdaff`, `15c30cba-c124-4696-8ab5-a89fba19a6d1`. 프레임 크기와 수, 미리보기 재생 방식은 이전 시안과 같다. 쓰러지는 중간에 얼굴과 머리 모양이 바뀌어 보이는 점을 사용자에게 알렸다.

사용자가 위 여성 시안과 청의 검객(검기 공격 포함)을 승인해 3스테이지 일반 상대에 적용했다. 쓰러짐의 중간 외형 변화는 승인된 원본 그대로다. 게임용 시트의 크기·피벗·재생 속도는 [에셋 목록](../../../docs/dev/assets.md)을 따른다.
