# TODO

프로젝트 작업 추적용 파일입니다.

## 사용 규칙

- 새 작업을 시작하기 전에 `Planned`에 작업을 추가합니다.
- 작업 진행 중에는 상태를 `In Progress`로 변경합니다.
- 작업 완료 시 `Done`에 완료일과 핵심 결과를 기록합니다.
- 보류/중단 작업은 `Blocked`에 기록하고 사유를 남깁니다.

## Planned

- [P0] 신규 코어 데이터 스키마/DTO/로더 작성 (`Adventurers`, `Missions`, `Traits`, `EffectBundle`)
- [P0] 런타임 상태 모델 구축 (`RunState`, `KingdomState`, `AdventurerState`, `MissionState`, `CandidateState`, `CemeteryState`)
- [P0] 매니저 골격 구현 (`GameManager`, `KingdomManager`, `AdventurerManager`, `MissionManager`, `AbilityTestManager`, `TraitManager`)
- [P0] 핵심 턴 루프 구현 (후보 생성/고용 -> 배치/재배치 -> 테스트 -> 원정 성공/실패 -> 턴 정산 -> 임무 실패 -> 게임오버)
- [P0] 원정/임무 판정 규칙 구현 (영웅심 1회 제한, 참여 잠금, 전원 사망 자동 포기, 임무 진행도 유지, 임무 실패 시 제거)
- [P0] 특성 시스템 구현 (성공 `60/30/10`, 실패 `60/10/30`, 임무당 1회 판정, 슬롯 부족 시 잠금 제외 랜덤 교체)
- [P1] 회복/자원 정산 규칙 구현 (전원 HP +1, 휴식자 Stamina +1, 최대 체력 clamp, 실패 효과 즉시 적용)
- [P1] 최소 플레이용 UI 연결 (후보/고용, 임무/기한/진행도, 배치, 테스트 실행, 결과/안정도 표시)
- [P1] 컴파일/플레이 스모크 테스트 (런 시작, 1턴 진행, 원정 실패, 임무 실패, 사망자 공동묘지 이동)
- [P1] 문서 동기화 (`Docs/GAME_STRUCTURE.md`, `Docs/ADVENTURER.md`, `Docs/MISSION.md`, `Docs/ABILITY_TEST.md`, `Docs/TRAIT.md`)
- [P2] 후순위 시스템 틀만 추가 (장비/시설/스킬·소모품/태그 상호작용 placeholder)

## In Progress

- (비어 있음)

## Blocked

- (비어 있음)

## Done

- 2026-02-14: `GameConfig.json` 로딩 파이프라인 구현 (`Bootstrap` 로드 -> `GameConfigProvider` -> `GameConfigData` 전역 접근) + `TooltipManager` 연동
