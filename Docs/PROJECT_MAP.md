# 프로젝트 맵

이 문서는 현재 레포의 주요 문서/코드 위치를 요약한다.

## 문서 구조

- 공통 규칙: `Docs/GENERAL_RULES.md`
- 메인 스펙: `Docs/GAME_STRUCTURE.md`
- 아이디어/백로그: `Docs/GAME_IDEA.md`
- 세부 시스템 문서:
  - `Docs/ADVENTURER.md`
  - `Docs/KINGDOM.md`
  - `Docs/MISSION.md`
  - `Docs/ABILITY_TEST.md`
  - `Docs/TRAIT.md`
  - `Docs/EQUIPMENT.md`
  - `Docs/FACILITY.md`
  - `Docs/SKILL_CONSUMABLE.md`

## 코드 구조(현황)

- `Assets/Scripts`
- 현재 플레이어블 코드(레거시 구조 포함)
- 다음 단계에서 신규 기획에 맞춰 단계적 교체 예정

- `Assets/StreamingAssets/Data`
- 게임 데이터 JSON 위치
- 신규 기획 데이터 스키마 전환 예정

- `Assets/Prefabs`
- UI/카드/주사위 프리팹
- 신규 기획 UI 구조에 맞춰 재사용 또는 교체 예정

- `Assets/Scenes`
- 씬 구성
- 신규 게임 루프 기준으로 씬 책임 재정의 예정

## 전환 메모

- 현재 문서 스펙이 코드보다 최신일 수 있다.
- 구현 단계에서는 `Docs/GAME_STRUCTURE.md`를 기준으로 코드/데이터를 정렬한다.
