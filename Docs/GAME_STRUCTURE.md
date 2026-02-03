# 게임 구조

이 문서는 ProjectP의 게임 컨셉, 핵심 루프, 코드/데이터 구조, 레포 탐색 규칙을 정리합니다.

## 프로덕트 요약 (한 줄)

- 핀볼 기반 로그라이크

## 게임 컨셉

- 핀볼 보드에서 볼과 핀의 상호작용을 통해 점수를 획득하고, 라운드/스테이지 진행과 상점 구매로 성장하는 구조

## 핵심 루프

- 매 스테이지마다 3번의 라운드를 제공
- 각 라운드마다 보드에 볼들을 소환
- 볼들은 핀들과 만나면 점수를 얻음
- 스테이지 종료 시 목표 점수에 도달하지 못하면 게임 오버
- 매 라운드 종료마다 상점이 열려 플레이어는 핀과 볼을 구매 가능

## 전제

- Unity 버전/코드 컨벤션/공통 규칙은 `Docs/GENERAL_RULES.md`를 참고
- 기능 구현은 `Assets/Scripts`와 `Assets/StreamingAssets` 구조에 맞춰 진행
- 데이터는 주로 `Assets/StreamingAssets`의 JSON을 통해 로드

## 레포 탐색 규칙(고신호 우선순위)

- 우선 확인 대상: `*Manager.cs`, `*Controller.cs`
- 추가 정보가 필요할 때만, 위 파일들이 참조하는 DTO/Instance/유틸/데이터 로더를 따라감
- `Assets/Scripts` 아래를 우선으로 보고, 데이터 정의/밸런스는 `Assets/StreamingAssets`를 확인

## 핵심 스크립트 위치(Manager/Controller)

| 위치 | 파일 |
| --- | --- |
| `Assets/Scripts` | `GameManager.cs`, `GameSpeedManager.cs`, `InputManager.cs`, `OptionManager.cs`, `ResultManager.cs` |
| `Assets/Scripts/Audio` | `AudioManager.cs`, `BgmManager.cs` |
| `Assets/Scripts/Beam` | `BeamController.cs` |
| `Assets/Scripts/Block` | `BlockController.cs`, `BlockManager.cs` |
| `Assets/Scripts/Bullet` | `ProjectileController.cs` |
| `Assets/Scripts/Currency` | `CurrencyManager.cs` |
| `Assets/Scripts/Damage` | `DamageManager.cs`, `DamageTextManager.cs`, `DamageTrackingManager.cs` |
| `Assets/Scripts/Data` | `StaticDataManager.cs` |
| `Assets/Scripts/Dev` | `DevCommandManager.cs` |
| `Assets/Scripts/Item` | `ItemController.cs`, `ItemEffectManager.cs`, `ItemManager.cs`, `ItemOrbitController.cs` |
| `Assets/Scripts/Mainmenu` | `MainMenuManager.cs` |
| `Assets/Scripts/Particles` | `ParticleManager.cs` |
| `Assets/Scripts/Play` | `PlayManager.cs` |
| `Assets/Scripts/Player` | `PlayerController.cs`, `PlayerManager.cs` |
| `Assets/Scripts/Save` | `SaveManager.cs` |
| `Assets/Scripts/Shop` | `ProductController.cs`, `ShopManager.cs`, `UpgradeProductController.cs` |
| `Assets/Scripts/Stage` | `StageManager.cs` |
| `Assets/Scripts/Token` | `ItemSlotController.cs`, `ItemSlotManager.cs` |
| `Assets/Scripts/Tooltip` | `TooltipManager.cs` |
| `Assets/Scripts/UI` | `FloatingTextManager.cs`, `GhostManager.cs`, `SellOverlayController.cs`, `ToastManager.cs`, `UpgradeInventorySlotController.cs`, `VirtualJoystickController.cs` |
| `Assets/Scripts/UI/Modal` | `ModalManager.cs` |
| `Assets/Scripts/Upgrade` | `UpgradeInventoryManager.cs`, `UpgradeManager.cs` |

## 관련 문서

- 디자인 규칙: `DESIGN_RULE.md`
