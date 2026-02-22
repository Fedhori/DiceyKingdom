# GLOSSARY
> 역할: 문서/코드/데이터에서 **용어 혼용을 방지**하기 위한 단일 기준표입니다.

- 마지막 갱신: `2026-02-21`

## 1) 사용 원칙

- 이 프로젝트에서 **동일 개념은 반드시 동일 용어로만** 표기한다.
- 코드/데이터의 필드명은 가능하면 **영문 용어(아래 EN)** 를 따른다.
- 한국어 문서/UI도 가능하면 **아래 KR 표기**를 따른다.

## 2) 확정 용어(한/영)

| 분류 | KR(확정) | EN(확정) | 의미/비고 |
|---|---|---|---|
| 진행 | 런 | Run | 게임오버까지의 1회 플레이 |
| 진행 | 전투 | Battle | 하나의 조우(전투 단위). 전투 패배 시 게임오버 |
| 진행 | 턴 | Turn | 전투 내 반복 단위 |
| 진행 | 페이즈 | Phase | 턴 내 단계(Recall/Deploy/…/Resolve) |
| 카드 | 편성 덱 | Roster Deck | 드로우 없음. 보급품 한도 내에서 선택한 **Squad/Support 리스트** |
| 카드 | 스쿼드 | Squad | 전투 시작 시 트리거되어 병력(Troop)을 소환/준비하는 카드 |
| 카드 | 서포트 | Support | 전투 시작 시 트리거되어 규칙/버프를 적용하는 카드 |
| 유닛 | 병력 | Troop | 전장에 배치 가능한 **주사위 유닛** |
| 전장 | 전장 | Battlefield | 병력을 배치해 승패를 가르는 장소 |
| 전장 | 진영 | Camp | 아직 전장에 배치되지 않은 병력이 대기하는 장소 |
| 전장 | 예비 편성 | Reserves | 덱(편성) 밖에 보관하는 카드 저장소(시스템 용어). **카드 이름 ‘예비대’와 구분** |
| 정보 | 적 의도 | Enemy Intent | 적이 이번 턴 어느 전장에 무엇을 배치할지. 프로토타입에서는 완전 공개 |
| 리소스 | 사기 | Morale | 전투 HP. `<= 0`이면 전투 종료. **플레이어 패배 시 게임오버** |
| 리소스 | 안정도 | Stability | **후퇴 자원**. `> 0`일 때만 후퇴 가능. 후퇴 시 `-1` & 최소 0으로 clamp |
| 리소스 | 마나 | Mana | 스킬 자원. 전투 시작 시 최대치로 충전. 턴마다 회복 |
| 리소스 | 보급품 한도 | Supply Limit | 덱(편성)에 넣을 수 있는 총 비용 상한 |
| 리소스 | 보급품 비용 | Supply Cost | Squad/Support 카드 1장이 차지하는 비용 |
| 주사위 | 파워 | Attack | 병력의 주사위 면수. 예: Attack 4 = d4 |
| 주사위 | 눈 | Attack Result | 굴림 결과 + 보정 후 최종 값. 최소 1, **최대치 없음** |
| 전투값 | 전투력 | Total Attack | 전장 내 아군/적의 Attack Result 합산값 (+전장 보너스 포함) |
| 판정 | 대승리 | Great Victory | `winnerCombatStrength >= loserCombatStrength * 2` |
| 판정 | 승리 | Victory | 승리했지만 Great Victory는 아님 |
| 판정 | 무승부 | Draw | Total Attack 동률 |
| 판정 | 패배 | Defeat | 패배했지만 Great Defeat는 아님 |
| 판정 | 대패 | Great Defeat | 상대의 Great Victory에 대응(대칭 개념) |
| 행동 | 후퇴 | Retreat | 배치(Player Deploy) 페이즈에서만 가능. 보상 없음, 즉시 전투 종료, Stability -1 |

## 3) 페이즈 명칭(영문 표준)

- Battle Start Triggers (전투 시작 트리거, 1회)
  - Squad → Support 우선순위
  - 같은 타입 내 순서: Roster Deck 배열 순서
- Turn Phases
  1) Recall
  2) Enemy Deploy
  3) Player Deploy
  4) Roll
  5) Tactics
  6) Resolve
  7) Turn End(내부 처리용: 쿨다운/마나/턴 종료 효과)

## 4) 금지/비권장 용어

- **전투력(단독 사용)**: Attack/Total Attack와 혼동되므로 금지. 반드시 Attack 또는 Total Attack로 표기.
- **덱(Deck)**: 본 프로젝트는 드로우가 없으므로 단독 ‘덱’ 대신 **Roster Deck/편성 덱**으로 표기.
- **벤치(Bench)**: Reserves(예비 편성)으로 통일.
