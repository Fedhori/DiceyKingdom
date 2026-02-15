# TODO

프로젝트 작업 추적용 파일입니다.

## 사용 규칙

- 새 작업을 시작하기 전에 `Planned`에 작업을 추가합니다.
- 작업 진행 중에는 상태를 `In Progress`로 변경합니다.
- 작업 완료 시 `Done`에 완료일과 핵심 결과를 기록합니다.
- 보류/중단 작업은 `Blocked`에 기록하고 사유를 남깁니다.

## Planned
- [P1-SPEC] Overlay `임무 카드/리스트` 스펙 확정 (표시: 임무명/기한/진행도/배치 인원, 선택/강조 규칙)
- [P1-SPEC] Overlay `시설 패널` 스펙 확정 (시설 목록, 상태 표시, 선택/사용 동작)
- [P1-SPEC] Overlay `모집 창` 스펙 확정 (후보 정보/고용 버튼, 오픈/닫기 규칙)
- [P1-SPEC] Overlay `임무 상세/배치 창` 스펙 확정 (선택 임무 상세, 배치/해제 상호작용)
- [P1-SPEC] Overlay `테스트 실행/결과 패널` 스펙 확정 (실행 버튼, 성공/실패/영웅심/피해 표시)
- [P1-SPEC] Overlay `턴 종료/정산 패널` 스펙 확정 (정산 결과 요약, 임무 실패 반영 방식)
- [P1] 최소 플레이용 UI 연결 (후보/고용, 임무/기한/진행도, 배치, 테스트 실행, 원정 실패/임무 실패 상태 표시)
- [P1] 검증/스모크 테스트 (런 시작, 1턴 루프, 원정 실패 즉시 효과, 임무 실패 제거, 조건부 규칙 onHpChanged 재계산 반영)
- [P1] 문서 동기화 (`Docs/GAME_STRUCTURE.md`, `Docs/ADVENTURER.md`, `Docs/MISSION.md`, `Docs/ABILITY_TEST.md`, `Docs/TRAIT.md`, `Docs/GENERAL_RULES.md`)
- [P2] 후순위 placeholder 정리 (장비/시설/스킬·소모품/태그 시스템 필드만 유지, 로직 비활성)

## In Progress

- (비어 있음)

## Blocked

- (비어 있음)

## Done

- 2026-02-14: 런타임 인력 저장구조 분리 완료 (`RunState`를 `candidates/adventurers/graveyard` 3리스트로 전환, 후보/공동묘지 uid 리스트 제거, 턴/임무 서비스 로직을 분리 구조 기준으로 수정)
- 2026-02-14: [P0] 특성 시스템 구현 완료 (`TraitService` 추가, 성공/실패 확률 `60/30/10`·`60/10/30` 적용, 임무당 단일 결과, 슬롯 초과 시 잠금 제외 랜덤 교체, mission 종료 시 mission layer modifier 제거 흐름 유지)
- 2026-02-14: [P0] 턴 루프 구현 완료 (`TurnLoopService` 추가, 후보 2명 생성/미채용 폐기/정원 6 채용 체크/임무 턴당 2개 생성/정산 시 deadline 감소 및 임무 실패 제거/전원 HP+1·휴식자 Stamina+1 반영)
- 2026-02-14: [P0] 핵심 원정/임무 규칙 구현 완료 (`MissionExpeditionService` 추가, 영웅심 임무당 1회/첫 테스트 시작 시 참여 잠금/전원 사망 자동 포기/원정 실패 시 임무·진행도 유지/임무 실패 시 제거 반영)
- 2026-02-14: [P0] Stat Modifier 시스템 구현 완료 (`StatService`, `ModifierService` 추가, `add -> mul -> set -> floor` 계산 순서, `layer/stackPolicy` 반영, owner dirty + on-demand recalc 연결)
- 2026-02-14: [P0] Effect 처리기 구현 완료 (`EffectApplier` 연결, `effectId` 기반 대상 분기, `params: List<float>` 적용, `paramCount` 검증 연동 유지, `Floor` 반올림 규칙 적용)
- 2026-02-14: [P0] Rule 시스템 구현 완료 (`RuleContext/RuleConditionEvaluator/RulePipeline/RuleRunner` 추가, `RuleDef = trigger + condition + effects` 공통 실행, Mission/Trait 파이프라인 통합, 전역 순서 `Trait -> Mission` 고정)
- 2026-02-14: [P0] 런타임 Instance 스키마 구현 완료 (`InstanceTypes.cs`에 `RunState/AdventurerInstance/MissionInstance/TraitInstance/ModifierInstance` 추가, uid-only reference 구조 반영, `GameManager.CurrentRunState` 단일 진입점/API 연결)
- 2026-02-14: [P0] 정적 Def 로더 안정화 (`StaticDataSet` 읽기 전용화/조회 API 보강 + `sa_manifest.json`을 `GameConfig/Adventurers/Missions/Traits` 기준으로 동기화)
- 2026-02-14: [P0] 정적 Def 스키마/로더 구현 완료 (`DefTypes.cs`, `StaticDataLoader.cs`, `StaticDataSet.cs` + `Adventurers/Missions/Traits` JSON 추가, Bootstrap 로드 검증 연동)
- 2026-02-14: `GameConfig.json` 로딩 파이프라인 구현 (`Bootstrap` 로드 -> `GameConfigProvider` -> `GameConfigData` 전역 접근) + `TooltipManager` 연동
