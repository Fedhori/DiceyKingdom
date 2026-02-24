# CURRENT_TASK

## 상태
- 현재 단계: **기획/정리**
- 구현 시작: **아직 시작하지 않음 (작업 착수 금지)**

## 목표
- `DEBUG PANEL` 중심 화면에서 벗어나, 실제 플레이어 경험 기준의 전투 UI 화면을 구현하기 위한 작업 기준을 고정한다.

## 확정된 화면 구성
1. 좌측 영역: 플레이어 영역
   - 플레이어 체력
   - 플레이어 Ability 목록
2. 중앙 영역: `Combat` 3개
   - 각 Combat는 좌/우로 분리
   - 좌측: 플레이어 Ability 배치 공간
   - 우측: 적 Ability 배치 공간
3. 우측 영역: 적 영역
   - 적 체력
   - 적 Ability 목록 (읽기 전용)
4. 상단바
   - `Phase`, `Turn`만 표시
5. 중앙 하단
   - `Roll` 버튼 1개

## Ability 카드 UI 규칙
- 정사각형 아이콘 카드 사용
- 타입별 테두리 색상
  - `Attack`: RED
  - `Skill`: BLUE
  - `Passive`: GREEN
- 카드 하단 `Power` 표기
  - Power 없는 Skill/Passive는 수치 표기 생략
- Ability별 전용 아이콘 사용
  - 데이터에서 Ability별 아이콘 참조 가능해야 함

## 상호작용 확정 사항
- 배치 방식: `선택 + 클릭`
  - Ability 선택 후 Combat 클릭으로 배치/이동
- Combat 배치 허용 타입: `Attack`만
- 적 영역 Ability는 `읽기 전용`
- Roll 처리: 버튼 1회로 `Roll -> Resolve` 연속 처리
- Resolve 진행은 Combat 0 -> 1 -> 2 순서로 시각적으로 보여줌
  - 숫자 카운트업 애니메이션 포함
- Tooltip: `Hover only` (모바일 고려하지 않음)
  - 내용은 Localization 우선, 실패 시 key fallback

## 구현 원칙(중요)
- UI 위치/크기/정렬은 가능한 한 **에디터에서 배치**
- 코드에서 UI 배치 강제 조정은 불가피한 경우만 허용
- 기존 전투 로직(`DuelSessionBuilder`, `DuelPhaseRunner`, `DuelTurnProcessor`)은 재사용 우선

## 이번 문서의 목적
- 지금은 작업 실행이 아니라, 다음 구현 턴에서 바로 착수 가능한 기준을 고정하는 것.
- 실제 코드/씬/프리팹 수정은 **다음 지시 후 시작**.
