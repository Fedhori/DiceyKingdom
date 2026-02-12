# 프로젝트 맵

이 문서는 템플릿 프로젝트의 주요 폴더/파일 위치와 책임을 요약합니다.

## 문서 범위/분리 기준

- 게임 구조/시스템/플로우: `Docs/GAME_STRUCTURE.md`
- 범용 규칙/컨벤션: `Docs/GENERAL_RULES.md`
- 아이디어 원문/백로그: `Docs/GAME_IDEA_BACKLOG.md`
- v0 진행 마일스톤: `Docs/V0_MILESTONE.md`
- 상황 컨셉 요약: `Docs/ENEMY_ROSTER.md`
- 이 문서: 파일 위치/책임 요약

## 주요 폴더 구조

- `Assets/Scripts`
  - 공용 런타임 유틸(데이터 로딩, 저장/로드, 로컬라이징, 오디오, UI 유틸 등)
  - `Game` 하위에 v0 데이터 DTO/런타임 상태 모델/정적 데이터 로더
- `Assets/Scenes`
  - 프로젝트별 씬 구성(템플릿은 비어 있음)
- `Assets/Prefabs`
  - 공용 UI 프리팹(모달/토스트/툴팁/플로팅 텍스트)
- `Assets/Resources/Music`
  - 배경음 리소스 폴더(비어 있음)
- `Assets/Resources/SFX`
  - 효과음 리소스 폴더(비어 있음)
- `Assets/Sprites/UI`
  - 공용 UI 스프라이트
- `Assets/Fonts`
  - 공용 폰트 에셋
- `Docs`
  - 공통 규칙 및 프로젝트 문서
  - `GENERAL_RULES.md`: Unity 공통 규칙
  - `GAME_STRUCTURE.md`: 현재 기준 게임 구조/시스템
  - `GAME_IDEA_BACKLOG.md`: 아이디어 수집/후보 관리
  - `V0_MILESTONE.md`: v0 진행률/체크리스트
  - `ENEMY_ROSTER.md`: 상황 컨셉/강점/약점/대응법 요약
  - `PROJECT_MAP.md`: 레포 위치/책임 요약

## 공용 시스템 위치

- 데이터 로딩: `Assets/Scripts/Data`
- v0 데이터 모델/로더: `Assets/Scripts/Game/Data`
- v0 런타임 상태: `Assets/Scripts/Game/Runtime`
- v0 턴 오케스트레이터: `Assets/Scripts/Game/Runtime/GameTurnOrchestrator.cs`
- v0 전투 입력 컴포넌트: `Assets/Scripts/Game/UI`
  - `AgentManager.cs`: 모험가 카드 목록 생성/상태 동기화(매니저 로직)
  - `SituationManager.cs`: 상황 카드 목록 생성/상태 동기화(매니저 로직)
  - `AgentController.cs`: 모험가 카드 프리팹 View(텍스트/주사위/롤·드래그 입력 배선)
  - `SituationController.cs`: 상황 카드 프리팹 View(요구치/기한/성공·실패/드롭·클릭 입력 배선)
    - 두 View는 인스펙터 직결 참조만 사용(런타임 자동 탐색/자동 컴포넌트 추가 금지)
  - `AssignmentDragArrowPresenter.cs`: 드래그 타게팅 중 연결 라인/화살표 표시(OverlayRoot)
  - `AgentRollButton.cs`: 모험가별 굴리기 버튼 입력
  - `BottomActionBarController.cs`: 하단 액션 바(스킬 슬롯 QWER 상태, 턴 종료 CTA) 런타임 생성/동기화
  - `SkillTargetingSession.cs`: 타깃 지정이 필요한 스킬의 선택/취소/적용 세션 상태 관리
  - `TopHudController.cs`: 상단 HUD(턴/스테이지/페이즈/안정도/골드/런 상태) 이벤트 동기화
  - `GameTurnHotkeyController.cs`: 롤(1~4) / 스킬(QWER) 단축키 입력
  - `AgentDragHandle.cs`, `EnemyDropTarget.cs`: 드래그 타게팅/공격 입력(`EnemyDropTarget`은 레거시 이름 유지)
- 저장/로드: `Assets/Scripts/Save`
- 로컬라이징: `Assets/Scripts/Data/LocalizationUtil.cs`
- 오디오: `Assets/Scripts/Audio`
- UI 공용 유틸: `Assets/Scripts/UI`, `Assets/Scripts/Tooltip`
- 게임 속도: `Assets/Scripts/GameSpeedManager.cs`
- 파티클 라우팅: `Assets/Scripts/Particles/ParticleManager.cs`
- 개발 콘솔: `Assets/Scripts/Dev/DevCommandManager.cs`

## 관련 문서

- 공통 규칙: `Docs/GENERAL_RULES.md`
- 게임 구조: `Docs/GAME_STRUCTURE.md`
- 아이디어 백로그: `Docs/GAME_IDEA_BACKLOG.md`


