## Relevant Files

- `Assets/Scripts/Save/SaveManager.cs` - 세이브/로드 플로우와 로드 모드 상태를 관리하는 신규 매니저.
- `Assets/Scripts/Save/SaveService.cs` - 파일 IO, 체크섬, 백업/손상 처리, 원자적 저장을 담당.
- `Assets/Scripts/Save/SaveDto.cs` - save.json 스키마 정의용 DTO.
- `Assets/Scripts/Save/SaveJson.cs` - 체크섬 계산용 직렬화 규칙 및 캐노니컬 JSON 생성.
- `Assets/Scripts/Save/SaveValidator.cs` - 필드 검증과 구조화 로그 생성.
- `Assets/Scripts/Save/SaveLogger.cs` - 콘솔 + `save_log.txt` 기록 유틸.
- `Assets/Scripts/Save/SavePaths.cs` - 세이브 경로/파일명 상수와 디렉토리 생성 헬퍼.
- `Assets/Scripts/Save/SaveService.cs` - 원자적 저장/체크섬/기본 읽기 처리.
- `Assets/Scripts/Save/SaveManager.cs` - 런 상태 수집 및 로드 모드 플래그 관리.
- `Assets/Scripts/Save/SaveLogger.cs` - 콘솔 + `save_log.txt` 기록 유틸.
- `Assets/Scripts/Save/SaveWebGlSync.cs` - WebGL 저장소 동기화 호출 래퍼.
- `Assets/Scripts/Save/SaveWebGlSyncBridge.cs` - WebGL sync 콜백 브릿지(DontDestroyOnLoad).
- `Assets/Scripts/Stage/StageManager.cs` - 스테이지 시작 직전 자동 저장 훅 추가.
- `Assets/Scripts/GameManager.cs` - 로드 플로우 분기와 RNG 재시드 처리.
- `Assets/Scripts/Mainmenu/MainMenuManager.cs` - 이어하기 로드 플로우 연결 및 버튼 노출 제어.
- `Assets/Scripts/Bootstrap.cs` - WebGL 저장소 초기 sync 시점 추가.
- `Assets/Scripts/Player/PlayerManager.cs` - 플레이어 상태 복원(통화/스탯 적용) 흐름.
- `Assets/Scripts/Player/PlayerInstance.cs` - Permanent 스탯 모디파이어 복원.
- `Assets/Scripts/Stat/GameStat.cs` - StatModifier 직렬화/복원 보조.
- `Assets/Scripts/Item/ItemManager.cs` - 인벤토리 복원 및 트리거 보정.
- `Assets/Scripts/Item/ItemInstance.cs` - Item UniqueId 주입/복원 지원.
- `Assets/Scripts/Upgrade/UpgradeInstance.cs` - Upgrade UniqueId 주입/복원 지원.
- `Assets/Scripts/Item/ItemEffectManager.cs` - 로드 모드에서 Permanent 효과 필터링.
- `Assets/Scripts/Item/ItemEffectManager.cs` - 로드 모드에서 Permanent 효과 필터링.
- `Assets/Scripts/Upgrade/UpgradeManager.cs` - 업그레이드 재적용 시 Permanent 필터링.
- `Assets/Scripts/Upgrade/UpgradeInventoryManager.cs` - 미장착 업그레이드 복원.
- `Assets/Localization/modal.asset` - 세이브/로드 모달 키 추가.
- `Assets/Localization/modal_ko-KR.asset` - 세이브/로드 모달 한글 문구 추가.
- `Assets/Localization/modal_en.asset` - 세이브/로드 모달 영문 문구 추가.
- `Assets/Scripts/UI/Modal/ModalManager.cs` - 모달 호출 경로 점검/연동.
- `Assets/Plugins/WebGL/SaveWebGlSync.jslib` - WebGL IndexedDB sync 호출 구현.
- `Assets/Tests/Save/SaveValidatorTests.cs` - 검증 로직 유닛 테스트(선택).
- `Assets/Tests/Save/SaveServiceTests.cs` - 파일 IO/백업 처리 테스트(선택).

### Notes

- 테스트를 추가할 경우 Unity Test Runner로 실행한다.
- 이어하기 버튼/SaveManager는 Unity 에디터에서 씬/프리팹/Inspector 연결이 필요하다(사용자 작업 필요).

## Tasks

- [x] 1.0 세이브 스키마/DTO, 검증 규칙, 구조화 로그 설계
  - [x] 1.1 Save DTO 스키마 정의(메타/런/플레이어/아이템/업그레이드/스탯 모디파이어).
  - [x] 1.2 체크섬 계산 규칙 정의(체크섬 필드 제외) 및 직렬화 포맷 결정.
  - [x] 1.3 SaveValidator 구현: 필수 필드/타입/범위/ID 존재 여부(Repository) 검증.
  - [x] 1.4 SaveLogger 구현: `fieldPath/expected/actual` 포함 구조화 로그를 콘솔 + `save_log.txt`에 기록.

- [x] 2.0 파일 IO/백업/원자적 저장 구현
  - [x] 2.1 `persistentDataPath/saves` 경로 헬퍼 및 디렉토리 생성 처리.
  - [x] 2.2 원자적 저장: `save.tmp` 작성 → 검증 → `save.json` 교체 → 기존 파일은 `save_backup.json`으로 이동.
  - [x] 2.3 로드: `save.json` 검증 실패 시 `save_invalid.json`으로 이동 후 백업 검증/사용.
  - [x] 2.4 유효 세이브 존재 여부 체크 API 제공(메인 메뉴 버튼 노출용).
  - [x] 2.5 게임오버/클리어 시 세이브 파일 삭제 API 제공.

- [x] 3.0 런 상태 직렬화/복원 구현
  - [x] 3.1 SaveManager에서 런 상태 수집(스테이지 인덱스, 플레이어 통화, Permanent 모디파이어, 인벤토리/업그레이드, runSeed).
  - [x] 3.2 Item/Upgrade UniqueId 주입을 위한 생성자/팩토리 추가 및 로드 경로 연결.
  - [x] 3.3 플레이어/아이템 Permanent 모디파이어 복원(StatLayer.Permanent만 저장/복원).
  - [x] 3.4 인벤토리 슬롯 복원 및 업그레이드 재적용(UpgradeManager.ApplyUpgrades 사용).
  - [x] 3.5 로드 모드 플래그 추가 후 ItemEffectManager에서 Permanent 효과 필터링.
  - [x] 3.6 로드 직후 `OnCurrencyChanged`/`OnItemChanged` 1회 호출로 Owned/조건부 효과 정합성 맞춤.
  - [x] 3.7 로드 시 `GameManager.Rng = new System.Random(runSeed)` 재시드.

- [ ] 4.0 게임 흐름/메뉴 연동
  - [x] 4.1 `StageManager.StartStage`에서 `OnPlayStart` 직전에 자동 저장 훅 추가.
  - [x] 4.2 `MainMenuManager`에 이어하기 진입 메서드 추가 및 유효 세이브 없으면 숨김 처리.
  - [x] 4.3 `GameManager.HandleGameStart`에서 로드 모드 분기(기본 초기화 우회 후 복원).
  - [x] 4.4 게임오버/클리어 시 세이브 삭제 호출.
  - [ ] 4.5 SaveManager를 managersRoot에 추가하고 이어하기 버튼과 연결(에디터 작업 안내).
  - [x] 4.6 시작하기 클릭 시 유효 세이브 존재하면 경고 모달 표시, 확인 시 세이브 삭제 후 새 런 시작.
  - [x] 4.7 1스테이지에서는 자동 저장을 수행하지 않음.

- [x] 5.0 로컬라이징/모달 연동 및 수동 검증
  - [x] 5.1 `modal` 테이블에 세이브/로드/새 게임 경고 키/문구 추가(ko).
  - [x] 5.2 저장 실패/로드 실패/백업 로드 안내 모달 연동.
  - [x] 5.3 수동 검증 체크리스트 작성(저장/로드/백업/삭제/로그 확인).

- [x] 6.0 WebGL 저장소 동기화
  - [x] 6.1 WebGL 저장 후 `FS.syncfs(false)` 호출 및 실패 로그.
  - [x] 6.2 WebGL 시작 시 `FS.syncfs(true)` 호출 후 메인 메뉴 진입.

### Manual Verification Checklist

- 새 런 시작 → 스테이지 시작 직후 `save.json` 생성 확인 (`Application.persistentDataPath/saves`).
- 이어하기 버튼 노출: 유효한 `save.json` 또는 `save_backup.json`이 있을 때만 표시.
- 이어하기 진입: 메인 메뉴에서 이어하기 클릭 → 로드 성공 시 동일 런 상태로 시작.
- `save.json` 손상(체크섬 불일치) 시:
  - `save_invalid.json` 생성/갱신 확인.
  - 백업 유효하면 백업으로 로드 + `modal.loadBackup.*` 모달 표시.
  - 백업도 실패하면 `modal.loadFailed.*` 모달 표시.
- 저장 실패 시 `modal.saveFailed.*` 모달 표시 + `save_log.txt`에 상세 로그 생성.
- 새 게임 시작 시 유효 세이브 존재하면 `modal.newRunOverwrite.*` 모달 표시.
  - 확인 시 `save.json`/`save_backup.json` 삭제, 실패해도 새 런 시작.
- 게임오버/클리어 시 `save.json`/`save_backup.json`/`save_invalid.json`/`save.tmp` 삭제 확인.
