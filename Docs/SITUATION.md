# 상황 (Situation)

이 문서는 상황 시스템의 데이터/런타임/UI 규칙을 정리합니다.

## 문서 범위/분리 기준

- 전체 턴/상태머신: `Docs/GAME_STRUCTURE.md`
- 상황 컨셉/대응 요약: `Docs/ENEMY_ROSTER.md`
- 이 문서: 상황 정의, 생성, 해결/실패, 카드 표시 규칙

## 역할

- 상황은 보드에 생성되는 위기 단위입니다.
- 각 상황은 주사위 묶음, 기한, 성공 보상, 실패 효과를 가집니다.
- 상황 주사위가 모두 파괴되면 즉시 해결됩니다.

## 데이터 정의 (`Assets/StreamingAssets/Data/Situations.json`)

- `situationId`: 고유 ID
- `nameKey`: 로컬라이즈 키
- `tags`: 분류 태그
- `diceFaces`: 상황이 보유한 주사위 면 수 목록
- `baseDeadlineTurns`: 기본 기한(턴)
- `successReward`: 성공 시 적용할 `EffectBundle`
- `failureEffect`: 실패 시 적용할 `EffectBundle`
- `failurePersistMode`: `remove` 또는 `resetDeadline`

### 현재 v0 운영값

- `failurePersistMode`는 전부 `remove`로 운영합니다.
- 상황 `행동(Action)` 및 `행동 풀(actionPool)` 로직은 사용하지 않습니다.

## 생성 규칙 (`GameRunBootstrap`)

- 런 시작 시 상황 `3`개 생성
- `4`턴마다 상황 `3`개 추가 생성
- 상황 풀에서 균등 랜덤(중복 허용) 선택
- 보드 최대 상황 수 상한 없음

## 대결 및 해결 규칙

1. 처리 중 요원의 주사위 `1`개와 상황 주사위 `1`개를 선택합니다.
2. 양쪽 주사위를 굴려 `요원 눈 >= 상황 눈`이면 성공입니다.
3. 요원 주사위는 성공/실패와 무관하게 소모됩니다.
4. 성공이면 선택된 상황 주사위 `1`개를 파괴합니다.
5. 실패여도 선택된 상황 주사위의 면 수(`dX`)를 `X -= 요원 눈`만큼 감소시킵니다.
6. 실패 감소 결과가 `0` 이하이면 해당 상황 주사위를 즉시 파괴합니다.
7. 상황 주사위가 `0`개가 되면 즉시 성공 보상 적용 후 제거합니다.

## 실패 규칙 (`Settlement`)

- 미해결 상황의 `deadlineTurnsLeft`를 `1` 감소시킵니다.
- `0` 이하가 되면 `failureEffect`를 즉시 적용합니다.
- 이후 `failurePersistMode`를 처리합니다.
  - `remove`: 상황 제거
  - `resetDeadline`: 기한을 `baseDeadlineTurns`로 초기화

## UI/프리팹 규칙

- 카드 프리팹: `Assets/Prefabs/Situation/SituationCard.prefab`
- 매니저: `Assets/Scripts/Game/UI/SituationManager.cs`
- 카드 View: `Assets/Scripts/Game/UI/SituationController.cs`
- 카드 값 표시(이름/남은 주사위/기한/성공·실패 요약)는 상황 상태 이벤트 기반으로 갱신합니다.
- 레이아웃/피벗/오프셋/크기 값은 프리팹에서 관리하며 코드에서 강제하지 않습니다.



