# Free or Die - GAME_STRUCTURE
> 역할: 구현 기준이 되는 확정 기획/규칙/시스템 구조의 단일 기준 문서입니다.

- 버전: `v1.17`
- 마지막 갱신: `2026-02-20`
- 상태: `핵심 규칙 26차 확정 + 데이터/DSL/액션큐/로컬라이즈 하이브리드 아키텍처 잠금`

## 1) 한 줄 정체성

다크 판타지 투기장에서 살아남아 자유를 쟁취하는, 카드(AP) + 주사위(해결 방식) 하이브리드 덱빌딩 로그라이크.

## 2) 게임 개요

- 프로젝트명: `Free or Die`
- 장르: 덱빌딩 카드 로그라이크
- 테마: 중세 다크 판타지, 검투사, 투기장
- 플레이어 목표: 연속 전투를 생존하며 마지막 경기까지 도달해 자유를 얻는다.

## 3) 핵심 재미 축

- 패/코스트(AP) 기반의 우선순위 판단
- 주사위 굴림이 만드는 확률 판단과 리스크 관리
- 덱빌딩/강화/장비 선택의 장기 최적화
- 매 런마다 다른 빌드/적 조합/이벤트 조합
- 다크 판타지 투기장 분위기와 서사적 긴장감
- 차별점: 교전이 고정 수치가 아니라 주사위 눈으로 해석되어, 매 판의 양상이 급변한다.

## 4) 패배/종료 조건

- 플레이어 체력 `<= 0`: 즉시 사망, 게임 오버
- 경기 승리: 현재 전투의 모든 적 처치 시 보상 단계 진입
- 런 클리어: 최종 경기 승리 시 자유 획득

## 5) 메타 게임 사이클(런 루프)

1. 경기 시작
2. 경기 종료 후 보상 화면 진입
3. 다음 스테이지 자동 진입(완전 선형)
4. 정비 시간(고정 회복)

### 5-1. 정비(프로토타입) UX

- 정비 단계에서는 고정 회복 결과만 표시
- 프로토타입에서는 추가 선택지 없이 다음 스테이지로 진행

### 5-2. 보상 화면 규칙(확정)

- 보상 화면은 슬더스와 유사한 단일 화면 흐름으로 제공
- 보상 타입:
  - 카드 보상
  - 장비(유물) 보상(해당 전투에서 발생한 경우)
- 프로토타입 범위:
  - 골드/상점/골드 보상 로직은 제외
  - 강화/단련 시스템은 제외
- 카드 보상 규칙:
  - `3장 제시 -> 1장 선택` 또는 `스킵`
  - 일반/엘리트/보스 전투 모두 동일 규칙 적용
  - 보상 화면에서 카드 보상을 보류하고 다른 보상을 먼저 확인 가능
  - 카드 보상을 선택하지 않고 전체 보상을 스킵 가능
  - 보상 화면 종료 시 미수령 보상은 폐기
  - 단일 보상 묶음(3장) 내 중복 카드 제시는 불가
  - 중복 금지는 "해당 보상 묶음 내부"에만 적용(다른 전투 보상과의 중복은 허용)
  - fallback 로직은 사용하지 않음
  - 카드 선정 실패 시 `카드풀 0번 인덱스` 카드를 강제로 제시하고 에러 로그를 남김
  - 시작 카드 `찌르기`/`휘두르기`는 보상 카드풀에서 제외, `결투사`는 보상 카드풀에 포함
- 카드 희귀도 규칙:
  - 카드 희귀도는 `일반(Common) / 고급(Uncommon) / 희귀(Rare)` 3티어 고정
  - 전투 보상 희귀도 기본 분포:
    - 기본 전투: `60 / 37 / 3`
    - 엘리트: `50 / 40 / 10`
    - 보스: `0 / 0 / 100` (희귀 3장 제시)
  - 희귀(Rare) 카드 풀은 최소 `5장` 유지(데이터 가이드라인)
  - 상점 분포(후속 도입 시 적용): `9 / 37 / 54`
  - 희귀 보정(슬더스형, 전투 보상 전역 적용):
    - 런 시작 시 `rareOffset = -5%`
    - 카드 1장이 `Common`으로 생성될 때마다 `rareOffset +1%`
    - 카드 1장이라도 `Rare`로 생성되면 `rareOffset = -5%`로 즉시 리셋(카드 선택 여부와 무관)
    - 오프셋 갱신은 "보상 1묶음(3장) 단위"로 처리:
      - Rare가 1장 이상이면 해당 보상 처리 후 `rareOffset = -5%`
      - Rare가 0장이면 해당 보상의 Common 출현 수만큼 `rareOffset` 증가(예: Common 3장 -> `+3%`)
    - 오프셋은 `Rare` 확률에만 적용하며 보정분은 `Common`에서만 차감
    - `Common`에서 차감 가능한 양을 초과하는 보정분은 무시
- 엘리트 유물 보상 규칙:
  - `1개 제시`
  - `수락/거절` 선택 가능
  - 거절 시 대체 보상 없음
  - 유물 중복 획득 불가

## 6) 경기(전투) 사이클

1. 턴 시작
2. 카드 드로우
3. AP 회복
4. 턴 진행(카드 사용, 주사위 획득/할당/해결)
5. 턴 종료(아래 내부 순서 고정)
6. 다음 턴 적 intent state 준비

### 6-1. 턴 종료 내부 순서(확정)

- (a) 플레이어 미사용 주사위 소멸
- (b) 좌->우 적 순서로 남은 적 주사위를 각각 굴려 플레이어에게 `적 주사위 피해` 적용
- (c) 위 피해 처리가 끝난 뒤 방어도 소멸
- (d) 각 적 `intents(FSM)` 전이 규칙으로 다음 턴 intent state 선정

### 6-2. 전투 기본 파라미터(확정)

- 전투 구도: `1:N` 지원(단일 플레이어 vs 복수 적)
- 동시 적 수 상한: `N max = 3`
- 기본 핸드: `5장`
- 기본 AP: `3`
- 턴 시작 시 AP는 기본값 `3`으로 회복
- 턴 시작 시 손패는 항상 새로 `5장` 드로우
- 턴 종료 시 손패는 기본적으로 전부 버림
- 드로우 더미 부족 시 버림 더미를 셔플해 드로우 더미로 재구성
- 타겟 선택 입력: 드래그 기반 타겟팅(슬더스형)
- 드래그 중 유효 타겟만 강조 표시
- 드래그 타겟팅 실패 시: 행동 취소
- 드래그 실패 피드백: 카드 원위치(추가 토스트/팝업 없음)
- 손패 유지(`보존`) 효과는 후속 확장(Backlog) 항목으로 보류
- 방어도는 `턴 종료 시 적 주사위 피해 처리`가 끝난 뒤 소멸

## 7) 항복 규칙

- 사용 가능 타이밍: 플레이어 턴 시작 직후, 첫 액션 전
- 사용 제한: 이미 액션을 수행한 턴에는 불가
- 비용: 명예 `-1`
- 결과: 즉시 경기 종료, 보상 스킵
- 불가 조건:
  - 명예 `0`
  - 보스전
- 보스전에서는 항복 버튼을 비활성 처리
- 보스전 항복 비활성 사유 토스트/문구는 프로토타입 범위에서 제외(Backlog)
- 예외 카드 `무조건 항복`은 Backlog 항목으로 보류(프로토타입 범위 제외)

## 8) 플레이어 핵심 스탯

- 체력: 생존 자원, `0` 이하면 즉시 사망
- AP: 카드 사용 자원
- 골드: 상점/정비 재화(프로토타입에서는 비활성)
- 장비(유물): 슬롯 제한 없이 획득 즉시 상시 적용
- 덱: 카드 풀
- 명예: 항복 자원

## 9) 전투 핵심 규칙

### 9-1. 적

- 적은 매 턴 intent state가 제시하는 주사위 리스트를 사용한다.
- 플레이어 턴 종료 시 미해결 적 주사위는 각각 굴림하며, 나온 눈만큼 플레이어에게 `적 주사위 피해`를 준다.
- 복수 적 액션은 전장 배치 순서 기준 `좌 -> 우`로 순차 해결한다.
- 적은 `passiveIds`로 분리 참조된 패시브를 가진다(인라인 패시브 정의 금지).
- 적 데이터 최소 스펙: `enemyId`, `hp`, `intents(FSM)`, `passiveIds`

#### intents(FSM) 표준 스키마(필드명 유지)

~~~json
{
  "enemyId": "pit_brute",
  "hp": 28,
  "intents": {
    "start": "A",
    "states": {
      "A": { "dice": [{"side": 6}, {"side": 4}], "next": [{"to": "B", "w": 1}] },
      "B": { "dice": [{"side": 10}, {"side": 2}], "next": [{"to": "A", "w": 1}] }
    }
  },
  "passiveIds": ["pit_brute_opening_clash_plus2"]
}
~~~
- 패턴 타입 예시 1: `A <-> B` 반복(100% 전이)
~~~json
{"start":"A","states":{"A":{"dice":[{"side":6}],"next":[{"to":"B","w":1}]},"B":{"dice":[{"side":8}],"next":[{"to":"A","w":1}]}}}
~~~
- 패턴 타입 예시 2: `A/B` 균등 랜덤(가중치 전이)
~~~json
{"start":"A","states":{"A":{"dice":[{"side":3},{"side":3}],"next":[{"to":"A","w":1},{"to":"B","w":1}]},"B":{"dice":[{"side":4},{"side":2}],"next":[{"to":"A","w":1},{"to":"B","w":1}]}}}
~~~
- 패턴 타입 예시 3: 고정 3턴 루프 `A->B->C->A`
~~~json
{"start":"A","states":{"A":{"dice":[{"side":10},{"side":10}],"next":[{"to":"B","w":1}]},"B":{"dice":[{"side":10},{"side":10}],"next":[{"to":"C","w":1}]},"C":{"dice":[{"side":50}],"next":[{"to":"A","w":1}]}}}
~~~
### 9-2. 카드

- 카드 사용 시 AP 소모.
- 카드가 주사위를 생성할 수 있으며, 생성된 주사위의 사용 가능 방식(격돌/방어/직접 공격)이 카드에 명시된다.
- `1:N` 전투에서 타겟 지정은 드래그 입력으로 선택한다.
- 유효 타겟에 드롭되지 않으면 카드 사용은 취소된다.
- 카드의 `dicePolicy`는 카드 단위 1개만 허용한다(해당 카드가 생성하는 모든 주사위에 동일 적용).

### 9-3. 주사위 사용 방식

- 격돌:
  - 내 주사위를 적 주사위에 대응시킴
  - 양측 굴림 결과 비교
  - 차이만큼 피해 발생 후 해당 주사위 해결
  - 동점일 경우 양측 주사위 모두 소멸(피해 없음)
- 방어:
  - 내 주사위를 방어로 사용
  - 굴림 결과만큼 방어도 획득
  - 방어도는 적용 가능한 모든 피해(미해결 적 주사위 피해 포함)에 우선 차감
  - 방어도는 `턴 종료 시 적 주사위 피해 처리`가 끝난 뒤 소멸
- 직접 공격:
  - 내 주사위를 적에게 직접 사용
  - 굴림 결과만큼 피해

### 9-4. 턴 종료 정리(확정)

- 플레이어가 생성했지만 사용하지 않은 주사위는 턴 종료 시 소멸
- 방어도는 `턴 종료 시 적 주사위 피해 처리`가 끝난 뒤 소멸

### 9-5. 피해 타입/용어 표준(확정)

- 내부 구현 표준: `DamageKind = Clash / PlayerDirect / EnemyDice`
  - `Clash`: 격돌 비교 결과로 발생한 피해
  - `PlayerDirect`: 플레이어가 직접 공격 방식으로 적에게 준 피해
  - `EnemyDice`: 턴 종료 미해결 적 주사위가 굴려 플레이어에게 준 피해
- 용어 규칙:
  - `직접 공격`은 플레이어의 사용 방식 용어로만 사용
  - 플레이어가 턴 종료에 받는 피해는 `적 주사위 피해`로 표준화
  - 기존 `직접 피해` 용어는 문서/구현에서 사용하지 않는다
- 효과 문구 표준:
  - `plate_armor`: `적 주사위 피해 -2`
  - `guard_stance`: `이번 턴 적 주사위 피해 -3`
  - `shield_slave`: `자신 주사위가 남아 있으면, 플레이어가 이 적에게 가하는 PlayerDirect 피해 -2`

### 9-6. 효과 시스템 아키텍처(확정)

#### 9-6-1. 데이터 레이어(JSON, 텍스트 제외)

- 원칙:
  - JSON에는 `name`, `desc` 등 텍스트 필드를 저장하지 않는다
  - 텍스트는 Unity Localization 키 + 템플릿 + 런타임 바인딩으로만 표시한다
  - `dice` 객체는 `side`만 허용한다(`side` 외 필드 금지)
  - 패시브는 `passiveIds` 분리 참조만 허용한다(인라인 정의 금지)

- `CardDef` 최소 필드:
  - `id, cost, rarity, targeting, keywords, dice[{side}], dicePolicy, stats, canPlay, triggers`
  - `dicePolicy`(카드 단위 1개): `usages / stable / contest / resolver`

- `EnemyDef` 최소 필드:
  - `enemyId, hp, intents(FSM), passiveIds`

- `PassiveDef` 최소 필드:
  - `id, stats, triggers`

#### 9-6-2. 소형 DSL(op) v0

- 프로토 범위 기본 op:
  - `gain_ap`
  - `draw`
  - `apply_status`
  - `remove_target_dice_by_sides_top`
  - `add_roll_mod_to_all_target_dice`
  - `add_roll_mod_to_all_self_dice`
  - `modify_damage`
  - `cancel_damage`
  - `gain_block`
  - `end_turn`
  - `deny_action`

- 값 참조(`ValueRef`) 표준:
  - `const(n)`
  - `stat(key)`
  - `ctx(key)` 예: `ctx.rollValue`, `ctx.damageAmount`, `ctx.targetRemainingDiceCount`

#### 9-6-3. TCE(Trigger-Condition-Effect) 표준

- 형식(고정): `event + phase(Pre/Post) + when(condition) + do(op[]) + priority + enqueue`
- `phase`:
  - `Pre`: 수정/취소 가능
  - `Post`: 반응/연쇄(액션 큐 추가)
- `enqueue`:
  - `addToBottom` 기본
  - 즉시 반응은 `addToTop`
- 결정적 순서:
  - `priority` 오름차순 -> `id` 오름차순

예시:
~~~json
{
  "event": "OnResolvePlayerDirect",
  "phase": "Pre",
  "when": [{"lhs":"ctx.targetRemainingDiceCount","op":"==","rhs":0}],
  "do": [{"op":"modify_damage","amount":{"const":6}}],
  "priority": 100,
  "enqueue": "addToTop"
}
~~~
#### 9-6-4. 실행 코어(구현 기준)

- 모든 상태 변화는 `Action`으로만 발생(직접 상태 변경 금지)
- `ActionQueue/Stack`:
  - 기본 `addToBottom`
  - 즉시 반응/가로채기는 `addToTop`
- RNG 결정성:
  - `seed` 고정
  - 스트림 분리: `combatRng`, `rewardRng`
  - 모든 랜덤은 지정 스트림만 사용
- 디버그/재현 최소 요구:
  - `seed` 출력
  - 액션/이벤트 트레이스(콘솔)

#### 9-6-5. 표시(UI) / Localization

- Unity Localization 패키지 사용
- 키 규칙:
  - `card.{id}.name`
  - `card.{id}.desc`
  - `enemy.{id}.name`
  - `passive.{id}.desc`
  - `keyword.{kw}.name`
  - `keyword.{kw}.desc`
- Smart String + 런타임 바인딩:
  - 바인딩 값 출처: `card.stats`, `passive.stats`, `ctx`
  - 텍스트 숫자 하드코딩 금지
- 키워드 토큰 표준:
  - 예: `{kw:Vulnerable}`
  - UI에서 아이콘/툴팁으로 렌더

#### 9-6-6. 구체 예시(데이터 -> 실행 -> 표시)

##### 예시 A) `flash_bomb` (주사위 제거)

JSON(텍스트 제외):
~~~json
{
  "id": "flash_bomb",
  "cost": 1,
  "rarity": "Rare",
  "targeting": "EnemySingle",
  "keywords": ["Exhaust"],
  "dice": [],
  "dicePolicy": {"usages":[],"stable":0,"contest":{},"resolver":"none"},
  "stats": {"removeCount": 2},
  "canPlay": [],
  "triggers": [
    {
      "event": "OnPlayCard",
      "phase": "Post",
      "when": [],
      "do": [
        {"op":"remove_target_dice_by_sides_top","count":{"stat":"removeCount"},"tieBreak":"left_to_right"}
      ],
      "priority": 100,
      "enqueue": "addToBottom"
    }
  ]
}
~~~
실행:
1. 카드 사용 -> `OnPlayCard/Post`
2. `remove_target_dice_by_sides_top` enqueue
3. 대상 적의 남은 주사위를 `side` 기준 내림차순 정렬
4. 상위 2개 제거(동률은 좌->우 고정 순서)

표시(Localization):
- `card.flash_bomb.name`
- `card.flash_bomb.desc = "소멸. 선택한 적의 최고 주사위 {removeCount}개 제거"`
- 바인딩: `{removeCount} <- stats.removeCount`

##### 예시 B) `net_throw` (피해 취소 + 적 주사위 roll mod)

JSON(텍스트 제외):
~~~json
{
  "id": "net_throw",
  "cost": 1,
  "rarity": "Uncommon",
  "targeting": "EnemySingle",
  "keywords": [],
  "dice": [{"side": 6}],
  "dicePolicy": {"usages":["PlayerDirect"],"stable":0,"contest":{},"resolver":"roll"},
  "stats": {"minMod": 1},
  "canPlay": [],
  "triggers": [
    {
      "event": "OnResolvePlayerDirect",
      "phase": "Pre",
      "when": [],
      "do": [
        {"op":"cancel_damage"},
        {"op":"add_roll_mod_to_all_target_dice","amount":{"ctx":"rollValue"},"min":{"stat":"minMod"},"sign":"minus"}
      ],
      "priority": 100,
      "enqueue": "addToTop"
    }
  ]
}
~~~
실행:
1. 직접 공격 해석 직전 `Pre`에서 피해 취소
2. 같은 `rollValue`만큼 대상 남은 주사위 전체 눈 감소
3. 최소값은 `minMod`로 바닥 처리

표시(Localization):
- `card.net_throw.desc = "피해 대신 대상의 모든 주사위 눈을 {ctx.rollValue}만큼 감소(최소 {minMod})"`

##### 예시 C) `buckler` (격돌 승리 시 취약)

JSON(텍스트 제외):
~~~json
{
  "id": "buckler",
  "cost": 1,
  "rarity": "Common",
  "targeting": "EnemySingle",
  "keywords": [],
  "dice": [{"side": 6}],
  "dicePolicy": {"usages":["Clash","Block"],"stable":0,"contest":{},"resolver":"roll"},
  "stats": {"vulnerable": 1},
  "canPlay": [],
  "triggers": [
    {
      "event": "OnClashResolved",
      "phase": "Post",
      "when": [{"lhs":"ctx.result","op":"==","rhs":"win"}],
      "do": [{"op":"apply_status","status":"Vulnerable","stacks":{"stat":"vulnerable"}}],
      "priority": 100,
      "enqueue": "addToBottom"
    }
  ]
}
~~~
실행:
1. 격돌 결과 확정(Post)
2. 승리 조건 만족 시 `Vulnerable 1` enqueue

표시(Localization):
- `card.buckler.desc = "격돌 승리 시 {kw:Vulnerable} {vulnerable}"`

##### 예시 D) `arena_sniper` 패시브 (피격 시 남은 주사위 -3)

EnemyDef:
~~~json
{
  "enemyId": "arena_sniper",
  "hp": 20,
  "intents": {"start":"A","states":{"A":{"dice":[{"side":20}],"next":[{"to":"B","w":1}]},"B":{"dice":[{"side":12},{"side":12}],"next":[{"to":"A","w":1}]}}},
  "passiveIds": ["arena_sniper_on_hit_reduce_self_dice"]
}
~~~
PassiveDef:
~~~json
{
  "id": "arena_sniper_on_hit_reduce_self_dice",
  "stats": {"delta": -3, "min": 1},
  "triggers": [
    {
      "event": "OnDamaged",
      "phase": "Post",
      "when": [],
      "do": [{"op":"add_roll_mod_to_all_self_dice","amount":{"stat":"delta"},"min":{"stat":"min"}}],
      "priority": 100,
      "enqueue": "addToBottom"
    }
  ]
}
~~~
실행:
1. `arena_sniper`가 피해를 받은 직후 Post 발동
2. 현재 남아있는 자신의 모든 주사위 눈을 `-3` 적용(최소 1)

표시(Localization):
- `passive.arena_sniper_on_hit_reduce_self_dice.desc`

##### 예시 E) `old_swordmaster` 패시브 (직접 공격 금지)

EnemyDef:
~~~json
{
  "enemyId": "old_swordmaster",
  "hp": 40,
  "intents": {"start":"A","states":{"A":{"dice":[{"side":25}],"next":[{"to":"B","w":1}]},"B":{"dice":[{"side":10},{"side":10},{"side":10}],"next":[{"to":"A","w":1}]}}},
  "passiveIds": ["old_swordmaster_deny_player_direct_if_has_dice"]
}
~~~
PassiveDef:
~~~json
{
  "id": "old_swordmaster_deny_player_direct_if_has_dice",
  "stats": {},
  "triggers": [
    {
      "event": "OnTryPlayerDirect",
      "phase": "Pre",
      "when": [{"lhs":"ctx.selfRemainingDiceCount","op":">","rhs":0}],
      "do": [{"op":"deny_action","reasonKey":"deny_player_direct"}],
      "priority": 10,
      "enqueue": "addToTop"
    }
  ]
}
~~~
실행:
1. 플레이어가 직접 공격 시도
2. Pre에서 남은 주사위 > 0이면 행동 거부
3. 카드/행동은 취소 처리

표시(Localization):
- `passive.old_swordmaster_deny_player_direct_if_has_dice.desc`
- `ui.reason.deny_player_direct`

## 10) 강화 규칙(후속 범위)

- 본 섹션은 후속 구현 기준이며, 현재 프로토타입에서는 비활성
- 각 카드는 최대 `1회` 강화 가능
- 강화된 카드는 추가 강화 불가

## 11) 콘텐츠 예시 정리

### 11-1. 적 예시

- 공격 시 눈 +1, 액션 `5, 5`
- 격돌 당할 때 눈 +1, 액션 `10`
- 카드 1장 사용 시 랜덤 적 주사위 +1, 액션 `4, 4, 4`
- 소드마스터:
  - 적 주사위가 남아 있는 동안 플레이어 직접 공격 불가

### 11-2. 카드 예시(역할별 압축)

- 시작 카드:
  - `찌르기` (AP 1, 직접 공격/격돌, d6)
  - `휘두르기` (AP 1, 격돌/방어, d6)
- 노출/마무리:
  - `무력화` (AP 1, 직접 공격 d6, 노출 보너스 눈 +6)
  - `처형` (AP 1, 직접 공격 d20, 노출 상태에서만 사용 가능)
- 격돌/방어:
  - `결투사` (AP 1, 격돌 d6/d6)
  - `방진 방격` (격돌 승리 시 피해량만큼 방어도)
- 유틸/디버프:
  - `그물 던지기` (피해 대신 대상 주사위 전체 눈 감소)
  - `자세 가다듬기` (`유리 3`)
  - `모래 뿌리기` (`불리 5`)
- 지정 카드 명확화:
  - `휠윈드`: 선택한 적의 모든 주사위와 각각 격돌하며, 각 격돌마다 내 d12를 새로 굴린다.
  - `무모함(데어데블)`: 자신보다 `면수(side)`가 높은 적 주사위와 격돌 시 +6.
  - `섬광탄 투척`: 최고 주사위 = `면수(side)` 기준, 상위 2개 제거(동률은 좌->우 고정 순서).

### 11-3. 장비 예시

- `plate_armor`: `적 주사위 피해 -2`
- `맞춤 신발`: 격돌 시 눈 +1
- `스멜 솔트`: 첫 턴 AP +1
- `가죽 헬멧`: 받는 `적 주사위 피해`가 5 이상이면 피해 -3
- `쿠나이`: 첫 턴 주사위 4 획득

### 11-4. 강화 예시

- 최고 주사위 x1.5
- 선택 주사위 +3
- 카드 내 모든 주사위 +1
- 카드 코스트 -1
- `승부 +4` 부여
- 모든 주사위 x2 + `소멸` 획득(소멸 없는 카드만)
- 해당 카드에 `방어` 사용 가능 플래그 추가
- `선천성` 획득(전투 시작 시 핸드 유입)

## 12) 설계 리스크(업데이트)

- 규칙 과밀 리스크:
  - `선봉`, `무모`, `소멸`, `선천성`, `승부`, `취약`, `유리/불리` 동시 노출 시 학습 피로 증가
- 확률 체감 왜곡:
  - 대형 주사위와 다중 소형 주사위의 분산 차이로 체감 밸런스 흔들림 가능
- AP 0 카드 폭주:
  - 조건 완화가 겹치면 저코스트 루프가 턴 의사결정을 잠식할 가능성
- 트리거 순서 리스크:
  - Pre/Post 또는 priority 정합성 누락 시 재현성 붕괴 가능

## 13) 키워드 정리(v0.3)

### 13-1. 현재 사용 중 키워드

- `격돌`: 내 주사위를 적 주사위에 대응시켜 양측 굴림 결과를 비교해 해결.
- `방어`: 내 주사위를 사용해 굴림 결과만큼 방어도를 획득.
- `직접 공격`: 내 주사위를 적에게 직접 사용해 굴림 결과만큼 피해.
- `선봉`: 해당 전투에서 첫 카드로만 사용 가능.
- `무모`: 해당 턴 동안 방어도를 획득할 수 없다.
- `노출`: 대상 적의 남은 주사위가 0개인 상태.
- `노출 보너스`: 카드 사용 시 대상이 노출이면 추가로 발동.
- `유리 N`: 2회 굴려 높은 눈 사용, 굴림 1회당 1스택 소모.
- `불리 N`: 2회 굴려 낮은 눈 사용, 굴림 1회당 1스택 소모.
- `안정 N`: 방어로 사용 시 추가 눈 보너스 `+N`.
- `취약 N`: 다음 `N`회 받는 피해를 `x2`로 증폭.
- `승부 N`: 격돌로 사용할 때 추가 눈 보너스 `+N`.
- `소멸`: 사용 후 카드가 런 중 덱에서 제거됨.
- `선천성`: 전투 시작 시 시작 핸드에 포함.
- `항복`: 명예를 소모해 전투를 즉시 종료(보상 없음).

### 13-2. 용어 표준

- `직접 피해` 용어는 사용하지 않는다.
- 플레이어가 턴 종료에 받는 피해는 `적 주사위 피해`로 표기한다.

### 13-3. 키워드 후보 상태

- `준비`, `마무리`, `약화`, `일제 격돌`, `경감`은 현재 설계에서 불필요 판정.
- `보존`은 프로토타입 범위 제외, Backlog 관리.

## 14) 이번 루프 확정 사항

- 기본 핸드: `5장`
- 기본 AP: `3` (턴 시작 회복 기준)
- 전투 구도: `1:N`
- 동시 적 수 상한: `N max = 3`
- 전투 승리 조건: 현재 전투의 모든 적 처치
- 턴 시작 손패: 항상 새로 `5장` 드로우
- 턴 종료 손패: 기본 전부 버림
- 드로우 더미 부족 시: 버림 더미 셔플 후 재구성
- 미해결 적 주사위: 턴 종료에 각각 굴려 플레이어에게 `적 주사위 피해`
- 방어도: `턴 종료 시 적 주사위 피해 처리`가 끝난 뒤 소멸
- 플레이어 미사용 주사위: 턴 종료 시 소멸
- 격돌 동점: 양측 주사위 소멸
- 피해 타입 표준: `DamageKind = Clash / PlayerDirect / EnemyDice`
- 항복 가능 여부: 보스전 여부 + 명예/턴 상태로 런타임 검증
- 장비: 슬롯 제한 없는 유물형 상시 보유
- 플레이어 시작 체력: `50`
- 정비 회복: 매 경기 종료 후 `최대 체력의 40%` 고정 회복(소수점 내림)
- 명예 초기 상한/시작값: `2 / 2`
- 유물 획득(엘리트): 1개 제시, 수락/거절, 중복 불가
- 카드 보상: `3택1 + 스킵`, 내부 중복 금지, 보상 종료 시 미수령 폐기
- 희귀도 확률: 기본 `60/37/3`, 엘리트 `50/40/10`, 보스 `0/0/100`
- 희귀 보정: 런 시작 `-5`, Common 노출당 `+1`, Rare 노출 시 `-5` 리셋
- 프로토타입 제외: 골드/상점, 강화/단련, 무조건 항복 카드
- 스테이지 구조: 총 5, 완전 선형, 3스테이지 엘리트, 5스테이지 보스
- 템플릿: `templateId`, `poolType`, `enemyIds[]`
- 템플릿 최소 수량: 일반 4 / 엘리트 2 / 보스 1
- 콘텐츠 최소량: 일반 적 5종, 엘리트 2종
- 보스 패턴 단계: 단일 패턴(페이즈 없음)
- 시작 덱: `찌르기 x5`, `휘두르기 x4`, `결투사 x1`
- 시작 카드 풀 제외: 찌르기/휘두르기 제외, 결투사 포함
- RNG 재현성: `seed` + `combatRng/rewardRng` 스트림 분리
- 로그: 콘솔 전용, 전투 종료 요약
- 카드 풀 최소 수량: `Common 8 / Uncommon 12 / Rare 5`
- 플레이테스트 1차 성공 기준: 5스테이지 핵심 사이클 경험 가능

## 15) 수치/밸런스 가이드라인(초안)

### 15-1. 플레이어 체력(확정)

- 시작 체력: `50`
- 설계 이유:
  - 고변동 주사위 피해를 허용하면서도 5스테이지 러닝이 끊기지 않는 완충값
  - 항복/명예 선택이 의미를 가지되, 단일 고롤 즉사 빈도 완화

### 15-2. 코스트별 카드 파워 가이드(직접 공격 기준)

- `0코스트`: 권장 기대 피해 `2 ~ 4`
- `1코스트`: 권장 기대 피해 `5 ~ 8`
- `2코스트`: 권장 기대 피해 `9 ~ 13`

### 15-3. 가이드 해석 원칙

- 고정 규칙이 아니라 디자인 보조선으로 사용
- 강한 조건/패널티(무모/소멸/선봉)는 기대값 상향 허용
- 다중 모드/즉시 이득/광역 처리 카드는 기대값 하향 권장

## 16) 개선 제안(후속 검토용)

1. 항복 로직 단순화
   - 조건식: `!isBossStage && honor > 0 && turnStart && !hasTakenAction`
2. 엘리트 보상 테이블 분리
3. 유물/상점 확장 시 경제 테이블 독립 검증

## 17) 최종 목표(현재)

- 목표: `5스테이지`까지 구현된 프로토타입 제작 후 플레이테스트
- 산출물 기준:
  - 플레이 가능 런 루프(전투/보상/정비)
  - 항복/명예/유물/카드보상 핵심 시스템 동작
  - 테스트 가능한 난이도 곡선 확보

