# 게임 구조

이 문서는 **인력 관리 중심 로그라이크** 프로젝트의 단일 메인 스펙이다.

- 마지막 갱신: **2026-02-15**
- Unity 버전: **Unity 6.2 (6000.2.x)**

> 주의: 현재 제공된 자료는 **스크립트(zip)만**이다. 씬/프리팹/YAML 구성은 이 문서에 “요구사항”으로만 명시하며,
> 실제 배치는 프로젝트에서 직접 확인/반영해야 한다.

## 문서 범위/분리

- 이 문서: 게임 전체 구조 + **구현 구조(씬/스코프/런 생명주기/데이터 로딩)**
- 세부 규칙:
  - 모험가: `Docs/ADVENTURER.md`
  - 왕국: `Docs/KINGDOM.md`
  - 임무: `Docs/MISSION.md`
  - 능력 테스트: `Docs/ABILITY_TEST.md`
  - 특성: `Docs/TRAIT.md`
  - 장비: `Docs/EQUIPMENT.md`
  - 시설: `Docs/FACILITY.md`
  - 스킬/소모품: `Docs/SKILL_CONSUMABLE.md`
- 아이디어 백로그: `Docs/GAME_IDEA.md`
- 프로젝트 구조: `Docs/PROJECT_MAP.md`

## 게임 한 줄 정의

- 모험가를 모집/관리해 주기적으로 몰려오는 위기 임무를 해결하고, 마지막 대위기를 돌파하는 **인력 관리 로그라이크**.

## 차별 포인트

- 런/임무 단위를 짧고 가볍게 설계한다.
- 전투 연출보다 **인력 운영(모집/배치/피로/손실/회복)**의 선택을 중심 재미로 둔다.
- 실험과 재시도(로그라이크 루프)를 빠르게 반복할 수 있게 한다.

## 테마

- 중세 다크 판타지

## 승리/패배 조건

- 승리: 마지막 위기(최종 고난도 임무군)를 버티고 해결
- 패배: 왕국 안정도 0

## 핵심 루프

1. 턴 시작

- 신규 임무/위기 갱신
- 면접자(신규 모험가 후보) 유입

2. 인력 운영

- 모험가 모집/해고/장비/시설 운용
- 임무별 인원 배치(인원 제한 준수)

3. 임무 수행

- 임무 내 능력 테스트를 순차 진행
- 실패 시 체력 손실, 사망 리스크 관리
- 필요 시 중도 포기

4. 턴 정산

- 성공 임무 보상 적용
- 기한 초과 임무 실패 효과 적용
- 미배치 모험가 자동 휴식(체력/기력 회복)
- 주기 위기 카운트 진행

5. 반복

- 최종 위기 도달 전까지 1~4 반복

## 플레이 감정 목표

- 우선순위/포기 선택의 재미
- 불확실성 하 전술 선택의 재미
- 높은 리스크를 감당해 높은 보상을 노리는 리스크 관리의 재미

## 시스템 구성

- 모험가
  - 능력치(힘/민첩/지능), 성장치, 레벨, 체력/기력/영웅심, 특성, 장비 귀속
  - 런타임 인력 풀 분리
    - 후보(`RunState.candidates`) / 고용(`RunState.adventurers`) / 공동묘지(`RunState.graveyard`)
- 왕국
  - 안정도 기반 생존 관리
- 임무
  - 기한/인원 제한/다중 능력 테스트/보상/실패 효과/태그
- 능력 테스트
  - 양측 롤 비교로 성공/실패 판정, 실패 리스크 누적
- 시설
  - 장기 운영 보정
- 스킬/소모품
  - 전술 보정 수단(세부 미확정)

---

# 구현 구조

## 목표(1인 인디 기준)

- **전역은 1개(GameApp)만 유지**한다.
- 런 상태/로직은 **순수 C# 서비스(RunServices)**로 묶고, MonoBehaviour 의존을 최소화한다.
- 씬 전환/오브젝트 파괴 순서에서도 **런 시작/종료가 예측 가능**해야 한다.

## 씬 구성(요구사항)

### Bootstrap.scene

- 목적: 앱 초기화(데이터 준비) + AppScope 생성
- 포함 오브젝트
  - `Bootstrap` (초기화/로딩/다음 씬 로드)
  - `managersRoot` (비활성 상태로 시작 권장)
    - `GameApp` (**유일한 DontDestroyOnLoad**) + AppScope 매니저들
  - AppScope UI Canvas(`TooltipCanvas`, `ModalCanvas`, `OptionCanvas`, `FloatingTextCanvas`)는 에디터에서 `GameApp` 하위로 배치해 유지한다.

### GameScene.scene

- 목적: 이번 런(Per-run) 시작/종료 + 게임 플레이
- 포함 오브젝트
  - 런 진입점 **1개만** (권장: `GameSceneInstaller`)
  - 게임 플레이 UI/컨트롤러
- 금지
  - `DontDestroyOnLoad` 호출
  - static Instance 싱글톤

## 부트스트랩(데이터 로딩) 순서(확정)

`Bootstrap.Awake()` 기준으로 아래 순서를 지킨다.

1. `SaCache.InitAsync(...)`

- StreamingAssets → Persistent(플랫폼별) 준비

2. `GameConfigProvider.LoadFromStreamingAssetsAsync()`

- `GameConfigProvider.Current`가 유효해야 런 로직이 정상 동작한다.

3. `StaticDataLoader.LoadAll()`

- `StaticDataLoader.Current`가 유효해야 룰/턴/임무 로직이 정상 동작한다.

4. `managersRoot.SetActive(true)`

- 이 시점에 `GameApp.Awake()`가 실행되며 `DontDestroyOnLoad` + `AppServices` 구축

5. `SaveWebGlSync.SyncFromPersistentAsync()`

- WebGL일 때 저장 동기화

6. `SceneManager.LoadSceneAsync(SceneIds.GameScene)`

## 스코프(수명) 분리

### App Scope (Persistent)

- 수명: 게임 실행 동안 유지
- 소유자: `GameApp` (유일한 싱글톤)
- 예: Tooltip/Modal/Option/Toast/FloatingText, Audio/BGM, Input, GameSpeed, Particles, Save, DevConsole
- App UI 뷰 루트는 런타임 보정 없이 에디터 배치로 고정한다.
- 참조 누락/계층 불일치는 에러 로그로 노출하고 초기화를 중단한다.

### Run Scope (Per-run)

- 수명: “이번 런” 동안만 유지
- 소유자: `GameApp.Run` (`RunServices` 인스턴스)
- 예: `RunState`, 턴 진행, 임무/룰/판정, 수정자/특성 처리
- UI 표시 스칼라 값은 `RunServices` Observable로 노출한다.
  - `gold`, `stability`, `stabilityMax`, `turn`, `barracksCapacity`
  - `candidatesCount`, `adventurersCount`, `missionsCount`
  - `uiRevision`(리스트/복합 UI 리빌드 트리거)
- 현재 `GameScene` 상단 HUD(`TopHud`)는 위 Observable 중 아래 4개만 표시한다.
  - `gold`, `stability/stabilityMax`, `turn`, `barracksCapacity`

### Scene Scope

- 수명: 씬 오브젝트와 동일
- 예: 씬 UI, 연출, 씬 전용 컨트롤러
- `TopHud`는 `GameScene` 전용 Overlay UI이며 `RunCoreStatsBinder`가 Run Observable을 구독해 텍스트를 갱신한다.
- 임무 UI는 `WorldCanvas` 기반 월드 카드(요약/선택)와 오버레이 상세 패널(배치/확정)로 분리한다.
  - 월드 카드 표시 최소 항목: 임무명, 남은 기한, 테스트 진행도(아이콘+난이도+통과 여부), 배치 인원(고정 2), 선택 강조
  - 모험가 배치/원정 시작 확정은 오버레이에서만 처리한다.

## Run 시작/종료 규칙(중요)

- `BeginRun()` / `EndRun()`은 **명시적으로만** 호출한다.
- **Getter/프로퍼티/유틸 함수가 BeginRun을 호출하면 금지** (부작용으로 디버깅 난이도 폭증, 종료 누락/재시작 버그 발생)
- 런 진입점은 **씬에 1개만** 둔다.
  - 권장: `GameSceneInstaller`
  - `GameService`는 “런 API 래퍼(퍼사드)”로만 두고 런 시작 책임을 지지 않는다.
- 런 진입점 컴포넌트가 복수로 배치되어도 안전해야 한다.
  - `GameApp.Run != null`이면 `BeginRun()`을 다시 호출하지 않는다.
  - `EndRun()`은 “내가 시작한 Run 인스턴스”(`ReferenceEquals`)일 때만 호출한다.

## 저장/로드 기준

- 런 저장 데이터 단위는 `RunState` JSON이다.
- 직렬화는 `Newtonsoft.Json`으로 통일한다.

## UI 갱신(이벤트 사용 범위)

- 이벤트/Observable은 **UI 갱신용**으로 제한한다.
- 로직 진행(턴 전환/룰 체인)은 이벤트로 연결하지 않는다(로직 체인 금지).
- UI 구독은 `OnEnable` 등록 / `OnDisable` 해제 + `IDisposable` 토큰으로 통일한다.
- `RunState`는 순수 데이터로 유지하고, UI 갱신은 `RunServices` Observable 구독으로만 처리한다.
- `RunServices`의 public 상태 변경 API 호출 후 `SyncUiBindingsFromRunState`로 Observable 값을 동기화한다.

## 용어 사전(확정)

- 모험가: 플레이어가 관리하는 인력 유닛
- 왕국 안정도: 패배를 결정하는 핵심 생존 자원
- 임무: 턴 단위로 대응하는 위기/기회 콘텐츠
- 능력 테스트: 임무 해결 단계의 판정 단위
- 기한: 임무 실패 판정 시점
- 실패 효과: 기한 초과 시 즉시 적용되는 패널티
- 보상: 임무 성공 시 획득 효과
- 특성: 모험가에 부여되는 긍정/부정 패시브
- 시설: 턴 간 영구적 보정을 주는 운영 자산

