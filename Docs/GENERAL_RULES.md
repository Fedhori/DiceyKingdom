# 일반 규칙

이 문서는 **1인 유니티 인디 게임 개발** 환경에서, 모든 Unity 프로젝트에 공통으로 적용되는 **환경/규칙/원칙**을 정리한다.  
또한 이 문서는 **CODEX CLI 작업 컨텍스트**로 사용된다(규칙 위반은 “버그”로 간주).

- 마지막 갱신: **2026-02-15**
- Unity 버전: **Unity 6.2 (6000.2.6f2)**

## 문서 범위/분리 기준

- 범용 규칙(모든 Unity 프로젝트 공통)은 이 문서에 기록한다.
- 프로젝트 특화 스펙/구조는 `GAME_STRUCTURE.md`와 세부 시스템 문서에 기록한다.

## 1인 개발 최적화 원칙(고정)

- **복잡도 상한을 의도적으로 낮춘다.** (프레임워크/추상화 추가는 “속도”가 아니라 “부채”가 되기 쉬움)
- **디버깅 가능한 구조를 우선한다.** (전역/암묵 의존/부작용 getter 금지)
- **명시적 생명주기(스코프) 분리**를 우선한다. (App vs Run vs Scene)
- **빠른 반복**을 위해, 변경 범위는 항상 최소화한다. (Surgical Changes)

---

## 코드 컨벤션

- C# 4-스페이스 들여쓰기
- Allman braces
- Unity C# 관례 준수
- 네이밍(기본):
  - 타입/메서드/프로퍼티: PascalCase
  - 지역 변수: camelCase
  - private 필드: \_camelCase
- 데이터/JSON 네이밍:
  - JSON 키: camelCase
  - JSON 효과 타입/파라미터 문자열: camelCase
- **기존 코드베이스의 네이밍을 임의로 대량 변경 금지** (요청 없는 rename/refactor 금지)

## JSON 직렬화/역직렬화 규칙(확정)

- `JsonUtility` 사용 금지
- JSON 직렬화/역직렬화는 `Newtonsoft.Json`으로 통일

## 공용 값 관리 파일(확정)

- 색상 상수는 `Assets/Scripts/Data/Colors.cs`에서 중앙 관리한다.
- `Colors.cs`는 `Primitive -> Semantic` 2단 구조로 관리한다.
  - `Primitive`: 프로젝트 톤앤매너(팔레트) 자체를 정의한다.
  - `Semantic`: 실제 UI/게임 의미 색상(`TextPrimary`, `StateDanger` 등)을 `Primitive`에 매핑한다.
- 코드에서는 의미 표현이 필요한 경우 `Semantic`만 사용하고, `Primitive`를 직접 사용하지 않는다.
- `Hud` 같은 기능별 불필요 prefix 네이밍을 사용하지 않는다.
- 인게임 수치/확률은 `Assets/StreamingAssets/Data/GameConfig.json`에서 중앙 관리한다.
  - `Bootstrap`에서 로드한 뒤 `Assets/Scripts/Data/GameConfigData.cs`를 통해 런타임에서 사용한다.
- Tooltip 표시 지연/배치 같은 **UI 표현 수치**는 해당 UI 컴포넌트 인스펙터에서 관리한다.
- 위 범주 값의 하드코딩을 금지한다.

## 런타임 오브젝트 생성 원칙(확정)

- 런타임 생성이 필요한 오브젝트는 반드시 Prefab 기반으로 관리한다.
- 코드에서 UI/게임오브젝트 구조를 임의 조립하지 않는다.

## 하드코딩 규칙(확정)

- 하드코딩은 대부분의 상황에서 금지한다.
- 불가피한 경우, 사용처/사유를 사용자에게 먼저 제시하고 허가받은 뒤 사용한다.

---

## 개발 실행 원칙(확정)

- Think Before Coding: 가정을 명시하고, 불확실하면 질문하고, 혼란스러우면 멈춘다.
- Simplicity First: 요청받지 않은 기능/추상화/에러 처리를 추가하지 않는다.
- Surgical Changes: 관련 없는 코드를 개선하지 않고 요청된 변경만 수행한다.
- Goal-Driven Execution: 작업 요청을 검증 가능한 목표(예: 컴파일/플레이 모드 동작)로 변환해 실행한다.
- 문서 동기화: 코드/설계 변경 시 관련 문서를 함께 갱신한다.

## CODEX CLI 작업 규칙(고정)

- **이 문서와 `GAME_STRUCTURE.md`를 우선 읽고** 작업한다.
- 요청 범위 밖의 리팩터링(정리/최적화/rename/폴더 이동) 금지.
- “추상화 추가”는 기본적으로 금지(요청/필요가 명확할 때만).
- 변경으로 인해 기존 API가 깨지면:
  - 1. 임시 shim으로 컴파일을 먼저 복구
  - 2. 호출부를 단계적으로 옮긴 후
  - 3. shim 제거(요청이 있을 때만)

---

## 앱/런/씬 스코프 원칙(확정)

### 전역 진입점(고정)

- 전역 진입점은 **`GameApp.I` 하나만** 사용한다.
- `static Instance` 패턴은 `GameApp` 외 금지한다.
- `DontDestroyOnLoad` 호출은 `GameApp` 외 금지한다.

### 스코프 정의(고정)

- **App Scope (Persistent)**
  - 게임 실행 동안 유지되는 서비스/시스템
  - 예: 툴팁/모달 같은 UI 시스템, 설정/세이브, 오디오 등
- **Run Scope (Per-run)**
  - “이번 런” 동안만 유지되는 상태/로직
  - 예: RunState, 턴/룰/판정, 임무/모험가 트리거, 수정자 적용 등
- **Scene Scope**
  - 씬에만 존재하는 MonoBehaviour/오브젝트
  - 씬 언로드 시 파괴되며, RunScope/MonoBehaviour 싱글톤을 만들지 않는다.

### 런 생명주기(고정)

- 런 생성/종료는 **명시적으로만** 수행한다.
  - `GameApp.BeginRun(sceneRefs, ...)`
  - `GameApp.EndRun()`
- 런 시작은 **EntryPoint 1곳에서만** 호출한다.
  - getter/유틸 함수/프로퍼티 접근에서 런을 “암묵적으로 생성” 금지
- EndRun 이후에는 RunScope가 null이 되는 것이 정상이며, 그 상태를 전제해 null-safe 하게 작성한다.

### 도메인 리로드(Enter Play Mode 옵션) 대응

- static 상태는 최소화한다.
- `GameApp.I` 같은 static 진입점은 필요 시 `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)`으로 reset 한다.
- static event / static 캐시 사용은 기본 금지(필요하면 reset 규약이 포함되어야 함).

### 시드/결정론(프로젝트 공통 권장)

- 시뮬레이션/룰 판정은 **RunScope RNG**(예: `System.Random`)를 사용한다.
- `UnityEngine.Random`(전역)은 연출/VFX 등 결정론이 필요 없는 영역에만 사용한다.
- “고정 시드”는 Run 시작 파라미터로 전달하고 RunScope가 소유한다.

---

## 이벤트/상태 관리 원칙(확정)

- 이벤트는 **UI 갱신 용도로만** 사용한다.
- 로직 진행/판정/턴 전환을 이벤트 체인으로 구성하지 않는다.
- 게임 로직은 서비스 간 직접 메서드 호출로 처리한다.

### 상태 표현(고정)

- 상태값/수치값은 Getter/Setter 또는 `ObservableValue<T>` 같은 래핑으로 관리한다.
- UI 갱신이 필요한 값은 Setter(또는 Value set) 내부에서 해당 이벤트를 발행한다.
- 범용 이벤트(`StateChanged`) 대신 값 단위 명시 이벤트를 사용한다.

### 구독/해제(고정)

- UI 구독은 `OnEnable`에서 등록하고 `OnDisable`에서 해제한다.
- `Start/OnDestroy`로 UI 구독/해제하는 패턴 금지(비활성 상태에서도 이벤트 수신/중복 구독 위험).
- 구독/해제는 `IDisposable` 토큰 기반으로 관리한다.
  - Subscribe는 **중복 구독 방지**(remove 후 add) 규칙을 포함한다.
  - Subscribe는 기본적으로 **현재 값 1회 push**(초기 UI 동기화)를 지원한다.

### 부작용 금지(고정)

- getter/프로퍼티 접근은 상태를 변경하지 않는다.
- “조회 함수”에서 `BeginRun`, `Init`, `Load` 같은 생성/초기화 사이드이펙트 호출 금지.

---

## 문서 갱신 규칙

- 코드/설계 변경 시 관련 문서를 함께 갱신한다.
- 사용자 지시가 없으면 의사결정 로그를 별도 기록하지 않는다.

## 문서 맵

- 핵심 스펙: `GAME_STRUCTURE.md`
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
