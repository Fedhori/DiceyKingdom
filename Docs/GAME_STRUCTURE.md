# GAME_STRUCTURE
> 역할: 구현 기준이 되는 확정 기획/규칙/시스템 구조의 단일 기준 문서.

- 버전: `v0.3`
- 마지막 갱신: `2026-02-23`
- 용어 기준: `Docs/GLOSSARY.md`

---

## 1) 한 줄 정체성

`Free or Die`는 다크 판타지 투기장에서, 플레이어(검투사)가 여러 Clash에서 Ability를 분배/굴림/해결하며 살아남는 1:1 결투 게임이다.

---

## 2) 게임 개요

- 프로젝트명: `Free or Die`
- 장르: 전술/빌드형 로그라이크 프로토타입
- 테마: 투기장 1:1 검투 결투
- 플레이어 역할: 검투단 운영자가 아니라 **플레이어 본인이 검투사**

### 핵심 상태

- Health: 결투 체력
- Honor: Surrender 가능 여부를 결정하는 값
- Capacity: 메타 편성 상한(런 준비 단계)
- 전투 중 별도 소모 자원: 사용하지 않음

---

## 3) 핵심 재미

1. Intent를 보고 어떤 Ability를 어느 Clash에 둘지 판단한다.
2. Attack Result를 얼마나 유리하게 만들지(배치/효과/타이밍) 선택한다.
3. Surrender 타이밍까지 포함해 리스크를 관리한다.

---

## 4) 턴/페이즈 루프

## Duel Start (1회)

1. Encounter 기준 Clash 목록 생성
2. Enemy Intent 생성(완전 공개)
3. 시작 Bag Ability 인스턴스 생성

## Turn Phases

1. `Reset`
2. `OpponentSetup`
3. `PlayerSetup`
4. `Roll`
5. `Skill`
6. `ClashResolve`

## Turn End(내부)

- 쿨다운 감소(`cooldownTickPerTurn`)
- TurnEnd 타이밍 효과 처리

---

## 5) 전투 규칙

## 5.1 Clash 수

- Clash 수는 고정 3이 아니다.
- Encounter의 `enemy.clashes` 개수를 그대로 사용한다.
- `enemy.clashes`는 필수이며, 비어 있으면 유효하지 않은 데이터로 처리한다.

## 5.2 배치

- Bag에 있는 플레이어 Ability를 Clash에 배치한다.
- 기본 슬롯 제한은 무제한.
- Clash에 `slotLimit`이 있으면 해당 Clash에서만 제한한다.

## 5.3 굴림/수치

- Attack 타입 Ability만 Roll 대상이다.
- `damage`는 항상 주사위 면수(굴림 범위)를 의미한다.
- Attack Result 최소값은 1, 상한은 없다.
- 기본 Attack 수치를 직접 변형하지 않고 Modifier로 처리한다.

## 5.4 판정

- Clash별 Total Attack을 비교한다.
- 결과는 `Victory / Draw / Defeat`만 사용한다.
- 대승리/대패배는 사용하지 않는다.
- 승리한 쪽이 해당 Clash의 `damage`만큼 상대 Health에 피해를 준다.

## 5.5 Surrender

- `PlayerSetup`에서만 가능하다.
- `Honor > 0`일 때만 허용한다.
- 실행 시 즉시 Duel 종료, 보상 없음, Honor를 1 소모한다.

---

## 6) Ability 시스템 (P0)

- Ability 타입: `Attack / Skill / Passive`
- Attack: Roll 및 Clash 판정에 직접 기여
- Skill: 특정 타이밍에 수동/자동으로 전장을 조정
- Passive: 상시 또는 조건부로 적용
- 적은 항상 1명이지만, Clash별로 서로 다른 Ability 조합을 가진다.

---

## 7) 데이터 구조 요약

- `DataIndex`: `configs`, `clashes`, `abilities`, `encounters`
- `player.start`: 시작 Health/Honor/Bag 구성
- `encounter`: enemy + enemy.clashes[].abilityLoadout 구조

상세 스키마는 `Docs/DATA_SCHEMA.md`를 따른다.
