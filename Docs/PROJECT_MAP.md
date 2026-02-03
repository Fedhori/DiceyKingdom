# PROJECT_MAP

이 문서는 레포의 주요 진입점/구성 요소를 지도로 훑기 위한 개요입니다. 게임 구조 설명은 `Docs/GAME_STRUCTURE.md`를 참고합니다.

## Goal
- Manager/Controller 스크립트의 위치와 책임을 1~2문장으로 요약해, 레포의 큰 지도를 빠르게 파악할 수 있도록 한다.
- 상세한 게임 구조/시스템 설명은 `Docs/GAME_STRUCTURE.md`에서 다룬다.

## Manager & Controller 목록
- `Assets/Scripts/GameManager.cs`: 싱글톤으로 게임 시작을 FlowManager에 위임하고, 게임 오버/클리어 시 오디오 재생 및 오버레이 활성화, 재시작 처리.
- `Assets/Scripts/GameSpeedManager.cs`: 일시정지/배속 상태를 관리하며 Time.timeScale 적용과 UI 아이콘 상태를 동기화.
- `Assets/Scripts/InputManager.cs`: PlayerInput 액션을 찾아 슬롯 선택 콜백을 바인딩하고 옵션/디버그/일시정지/배속 토글 입력을 중계.
- `Assets/Scripts/OptionManager.cs`: 옵션 오버레이 토글과 씬에 따른 버튼 활성화, 재시작/메인메뉴/종료 모달 호출 및 실행.
- `Assets/Scripts/StatisticsManager.cs`: 라운드 종료 후 보상 오버레이 표시, 획득 점수/재화/볼 수치 갱신 후 닫힘 시 FlowManager로 복귀 신호 전송.
- `Assets/Scripts/Audio/AudioManager.cs`: SFX 테이블과 제한(딜레이/동시 재생) 적용, 챕터별 BGM 세트 전환과 페이드, 경고/보스/상점 테마 제어.
- `Assets/Scripts/Ball/BallEffectManager.cs`: BallEffectDto에 따라 자가/타볼/점수 효과를 적용하고 StatModifier를 추가.
- `Assets/Scripts/Ball/BallManager.cs`: 라운드 스폰 시퀀스 준비·코루틴 스폰, 활성 볼 수 카운트, 모든 볼 소멸 감지 후 StageManager에 종료 통보.
- `Assets/Scripts/Currency/CurrencyManager.cs`: PlayerInstance의 통화를 구독해 HUD를 갱신하고 통화 추가/차감/지불 시도를 제공.
- `Assets/Scripts/Data/StaticDataManager.cs`: Awake에서 StreamingAssets 캐시를 통해 Stages/Balls/Pins/Players JSON을 읽어 리포지토리를 초기화.
- `Assets/Scripts/Dev/DevCommandManager.cs`: 디버그 콘솔 토글과 입력 처리, 핀/볼 스폰·교체 명령을 등록하여 실행.
- `Assets/Scripts/Flow/FlowManager.cs`: 런/스테이지/라운드 진행 상태를 추적하며 라운드 시작→보상→상점→다음 라운드/스테이지 전환을 총괄.
- `Assets/Scripts/Mainmenu/MainMenuManager.cs`: 메인 메뉴에서 핀 격자 생성 후 주기적으로 볼을 스폰하며 StartGame으로 GameScene을 로드.
- `Assets/Scripts/Pin/PinDragManager.cs`: 핀 드래그 시작/업데이트/종료를 관리해 위치 이동, 하이라이트, 콜라이더·알파 토글을 처리.
- `Assets/Scripts/Pin/PinEffectManager.cs`: 핀 효과를 플레이어/핀/볼에 적용하거나 기본 핀 교체, 통화 지급, 부활 등을 실행하며 볼 파괴 트리거를 전체 핀에 브로드캐스트.
- `Assets/Scripts/Pin/PinManager.cs`: 핀 격자 생성과 기본 핀 스폰, 좌표 계산, 핀 등록/교체/스왑/판매 처리 및 기본 핀 슬롯 조회.
- `Assets/Scripts/Player/PlayerManager.cs`: 플레이어 인스턴스 생성과 덱 UI 빌드, 스탯 HUD 주기 업데이트, 라운드 시작 시 플레이어 리셋.
- `Assets/Scripts/Score/ScoreManager.cs`: 볼-핀/볼-환경 충돌 시 점수를 계산해 누적하고, 크리티컬 색상/크기 조정된 플로팅 텍스트를 표시.
- `Assets/Scripts/Shop/ShopManager.cs`: 스테이지별 상점 열기/닫기, 판매 가능 핀·볼 롤링, 통화 검증 후 구매·배치, 드래그 UI와 리롤 비용 관리.
- `Assets/Scripts/Stage/StageManager.cs`: 스테이지 HUD와 라운드 시작 버튼, 라운드 시작 시 리셋/스폰 준비, 모든 볼 소멸 시 FlowManager에 종료 알림.
- `Assets/Scripts/Tooltip/TooltipManager.cs`: 툴팁 데이터와 위치 앵커를 받아 지연 후 캔버스 상에 표시하고 씬 전환 시 정리.
- `Assets/Scripts/UI/FloatingTextManager.cs`: 플로팅 텍스트 풀링/생성 및 월드 좌표를 캔버스 위치로 변환해 표시.
- `Assets/Scripts/UI/ToastManager.cs`: 로컬라이즈된 토스트 메시지를 생성하고 대기 후 페이드아웃시키는 코루틴 관리.
- `Assets/Scripts/UI/Modal/ModalManager.cs`: 확인/정보 모달 프리팹을 인스턴스화해 텍스트 설정, 표시/숨김, 콜백 연결을 담당.
- `Assets/Scripts/Ball/BallController.cs`: 볼 초기화와 스프라이트 세팅, 속도/사이즈 보정, 핀/볼 충돌 시 트리거 및 점수 계산 호출, 비활성화 시 정리.
- `Assets/Scripts/Ball/BallMainMenuController.cs`: 메인 메뉴용 볼 스프라이트를 랜덤 선택해 표시.
- `Assets/Scripts/Pin/PinController.cs`: 핀 인스턴스 초기화와 HUD 업데이트, 히트 연출, 드래그·클릭 처리(상점 교체/셀), 선택 하이라이트 동기화.
- `Assets/Scripts/Pin/PinMainMenuController.cs`: 메인 메뉴용 핀 스프라이트 랜덤 선택과 충돌 시 크기 연출.

## 주요 플로우 엔트리포인트 / 콜 체인
- 게임 시작/스테이지 초기화: `GameManager.Start` → `HandleGameStart` → `FlowManager.StartRun`(StageRepository 검증) → `FlowManager.StartStage` → `StageManager.BindStage` + `StageManager.ShowRoundStartButton`(CurrentPhase=None).
- 라운드 시작·스폰 준비: `StageManager.OnRoundStartButtonClicked` → `FlowManager.OnRoundStartRequested`(CurrentPhase=Round) → `StageManager.StartRound`(Score 이전값 저장, Player/Pin/Ball 리셋) → `PlayerManager.Current.BallDeck.BuildSpawnSequence` → `BallManager.PrepareSpawnSequence` → `BallManager.StartSpawning`.
- 볼 스폰/종료 감시: `BallManager.SpawnRoutine` → 주기적으로 `BallFactory.SpawnBall` → `BallController.Initialize` 시 `BallManager.RegisterBall`; 스폰 완료 후 또는 `BallController.OnDisable`에서 `BallManager.UnregisterBall`, 모든 스폰 소진+활성 0이면 `StageManager.NotifyAllBallsDestroyed` → `FlowManager.OnRoundFinished`.
- 볼 희귀도 패널: `BallRarityPanel`이 플레이어 희귀도/성장율로 프리팹을 인스턴스해 색상·배율·확률을 표시(컨테이너/프리팹 인스펙터 연결 필요).
- 점수 처리: `BallController.OnCollisionEnter2D`에서 핀 충돌 시 `PinInstance.OnHitByBall` → `ScoreManager.CalculateScore` → `ScoreManager.AddScore` → `FloatingTextManager.ShowText` 및 TotalScore 갱신; 이 과정에서 `PinInstance.HandleTrigger`/`PinEffectManager.ApplyEffect`와 `BallInstance.HandleTrigger`/`BallEffectManager.ApplyEffect`가 추가 효과(통화/스탯/속도 등)를 적용.
- 보상→상점→스테이지 진행: `FlowManager.OnRoundFinished`가 모든 핀에 `HandleRoundFinished` 전달 후 마지막 라운드+점수 미달이면 `GameManager.HandleGameOver`; 클리어 또는 중간 라운드는 `StatisticsManager.Open(isStageClear)` → 닫기 시 `StatisticsManager.Close` → `FlowManager.OnRewardClosed` → `ShopManager.Open`; 상점 `ShopManager.Close` → `FlowManager.OnShopClosed`: 마지막 라운드였다면 다음 스테이지 존재 여부에 따라 `FlowManager.StartStage` 또는 `GameManager.HandleGameClear`, 아니면 라운드 인덱스 증가 후 `StageManager.UpdateRound`/`ShowRoundStartButton`으로 대기.
