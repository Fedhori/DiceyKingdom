# 프로젝트 맵

이 문서는 템플릿 레포의 주요 경로를 요약한다.

## 문서

- 공통 규칙: `Docs/GENERAL_RULES.md`
- 템플릿 구조/실행 흐름: `Docs/GAME_STRUCTURE.md`
- 재사용 자산 목록: `Docs/TEMPLATE_REUSABLES.md`

## 코드

- 앱 수명/스코프
  - `Assets/Scripts/App`
  - `Assets/Scripts/Bootstrap.cs`

- 공통 유틸
  - `Assets/Scripts/Common`

- 데이터 로더/설정
  - `Assets/Scripts/Data`
  - `Assets/StreamingAssets/Data/GameConfig.json`

- 저장
  - `Assets/Scripts/Save`

- UI 공용 서비스
  - `Assets/Scripts/UI`
  - `Assets/Scripts/Tooltip`

- 런타임 서비스
  - `Assets/Scripts/Audio`
  - `Assets/Scripts/Particles`
  - `Assets/Scripts/InputService.cs`
  - `Assets/Scripts/GameSpeedService.cs`
  - `Assets/Scripts/OptionService.cs`

## 씬

- `Assets/Scenes/Bootstrap.unity`
- `Assets/Scenes/GameScene.unity`
- `Assets/Scenes/MainMenuScene.unity`

## 비고

- 기존 DiceyKingdom 전용 게임 로직/데이터/와이어프레임 문서는 정리되었다.
- 새 프로젝트 시작 시 구조 확장은 `Docs/GAME_STRUCTURE.md`를 기준으로 진행한다.
