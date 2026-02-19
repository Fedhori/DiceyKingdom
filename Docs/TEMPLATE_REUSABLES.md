# TEMPLATE 재사용 목록
> 역할: 현재 템플릿에서 타 프로젝트로 재사용 가능한 구조/코드/리소스를 분류해 둔 문서입니다.

이 문서는 현재 템플릿에서 다른 게임 프로젝트로 그대로 가져갈 수 있는 구조/코드/리소스를 정리한다.

- 마지막 갱신: **2026-02-19**

## 1) 구조(Architecture)

- App Scope + Run Scope + Scene Scope 분리
  - `Assets/Scripts/App/GameApp.cs`
  - `Assets/Scripts/App/AppServices.cs`
  - `Assets/Scripts/App/RunServices.cs`
  - `Assets/Scripts/App/GameSceneInstaller.cs`
  - `Assets/Scripts/App/GameSceneRefs.cs`
- 부트스트랩 파이프라인
  - `Assets/Scripts/Bootstrap.cs`
  - `Assets/Scripts/App/SceneIds.cs`

## 2) 코드(Core Services)

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

## 3) 리소스(Reusable Assets)

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

## 4) 폐기된 항목(이번 정리에서 제거)

- 게임 전용 런타임 로직
  - `Assets/Scripts/Game/*`
- 게임 전용 UI 로직/프리팹
  - `Assets/Scripts/UI/Mission/*`
  - `Assets/Prefabs/Ui/Mission/*`
- 게임 전용 데이터/리소스
  - `Assets/StreamingAssets/Data/Adventurers.json`
  - `Assets/StreamingAssets/Data/Missions.json`
  - `Assets/StreamingAssets/Data/Traits.json`
  - `Assets/Resources/GameData/*`
  - `Assets/Resources/Portraits/*`
- 게임 전용 문서/와이어프레임
  - `Docs/ADVENTURER.md`, `Docs/MISSION.md`, `Docs/TRAIT.md` 등
  - `Docs/Wireframes/*`

## 5) 새 프로젝트 시작 시 우선 수정 파일

1. `Assets/Scripts/Data/GameConfigData.cs`
2. `Assets/StreamingAssets/Data/GameConfig.json`
3. `Assets/Scripts/App/RunServices.cs`
4. `Assets/Scripts/GameService.cs` (게임 전용 퍼사드로 교체)
5. `Docs/GAME_STRUCTURE.md` (새 프로젝트 스펙 반영)
