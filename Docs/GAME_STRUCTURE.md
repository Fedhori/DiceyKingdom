# GAME_STRUCTURE
> 역할: 구현 기준이 되는 확정 기획/규칙/시스템 구조의 단일 기준 문서.

- 버전: `v0.4`
- 마지막 갱신: `2026-02-24`
- 용어 기준: `Docs/GLOSSARY.md`

---

## 1) 한 줄 정체성

`Free or Die`는 투기장 1:1 결투에서, 플레이어와 적이 Ability를 `Combat 3개`에 배치하고 주사위를 굴려 피해를 주고받는 전술 결투 게임이다.

---

## 2) 게임 개요

- 프로젝트명: `Free or Die`
- 장르: 전술/빌드형 로그라이크 프로토타입
- 테마: 투기장 1:1 검투 결투
- 플레이어 역할: 검투단 운영자가 아니라 **플레이어 본인(검투사)**

### 핵심 상태

- `Health`: 결투 체력
- `Honor`: `Surrender` 가능 여부를 결정
- `Capacity`: 메타 편성 상한(런 준비 단계)
- 전투 중 별도 소모 자원: 사용하지 않음

---

## 3) 핵심 재미

1. 어떤 Ability를 어느 Combat에 둘지 선택한다.
2. Roll 결과와 효과로 `Total Power`를 유리하게 만든다.
3. `Surrender` 시점을 포함해 리스크를 관리한다.

---

## 4) 턴/페이즈 루프

## Duel Start (1회)

1. 선택한 `enemyId` 기준으로 적 데이터를 로드한다.
2. `Combat 3개`를 고정 생성한다. (`combatIndex: 0, 1, 2`)
3. 플레이어 시작 Loadout Ability 인스턴스를 생성한다.

## Turn Phases

1. `Reset`
2. `OpponentSetup`
3. `PlayerSetup`
4. `Roll`
5. `Resolve`

## Turn End(내부 처리)

- 쿨다운 감소(`cooldownTickPerTurn`)
- TurnEnd 타이밍 효과 처리
- 플레이어 배치 Ability를 Loadout으로 복귀

---

## 5) 전투 규칙

## 5.1 Combat

- Combat은 **항상 3개 고정**이다.
- Combat은 ID로 관리하지 않는다.
- 참조는 `combatIndex(0~2)`를 사용한다.

## 5.2 배치

- 플레이어는 Loadout Ability를 Combat에 배치한다.
- 플레이어 Combat 배치 상한은 없다.
- 적은 자신의 `abilityLoadout`을 `OpponentSetup` 때마다 랜덤 배치한다.
- 초기 배치 규칙은 **균등 무작위**다. (각 Ability 인스턴스를 0~2 Combat 중 하나에 배치)

## 5.3 굴림/수치

- `Attack` 타입 Ability만 Roll 대상이다.
- `Power`는 주사위 굴림 범위를 의미한다.
- `Power Result` 최소값은 1, 상한은 없다.
- 기본 Power 직접 변경 대신 Modifier 적용을 우선한다.

## 5.4 판정/피해

- Combat별로 `Total Power`를 비교한다.
- 결과는 `Victory / Draw / Defeat`만 사용한다.
- Victory가 발생하면 승자 측이 패자에게 `Damage 1`을 준다.
- 일부 Effect(예: `PreventOutgoingDamageOnWin`)는 승리 피해를 0으로 만들 수 있다.
- Draw는 피해가 없다.

## 5.5 Surrender

- `PlayerSetup`에서만 가능하다.
- `Honor > 0`일 때만 허용한다.
- 실행 시 즉시 Duel 종료, 보상 없음, Honor 1 소모.

---

## 6) Ability 시스템 (현재 기준)

- Ability 타입: `Attack / Skill / Passive`
- Attack: Combat에 배치되고 Roll/Resolve에 직접 기여
- Skill: 설계상 지원하되, 상세 발동 규칙은 확장 단계에서 고정
- Passive: 상시/조건부 효과로 적용
- 현재 프로토타입의 기본 검증은 Attack 중심으로 진행

---

## 7) 데이터 구조 요약

- `DataIndex`: `configs`, `abilities`, `enemies`
- `player.start`: 시작 Health/Honor/Loadout 구성
- `enemy`: 적 Health + `abilityLoadout`
- Combat은 데이터가 아니라 런타임 고정 슬롯(3개)

상세 스키마는 `Docs/DATA_SCHEMA.md`를 따른다.
