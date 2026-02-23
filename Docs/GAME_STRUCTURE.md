# (TBD) - GAME_STRUCTURE
> 역할: 구현 기준이 되는 **확정 기획/규칙/시스템 구조**의 단일 기준 문서입니다.

- 버전: `v0.2`
- 마지막 갱신: `2026-02-23`
- 용어 기준: `Docs/GLOSSARY.md`

---

## 1) 한 줄 정체성

다크 판타지 투기장에서, 플레이어는 **3개의 Clash**에 주사위 Action을 배치하고 Opponent Intent를 읽어 스킬로 흐름을 뒤집는다. 단, **Duel 패배는 즉시 Game Over**이며 **후퇴는 Honor가 허락할 때만 가능**하다.

## 2) 게임 개요

- 프로젝트명: `TBD`
- 장르: 덱(편성) 기반 전술 로그라이크
  - 드로우/핸드 없음. `Roster Deck`은 “이번 Duel에 들고 갈 Squad/Support 편성 리스트”
- 테마: 다크 판타지 검투
- 플레이어 역할: 검투단 운영자

### 승리/패배/Game Over

- Duel 종료 조건: 한쪽 Health이 `<= 0`
- Game Over 조건: **플레이어가 Duel에서 패배(플레이어 Health `<= 0`)**
- Honor는 Game Over 조건이 아니다.
  - Honor는 **후퇴 자원**이다.
  - Honor가 `<= 0`이면 **후퇴 불가**

## 3) 핵심 재미

1) **분배 퍼즐**: Opponent Intent를 보고, 내 Action을 3개 Clash에 어떻게 분배할지 고민
2) **편성 고민**: Supply Limit 안에서 어떤 Squad/Support를 들고 갈지 결정
3) **리스크 관리**: 배치 페이즈마다 “이번 Duel을 계속할지 / 후퇴할지”를 Honor를 기준으로 판단

## 4) 메타 게임 사이클(런 루프)

1) Duel(결투)
2) 보상(Reward)
   - 카드 보상(프로토타입: STS식 선택)
3) 정비(Maintenance)
   - Roster Deck 편집(Supply Limit 준수)
   - Reserves(예비 편성)에서 교체

> 후퇴(Retreat)로 Duel이 종료되면: **보상 없이** 정비로 넘어간다(프로토타입 기준).

## 5) Duel 구조

### 5.1 Duel 시작(1회)

1) Clash 3개 세팅(프로토타입: Duel 동안 고정)
2) Opponent Intent 생성 및 **완전 공개**
3) 리소스 초기화
   - Focus: 최대치로 충전(기본 5)
   - 스킬 쿨다운: 0(사용 가능)
4) Duel Start Triggers
   - Roster Deck의 모든 카드가 1회씩 트리거
   - 우선순위: **Squad → Support**
   - 같은 타입 내 순서: **Roster Deck 배열 순서**

### 5.2 턴 루프(반복)

페이즈 순서:

1) Reset
   - Clash에 배치되어 있던 모든 Action은 ActionHolder로 돌아온다.

2) Opponent Deploy
   - 적이 Opponent Intent대로 Action을 Clash에 배치한다.

3) Player Deploy
   - 플레이어가 ActionHolder의 Action을 Clash에 배치한다.
   - 이 페이즈에서 Retreat 가능(보스전 제외).
     - Retreat 가능 조건: `Honor > 0`
     - Retreat 시: Duel 즉시 종료, 보상 없음, `Honor -= 1`, 최소 0으로 clamp

4) Roll
   - Clash에 배치된 모든 Action의 주사위를 굴린다.
   - 굴림 결과는 “기본 굴림(Base) → 보정(Mod) → 최종 Attack Result”로 기록되며 UI에 반영되어야 한다.

5) Skill
   - 플레이어가 스킬/효과로 Clash를 조정한다.
   - 재배치/유인책 등 이동 스킬은 **굴림값(최종 Attack Result) 유지**

6) ClashResolve
   - Clash를 **하나씩 순서대로(0→1→2) 판정**한다.
   - **각 Clash 판정 직후 양측 Health을 즉시 체크**하며, `<= 0`이면 그 즉시 Duel이 종료될 수 있다.
   - Clash 판정 결과(Outcome)에 따라 해당 Clash에 정의된 `outcomeEffects`를 발동한다.

7) Turn End(내부 처리)
   - 쿨다운: 턴 종료 시 `-1`
   - Focus 회복: 턴 종료 시 `+2` (최대 5)
   - 턴 종료 트리거(예: 예비군이 ActionHolder에 남아있으면 다음 굴림 +2 누적)

## 6) Duel 핵심 규칙

### 6.1 배치 제한

- 기본적으로 Clash별 배치 수는 **무제한**이다.
- Clash 데이터에 `slotLimit`이 **명시된 경우에만 제한 적용**.
- `slotLimit`을 초과하는 배치/이동은 **불가능**(입력 단계에서 차단)

### 6.2 주사위/수치

- Action은 Attack(dX)를 가진다.
  - 예: `Attack 4` = d4
- Roll에서 Base를 굴린다.
  - Base Attack Result 범위: `1..Attack`
- 보정은 기본적으로 Attack Result에 적용된다.
  - Attack Result 최소: 1
  - Attack Result 최대: 없음
- Base Attack 값을 직접 바꾸는 방식은 프로토타입(P0)에서 금지한다.
- 단, Modifier를 통한 런타임 Attack 보정은 허용한다.

### 6.3 Total Attack 계산

- Clash Total Attack는 다음의 합으로 계산한다.
  1) Clash 내 모든 Action의 **최종 Attack Result 합**
  2) Clash 단위 보너스(예: Reinforce로 부여되는 `+2`)

> Reinforce는 “Total Attack 보너스”이며, **Action의 Attack Result가 변하지 않는다**(UI 예외 표기 필요).

### 6.4 판정(Outcome)

- `Total Attack` 비교로 승패를 가른다.
- 승리(Victory): `playerTotalAttack > opponentTotalAttack`
- 무승부(Draw): `playerTotalAttack == opponentTotalAttack`
- 패배(Defeat): `playerTotalAttack < opponentTotalAttack`
- 대승리/대패배 판정은 사용하지 않는다.

### 6.5 Ability 공통 규칙

- 전투 중 별도 소모 자원(Focus Cost 등)은 사용하지 않는다.
- 쿨다운은 Ability 인스턴스 단위로 관리한다.
- 쿨다운은 턴 종료에 `-1`
- Attack 타입 Ability만 Roll 대상이다.
- 동일 Clash에서 Attack 사용은 1회 기준으로 처리한다.
- 이동 효과는 이동 후 Attack Result를 유지한다.

### 6.6 Ability 타입(P0)

- Attack
  - `damage`를 가진다.
  - Clash 판정에서 승리한 쪽이 해당 Clash의 `damage`만큼 상대 Health에 피해를 준다.
- Skill
  - 상황/타이밍 조건에 따라 발동하는 능력.
  - 현재 프로토타입에서 적 Skill 사용은 제외한다.
- Passive
  - 전투 시작 시 적용되거나, 조건부 상시 적용되는 능력.

## 7) 데이터/구현 기준(요약)

- JSON: `Newtonsoft.Json`으로 통일 (`JsonUtility` 금지)
- 정적 데이터는 StreamingAssets 기준으로 로딩(SaCache 활용)
- 문자열 하드코딩 금지:
  - 카드/Action/Clash/스킬 효과 문구는 Localization Key + Args 기반

세부 구현 구조는 `Docs/TECH_ARCHITECTURE.md`를 따른다.

## 8) 확정 사항(P0)

- Clash 수: 3개
- Clash 변경: 프로토타입에서는 Duel 내 고정
- Opponent Intent: 완전 공개
- ClashResolve: Clash 0→1→2 순서대로 판정, 매 판정 후 Health 즉시 체크
- Retreat:
  - Player Deploy에서만
  - Honor > 0일 때만 가능
  - Retreat 시 Duel 종료, 보상 없음, Honor -1, clamp 0
- Focus:
  - 최대 5
  - Duel 시작 시 최대 충전
  - 턴 종료 시 +2 회복
- 쿨다운:
  - 턴 종료 시 -1
- 수치 증감 기본 규칙:
  - 기본은 Attack Result 변화
  - Attack Result 최소 1 / 최대 없음
  - Base Attack 직접 변경은 P0에서 금지(Modifier를 통한 런타임 Attack 보정은 허용)
- 배치 제한:
  - 기본 무제한
  - 명시된 Clash만 slotLimit 적용
