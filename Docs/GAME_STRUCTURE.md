# 게임 구조

이 문서는 현재 레포를 **새 게임 시작용 템플릿 프로젝트**로 유지하기 위한 구조/실행 흐름을 정의한다.

- 마지막 갱신: **2026-02-18**
- Unity 버전: **Unity 6.2 (6000.2.x)**

## 목표

- 기존 DiceyKingdom 전용 게임 로직/데이터/리소스는 폐기한다.
- 재사용 가능한 앱 인프라(App/Save/UI/Audio/Input/Bootstrap)만 유지한다.
- 새 프로젝트는 이 템플릿 위에서 게임 전용 런타임을 다시 구축한다.

## 씬 구성

### Bootstrap.scene

- 목적: 앱 부트스트랩 + 영속 AppScope 초기화
- 핵심 순서(`Bootstrap.Awake`)
  1. `SaCache.InitAsync(...)`
  2. `GameConfigProvider.LoadFromStreamingAssetsAsync()`
  3. `managersRoot.SetActive(true)` -> `GameApp` 초기화
  4. `SaveWebGlSync.SyncFromPersistentAsync()`
  5. `SceneManager.LoadSceneAsync(SceneIds.GameScene)` (`TemplateStartScene` 별칭 포함)

### GameScene.scene

- 목적: 템플릿 기본 시작/실행 씬
- 역할: 새 게임 전용 플레이 루프를 붙이기 전까지 런타임 시작점

### MainMenuScene.scene

- 목적: 선택 사용 가능한 보조 시작 씬
- 역할: 필요 시 타이틀/런처 UX로 재구성

## 스코프(수명) 분리

### App Scope (Persistent)

- 소유자: `GameApp` (유일한 영속 싱글톤)
- 구성: `AppServices`
  - UI: Tooltip/Modal/Option/FloatingText/Toast
  - Runtime: Audio/Bgm/Input/GameSpeed/Particle/Save/StaticData/DevCommand

### Run Scope (Per-run)

- 소유자: `GameApp.Run` (`RunServices`)
- 기본 상태: `RunState`
  - `uid`, `seed`, `tick`, `primaryValue`, `secondaryValue`
- 용도: 새 프로젝트가 도입할 게임 전용 상태/룰의 최소 뼈대

### Scene Scope

- 씬 단위 오브젝트/연출/UI
- 런 시작 책임은 `GameSceneInstaller` 또는 동등한 단일 진입 컴포넌트에 둔다.

## 저장/로드

- 저장 DTO: `SaveData(meta, payloadJson)`
- 직렬화: `Newtonsoft.Json`
- 런 상태는 `RunServices.ExportRunStateJson` / `TryImportRunStateJson`으로 다룬다.

## 데이터 구조

- `Assets/StreamingAssets/Data/GameConfig.json`
  - 템플릿 기본 실행 설정
  - 기본 필드: `templateName`, `defaultRunSeed`, `startingPrimaryValue`, `startingSecondaryValue`, `ticksPerAutoSave`

## 빌드 씬

- `Assets/Scenes/Bootstrap.unity`
- `Assets/Scenes/GameScene.unity`
- `Assets/Scenes/MainMenuScene.unity`

## 새 프로젝트 시작 체크리스트

1. `GameConfigData`/`GameConfig.json`을 게임 도메인에 맞게 확장한다.
2. `RunState`/`RunServices`에 게임 전용 상태/로직을 추가한다.
3. `GameScene`(또는 `MainMenuScene`) 기준으로 실제 플레이 씬 진입 흐름을 정의한다.
4. `Docs/TEMPLATE_REUSABLES.md`를 기준으로 필요한 모듈만 유지/교체한다.
