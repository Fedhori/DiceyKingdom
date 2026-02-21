# PROTOTYPE
> 역할: 프로토타입 목표/확정표/진행도를 관리하는 실행 계획 문서입니다.

- 마지막 갱신: `2026-02-21`
- 상태: `초안(확정 룰 반영)`

> 용어 단일 기준: `Docs/GLOSSARY.md`

## 1) 목표

- “전장 3개에 병력 주사위를 분배 배치 → 굴림/조정 → 결과별 효과 발동”까지 1회 전투가 끝까지 진행되는 **플레이어블 프로토타입**을 만든다.
- 핵심 검증 포인트:
  - Enemy Intent가 공개된 상태에서 “배치/조정”의 재미가 성립하는가?
  - Squad/Support 편성(보급품 한도)이 전략 선택을 의미 있게 만드는가?
  - 눈(Face Value) 보정이 UI로 명확히 피드백되는가?

## 2) 범위

### 포함(P0)

- 전투당 Battlefield 3개 (전투 중 고정)
- 덱(편성) / 예비 편성(Reserves) / 보급품 한도(Supply Limit)
- Battle Start Triggers: Squad → Support (전투 시작 1회 트리거)
- 턴 루프:
  - Recall → Enemy Deploy → Player Deploy → Roll → Tactics → Resolve
- 주사위 규칙:
  - Power 기반 굴림(1~Power)
  - 눈 최소 1 / 상한 없음
  - Combat Strength 단순 합
  - Great Victory 판정식 적용
- 마나/쿨다운:
  - manaMax=5, 전투 시작 시 최대
  - 턴 회복 +2
  - 턴 종료 시 쿨다운 -1
- 스킬(초기 5개): Redeploy / Decoy / Risky Approach / Safe Approach / Reinforce
- 패배/후퇴 처리:
  - 패배 시 Stability -2
  - 후퇴 시 Stability -1 (보상 없음)

### 제외(프로토타입 범위 밖)

- 유물, 강화, 소모품
- 상점/이벤트
- 매 턴 전장 랜덤 교체(프로토타입은 전장 고정)
- 장기 런 구조(맵/분기/보스 등) 디테일

## 3) 확정표 (P0)

| 항목 | 값 | 상태 |
|---|---|---|
| Battlefield 개수 | 3 | 확정 |
| Battlefield 턴별 교체 | 없음(전투 중 고정) | 확정 |
| Enemy Intent | 완전 공개 | 확정 |
| 덱 시스템 | 드로우 없음, 편성 리스트(모든 카드 전투 시작 1회 트리거) | 확정 |
| 카드 타입 | Squad / Support | 확정 |
| 트리거 우선순위 | Squad → Support | 확정 |
| 병력 수치 | Power(dX) + 눈(Face Value) | 확정 |
| 눈 최소/최대 | 최소 1 / 최대 없음 | 확정 |
| Combat Strength | 단순 합 | 확정 |
| Great Victory | 승자 >= 패자*2 | 확정 |
| 이동 후 눈 유지 | 유지 | 확정 |
| Reinforce | 전장 Combat Strength +2(눈 변화 없음) | 확정 |
| 마나 | max=5, 시작 시 최대, 턴 회복 +2 | 확정 |
| 쿨다운 감소 | 턴 종료 시 -1 | 확정 |

## 4) 미확정(TBD)

- 같은 타입 카드 내부 트리거 순서(스쿼드끼리/서포트끼리)
- 예비군(Troop) “미배치 시 +2”의 정확한 누적/적용 방식
- Risky/Safe Approach의 “적도 마찬가지” 적용 범위(승리/패배 양쪽 모두 승격/하향인지)

## 5) 진행도(체크리스트)

- [ ] 전투 씬에서 Battlefield 3개 생성 및 고정
- [ ] Enemy Deploy + Intent UI 표시
- [ ] Player Deploy(드래그/클릭 배치)
- [ ] Roll Phase: 개별 병력 굴림 + 전장 Combat Strength 계산 표시
- [ ] Tactics Phase: 스킬 사용(이동/보정) + 눈 변경 UI 반영
- [ ] Resolve Phase: 결과 판정 + 결과별 효과 발동 + Morale 감소
- [ ] 전투 종료(승/패) + 보상 단계 진입
- [ ] Retreat 처리(배치 단계) + Stability 감소

## 6) DoD(Definition of Done)

- “한 전투를 시작해서 2~3턴을 진행한 뒤 승/패/후퇴로 종료”까지 **버그 없이 재현 가능**해야 한다.
- 눈 보정이 들어갈 때마다 플레이어가 즉시 이해할 수 있도록 **최종 눈**과 **보정 근거(툴팁/로그)**가 표시되어야 한다.
- 모든 수치/용어는 `Docs/GLOSSARY.md` 기준을 따른다.
