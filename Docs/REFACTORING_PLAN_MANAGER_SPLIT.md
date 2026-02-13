# 매니저 분리 리팩토링 계획서

기준일: 2026-02-13

## 목표

- `GameTurnOrchestrator`를 완전 제거합니다.
- 런타임 로직을 `GameManager`, `PhaseManager`, `PlayerManager`, `AgentManager`, `SituationManager`, `DuelManager`로 분리합니다.
- 이벤트는 UI 갱신 용도로만 사용하고, 로직 처리에는 사용하지 않습니다.
- 상태값 관리는 Getter/Setter 중심으로 전환합니다.

## 고정 원칙

- 이벤트는 UI 갱신용으로만 사용합니다.
- 로직은 `xxxManager.Instance` 직접 호출로 처리합니다.
- 상태/수치 변경은 Setter에서 관리하고, 필요한 UI 이벤트를 Setter 내부에서 발행합니다.
- `FindFirstObjectByType` 기반 자동 탐색은 제거하고, 인스펙터 참조를 사용합니다.
- 매니저는 `DontDestroyOnLoad`를 사용하지 않습니다.

## 씬 구조

`Managers` 오브젝트 하위에 다음 자식 오브젝트를 두고 컴포넌트를 붙입니다.

- `Managers/GameManager` (`GameManager`)
- `Managers/PhaseManager` (`PhaseManager`)
- `Managers/PlayerManager` (`PlayerManager`)
- `Managers/AgentManager` (`AgentManager`)
- `Managers/SituationManager` (`SituationManager`)
- `Managers/DuelManager` (`DuelManager`)

## RunState 단일 진입점

- 읽기 진입점은 `GameManager.CurrentRunState`로 고정합니다.
- `CurrentRunState`는 읽기 전용 프로퍼티로 공개합니다.

## 책임 분리

- `GameManager`
  - 런 시작/재시작, 정적 데이터 로드, RNG/seed 초기화
  - `RunStarted`, `RunEnded` 이벤트 발행
  - 게임오버 최종 판정
- `PhaseManager`
  - 턴 phase 진행/전환
  - `CurrentPhase`, `TurnNumber` 프로퍼티 관리
  - `PhaseChanged`, `TurnNumberChanged` 이벤트 발행
- `PlayerManager`
  - `Stability`, `MaxStability`, `Gold` 프로퍼티 관리
  - Setter에서 `StabilityChanged`, `MaxStabilityChanged`, `GoldChanged` 발행
- `AgentManager`
  - 요원 선택/주사위 선택/소진 처리
  - `TryRollAgent*`, `TrySelectProcessingAgentDie`, `CanRollAgent`, `IsCurrentProcessingAgent`
  - 요원 카드 생성/갱신(UI) 로직 포함
- `SituationManager`
  - 상황 스폰/기한 감소/성공/실패 처리
  - `TryTestAgainstSituationDie` 진입점 제공
  - `StageSpawned` 이벤트 발행
  - 상황 카드 생성/갱신(UI) 로직 포함
- `DuelManager`
  - 대결 굴림/판정/연출 대기/결과 반영
  - `DuelRollStarted` 이벤트 발행(UI 오버레이용)
  - `NotifyDuelPresentationFinished` 수신 및 후속 로직 호출

## 이벤트 목록 (UI 용도만 유지)

- `GameManager.RunStarted`
- `GameManager.RunEnded`
- `PhaseManager.PhaseChanged`
- `PhaseManager.TurnNumberChanged`
- `PlayerManager.StabilityChanged`
- `PlayerManager.MaxStabilityChanged`
- `PlayerManager.GoldChanged`
- `SituationManager.StageSpawned`
- `DuelManager.DuelRollStarted`

제거 대상:

- 범용 이벤트(`StateChanged`)
- 로직 제어 목적 이벤트(`AssignmentCommitConfirmationRequested` 등)

## 기존 API 치환표

- `GameTurnOrchestrator.TryRollAgentBySlotIndex` -> `AgentManager.TryRollAgentBySlotIndex`
- `GameTurnOrchestrator.TryRollAgent` -> `AgentManager.TryRollAgent`
- `GameTurnOrchestrator.TrySelectProcessingAgentDie` -> `AgentManager.TrySelectProcessingAgentDie`
- `GameTurnOrchestrator.TryTestAgainstSituationDie` -> `SituationManager.TryTestAgainstSituationDie`
- `GameTurnOrchestrator.RequestCommitAssignmentPhase` -> `PhaseManager.RequestCommitAssignmentPhase`
- `GameTurnOrchestrator.ConfirmCommitAssignmentPhase` -> `PhaseManager.ConfirmCommitAssignmentPhase`
- `GameTurnOrchestrator.NotifyDuelPresentationFinished` -> `DuelManager.NotifyDuelPresentationFinished`
- `GameTurnOrchestrator.CanRollAgent` -> `AgentManager.CanRollAgent`
- `GameTurnOrchestrator.IsCurrentProcessingAgent` -> `AgentManager.IsCurrentProcessingAgent`

## 구현 순서 (원샷)

1. 신규 매니저 클래스(Phase/Player/Duel) 생성 및 싱글톤/인스펙터 참조 배치
2. `GameManager` 확장(`CurrentRunState`, 런 초기화, 게임오버 판정)
3. `GameTurnOrchestrator`의 phase 로직을 `PhaseManager`로 이동
4. 플레이어 자원 로직을 `PlayerManager`로 이동(프로퍼티+수치별 이벤트)
5. 에이전트 처리 로직을 기존 `AgentManager`로 통합
6. 상황 처리 로직을 기존 `SituationManager`로 통합
7. 대결 처리 로직/오버레이 콜백을 `DuelManager`로 이동
8. UI 스크립트의 `GameTurnOrchestrator` 참조를 각 매니저 참조로 교체
9. `GameTurnOrchestrator.cs` 삭제 및 참조 정리
10. 컴파일/플레이 검증(런 시작, 대결, 기한 만료, 게임오버, UI 갱신)

## 검증 체크리스트

- 런 시작 시 3개 상황 생성 및 UI 표시
- 요원 선택 -> 주사위 선택 -> 상황 주사위 선택 -> 오버레이 연출 -> 결과 반영
- 실패 시 상황 주사위 면수 감소 규칙 정상 반영
- 상황 해결 시 보상 반영, 만료 시 실패 효과 반영
- `Stability/Gold/Phase/Turn` UI가 각 수치 이벤트에 맞춰 즉시 갱신
- `GameTurnOrchestrator` 참조/파일 완전 제거
