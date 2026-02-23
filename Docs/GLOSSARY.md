# GLOSSARY
> 역할: 문서/코드/데이터 용어를 단일 기준으로 고정하는 사전.

- 마지막 갱신: `2026-02-23`

---

## 1) 사용 원칙

- 동일 개념은 항상 동일 용어로 표기한다.
- 신규 코드/데이터는 아래 EN 용어를 우선 사용한다.
- 구용어(Action, Retreat, Opponent Intent, Reserves, Roster Deck, Focus)는 신규 작성에서 사용하지 않는다.

---

## 2) 확정 용어

| 분류 | KR | EN | 설명 |
|---|---|---|---|
| 프로젝트 | Free or Die | Free or Die | 프로젝트명 |
| 전투 | 결투 | Duel | 하나의 전투 단위 |
| 전투 | 페이즈 | Phase | 턴 내 진행 단계 |
| 전장 | 클래시 | Clash | 공격 결과를 비교하는 슬롯/공간 |
| 정보 | 의도 | Intent | 적 배치 계획(공개 정보) |
| 능력 | 어빌리티 | Ability | Attack/Skill/Passive 통합 상위 개념 |
| 능력 | 공격 | Attack | 주사위를 굴려 공격 결과를 만드는 Ability 타입 |
| 능력 | 스킬 | Skill | 특정 타이밍에 발동하는 Ability 타입 |
| 능력 | 패시브 | Passive | 상시/조건부 효과 타입 |
| 결과 | 공격 결과 | Attack Result | 굴림 + 보정 이후 최종 값 |
| 결과 | 총 공격력 | Total Attack | Clash 단위 합산 공격력 |
| 보관 | 가방 | Bag | Clash 미배치 Ability 보관 영역 |
| 상태 | 체력 | Health | 0 이하면 결투 종료 |
| 상태 | 명예 | Honor | Surrender 가능 여부를 결정 |
| 메타 | 용량 | Capacity | 준비 단계 편성 상한 |
| 행동 | 항복 | Surrender | 결투 즉시 종료(보상 없음) |

---

## 3) 구용어 매핑

| 구용어 | 신용어 |
|---|---|
| Action | Ability |
| ActionHolder | AbilityHolder (UI 표기: Bag) |
| Opponent Intent | Intent |
| Retreat | Surrender |
| Roster Deck | Ability Deck |
| Reserves | Bag |
| Supply Limit | Capacity |
| Supply Cost | Cost |
| Focus | 제거(전투 자원 미사용) |

