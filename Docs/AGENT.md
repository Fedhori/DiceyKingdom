# 요원 (Agent)

이 문서는 요원 시스템의 데이터/런타임/UI 규칙을 정리합니다.

## 문서 범위/분리 기준

- 전체 턴/상태머신: `Docs/GAME_STRUCTURE.md`
- 이 문서: 요원 정의, 스타팅 구성, 룰 데이터, 카드 표시 규칙

## 데이터 정의 (`Assets/StreamingAssets/Data/Agents.json`)

- `agentId`: 고유 ID (`agent.xxx` 점 표기)
- `diceFaces`: 요원이 보유한 주사위 면 수 목록
- `gearSlotCount`: 장비 슬롯 수
- `rules`: 고유 룰 배열
  - `trigger`: 룰 평가 타이밍
  - `condition`: 발동 조건
  - `effect`: 실제 효과

## 스타팅 요원 4종 (현재 데이터)

- `agent.warrior`
  - 주사위: `[6, 6, 6, 6]`
  - 룰: `onRoll` 시 가장 낮은 눈 `2`개 `+1`
- `agent.archer`
  - 주사위: `[6, 6, 6, 6]`
  - 룰: `onRoll` 시 가장 높은 눈 `1`개 `+2`
- `agent.mage`
  - 주사위: `[6, 6, 6]`
  - 룰: `onCalculation` 시 `6` 이상 눈 개수 기반 보너스(`attackBonusByThreshold`)
- `agent.rogue`
  - 주사위: `[6, 6, 6, 6, 6]`
  - 룰: `onRoll` 시 모든 눈 `-1`(최소값은 엔진 클램프 규칙 적용)

## 룰 시스템 (데이터/검증 기준)

- 룰 실행 순서는 `rules` 배열 인덱스 오름차순입니다.
- 트리거는 `onRoll`, `onCalculation`만 허용합니다.
- 조건 타입은 `always`, `diceAtLeastCount`를 허용합니다.
- 효과 타입은 아래 조합을 허용합니다.
  - `onRoll`: `dieFaceDelta`, `stabilityDelta`, `goldDelta`
  - `onCalculation`: `attackBonusByThreshold`, `flatAttackBonus`
- 현재 주사위 대결 전환 단계에서는 룰 적용 엔진이 완전히 재연결되지 않아, 룰은 데이터 검증/문구 표시 중심으로 사용합니다.

## 로컬라이징 규칙 (Agent 테이블)

- 테이블: `Agent`
- 이름 키: `{agentId}.name`
- 룰 키: `{agentId}.rule.{index}`
- 로드 시 누락 키/고아 키는 경고 로그로 검증합니다.

## 런타임 상태

- 요원 상태는 `AgentState`로 관리합니다.
  - `instanceId`
  - `agentDefId`
  - `remainingDiceFaces`
  - `actionConsumed`
- 턴 시작 시 `remainingDiceFaces`는 정의 데이터(`diceFaces`) 기준으로 리필됩니다.
- 남은 주사위가 `0`이 되면 해당 요원은 소진 상태가 됩니다.

## UI/프리팹 규칙

- 카드 프리팹: `Assets/Prefabs/Agent/AgentCard.prefab`
- 매니저: `Assets/Scripts/Game/UI/AgentManager.cs`
- 카드 View: `Assets/Scripts/Game/UI/AgentController.cs`
- 카드 값 표시(이름/룰 문구/남은 주사위/상태)는 런타임 이벤트 기반으로 갱신합니다.
- 레이아웃/피벗/오프셋/크기 값은 프리팹에서 관리하며 코드에서 강제하지 않습니다.

