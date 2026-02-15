# 일반 규칙

이 문서는 모든 Unity 프로젝트에서 공통으로 적용되는 환경/규칙/원칙을 정리한다.

- 마지막 갱신: **2026-02-15**

## 문서 범위/분리 기준

- 범용 규칙(모든 Unity 프로젝트 공통)은 이 문서에 기록한다.
- 프로젝트 특화 구조/설정/데이터는 `Docs/GAME_STRUCTURE.md` 및 세부 시스템 문서에 기록한다.

## Unity 버전

- Unity 6.2 (6000.2.x)

## 코드 컨벤션

- C# 4-스페이스 들여쓰기
- Allman braces
- Unity C# 관례 준수
- 코드/JSON 데이터 모델 네이밍은 camelCase를 사용한다.

## 데이터 네이밍 규칙

- JSON 키는 camelCase를 사용한다.
- JSON 효과 타입/파라미터 문자열도 camelCase를 사용한다.

## JSON 직렬화/역직렬화 규칙

- `JsonUtility` 사용 금지
- JSON 직렬화/역직렬화는 `Newtonsoft.Json`으로 통일

## 공용 값 관리

- 색상 상수는 `Assets/Scripts/Data/Colors.cs`에서 중앙 관리한다.
- `Colors.cs`는 `Primitive -> Semantic` 2단 구조로 관리한다.
  - `Primitive`: 프로젝트 톤앤매너(팔레트)
  - `Semantic`: 실제 UI/게임 의미 색상(`TextPrimary`, `StateDanger` 등)
- 코드에서는 의미 표현이 필요한 경우 `Semantic`만 사용하고, `Primitive`를 직접 사용하지 않는다.
- 인게임 수치/확률은 `Assets/StreamingAssets/Data/GameConfig.json`에서 중앙 관리한다.
  - 로드는 Bootstrap 단계에서 수행한다.
- Tooltip 표시 지연/배치 같은 UI 표현 수치는 해당 UI 컴포넌트 인스펙터에서 관리한다.
- 위 범주 값의 하드코딩은 금지한다.

## 런타임 오브젝트 생성 원칙

- 런타임 생성이 필요한 오브젝트는 반드시 Prefab 기반으로 관리한다.
- 코드에서 UI/게임오브젝트 구조를 임의 조립하지 않는다.

## 하드코딩 규칙

- 하드코딩은 대부분의 상황에서 금지한다.
- 불가피한 경우:
  - 사용처/사유를 문서 또는 주석으로 명시
  - 가능하면 Config/데이터로 이전 계획 포함

## 개발 실행 원칙

- Think Before Coding: 가정을 명시하고, 불확실하면 질문하고, 혼란스러우면 멈춘다.
- Simplicity First: 요청받지 않은 기능/추상화/에러 처리를 추가하지 않는다.
- Surgical Changes: 관련 없는 코드를 개선하지 않고 요청된 변경만 수행한다.
- Goal-Driven Execution: 작업 요청을 검증 가능한 목표(예: 컴파일/테스트 통과)로 변환해 실행한다.

---

# 구조/스코프 원칙 (1인 인디 최적화)

## 싱글톤 / 전역 접근

- **영속 싱글톤은 `GameApp` 하나만 허용**한다.
- `DontDestroyOnLoad` 호출은 `GameApp` 외 금지한다.
- `static Instance` 패턴은 `GameApp` 외 금지한다.

> 허용되는 static:
>
> - 순수 함수 유틸
> - 상태를 갖더라도 “부트스트랩에서만 로드되고, SubsystemRegistration에서 Reset 되는” 데이터 캐시(예: Config/StaticData 로더)

## 스코프 분리

- App Scope(영속): `GameApp` + `AppServices`
  - UI/Audio/Input/Save 등 앱 전역
- Run Scope(런 단위): `RunServices`
  - `RunState` + 런 로직
- Scene Scope(씬 단위): 씬 오브젝트

## 런 생명주기 규칙

- 런 시작/종료는 `GameApp.BeginRun(...)` / `GameApp.EndRun()`으로만 한다.
- **Getter/Property/헬퍼가 BeginRun을 호출하는 구조 금지**
  - “읽기”가 “상태 변경”을 일으키면 디버깅이 불가능해진다.
- 런 진입점은 씬에 1개만 둔다.
  - (권장) `GameSceneInstaller` 같은 Installer 계열 컴포넌트
- 런 진입점 코드는 idempotent 해야 한다.
  - `GameApp.Run != null`이면 `BeginRun(...)`을 다시 호출하지 않는다.
  - `EndRun()`은 “내가 시작한 Run 인스턴스”인지 `ReferenceEquals`로 확인 후 호출한다.

## Domain Reload Off(에디터 빠른 플레이) 안전 규칙

- static 상태를 가진 클래스는 반드시 아래 중 하나를 만족해야 한다.
  1. `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)`에서 ResetStatic()을 제공한다.
  2. Reset이 불가능하면 static 상태를 제거한다.

(예: `GameApp`가 I를 Reset하는 것처럼, `SaCache`, `GameConfigProvider`, `StaticDataLoader` 같은 정적 캐시도 Reset 필요)

## App UI 영속 규칙

- AppScope UI 매니저가 참조하는 뷰/캔버스 루트는 반드시 `GameApp` 하위여야 한다.
- AppScope UI 뷰/캔버스 배치는 **에디터에서 미리 고정**한다(런타임 자동 편입 금지).
- 영속 매니저가 SceneScope 뷰를 직접 참조한 채로 유지되는 구조를 금지한다.

## Fallback/Ensure 금지 규칙

- `Fallback`, `Ensure` 패턴을 기본적으로 금지한다.
  - 예: 런타임에서 누락 참조를 자동 탐색/자동 생성/자동 재할당하는 로직
- 에디터에서 할당 가능한 참조는 코드가 아니라 **에디터 직렬화 참조**로 해결한다.
- 구성 오류를 코드가 몰래 보정하지 않는다.
  - 누락/불일치가 있으면 `Debug.LogError`로 명확히 노출하고 초기화를 중단한다.
- 불가피하게 Fallback/Ensure가 필요한 경우, 사용자 승인 후에만 도입한다.

---

# 이벤트/상태 관리 원칙

- 이벤트는 **UI 갱신 용도로만** 사용한다.
- 로직 진행/판정/턴 전환을 이벤트 체인으로 구성하지 않는다.
- 게임 로직은 서비스 간 직접 메서드 호출로 처리한다.

## UI 구독/해제 규칙

- UI 구독은 `OnEnable`에서 등록하고 `OnDisable`에서 해제한다.
- 구독/해제는 `IDisposable` 토큰 기반으로 관리한다.
  - 예: `DisposableBag`, `DisposableToken`, `EventSubscription`
  - 기본 패턴은 `DisposableBag`에 등록하고 `OnDisable`에서 `Clear()` 호출이다.

## 이벤트 설계 규칙

- 범용 이벤트(`StateChanged`) 대신 값 단위 명시 이벤트를 사용한다.
- **static event 버스는 금지**한다.
  - (예외가 필요하면) SubsystemRegistration에서 이벤트를 강제로 null로 Reset해야 하며, 사용처도 최소화한다.

## RunState-UI 바인딩 규칙

- `RunState`는 저장/로드 스냅샷 역할의 **순수 데이터 타입**으로 유지한다.
  - `RunState` 내부에 이벤트/Observable/델리게이트/Unity 오브젝트 참조를 넣지 않는다.
- UI 표시용 값은 `RunServices`가 `ObservableValue`로 노출한다.
  - 최소 노출: `gold`, `stability`, `stabilityMax`, `turn`, `barracksCapacity`, `candidatesCount`, `adventurersCount`, `missionsCount`, `uiRevision`
- `RunServices`의 public 상태 변경 API는 호출 경계에서 `SyncUiBindingsFromRunState`를 통해 Observable 값을 동기화한다.
- UI는 Observable을 `OnEnable`에서 구독하고 `OnDisable`에서 해지한다.
  - `IDisposable` 토큰은 `DisposableBag`로 관리한다.
- UI는 Observable에 값을 쓰지 않는다.
  - UI는 `Value` 읽기/`Subscribe`만 사용한다.
- `EndRun`/`RunServices.Dispose` 시에는 Observable 리스너를 정리해 과거 런 인스턴스 참조가 남지 않게 방어한다.
- 임무 배치 드래프트 같은 임시 편집 상태는 UI 로컬 상태로 관리한다.
  - 확정 전에는 `RunState`를 직접 수정하지 않는다.
  - 확정 반영은 `RunServices` public API 경계에서 원자적으로 처리한다.

---

# 문서 갱신 규칙

- 코드/설계 변경 시 관련 문서를 함께 갱신한다.
- 사용자 지시가 없으면 의사결정 로그를 별도 기록하지 않는다.

## 문서 맵

- 핵심 스펙: `Docs/GAME_STRUCTURE.md`
- 레포 구조: `Docs/PROJECT_MAP.md`
- 아이디어/백로그: `Docs/GAME_IDEA.md`
- 시스템 세부:
  - `Docs/ADVENTURER.md`
  - `Docs/KINGDOM.md`
  - `Docs/MISSION.md`
  - `Docs/ABILITY_TEST.md`
  - `Docs/TRAIT.md`
  - `Docs/EQUIPMENT.md`
  - `Docs/FACILITY.md`
  - `Docs/SKILL_CONSUMABLE.md`
