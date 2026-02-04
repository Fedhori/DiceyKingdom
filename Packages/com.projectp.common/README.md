# 공용 패키지 README (초안)

이 문서는 ProjectP에서 공용화 가능한 요소를 패키지로 사용할 때의 가이드입니다.

## 목적

- 신규 Unity 프로젝트 시작 시 공용 런타임 유틸을 빠르게 재사용하기 위함

## 범위

- 런타임 유틸 중심
  - 데이터 로딩/캐시: `SaCache`, `StaticDataManager`
  - 저장/로드: `SaveManager`
  - 로컬라이징 유틸: `LocalizationUtil`, `StringUtil`
  - UI 공용 유틸: `Modal`, `ToastManager`, `Tooltip`, `FloatingTextManager`
  - 오디오: `AudioManager`, `BgmManager`
  - 게임 속도: `GameSpeedManager`
  - 파티클/VFX 라우팅: `ParticleManager`
  - Dev/디버그 커맨드: `DevCommandManager` (선택)

## 제외

- 상태/트리거/롤링/상점 구조
- 입력 구현(키보드/가상 조이스틱 등)

## Docs 생성 정책

- 공용 패키지에 `Docs/GENERAL_RULES.md`를 포함한다.
- 프로젝트 시작 시 `Docs/GAME_STRUCTURE.md`, `Docs/PROJECT_MAP.md`는 **새로 생성**한다.

## 자동 초기 세팅(에디터)

- 패키지 임포트 시 다음 작업을 자동 수행한다.
  - 최소 필수 폴더 생성
  - `Docs/GENERAL_RULES.md` 복사(없을 때)
  - `Docs/GAME_STRUCTURE.md`, `Docs/PROJECT_MAP.md` 템플릿 생성(없을 때)

## 디렉토리 구조(최소 필수)

- `Assets/Scenes`
- `Assets/Scripts`
- `Assets/Prefabs`
- `Assets/Resources/Music`
- `Assets/Resources/SFX`
- `Assets/Sprites`
- `Docs`
