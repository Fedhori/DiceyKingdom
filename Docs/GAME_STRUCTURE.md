# 게임 구조

이 문서는 템플릿 프로젝트의 게임 구조/시스템과 런타임 흐름을 정리합니다. 레포 지도는 `Docs/PROJECT_MAP.md`, 범용 규칙은 `Docs/GENERAL_RULES.md`를 참고합니다.

## 문서 범위/분리 기준

- 범용 규칙/컨벤션: `Docs/GENERAL_RULES.md`
- 레포 지도(파일 위치/책임 요약): `Docs/PROJECT_MAP.md`
- 이 문서: 게임 구조, 시스템, 데이터/씬 흐름

## 프로덕트 요약 (한 줄)

- TODO: 한 줄 요약을 작성합니다.

## 게임 컨셉

- TODO: 핵심 컨셉과 플레이어 역할을 요약합니다.

## 씬/런타임 흐름

- TODO: 부팅/메인 메뉴/게임 진입 흐름을 정리합니다.

## 핵심 루프

- TODO: 스테이지/라운드/상점 등 루프를 정리합니다.

## 데이터 로딩

- 템플릿은 `SaCache` + `StaticDataManager` 기반의 JSON 로딩 흐름을 제공합니다.
- 실제 로딩 규칙/파일 구성은 프로젝트별로 정의합니다.

## 저장/로드

- 템플릿은 `SaveService` 기반의 일반 저장/로드 유틸을 제공합니다.
- 실제 저장 데이터 스키마는 프로젝트별로 정의합니다.

## UI/UX 공용 시스템

- 모달/토스트/툴팁/플로팅 텍스트는 공용 UI 유틸로 제공됩니다.

## 오디오

- `AudioManager`(SFX), `BgmManager`(BGM) 공용 유틸 제공.

## 난수

- RNG 정책은 프로젝트별로 정의합니다.

## 텍스트 스타일(프로젝트별)

- 기준: `Assets/GlobalTextStyles.asset`
- TODO: 프로젝트별 스타일 이름을 기록합니다.

## 관련 문서

- 공통 규칙: `Docs/GENERAL_RULES.md`
- 레포 지도: `Docs/PROJECT_MAP.md`
