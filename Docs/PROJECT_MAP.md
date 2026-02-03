# PROJECT_MAP

이 문서는 레포의 주요 진입점/구성 요소를 지도로 훑기 위한 개요입니다. 게임 구조 설명은 `Docs/GAME_STRUCTURE.md`를 참고합니다.

## Goal
- Manager/Controller 스크립트의 위치와 책임을 1~2문장으로 요약해, 레포의 큰 지도를 빠르게 파악할 수 있도록 한다.
- 상세한 게임 구조/시스템 설명은 `Docs/GAME_STRUCTURE.md`에서 다룬다.

## Manager & Controller 목록

| 경로 | 역할 |
| --- | --- |
| `Assets/Scripts/GameManager.cs` | 게임 시작/오버/클리어 처리와 저장/로드 분기, 오버레이 표시, RNG 관리. |
| `Assets/Scripts/GameSpeedManager.cs` | 일시정지/배속(Time.timeScale) 제어와 UI 버튼 상태 동기화. |
| `Assets/Scripts/InputManager.cs` | Input System 액션 조회, 이동 입력 벡터 제공, 옵션/디버그/일시정지/배속 토글 중계. |
| `Assets/Scripts/OptionManager.cs` | 옵션 오버레이 표시, 재시작/메인메뉴/종료 모달 호출, BGM 슬라이더 연동. |
| `Assets/Scripts/ResultManager.cs` | 스테이지 결과 패널 및 수입 계산/지급, 아이템별 피해 기록 표시. |
| `Assets/Scripts/Audio/AudioManager.cs` | SFX 테이블/리미터 기반 재생. |
| `Assets/Scripts/Audio/BgmManager.cs` | BGM 재생/볼륨/로우패스(상점 뮤플) 제어. |
| `Assets/Scripts/Data/StaticDataManager.cs` | StreamingAssets JSON을 읽어 Stage/Player/Item/Upgrade/BlockPattern Repository 초기화. |
| `Assets/Scripts/Block/BlockManager.cs` | 블록 스폰 램프/패턴/예산 관리, 활성 블록 관리, 플레이 종료 조건 검사. |
| `Assets/Scripts/Block/BlockController.cs` | 블록 HP/상태/피격 연출 및 낙하 이동 처리. |
| `Assets/Scripts/Beam/BeamController.cs` | 빔 공격의 시각 효과/길이/피해 적용 제어. |
| `Assets/Scripts/Bullet/ProjectileController.cs` | 투사체 이동/충돌/관통/바운스/폭발/수명 처리. |
| `Assets/Scripts/Currency/CurrencyManager.cs` | 플레이어 통화 HUD 갱신, 통화 추가/지출 API. |
| `Assets/Scripts/Damage/DamageManager.cs` | 단일/범위 피해 적용, 피해 텍스트/파괴/VFX/트리거 처리. |
| `Assets/Scripts/Damage/DamageTextManager.cs` | 피해 값에 따른 폰트 크기 산정 및 플로팅 텍스트 호출. |
| `Assets/Scripts/Damage/DamageTrackingManager.cs` | 아이템별 누적 피해 기록 수집(결과 패널용). |
| `Assets/Scripts/Dev/DevCommandManager.cs` | 개발 콘솔 명령 등록/실행(아이템/업그레이드/재화/클리어 등). |
| `Assets/Scripts/Item/ItemManager.cs` | 아이템 인벤토리/트리거/오브젝트 아이템 컨트롤러 관리. |
| `Assets/Scripts/Item/ItemController.cs` | 아이템 공격 주기 제어 및 투사체/빔 발사. |
| `Assets/Scripts/Item/ItemEffectManager.cs` | 아이템 효과 실행(스탯/상태/투사체/통화 등). |
| `Assets/Scripts/Item/ItemOrbitController.cs` | 오브젝트 아이템 궤도 배치/회전 이동. |
| `Assets/Scripts/Mainmenu/MainMenuManager.cs` | 새 게임/이어하기 분기 및 씬 전환. |
| `Assets/Scripts/Particles/ParticleManager.cs` | 파티클 프리팹 매핑 및 블록 파괴/폭발 VFX 재생. |
| `Assets/Scripts/Play/PlayManager.cs` | 플레이 시작/종료 시퀀스(아이템 시작, 블록 스폰, 종료 후 정리). |
| `Assets/Scripts/Player/PlayerManager.cs` | 플레이어 인스턴스 생성/리셋, 스탯 UI 갱신. |
| `Assets/Scripts/Player/PlayerController.cs` | 플레이어 이동 입력 처리 및 플레이 영역 내 이동/리셋. |
| `Assets/Scripts/Save/SaveManager.cs` | 저장 데이터 작성/적용, 스테이지 시작 저장, 로드 후 후처리. |
| `Assets/Scripts/Shop/ShopManager.cs` | 상점 열기/닫기, 상품 롤링, 구매/드래그/리롤 처리. |
| `Assets/Scripts/Shop/ProductController.cs` | 상점 아이템 카드 UI 바인딩/툴팁/선택 처리. |
| `Assets/Scripts/Shop/UpgradeProductController.cs` | 상점 업그레이드 카드 UI 바인딩/툴팁/선택 처리. |
| `Assets/Scripts/Stage/StageManager.cs` | 스테이지 시작/종료, 플레이↔상점 페이즈 전환, 다음 스테이지 진행. |
| `Assets/Scripts/Token/ItemSlotManager.cs` | 아이템 슬롯 UI 구성/드래그/스왑/판매/하이라이트 관리. |
| `Assets/Scripts/Token/ItemSlotController.cs` | 개별 아이템 슬롯 UI 바인딩/드래그/클릭 처리. |
| `Assets/Scripts/Tooltip/TooltipManager.cs` | 툴팁 표시/고정/갱신 및 위치 계산. |
| `Assets/Scripts/UI/Modal/ModalManager.cs` | 확인/정보 모달 생성 및 표시. |
| `Assets/Scripts/UI/FloatingTextManager.cs` | 플로팅 텍스트 풀링/표시. |
| `Assets/Scripts/UI/GhostManager.cs` | 드래그 고스트 아이콘 표시/이동/숨김. |
| `Assets/Scripts/UI/SellOverlayController.cs` | 판매 오버레이 표시 및 드롭 판정. |
| `Assets/Scripts/UI/ToastManager.cs` | 로컬라이즈된 토스트 메시지 표시. |
| `Assets/Scripts/UI/UpgradeInventorySlotController.cs` | 업그레이드 인벤토리 슬롯 드래그/선택 처리. |
| `Assets/Scripts/UI/VirtualJoystickController.cs` | 모바일 가상 조이스틱 입력 처리. |
| `Assets/Scripts/Upgrade/UpgradeInventoryManager.cs` | 업그레이드 인벤토리 관리/선택/판매. |
| `Assets/Scripts/Upgrade/UpgradeManager.cs` | 업그레이드 적용/교체/파손 처리. |

## 주요 플로우 엔트리포인트 / 콜 체인
- 부팅: `Bootstrap.Awake` → `SaCache.InitAsync` → 매니저 루트 활성화/`DontDestroyOnLoad` → `SaveWebGlSync.SyncFromPersistentAsync` → `MainMenuScene` 로드.
- 새 게임: `MainMenuManager.StartGame` → `GameScene` 로드 → `GameManager.HandleGameStart` → `ItemManager.InitializeFromPlayer` → `StageManager.StartRun`.
- 이어하기: `MainMenuManager.ContinueGame` → `SaveManager.BeginLoadMode` → `GameScene` 로드 → 저장 적용 후 `StageManager.StartRunFromIndex`.
- 플레이: `StageManager.StartStage` → `PlayManager.StartPlay` → `ItemManager.BeginPlay` + `BlockManager.BeginSpawnRamp` → 블록 스폰/전투 진행.
- 플레이 종료/상점: 스폰 종료+블록 0 → `PlayManager.FinishPlay` → `StageManager.OnPlayFinished` → `ResultManager.OpenWithIncome` + `ShopManager.Open`.
- 상점 종료: `ShopManager.Close` → `StageManager.OnShopClosed` → 아이템 트리거/업그레이드 파손 처리 → 다음 스테이지 또는 클리어.
- 게임 오버: `BlockDestroyZone.OnTriggerEnter2D` → `GameManager.HandleGameOver`.
