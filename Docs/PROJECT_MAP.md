# 프로젝트 맵

이 문서는 레포의 파일 위치와 책임을 요약합니다.

## 현재 상태 (중요)

- **기획 기준 문서**는 `Docs/GAME_STRUCTURE.md` 입니다.
- 코드베이스에는 이전 `Adventurer/Enemy` 실험 구현이 남아 있습니다.
- 이전 기획 문서는 `Docs/Backup_Adventurer/`에 백업되어 있습니다.

## 문서 역할

- `Docs/GENERAL_RULES.md`
  - 공통 개발 규칙, 컨벤션, 문서 갱신 원칙
- `Docs/GAME_STRUCTURE.md`
  - 신규 게임 구조 단일 기준 문서
- `Docs/GAME_IDEA_BACKLOG.md`
  - 아이디어/리스크/질문 백로그
- `Docs/ENEMY_ROSTER.md`
  - 상황 로스터(파일명 유지)
- `Docs/V0_MILESTONE.md`
  - 기획 리셋 마일스톤 진행표

## 코드 위치 (현행 구현 기준)

### 런타임/데이터
- `Assets/Scripts/Game/Data`
  - JSON DTO/로더
- `Assets/Scripts/Game/Runtime`
  - 런 상태 모델, 턴 오케스트레이션

### UI
- `Assets/Scripts/Game/UI`
  - 전투/보드 UI 컨트롤러
  - 하단 액션 바/상단 HUD/드래그 시각화 등

### 씬
- `Assets/Scenes/GameScene.unity`
  - 현재 테스트용 메인 씬

## 데이터 위치

- `Assets/StreamingAssets/Data/*.json`
  - 정적 데이터(스킬/상황/프리셋 등)

## 참고

- 대규모 기획 전환 시, 코드보다 문서를 먼저 기준으로 잠금 후 구현을 따라오게 유지합니다.

