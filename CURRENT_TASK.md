# CURRENT_TASK

## 상태
- 현재 단계: **전투 UI 구현 계획 확정**
- 기준 와이어프레임: `Docs/Wireframes/BattleScreen_Wireframe_v27_MetalCompactTotal.png`
- 다음 단계: **GameScene에 v27 레이아웃 구현**

## 목표
- 디버그 패널 중심 화면에서 벗어나, 실제 플레이어용 전투 화면(v27)을 `GameScene`에 고정한다.

## v27 확정 레이아웃
1. 상단 메탈릭 바는 화면 최상단에 붙는다. (`Turn` 표시)
2. `SURRENDER` 버튼은 좌상단, `COMBAT START` 버튼은 우하단에 배치한다.
3. 적 Ability 대기열은 상단 중앙 1줄, 플레이어 Ability 대기열은 하단 중앙 1줄로 배치한다.
4. 적/플레이어 체력(하트)은 각각 상단/하단 중앙에 배치한다.
5. 중앙에는 가로 3개의 Combat Zone을 둔다.
6. 각 Combat Zone 내부 규칙:
   - 위 행: 적 슬롯 1줄 6칸
   - 아래 행: 플레이어 슬롯 1줄 6칸
   - 슬롯은 중앙 정렬
   - `TOTAL POWER`는 슬롯과 분리된 전용 영역(메탈릭 직사각형)으로 표시
7. Combat Zone에는 `Attack` 타입만 배치 가능하다.
8. `Talent` UI는 이번 작업 범위에서 제외한다.

## 카드 UI 규칙
1. 카드 크기: 세로가 더 긴 직사각형.
2. 대기열 카드와 Combat Zone 카드 크기는 동일.
3. 타입별 테두리 색상:
   - `Attack`: RED
   - `Skill`: BLUE
4. Power는 카드 우하단 원형 배지로 표시한다.
5. 아이콘은 `iconId -> Sprite` 매핑으로 렌더한다.

## 구현 순서
1. `GameScene` UI 루트 정리
   - 기존 `DuelDebugPanel` 의존 제거
   - v27 배치용 UI 루트/컨테이너 생성
2. 레이아웃 프리팹/오브젝트 구성 (에디터 우선)
   - 상단바, 버튼 2개, 상/하단 대기열, 하트 영역, Combat Zone 3개
3. Combat Zone 프리팹화
   - 적 슬롯 6, 플레이어 슬롯 6, 구분선, TOTAL POWER 메탈릭 박스 2개
4. 카드 프리팹 정리
   - 직사각형 카드, 테두리/아이콘/원형 파워 배지 고정
5. Presenter 바인딩
   - Enemy/Player 대기열 렌더
   - Combat 배치 렌더
   - TOTAL POWER 갱신
   - `COMBAT START` / `SURRENDER` 이벤트 연결
6. 규칙 연결
   - Combat 슬롯은 Attack-only
   - Skill은 대기열에서만 보이고 Combat 배치 불가
   - Talent는 별도 시스템으로 분리 예정 (현 단계 미표시)
7. 툴팁 연결 (Hover only)
   - 표시: 이름, 설명, 타입, Power

## 검증 기준
1. 컴파일 에러 0
2. EditMode 테스트 통과
3. GameScene 수동 검증
   - v27 레이아웃 일치
   - Combat 슬롯 1줄 6칸, 중앙 정렬 확인
   - TOTAL POWER가 슬롯과 겹치지 않는지 확인
   - `COMBAT START` 동작 확인
   - `SURRENDER` 즉시 처리 확인
   - Talent UI 비노출 확인
