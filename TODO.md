# TODO

프로젝트 작업 추적용 파일입니다.

## 사용 규칙

- 새 작업을 시작하기 전에 `Planned`에 작업을 추가합니다.
- 작업 진행 중에는 상태를 `In Progress`로 변경합니다.
- 작업 완료 시 `Done`으로 이동하고 완료일/비고를 기록합니다.
- 보류/중단 작업은 `Blocked`에 기록하고 사유를 남깁니다.

## Planned

- (비어 있음)

## In Progress

- (비어 있음)

## Blocked

- (비어 있음)

## Done

- 2026-02-13: 규칙 위반 리팩토링 1단계 완료 — 런타임 `new GameObject`/`AddComponent` 제거, UI 바인딩을 씬/프리팹 참조 기반으로 전환, `BottomActionBarController`/`SituationController`의 레이아웃 강제 코드 제거
- 2026-02-13: 전 스크립트 `OnEnable/OnDisable` 제거 및 `Start/OnDestroy` 일괄 전환(이벤트 구독/해제 포함) + 규칙 문서화(`Docs/GENERAL_RULES.md`)
- 2026-02-13: `GameTurnOrchestrator` 제거 및 매니저 분리 리팩토링 완료(`GameManager`, `PhaseManager`, `PlayerManager`, `DuelManager`, `AgentManager`, `SituationManager`) + `Managers` 하위 오브젝트/컴포넌트 반영
- 2026-02-13: 이벤트는 UI 갱신 전용/로직 이벤트 금지 원칙을 `Docs/GENERAL_RULES.md`에 반영하고, `Docs/REFACTORING_PLAN_MANAGER_SPLIT.md` 리팩토링 계획서 작성
- 2026-02-13: 중앙 결투 오버레이 UI 추가(상황/요원 주사위 연출) 및 오버레이 종료 후 상태 반영 방식으로 전환
- 2026-02-13: 대결 실패 시 상황 주사위 면수 감소(`X -= 요원 눈`) 및 `0 이하` 즉시 파괴 규칙 구현
- 2026-02-13: 의사결정 기록 정책 변경(사용자 지시 시에만 기록) 반영 및 `Docs/GAME_IDEA.md` 기준으로 아이디어 문서 재정리
- 2026-02-13: 아이디어 문서를 `Docs/GAME_IDEA.md`로 리네임하고 요소별 분리 구조를 시도
- 2026-02-13: 사용자 피드백 반영으로 아이디어 문서를 단일 파일(`Docs/GAME_IDEA.md`)로 재통합하고 분리 문서 삭제
- 2026-02-13: 문서 전역 레거시 용어 정리(요원/상황 용어 통일, 전환 로그는 레거시 표기로 명시)
- 2026-02-13: `GAME_STRUCTURE` 구성요소 문서 분리 (`Docs/SITUATION.md`, `Docs/AGENT.md`, `Docs/SKILL.md`, `Docs/DECREE.md`) 및 문서 맵 업데이트
- 2026-02-13: `TODO.md` 파일 생성 및 추적 규칙 초기화
