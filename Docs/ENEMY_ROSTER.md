# 상황 로스터 (구 ENEMY_ROSTER)

파일명은 유지하지만, 이 문서는 `Situation` 기준으로 관리합니다.

## 목적

- 프로토타입에서 사용할 Situation 초안을 빠르게 고정한다.
- `risk_value`를 감이 아닌 비교 가능한 기준으로 관리한다.

## v0 스코프 고정

- 이번 프로토타입 스폰 풀에서는 `opportunity`를 제외한다.
- 스폰 대상은 위기형 Situation만 사용한다.
- `external/internal`은 태그/시스템 분류로 사용하지 않는다.
- 스폰 추첨은 가중치 없는 균등 추첨을 사용한다.
- 위험예산은 고정값 `2000.0`을 사용한다.
- v0에서는 Gold 시스템을 사용하지 않는다. (골드 관련 효과는 치환 완료)

## v0 골드 치환 결과 (확정)

1. `nomad_raid`
- 성공 효과: `Gold +1` -> `Defense +1`

2. `grain_warehouse_loot`
- 성공 효과: `Gold +1` -> `Stability +1`

3. `orc_incursion`
- 성공 효과: `Gold +2` -> `Defense +2`

4. `noble_conspiracy`
- 성공 효과: `Gold +1` -> `deadline_delta(+1)` on random other situation

5. `smuggling_ring`
- 실패 효과: `Gold -2`, `Stability -1` -> `Stability -2`
- 성공 효과: `Gold +2` -> `Stability +1`

## risk_value 산정 가이드 (v0)

- 기준식:
  - `risk_value = clamp(100 + pressure + fail + persist + volatility - mitigation + designer_delta, 100, 500)`
- 항목 범위(권장):
  - `pressure`: 0~180
  - `fail`: 0~180
  - `persist`: 0~120
  - `volatility`: 0~60
  - `mitigation`: 0~140
  - `designer_delta`: -40~+40
- 최종 `risk_value`는 실수형으로 기록한다.

### 빠른 환산 규칙 (v0)

- `demand +1`의 기본 위험 기여값: `+6`
- deadline 보정:
  - `deadline = 1` -> demand 기여에 `x1.5`
  - `deadline = 2` -> demand 기여에 `x1.2`
  - `deadline >= 3` -> demand 기여에 `x1.0`
- `deadline_delta(-1)` (random 대상) 위험 가중: `+120` 고정 가산
- 턴 시작 `demand_delta(+3)` self 효과 가중: `+90` (턴 누적 압박 반영)

## v0 Situation 초안 (위기형 8종)

| situation_id | deadline | demand(초안) | 턴 시작 효과(초안) | 실패 효과(초안) | 성공 효과(초안) | breakdown (P/F/Pe/V/M/D) | risk_value |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `nomad_raid` | 1 | 12 | 없음 | `Defense -2` | `Defense +1` | 120/80/0/20/30/+0 | 290.0 |
| `watchtower_fire` | 1 | 10 | 없음 | `Defense -1`, `deadline_delta(-1)` to random situation | 없음 | 125/140/20/35/10/+20 | 430.0 |
| `grain_warehouse_loot` | 1 | 11 | 없음 | `Stability -2` | `Stability +1` | 115/85/0/15/25/+0 | 290.0 |
| `orc_incursion` | 2 | 20 | 없음 | `Defense -3` | `Defense +2` | 140/120/10/20/20/+10 | 380.0 |
| `plague_outbreak` | 3 | 14 | 없음 | `Stability -2`, `demand_delta(+2)` on all other situations | 없음 | 70/150/25/20/10/+0 | 355.0 |
| `noble_conspiracy` | 3 | 17 | 없음 | `Stability -3` | `deadline_delta(+1)` on random other situation | 95/130/20/15/25/+5 | 340.0 |
| `rebel_cells` | 4 | 16 | `demand_delta(+3)` on self (every turn start) | `Stability -2` | 없음 | 80/95/95/25/15/+10 | 390.0 |
| `smuggling_ring` | 3 | 16 | 없음 | `Stability -2` | `Stability +1` | 85/90/40/20/30/+0 | 305.0 |

## 작성 메모

- `demand` 수치는 런 파라미터/효과 체감을 보기 위한 1차 초안이다.
- `risk_value`는 위 표의 breakdown 기준으로 1차 배정했으며, 플레이테스트 로그로 재보정한다.
- 실패/성공/턴 시작 효과 문구는 현재 v1 효과 스키마(`resource_delta`, `gold_delta`, `demand_delta`, `deadline_delta`, `resource_guard`)를 기준으로 작성했다.
- v0에서는 Gold 시스템을 제외하므로 Situation 데이터에서는 `gold_delta`를 사용하지 않는다.
- 실패 시 상황은 즉시 제거되므로, 실패 효과는 "상황 제거 이후에도 의미 있는 효과" 위주로 작성한다.
- `nomad_raid`, `grain_warehouse_loot`는 빠른 우선순위 판단 강제용 저~중위험 카드다.
- `orc_incursion`, `rebel_cells`는 스폰 파동에서 고위험 앵커 역할을 맡는다.
- `plague_outbreak`는 자체 요구치는 낮췄지만, 실패 시 전역 demand 가중으로 총 위협도를 끌어올리는 카드다.

## 리스크 메모

- `동시 상한 없음 + 중복 허용` 조합에서 고위험 카드 연속 등장 시 급사가 발생할 수 있다.
- `watchtower_fire`, `rebel_cells`처럼 다른 Situation을 건드리는 카드가 많아지면 원인 추적이 어려워진다.

## 다음 확인 항목

1. `risk_value` 분포가 너무 상단(300+)에 몰려있는지 확인
2. 실패 효과의 체감 강도(`Defense`/`Stability` 감소)가 실제 난이도와 일치하는지 확인
3. 위험예산 고정값(`budget = 2000.0`) 기준으로, 웨이브별 실제 스폰 카드 수 분포 확인
