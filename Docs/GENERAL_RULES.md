# 일반 규칙

이 문서는 모든 Unity 프로젝트에서 공통으로 적용되는 환경/규칙/원칙을 정리한다.

## 문서 범위/분리 기준

- 범용 규칙(모든 Unity 프로젝트 공통)은 이 문서에 기록한다.
- 프로젝트 특화 규칙/구조는 `Docs/GAME_STRUCTURE.md`와 세부 시스템 문서에 기록한다.

## Unity 버전

- Unity 6.2 (6000.2.6f2)

## 코드 컨벤션

- C# 4-스페이스 들여쓰기
- Allman braces
- Unity C# 관례 준수
- 코드/JSON 데이터 모델 네이밍은 camelCase를 사용한다.

## 데이터 네이밍 규칙

- JSON 키는 camelCase를 사용한다.
- JSON 효과 타입/파라미터 문자열도 camelCase를 사용한다.

## JSON 직렬화/역직렬화 규칙(확정)

- `JsonUtility` 사용을 금지한다.
- JSON 직렬화/역직렬화는 `Newtonsoft.Json`으로 통일한다.

## 공용 값 관리 파일(확정)

- 색상 상수는 `Assets/Scripts/Data/Colors.cs`에서 중앙 관리한다.
- `Colors.cs`는 `Primitive -> Semantic` 2단 구조로 관리한다.
- `Primitive`: 프로젝트 톤앤매너(팔레트) 자체를 정의한다.
- `Semantic`: 실제 UI/게임 의미 색상(`TextPrimary`, `StateDanger` 등)을 `Primitive`에 매핑한다.
- 코드에서는 의미 표현이 필요한 경우 `Semantic`만 사용하고, `Primitive`를 직접 사용하지 않는다.
- `Hud` 같은 기능별 불필요 prefix 네이밍을 사용하지 않는다.
- 인게임 수치/확률은 `Assets/StreamingAssets/Data/GameConfig.json`에서 중앙 관리한다.
- `Bootstrap`에서 로드한 뒤 `Assets/Scripts/Data/GameConfigData.cs`를 통해 런타임에서 사용한다.
- Tooltip 표시 지연/배치 같은 UI 표현 수치는 해당 UI 컴포넌트 인스펙터에서 관리한다.
- 위 범주 값의 하드코딩을 금지한다.

## 런타임 오브젝트 생성 원칙(확정)

- 런타임 생성이 필요한 오브젝트는 반드시 Prefab 기반으로 관리한다.
- 코드에서 UI/게임오브젝트 구조를 임의 조립하지 않는다.

## 하드코딩 규칙(확정)

- 하드코딩은 대부분의 상황에서 금지한다.
- 불가피한 경우, 사용처/사유를 사용자에게 먼저 제시하고 허가받은 뒤 사용한다.

## 개발 실행 원칙(확정)

- Think Before Coding: 가정을 명시하고, 불확실하면 질문하고, 혼란스러우면 멈춘다.
- Simplicity First: 요청받지 않은 기능/추상화/에러 처리를 추가하지 않는다.
- Surgical Changes: 관련 없는 코드를 개선하지 않고 요청된 변경만 수행한다.
- Goal-Driven Execution: 작업 요청을 검증 가능한 목표(예: 테스트 통과)로 변환해 실행한다.

## 이벤트/상태 관리 원칙(확정)

- 이벤트는 UI 갱신 용도로만 사용한다.
- 로직 진행/판정/턴 전환을 이벤트 체인으로 구성하지 않는다.
- 게임 로직은 서비스 간 직접 메서드 호출로 처리한다.
- 전역 진입점은 `GameApp.I` 하나만 사용한다.
- 상태값/수치값은 Getter/Setter로 관리한다.
- UI 갱신이 필요한 값은 Setter 내부에서 해당 이벤트를 발행한다.
- 범용 이벤트(`StateChanged`) 대신 값 단위 명시 이벤트를 사용한다.
- UI 구독은 `OnEnable`에서 등록하고 `OnDisable`에서 해제한다.
- 구독/해제는 `IDisposable` 토큰 기반으로 관리한다.

## 앱 스코프 원칙(확정)

- 영속 싱글톤은 `GameApp` 하나만 허용한다.
- `DontDestroyOnLoad` 호출은 `GameApp` 외 금지한다.
- `static Instance` 패턴은 `GameApp` 외 금지한다.
- 앱 전역 서비스는 `AppServices`, 런 단위 상태/로직은 `RunServices`로 분리한다.

## 문서 갱신 규칙

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
