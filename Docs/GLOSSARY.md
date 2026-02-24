# GLOSSARY
> 역할: 문서/코드/데이터 용어를 단일 기준으로 고정하는 사전.

- 마지막 갱신: `2026-02-24`

---

## 1) 사용 원칙

- 동일 개념은 항상 동일 용어를 사용한다.
- 신규 코드/데이터는 아래 EN 용어를 우선 사용한다.
- 구용어(`Clash`, `Intent`, `Pattern`, `Bag`)는 신규 작성에서 사용하지 않는다.

---

## 2) 확정 용어

| 분류 | KR | EN | 설명 |
|---|---|---|---|
| 프로젝트 | Free or Die | Free or Die | 프로젝트명 |
| 전투 | 결투 | Duel | 하나의 전투 단위 |
| 전투 | 페이즈 | Phase | 턴 내 진행 단계 |
| 전투 | 전투 지점 | Combat | Ability를 배치하고 수치를 비교하는 고정 슬롯(3개) |
| 전투 | 전투 인덱스 | combatIndex | Combat 참조 인덱스(0,1,2) |
| 능력 | 어빌리티 | Ability | Attack/Skill/Passive 통합 상위 개념 |
| 능력 | 공격 | Attack | 주사위를 굴려 Power Result를 만드는 Ability 타입 |
| 능력 | 스킬 | Skill | 특정 타이밍에 발동하는 Ability 타입 |
| 능력 | 패시브 | Passive | 상시/조건부 효과 타입 |
| 수치 | 주사위 수치 | Power | 주사위 굴림 범위를 정하는 기본 수치 |
| 수치 | 주사위 굴림 후 수치 | Power Result | 굴림 + 보정 이후 최종 수치 |
| 수치 | 총 파워 | Total Power | Combat 단위 합산 수치 |
| 수치 | 피해량 | Damage | 체력에 적용되는 최종 피해량 |
| 보관 | 로드아웃 | Loadout | Combat 미배치 Ability 보관 영역 |
| 상태 | 체력 | Health | 0 이하면 결투 종료 |
| 상태 | 명예 | Honor | Surrender 가능 여부를 결정 |
| 메타 | 용량 | Capacity | 준비 단계 편성 상한 |
| 행동 | 항복 | Surrender | 결투 즉시 종료(보상 없음) |

---

## 3) 제거된 구용어

- `Clash` -> `Combat`
- `Intent` -> 사용하지 않음
- `Pattern` -> 사용하지 않음
- `Bag` -> `Loadout`
