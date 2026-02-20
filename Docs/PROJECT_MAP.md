# 프로젝트 맵
> 역할: 프로젝트의 주요 문서/코드/씬 경로를 빠르게 찾기 위한 내비게이션 문서입니다.

이 문서는 현재 프로젝트의 주요 경로를 요약한다.

## 문서

- 공통 규칙: `Docs/GENERAL_RULES.md`
- 확정 기획/구조/실행 흐름: `Docs/GAME_STRUCTURE.md`
- 프로토타입 계획/진행도: `Docs/PROTOTYPE.md`
- 아이디어 브레인스토밍: `Docs/BRAINSTORMING.md`

## 코드

- 앱 수명/스코프
  - `Assets/Scripts/App`
  - `Assets/Scripts/Bootstrap.cs`

- 공통 유틸
  - `Assets/Scripts/Common`

- 데이터 로더/설정
  - `Assets/Scripts/Data`
  - `Assets/StreamingAssets/Data/GameConfig.json`

- 저장
  - `Assets/Scripts/Save`

- UI 공용 서비스
  - `Assets/Scripts/UI`
  - `Assets/Scripts/Tooltip`

- 런타임 서비스
  - `Assets/Scripts/Audio`
  - `Assets/Scripts/Particles`
  - `Assets/Scripts/InputService.cs`
  - `Assets/TemplateInputActions.inputactions`
  - `Assets/Scripts/GameSpeedService.cs`
  - `Assets/Scripts/OptionService.cs`

## 씬

- `Assets/Scenes/Bootstrap.unity`
- `Assets/Scenes/GameScene.unity`
- `Assets/Scenes/MainMenuScene.unity`

## 템플릿 재사용 레퍼런스

### 구조(Architecture)

- App Scope + Run Scope + Scene Scope 분리
  - `Assets/Scripts/App/GameApp.cs`
  - `Assets/Scripts/App/AppServices.cs`
  - `Assets/Scripts/App/RunServices.cs`
  - `Assets/Scripts/App/GameSceneInstaller.cs`
  - `Assets/Scripts/App/GameSceneRefs.cs`
- 부트스트랩 파이프라인
  - `Assets/Scripts/Bootstrap.cs`
  - `Assets/Scripts/App/SceneIds.cs`

### 코드(Core Services)

- 데이터/설정
  - `Assets/Scripts/Data/SACache.cs`
  - `Assets/Scripts/Data/GameConfigProvider.cs`
  - `Assets/Scripts/Data/GameConfigData.cs`
  - `Assets/Scripts/Data/StaticDataService.cs`
- 저장
  - `Assets/Scripts/Save/*`
- 앱 서비스
  - `Assets/Scripts/Audio/*`
  - `Assets/Scripts/InputService.cs`
  - `Assets/Scripts/GameSpeedService.cs`
  - `Assets/Scripts/Particles/ParticleService.cs`
  - `Assets/Scripts/Dev/DevCommandService.cs`
- 공통 유틸
  - `Assets/Scripts/Common/*`
- UI 공용 서비스
  - `Assets/Scripts/UI/ModalService.cs`
  - `Assets/Scripts/UI/ToastService.cs`
  - `Assets/Scripts/UI/FloatingTextService.cs`
  - `Assets/Scripts/Tooltip/*`
  - `Assets/Scripts/OptionService.cs`

### 리소스(Reusable Assets)

- 공용 UI 프리팹
  - `Assets/Prefabs/Tooltip/*`
  - `Assets/Prefabs/Ui/ConfirmationModal.prefab`
  - `Assets/Prefabs/Ui/InfoModal.prefab`
  - `Assets/Prefabs/Ui/FloatingText.prefab`
  - `Assets/Prefabs/Ui/ToastMessage.prefab`
- 설정/매니페스트
  - `Assets/StreamingAssets/sa_manifest.json`
  - `Assets/StreamingAssets/sa_state.json`
  - `Assets/StreamingAssets/Data/GameConfig.json`
  - `Assets/TemplateInputActions.inputactions`
- 씬 시작점
  - `Assets/Scenes/Bootstrap.unity`
  - `Assets/Scenes/GameScene.unity`
  - `Assets/Scenes/MainMenuScene.unity`

### 새 프로젝트 시작 시 우선 수정 파일

1. `Assets/Scripts/Data/GameConfigData.cs`
2. `Assets/StreamingAssets/Data/GameConfig.json`
3. `Assets/Scripts/App/RunServices.cs`
4. `Assets/Scripts/GameService.cs` (게임 전용 퍼사드로 교체)
5. `Docs/GAME_STRUCTURE.md` (새 프로젝트 스펙 반영)

## 비고

- 기존 DiceyKingdom 전용 게임 로직/데이터/와이어프레임 문서는 정리되었다.
- 새 프로젝트 시작 시 구조 확장은 `Docs/GAME_STRUCTURE.md`를 기준으로 진행한다.
