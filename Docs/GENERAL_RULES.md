# 일반 규칙
> 역할: 모든 Unity 프로젝트에서 공통으로 지켜야 할 개발 규칙/원칙의 기준 문서입니다.

이 문서는 모든 Unity 프로젝트에서 공통으로 적용되는 환경/규칙/원칙을 정리한다.

- 마지막 갱신: **2026-02-22**

## 문서 범위/분리 기준

- 범용 규칙(모든 Unity 프로젝트 공통)은 이 문서에 기록한다.
- 현재 프로젝트의 확정 기획/구조/실행 흐름은 `Docs/GAME_STRUCTURE.md`에 기록한다.
- 레포 구조/경로 안내는 `Docs/PROJECT_MAP.md`에 기록한다.

## Unity 버전

- Unity 6.2 (6000.2.x)

## 입력 시스템 규칙

- 입력 처리는 `Input System` 패키지(`UnityEngine.InputSystem`)만 사용한다.
- `UnityEngine.Input` API(`Input.GetKeyDown`, `Input.GetAxis`, `Input.GetButton` 등)는 사용 금지한다.

## 코드 컨벤션

- C# 4-스페이스 들여쓰기
- Allman braces
- Unity C# 관례 준수
- 코드/JSON 데이터 모델 네이밍은 camelCase를 사용한다.

## JSON 직렬화/역직렬화 규칙

- `JsonUtility` 사용 금지
- JSON 직렬화/역직렬화는 `Newtonsoft.Json`으로 통일

## 난수(Random) 규칙

- `UnityEngine.Random` 사용 금지
- 난수 생성은 `System.Random`으로 통일
- 테스트/디버깅 재현을 위해 seed 주입 가능한 구조를 우선한다.

## 공용 값 관리

- 색상 상수는 `Assets/Scripts/Data/Colors.cs`에서 중앙 관리한다.
- 런타임 공용 설정값은 `Assets/StreamingAssets/Data/GameConfig.json`에서 관리한다.
- 하드코딩은 금지하고, 필요 시 Config 또는 직렬화 필드로 이동한다.

## 런타임 오브젝트 생성 원칙

- 런타임 생성이 필요한 오브젝트는 Prefab 기반으로 관리한다.
- 에디터에서 직렬화로 해결 가능한 참조는 런타임 탐색(Fallback/Ensure)으로 보정하지 않는다.
- 구성 오류는 `Debug.LogError`로 즉시 노출하고 초기화를 중단한다.

## 앱 구조/수명 규칙

- 영속 싱글톤은 `GameApp` 하나만 허용한다.
- `DontDestroyOnLoad`는 `GameApp` 외 사용하지 않는다.
- 런 시작/종료는 `GameApp.BeginRun(...)` / `GameApp.EndRun()`으로만 처리한다.
- 런 진입점(`GameSceneInstaller` 또는 동등 컴포넌트)은 씬당 1개를 원칙으로 한다.
- static 상태를 가진 클래스는 `SubsystemRegistration`에서 ResetStatic을 제공한다.

## 이벤트/상태 관리 원칙

- 이벤트/Observable은 UI 갱신 통보 용도로만 사용한다.
- 로직 전개(턴 진행, 판정 체인)는 이벤트 체인 대신 명시적 메서드 호출로 연결한다.
- UI 구독은 `OnEnable` 등록 / `OnDisable` 해제 + `DisposableBag` 패턴을 따른다.

## 개발 실행 원칙

- Think Before Coding: 가정 명시, 불확실하면 질문, 혼란스러우면 중단 후 확인
- Simplicity First: 요청받지 않은 기능/추상화 추가 금지
- Surgical Changes: 요청 범위 밖 리팩터링 금지
- Goal-Driven Execution: 작업을 검증 가능한 목표(컴파일/테스트/동작 확인)로 전환

## 데이터 보정/검증 원칙

- 조용한 수정(silent fix)은 금지한다.
- 자동 보정이 필요한 경우, 최소 `Debug.LogWarning`으로 보정 사실과 대상 필드를 남긴다.
- 규칙 위반 데이터는 숨기지 말고 검증 단계에서 실패로 처리한다.

## 문서/커뮤니케이션 표현 원칙

- 문서와 피드백은 **가독성 좋고 이해하기 쉬운 표현**을 우선한다.
- 어려운 용어를 써야 할 때는 같은 문단에서 바로 쉬운 말로 풀어쓴다.
- 모호한 표현(예: “적당히”, “대충”) 대신, 구체적인 동작/필드/조건을 적는다.

예시:
- `클래스 필드 계약 고정` = 클래스에 어떤 변수(필드)를 넣을지, 이름과 자료형을 먼저 확정해 코드로 정해두는 것

## 문서 갱신 규칙

- 코드/설계 변경 시 관련 문서를 함께 갱신한다.
- 사용자 지시가 없으면 의사결정 로그를 별도 문서에 기록하지 않는다.

## 문서 맵

- 핵심 구조: `Docs/GAME_STRUCTURE.md`
- 레포 구조: `Docs/PROJECT_MAP.md`
- 템플릿 재사용 레퍼런스: `Docs/PROJECT_MAP.md`의 "템플릿 재사용 레퍼런스" 섹션
