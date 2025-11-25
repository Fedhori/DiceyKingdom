# PREVIEW 가이드

이 문서는 Codex 세션 전반에서 일관되게 지켜야 할 공통 규칙을 정리합니다.

## 전제

- 우리는 Unity 기반 게임을 개발/유지 보수 중입니다. (버전: 6.2 - 6000.2.6f2)
- 기능 구현은 `Assets/Scripts`와 `Assets/StreamingAssets` 구조에 맞춰 진행합니다.
- 코드는 Unity C# 컨벤션(4-스페이스, Allman braces 등)을 따릅니다.
- 데이터는 주로 StreamingAssets의 JSON을 통해 로드됩니다.

## 일반 규칙

1. 사용자가 “코드 작업”을 지시하기 전에는 구현을 시작하지 않습니다.
2. 코드 변경 시 기존 시스템과 호환되도록 유지합니다.
3. 씬/프리팹/Inspector 수정이 필요하면 반드시 사용자에게 강조하여 안내합니다.
4. `.meta` 파일은 Unity가 관리하므로 직접 생성하거나 수정하지 않습니다.

## 추가 지침

- 모든 질의응답과 설명은 한국어로 작성합니다.

## 컴포넌트 참조 관련 주의

- 런타임 null 체크나 자동 할당을 위한 보일러플레이트(예: `EnsureComponents()`)를 추가하지 않습니다.
- 필요한 컴포넌트(`Collider2D` 등)는 **항상** 프리팹 인스펙터에서 직접 지정해야 합니다.

## 출력 안내

- “Goal” 섹션에서 목표를 간결히 요약합니다.
- 구현/검증 단계에서 필요한 수동 작업이나 테스트 방법이 있으면 명확히 안내합니다.

## Scripts 폴더 구조 메모

- 상위 부트/매니저: `Bootstrap`에서 SaCache 초기화 후 `GameScene` 로드 및 `managersRoot`를 DDoL로 활성화. `GameManager`는 단일톤 RNG/재시작, `InputManager`는 PlayerInput 매핑과 슬롯 액션, 옵션/디버그 콘솔 토글을 중계. `OptionManager`는 옵션 오버레이와 씬별 버튼 표시, `ScoreManager`는 점수 합산과 `FloatingTextManager` 호출을 담당.
- 데이터·캐시: `Data` 폴더에 SaCache(StreamingAssets→persistent 동기화), `StaticDataManager`(게임 시작 시 Stages/Balls/Pins JSON 로드), DTO/리포지토리(`BallRepository`, `PinRepository`), 공용 컬러/문자열/로컬라이징 유틸, 스프라이트 캐시가 모여 있음. 주요 입력 JSON은 `StreamingAssets/Data/*.json` 기준.
- 엔티티 계층: `Ball` 폴더는 DTO/Repository/Instance와 `BallFactory`(스폰), `BallController`(물리·충돌), `BallManager`(주기적 생성), `DeathZone`(퇴장 처리)로 분리. `Pin` 폴더도 동일 패턴의 DTO/Repository/Instance와 `PinFactory`(생성), `PinManager`(격자 배치), `PinController`(충돌·히트 이펙트)로 구성.
- 스테이지: `Stage` 폴더는 `StageDto`/`StageInstance` 자리만 잡혀 있고, 데이터는 `StaticDataManager`가 Stages.json에서 읽어들임.
- UI 계층: `UI` 루트에 레이아웃/효과(`FixedAspectRatio`, `HoverScale`, `SlidePanelLean`, `PersistentCanvas`), 게이지/숫자/플로팅 텍스트, 토스트/옵션 오버레이, 강조/클릭 캐처 등이 위치. `UI/Modal`(ModalManager, Info/Confirmation), `UI/Tooltip`(TooltipManager/Target/View/Kind/Content)처럼 기능별 하위 폴더로 나뉨.
- 기타: `Dev/DevCommandManager`는 콘솔 열기 토글과 연동, `Audio/AudioManager`는 오디오 제어용 자리. 구조적으로 `managersRoot`에 여러 매니저가 담겨 씬 전환 간 유지되는 패턴이 고정되어 있음.
