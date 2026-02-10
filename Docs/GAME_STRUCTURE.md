# 게임 구조 (vNext 리셋)

이 문서는 DiceyKingdom의 **신규 기획 기준 문서**입니다.
현재 코드베이스에 남아 있는 `Adventurer/Enemy` 실험 구현은 참고용이며, 기획 기준은 이 문서를 우선합니다.

- 기준일: 2026-02-10
- 슬로건: **The Kingdom Must Survive**
- 장르: 자원관리 로그라이크
- 테마: 진지한 다크 판타지 (고어 강조 없음)

## 1) 게임의 핵심 재미 (확정)

1. 우선순위의 재미

- 자원이 한정되어 모든 목표를 동시에 달성하기 어렵다.
- 플레이어는 매 턴 "무엇을 살리고 무엇을 버릴지"를 결정한다.

2. 분배의 재미

- 주사위 결과가 비결정적이라, 동일한 분배도 다른 결과를 만든다.
- 플레이어는 각 상황에 "얼마를 넣어야 충분한가"를 계속 재판단한다.

3. 조정의 재미

- 롤 이후 조정 단계에서 조언자/칙령으로 결과를 보정한다.
- 제한된 조정 수단을 어디에 쓰는지가 고급 의사결정이 된다.

## 2) 세계관/톤 (확정)

- 플레이어는 몰락해가는 왕국에 즉위한 새 왕.
- 왕국은 침략, 약탈, 괴물, 야만 세력과 같은 위협에 노출되어 있다.
- 동시에 부패, 반란, 질병, 불만 같은 붕괴 요인도 안고 있다.
- 마법은 소수 특권이며 강력하지만 위험하고 통제된다.
- 전체 톤은 암울하지만, 인물/선택을 통해 희망의 여지를 남긴다.

## 3) 플레이어 판타지 (확정)

- 플레이어는 왕국 운영자이자 최종 의사결정자다.
- 목표는 단순 생존이 아니라, 연속된 위기 속에서 **합리적 선택으로 체제를 유지**하는 것.

## 4) 타겟 유저 (확정)

- 전략적 선택과 제한 자원 압박을 즐기는 유저
- 보드게임, 4X, 턴제 전략, 대전략 장르 선호 유저

## 5) 용어 사전 (신규 기준)

- `Kingdom` / 왕국: 플레이 보드 전체
- `Dice` / 주사위: 턴마다 받는 핵심 행동 자원 (d6)
- `Dice Upgrade` / 주사위 강화: 주사위 개별 강화(주사위당 1개)
- `Situation` / 상황: 보드에 생성되는 위기/기회 카드
- `Advisor` / 조언자: 쿨다운 기반 능동 스킬
- `Structure` / 구조물: 영구 패시브
- `Decree` / 칙령: 일회성 강력 소모품
- `Defense` / 방어: 왕국 존속을 나타내는 핵심 자원
- `Stability` / 안정: 왕국 질서를 나타내는 핵심 자원
- `Turn` / 턴: 시간 단위 (계절 컨셉 사용)

## 6) 승패 조건 (현재안)

- `Defense <= 0` -> 게임오버
- `Stability <= 0` -> 게임오버

## 7) 핵심 시스템 구조

### 7.1 주사위

- 매 턴 리필된다.
- 상황에 원하는 만큼 분배한다.
- 분배 후 롤 결과로 성과가 결정된다.
- Situation별 주사위 배치 제한은 두지 않는다. (v0)
- 미배치 주사위가 있어도 턴 진행은 가능하다. (v0)
- 분배가 끝난 뒤에는 롤/조정 단계에서 주사위 재할당을 허용하지 않는다. (v0)

### 7.2 주사위 강화

- 주사위당 강화 1개만 장착 가능.
- 다른 강화를 장착하면 기존 강화는 교체된다.
- v0 시작 시점에 전체 주사위 중 랜덤 3개를 선정해 강화를 부여한다.
- 시작 강화 선정 시 동일 강화의 중복 부여를 허용한다. (v0)
- 설계 목적: 런마다 주사위 성격을 바꿔 분배 판단을 변화시킨다.

#### 7.2.1 주사위 강화 조건 규칙 (v0 잠금)

- v0에서는 강화의 발동 형태를 아래 3가지 중 하나로 고정한다.
  - 무조건 발동(조건 없음)
  - 조건 1개 기반 발동
  - 이벤트 트리거 1개 기반 발동
- v0 허용 조건 타입:
  - `assigned_situation_deadline_lte`
  - `die_face_lte`
  - `die_face_eq`
  - `die_face_gte`
  - `assigned_dice_count_in_situation_gte`
  - `assigned_dice_count_in_situation_lte`
  - `player_defense_lte`
  - `player_defense_gte`
  - `player_stability_lte`
  - `player_stability_gte`
  - `board_order_first`
  - `board_order_last`
- v0 허용 이벤트 트리거:
  - `on_resolve_success`
- v0에서는 강화 1개당 조건/트리거를 1개만 허용한다. (복수 미지원)
- v0 강화 발동 타입은 2종으로 잠금한다.
  - `roll_once_after_roll`: 주사위를 굴린 직후 1회만 평가/적용
  - `recheck_on_die_face_change`: 주사위 눈이 바뀔 때마다 재검증/적용

#### 7.2.2 v0 주사위 강화 풀 (확정 6종)

1. `steady_bonus`

- 조건: 없음
- 발동 시점: **주사위를 굴린 직후** 1회
- 효과: 해당 주사위 눈 `+1`

2. `one_to_six`

- 조건: `die_face_eq(value=1)`
- 발동 시점: **주사위 눈이 바뀔 때마다 재검증**
- 효과: 해당 주사위 눈을 `6`으로 변경

3. `deadline_surge`

- 조건: `assigned_situation_deadline_lte(value=1)`
- 발동 시점: **주사위를 굴린 직후** 1회
- 효과: 해당 주사위 눈 `+2`

4. `pack_bonus`

- 조건: `assigned_dice_count_in_situation_gte(value=3)`
- 발동 시점: **주사위를 굴린 직후** 1회
- 효과: 해당 주사위 눈 `+2`

5. `critical_six`

- 조건: `die_face_eq(value=6)`
- 발동 시점: **주사위 눈이 바뀔 때마다 재검증**
- 효과: 해당 주사위 눈 `x2`
- 규칙: 주사위가 다시 `6`이 될 때마다 `x2`를 다시 적용한다. (턴당 횟수 제한 없음, 누적 허용)

6. `solo_bonus`

- 조건: `assigned_dice_count_in_situation_lte(value=1)`
- 발동 시점: **주사위를 굴린 직후** 1회
- 효과: 해당 주사위 눈 `+2`

#### 7.2.3 주사위 눈 변경 재검증 규칙 (v0 잠금)

- `recheck_on_die_face_change` 타입은 아래 시점마다 재검증한다.
  - 롤 결과가 결정된 직후
  - 조정 단계에서 Advisor/Decree 등으로 해당 주사위 눈이 변경될 때
- 재검증 대상: `one_to_six`, `critical_six`
- `critical_six`은 "6이 되는 이벤트"마다 반복 적용한다. (예외처리/턴당 제한 없음)
- `roll_once_after_roll` 타입은 롤 직후 1회만 적용하며, 이후 눈이 바뀌어도 재적용하지 않는다.

### 7.3 상황 (Situation)

모든 상황은 아래 공통 필드를 가진다.

- `demand` (요구치): 해결에 필요한 누적 수치
- `deadline` (기한): 실패까지 남은 턴
- `risk_value` (위험가치): 상황 난이도/위협 수준을 나타내는 값 (실수형)
  - v0 권장 범위: `100.0 ~ 500.0`
- `on_turn_start_effects` (턴 시작 효과, 선택)
- `on_success` (성공 효과)
- `on_fail` (실패 효과)
- `tags` (분류/검색/연출용 메타데이터)
  - 시스템 분류 강제 규칙으로 사용하지 않는다.
  - `external`, `internal` 태그는 사용하지 않는다.
  - `urgent_pressure`, `attrition_pressure` 태그는 사용하지 않는다.
- `board_order` (배치 순서 인덱스: 좌->우, 위->아래)
  - 인덱스 기반으로 관리한다.
  - 보드 상황이 바뀔 때마다 빈 인덱스를 남기지 않고 즉시 자동 재정렬한다. (`0..n-1`)
  - 생성/제거/순서 변경이 발생하면 그 즉시 재인덱싱한다.
- `pressure_type` 필드는 데이터 스키마에서 사용하지 않는다.

상황은 위기일 수도, 기회일 수도 있다.

- 위기 예: 유목민 약탈
- 기회 예: 운하 건설

#### 7.3.1 상황 효과 최소 스키마 (잠금)

- 공통 규칙
  - `effect_type`는 enum으로 관리한다.
  - `value`의 부호 규칙: `+` 증가, `-` 감소.
  - 피해/회복 등 자원 변화 효과는 `target_resource`를 반드시 명시한다.
  - `target_resource`는 enum(`defense | stability`)으로 고정한다.

- `effect_type` v1 목록 (잠금)
  - `resource_delta`
  - `gold_delta`
  - `demand_delta`
  - `deadline_delta`
  - `resource_guard`
  - v0 검증 범위에서는 `gold_delta`를 사용하지 않는다.

- 타입별 필수 키 규칙 (잠금)
  - `resource_delta`
    - required: `value`, `target_resource`
  - `gold_delta`
    - required: `value`
  - `demand_delta`
    - required: `value`, `target_mode`
    - `target_mode` enum: `self | selected_situation | by_tag | all_other_situations`
    - `target_mode = all_other_situations`일 때 대상은 "효과 실행 시점에 보드에 남아 있는 미해결 Situation 중 자기 자신 제외"로 고정
    - 대상 집합은 효과 실행 순간에 확정한다. 이미 성공/실패로 제거된 Situation은 대상에 포함하지 않는다.
    - 예시 A: `plague_outbreak`가 먼저 실패하면, 뒤에 남아 있는 Situation들만 `demand_delta(+2)`를 받는다.
    - 예시 B: `plague_outbreak`가 마지막에 실패하고 보드에 자기 자신만 남아 있으면, `all_other_situations` 대상이 없어 효과가 적용되지 않는다.
    - `target_mode = by_tag`일 때 required: `target_tag`
  - `deadline_delta`
    - required: `value`, `target_mode`
    - `target_mode` enum: `self | selected_situation | by_tag | random_other_situation`
    - `target_mode = random_other_situation`일 때 대상은 "효과 실행 시점에 보드에 남아 있는 미해결 Situation 중 자기 자신 제외"에서 1개를 랜덤 선택한다.
    - `random_other_situation` 대상이 없으면 효과는 무효(no-op) 처리한다.
    - `target_mode = by_tag`일 때 required: `target_tag`
  - `resource_guard`
    - required: `target_resource`, `duration`
    - 의미: `duration` 동안 지정 자원의 감소를 무효화

- 스코프 제한
  - `status` 계열 효과는 현재 스코프에서 제외한다. 필요 시 별도 잠금 후 추가한다.

### 7.4 조언자 (Advisor)

- 슬롯을 점유하는 액티브 능력.
- 쿨다운이 있고 사용 시 소모된다.
- 사용 타이밍은 전 단계에서 허용한다. (v0)
- 동일 Situation에 한 턴 내 다중 Advisor 적용을 허용한다. (v0)
- 시작 보유 Advisor 선정 시 중복은 허용하지 않는다. (v0)
- 설계 목적: 롤 결과에 대한 사후 의사결정 깊이를 만든다.

#### 7.4.1 v0 조언자 풀 (확정)

1. `godfather`

- 효과: `Stability -1`, 선택 Situation `demand -10`
- 쿨다운: `1`
- 타겟: `selected_situation`
- 제약: 없음

2. `diplomat`

- 효과: 선택 Situation `deadline +1`
- 쿨다운: `4`
- 타겟: `selected_situation`
- 제약: Situation당 1회

3. `jester`

- 효과: 선택 주사위가 `1`일 때 `6`으로 변경
- 쿨다운: `1`
- 타겟: `selected_die`
- 제약: 선택 주사위 값이 `1`이어야 함

4. `knight`

- 효과: 선택 주사위 `+2`
- 쿨다운: `2`
- 타겟: `selected_die`
- 제약: 없음

5. `sage`

- 효과: 선택 Situation에 할당된 주사위 전부 리롤
- 쿨다운: `2`
- 타겟: `selected_situation`
- 제약: 없음

6. `blacksmith`

- 효과: 선택 Situation에 할당된 주사위 전부 `+1`
- 쿨다운: `3`
- 타겟: `selected_situation`
- 제약: 없음

### 7.5 구조물 (Structure)

- 영구 패시브.
- 수량 제한은 두지 않되 획득 난이도/기회비용으로 제어한다.

### 7.6 칙령 (Decree)

- 일회성 소모형 강력 효과.
- 포지션은 "긴급 대응 안전장치".
- 소모품 슬롯(기본 3) 기준 운영.
- 사용 타이밍은 전 단계에서 허용한다. (v0)

#### 7.6.1 v0 칙령 풀 (확정)

1. `total_censorship`

- `resource_guard(target_resource=stability, duration=1)`

2. `fortify_walls`

- `resource_guard(target_resource=defense, duration=1)`

3. `efficient_bureaucracy`

- `demand_delta(value=-6, target_mode=selected_situation)`

4. `time_extension`

- `deadline_delta(value=+1, target_mode=selected_situation)`

5. `focused_support`

- `die_face_delta(value=+1, target_mode=assigned_dice_in_selected_situation)`

## 8) 턴 흐름 (현재안)

1. 턴 시작

- 주사위 리필
- `on_turn_start_effects` 적용 (주사위 리필 직후, 분배 전)
- 상황 생성 판정(4턴 주기 위험예산 스폰: `1, 5, 9, ...`)

2. 주사위 분배

- 상황들에 주사위를 배치

3. 롤

- 배치된 주사위 굴림

4. 조정

- 조언자/칙령으로 수치와 상태를 보정

5. 확정/정산

- `board_order`(좌->우, 위->아래) 순서로 상황을 순차 처리
- 상황별 최종 수치를 적용해 `demand` 감소
- `demand`가 감소할 때마다 즉시 성공 판정을 수행한다. (`demand <= 0`이면 즉시 성공 처리, 상황 제거)
- 성공 효과는 즉시 획득/즉시 사용 가능
- 미해결 상황은 `deadline -1` (턴 종료 시 감소 규칙)
- 감소 후 `deadline <= 0`이면 즉시 실패 처리, 실패 효과 즉시 적용, 상황 제거

## 8.2 위험예산 스폰 규칙 (잠금)

- 스폰 시점: `1, 5, 9, ...` 턴의 턴 시작.
- 스폰 소스: `Situation` 전체 랜덤 풀(중복 허용).
- 위험예산 `budget`은 성장형이 아닌 고정값으로 사용한다. (v0)
- v0 고정값: `budget = 2000.0`
- 추첨 방식: 균등 추첨(가중치 없음)
- 각 Situation은 `risk_value`를 가진다.
- 스폰 알고리즘:
  1. 해당 스폰 턴의 위험예산 `budget`을 가져온다.
  2. 랜덤 풀에서 Situation을 균등 확률로 1장 선택한다.
  3. 선택한 Situation 추가 시 누적 위험가치가 `budget`을 초과하면 즉시 스폰 종료한다.
  4. 초과하지 않으면 Situation을 생성하고 2번으로 반복한다.
- 초과 카드가 나온 경우 재추첨하지 않는다. (그 시점에서 종료)
- 로그 노출:
  - 이번 스폰 턴의 `budget`
  - 누적 사용 위험가치
  - 스폰된 Situation 수
  - 로그 표시만 우선하고, 별도 UI 패널은 v0 범위에서 제외한다.

## 8.3 조언자/칙령 타겟 유효성 규칙 (D3-2 잠금)

- UI 입력 단계:
  - 유효한 타겟만 하이라이트/선택 가능하게 한다.
  - 타겟 타입 불일치(`selected_die` 능력을 Situation에 사용 등)는 입력 단계에서 차단한다.
- 런타임 안전장치:
  - 비정상 경로로 유효하지 않은 타겟 시전이 들어오면 효과는 `no-op` 처리한다.
- 소모 규칙:
  - 유효하지 않은 시전 또는 `no-op` 시전에서는 쿨다운/소모품/부가비용을 소모하지 않는다.
- 실효 0 처리:
  - 타입은 맞지만 실효가 0인 경우는 시전 불가로 처리한다.
  - 예: `sage`/`blacksmith`를 "할당 주사위 0개 Situation"에 사용 시 차단.
- 개별 제약 처리:
  - `diplomat`의 Situation당 1회 제한을 초과한 재시전은 차단 + 미소모.
  - 플레이어 발동 효과에서 `random_other_situation` 대상이 없으면 `no-op` + 미소모.
- 피드백:
  - 유효하지 않은 시도에는 짧은 토스트 1줄을 노출한다.
  - 대상 또는 버튼에 붉은 플래시를 함께 노출한다.
- 디버그 로그:
  - invalid 사유코드(`INVALID_TARGET_TYPE` 등) 상세 로그는 남기지 않는다. (v0)

## 8.4 `demand` 감소/성공 판정 처리 규칙 (D3-3 잠금)

- 적용 타이밍:
  - `demand` 감소는 이벤트가 발생하는 즉시 반영한다. (분배 정산/Advisor/Decree 공통)
- 판정 단위:
  - `demand` 값이 바뀔 때마다 즉시 성공 판정을 수행한다.
- 다중 대상 효과 순서:
  - 효과 실행 시점의 `board_order` 순차로 적용한다.
- 성공 즉시 처리:
  - `demand <= 0`이 되는 순간 성공 처리한다.
  - 성공 효과를 즉시 적용하고, 해당 Situation을 즉시 제거한다.
  - 제거 직후 `board_order`를 즉시 재정렬한다.
- 제거된 대상 처리:
  - 이미 제거된 Situation을 참조하는 후속 효과는 `no-op` 처리한다.
- 오버킬 처리:
  - `demand` 초과 감소분은 폐기한다. (다른 Situation으로 이월하지 않음)
- 연쇄 처리:
  - 성공/효과로 새 이벤트가 발생하면 FIFO 큐로 더 이상 이벤트가 없을 때까지 처리한다.
- 하한 처리:
  - `demand`는 음수 값을 허용한다. (`0` clamp 없음)

## 8.5 `board_order` 재정렬 트리거 (D3-4 잠금)

- 재인덱싱 트리거:
  - Situation 생성 직후
  - Situation 제거 직후 (성공/실패 포함)
  - 수동 순서 변경 직후
- 재인덱싱 규칙:
  - 매 트리거 시점마다 즉시 `0..n-1`로 재인덱싱한다.
  - 빈 인덱스/중복 인덱스를 허용하지 않는다.
- 예시:
  - 4개 Situation의 인덱스가 `0,1,2,3`일 때 `1`번이 제거되면 즉시 `0,1,2`로 재정렬한다.

## 8.1 효과 표시 UI 규칙 (잠금)

- 상황 카드의 `성공 효과`/`실패 효과` 표시 영역에서, `target_resource`가 명시된 효과는 영향 아이콘을 함께 노출한다.
- 아이콘 노출은 정산 시점만이 아니라 카드 상세/툴팁에서도 동일 규칙으로 유지한다.
- `target_resource`가 없는 효과(`gold_delta` 등)는 자원 영향 아이콘을 노출하지 않는다.

## 9) 설계 가드레일 (중요)

1. 의사결정 피로 제어

- 턴당 상황 수, 조정 수단 수, 화면 정보량을 제한한다.
- "복잡함"이 아니라 "의미 있는 선택"이 느껴져야 한다.

2. 우선순위 게임성 보존

- 조언자/칙령이 강해도 분배 판단 자체를 무력화하면 실패다.
- 최강 수단은 항상 희소하거나 대가가 있어야 한다.

3. 효과 문장 길이 제한

- 한 번 읽고 이해 가능한 길이를 기본으로 한다.
- 긴 조건문 효과는 예외로만 허용한다.

## 10) 리스크 리뷰 (비판적 체크)

### 좋은 점

- 주사위 분배 + 사후 조정의 2단 의사결정 구조가 명확하다.
- 다크 판타지 경영 테마와 시스템 목적이 잘 맞는다.
- 위기/기회 상황을 같은 규칙으로 처리할 수 있어 확장성이 좋다.

### 나쁜 점

- `Defense + Stability` 이중 체력은 UI/판단 난이도를 빠르게 올린다.
- `deadline`이 많은 상황이 동시에 등장하면 턴당 계산량이 폭증한다.
- 조정 수단이 강해질수록 분배 단계가 형식화될 위험이 있다.

### 놓친 점

- `target_mode`가 `selected_situation`인 효과의 UI 입력/유효성 처리 규칙은 D3-2로 잠금 완료했다.

## 11) 규칙 잠금 (2026-02-10 확정)

1. `deadline` 감소 시점

- 턴 종료 정산에서 감소한다.

2. 실패 판정 시점

- 감소 후 `deadline <= 0`이 되면 즉시 실패 처리한다.

3. 성공/실패 효과 처리 순서

- `board_order` 순차 처리로 고정한다.
- `board_order`는 인덱스 기반이며, 보드 상황 변경 시 즉시 자동 재정렬한다.

4. 성공 보상 사용 가능 시점

- 획득 즉시 사용 가능하다.

5. `Defense`/`Stability` 피해 정의 방식

- 상황별 개별 정의를 사용한다.
- `tags`는 분류/연출 기준이며, 피해 대상은 각 상황 효과 데이터에서 직접 명시한다.
- 피해/회복 자원 대상 필드는 `target_resource` enum(`defense | stability`)으로 고정한다.
- `external`/`internal`은 태그/시스템 규칙으로 사용하지 않는다.

6. `deadline_delta` 적용 범위 (v0)

- `deadline_delta`는 `selected_situation` 또는 `random_other_situation` 대상으로 사용한다.
- `value = -1` 페널티형은 랜덤 대상(`random_other_situation`)으로만 사용한다.
- `random_other_situation` 대상이 없으면 효과는 무효(no-op) 처리한다.

7. `demand` 감소 시 판정 규칙

- `demand` 감소가 발생하는 모든 시점에서 즉시 성공 판정을 수행한다.
- 감소 원인이 분배 결과인지, Advisor/기타 효과인지와 무관하게 동일 규칙을 적용한다.
- 다중 대상 효과는 실행 시점의 `board_order` 순차로 처리한다.
- 오버킬 초과분은 폐기하며, `demand`는 음수 허용(0 clamp 없음)으로 처리한다.
- 연쇄 효과는 FIFO 큐로 빈 큐가 될 때까지 처리한다.

8. Advisor/Decree 유효하지 않은 타겟 처리

- 유효하지 않은 타겟은 입력 단계에서 차단한다.
- 비정상 시전 유입 시 `no-op` 처리하며, 쿨다운/소모품/부가비용은 소모하지 않는다.
- 유효하지 않은 시도에는 토스트 + 붉은 플래시를 노출한다.

## 12) v0 테스트 파라미터 잠금 (2026-02-10)

1. 초기값

- `Defense`: 5
- `Stability`: 5
- 시작 주사위: 10
- 시작 `Advisor` 슬롯: 4
- 시작 `Advisor` 보유: 전체 6개 풀에서 랜덤 3개
- 시작 `Decree` 슬롯: 3
- 시작 `Decree` 보유량: 랜덤 2개
- Gold 시스템(v0): 비활성 (`획득/소비/관련 효과` 제외)

2. 턴 파라미터

- 주사위 리필: 보유한 모든 주사위를 턴 시작에 전부 리필
- 상황 생성: 랜덤 풀 기반
- v0 스폰 풀: 위기형 Situation만 포함 (`opportunity` 제외)
- 상황 중복 생성: 허용
- 동시 `Situation` 상한: 없음
- 상황 생성 시점: `1, 5, 9, ...` 턴 시작
- 4턴마다 위험예산을 할당하고, `risk_value` 누적이 예산을 초과하는 순간 즉시 생성 종료
- `risk_value`는 실수형으로 운영하고, v0 작성 범위는 `100.0 ~ 500.0`으로 맞춘다.
- 위험예산 `budget`은 고정값으로 유지한다. (성장 없음)
- 위험예산 고정값: `2000.0`
- 스폰 추첨 방식: 균등 추첨(가중치 없음)
- 주사위 분배 제한: 없음
- 미배치 주사위: 허용
- 롤/조정 단계 재할당: 불가

## 13) 다음 단계 (문서 기준)

- 본 문서 기준으로 수치 없는 시스템 규칙을 먼저 잠금.
- 이후 최소 플레이테스트 명세(상황 수/주사위 수/조언자 수)만 정하고 실험.
- 수치 밸런스는 테스트 로그 기반으로 별도 문서에서 반복 조정.

## 관련 문서

- 아이디어 백로그: `Docs/GAME_IDEA_BACKLOG.md`
- 상황 로스터: `Docs/ENEMY_ROSTER.md` (파일명 유지, 내용은 상황 기준)
- 리셋 마일스톤: `Docs/V0_MILESTONE.md`
- 레포 지도: `Docs/PROJECT_MAP.md`
