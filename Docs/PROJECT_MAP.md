# 프로젝트 맵

이 문서는 템플릿 프로젝트의 주요 폴더/파일 위치와 책임을 요약합니다.

## 문서 범위/분리 기준

- 게임 구조/시스템/플로우: `Docs/GAME_STRUCTURE.md`
- 범용 규칙/컨벤션: `Docs/GENERAL_RULES.md`
- 아이디어 원문/백로그: `Docs/GAME_IDEA_BACKLOG.md`
- v0 진행 마일스톤: `Docs/V0_MILESTONE.md`
- 몬스터 컨셉 요약: `Docs/ENEMY_ROSTER.md`
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
  - `ENEMY_ROSTER.md`: 몬스터 컨셉/강점/약점/대응법 요약
  - `PROJECT_MAP.md`: 레포 위치/책임 요약

## 공용 시스템 위치

- 데이터 로딩: `Assets/Scripts/Data`
- v0 데이터 모델/로더: `Assets/Scripts/Game/Data`
- v0 런타임 상태: `Assets/Scripts/Game/Runtime`
- v0 턴 오케스트레이터: `Assets/Scripts/Game/Runtime/GameTurnOrchestrator.cs`
- v0 전투 입력 컴포넌트: `Assets/Scripts/Game/UI`
  - `AdventurerPanelController.cs`: 좌측 모험가 패널(4슬롯) 런타임 생성/상태 표시/입력 컴포넌트 배선
  - `EnemyPanelController.cs`: 우측 Enemy 패널 런타임 생성/체력·행동·준비 카운트 표시/드롭 타깃 배선
  - `AssignmentDragArrowPresenter.cs`: 드래그 타게팅 중 연결 라인/화살표 표시(OverlayRoot)
  - `AdventurerRollButton.cs`: 모험가별 굴리기 버튼 입력
  - `BottomActionBarController.cs`: 하단 액션 바(스킬 슬롯 QWER 상태, 턴 종료 CTA) 런타임 생성/동기화
  - `GameTurnHotkeyController.cs`: 롤(1~4) / 스킬(QWER) 단축키 입력
  - `AdventurerDragHandle.cs`, `EnemyDropTarget.cs`: 드래그 타게팅/공격 입력
  - `AdventurerProcessingHighlight.cs`: 현재 처리 중 모험가 강조 표시
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

