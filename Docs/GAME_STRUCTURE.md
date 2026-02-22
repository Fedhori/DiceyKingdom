# (TBD) - GAME_STRUCTURE
> 역할: 구현 기준이 되는 **확정 기획/규칙/시스템 구조**의 단일 기준 문서입니다.

- 버전: `v0.2`
- 마지막 갱신: `2026-02-21`
- 용어 기준: `Docs/GLOSSARY.md`

---

## 1) 한 줄 정체성

중세 다크 판타지에서, 왕은 **3개의 전장**에 주사위 병력을 분배하고 적의 의도를 읽어 스킬로 전황을 뒤집는다. 단, **전투 패배는 즉시 게임오버**이며 **후퇴는 Stability가 허락할 때만 가능**하다.

## 2) 게임 개요

- 프로젝트명: `TBD`
- 장르: 덱(편성) 기반 전술 로그라이크
  - 드로우/핸드 없음. `Roster Deck`은 “이번 전투에 들고 갈 Squad/Support 편성 리스트”
- 테마: 중세 다크 판타지
- 플레이어 역할: 왕(왕국 수호)

### 승리/패배/게임오버

- 전투 종료 조건: 한쪽 Morale이 `<= 0`
- 게임오버 조건: **플레이어가 전투에서 패배(플레이어 Morale `<= 0`)**
- Stability는 게임오버 조건이 아니다.
  - Stability는 **후퇴 자원**이다.
  - Stability가 `<= 0`이면 **후퇴 불가**

## 3) 핵심 재미

1) **분배 퍼즐**: 적 의도(Enemy Intent)를 보고, 내 병력을 3개 전장에 어떻게 분배할지 고민
2) **편성 고민**: Supply Limit 안에서 어떤 Squad/Support를 들고 갈지 결정
3) **리스크 관리**: 배치 페이즈마다 “이번 전투를 계속할지 / 후퇴할지”를 Stability를 기준으로 판단

## 4) 메타 게임 사이클(런 루프)

1) 전투(Battle)
2) 보상(Reward)
   - 카드 보상(프로토타입: STS식 선택)
3) 정비(Maintenance)
   - Roster Deck 편집(Supply Limit 준수)
   - Reserves(예비 편성)에서 교체

> 후퇴(Retreat)로 전투가 종료되면: **보상 없이** 정비로 넘어간다(프로토타입 기준).

## 5) 전투 구조

### 5.1 전투 시작(1회)

1) 전장 3개 세팅(프로토타입: 전투 동안 고정)
2) Enemy Intent 생성 및 **완전 공개**
3) 리소스 초기화
   - Mana: 최대치로 충전(기본 5)
   - 스킬 쿨다운: 0(사용 가능)
4) Battle Start Triggers
   - Roster Deck의 모든 카드가 1회씩 트리거
   - 우선순위: **Squad → Support**
   - 같은 타입 내 순서: **Roster Deck 배열 순서**

### 5.2 턴 루프(반복)

페이즈 순서:

1) Recall
   - 전장에 배치되어 있던 모든 병력은 Camp로 돌아온다.

2) Enemy Deploy
   - 적이 Enemy Intent대로 병력을 전장에 배치한다.

3) Player Deploy
   - 플레이어가 Camp의 병력을 전장에 배치한다.
   - 이 페이즈에서 Retreat 가능(보스전 제외).
     - Retreat 가능 조건: `Stability > 0`
     - Retreat 시: 전투 즉시 종료, 보상 없음, `Stability -= 1`, 최소 0으로 clamp

4) Roll
   - 전장에 배치된 모든 병력의 주사위를 굴린다.
   - 굴림 결과는 “기본 굴림(Base) → 보정(Mod) → 최종 Attack Result”로 기록되며 UI에 반영되어야 한다.

5) Tactics
   - 플레이어가 스킬/효과로 전장을 조정한다.
   - 재배치/유인책 등 이동 스킬은 **굴림값(최종 Attack Result) 유지**

6) Resolve
   - 전장을 **하나씩 순서대로(0→1→2) 판정**한다.
   - **각 전장 판정 직후 양측 Morale을 즉시 체크**하며, `<= 0`이면 그 즉시 전투가 종료될 수 있다.
   - 전장 판정 결과(Outcome)에 따라 해당 전장에 정의된 `outcomeEffects`를 발동한다.

7) Turn End(내부 처리)
   - 쿨다운: 턴 종료 시 `-1`
   - 마나 회복: 턴 종료 시 `+2` (최대 5)
   - 턴 종료 트리거(예: 예비군이 Camp에 남아있으면 다음 굴림 +2 누적)

## 6) 전투 핵심 규칙

### 6.1 배치 제한

- 기본적으로 전장별 배치 수는 **무제한**이다.
- 전장 데이터에 `slotLimit`이 **명시된 경우에만 제한 적용**.
- `slotLimit`을 초과하는 배치/이동은 **불가능**(입력 단계에서 차단)

### 6.2 주사위/수치

- Troop은 Attack(dX)를 가진다.
  - 예: `Attack 4` = d4
- Roll에서 Base를 굴린다.
  - Base Attack Result 범위: `1..Attack`
- 보정은 기본적으로 Attack Result에 적용된다.
  - Attack Result 최소: 1
  - Attack Result 최대: 없음
- Attack 변경은 프로토타입(P0)에서 금지한다.

### 6.3 Total Attack 계산

- 전장 Total Attack는 다음의 합으로 계산한다.
  1) 전장 내 모든 Troop의 **최종 Attack Result 합**
  2) 전장 단위 보너스(예: Reinforce로 부여되는 `+2`)

> Reinforce는 “Total Attack 보너스”이며, **Troop의 Attack Result가 변하지 않는다**(UI 예외 표기 필요).

### 6.4 판정(Outcome)

- `Total Attack` 비교로 승패를 가른다.
- Great Victory 조건:
  - `winnerCombatStrength >= loserCombatStrength * 2`
- Draw:
  - `winnerCombatStrength == loserCombatStrength`

### 6.5 스킬 공통 규칙

- 마나 소모 + 쿨다운 존재
- 쿨다운은 턴 종료에 `-1`
- 이동 스킬(재배치/유인책): 이동 후 Attack Result 유지

### 6.6 스킬(P0 대상)

- 재배치(Redeploy)
  - Mana 2 / Cooldown 2 / Tactics
  - 아군 Troop 1개를 다른 전장으로 이동(슬롯 제한 준수)

- 유인책(Decoy)
  - Mana 2 / Cooldown 2 / Deploy
  - 적 Troop 1개를 다른 전장으로 이동(슬롯 제한 준수)

- 위험한 접근(Risky Approach)
  - Mana 1 / Cooldown 1 / Deploy
  - **플레이어의 Victory를 Great Victory로 승격**(적에게는 적용되지 않음)

- 안전한 접근(Safe Approach)
  - Mana 1 / Cooldown 1 / Deploy
  - **플레이어의 Great Victory를 Victory로 강등**(적에게는 적용되지 않음)
  - 용도 예: Great Victory 트리거에 리스크가 있는 전장 효과를 회피

- 증원(Reinforce)
  - Mana 2 / Cooldown 2 / Tactics
  - 선택한 전장에 아군 Total Attack `+2`(Attack Result 변화 없음)

## 7) 데이터/구현 기준(요약)

- JSON: `Newtonsoft.Json`으로 통일 (`JsonUtility` 금지)
- 정적 데이터는 StreamingAssets 기준으로 로딩(SaCache 활용)
- 문자열 하드코딩 금지:
  - 카드/병력/전장/스킬 효과 문구는 Localization Key + Args 기반

세부 구현 구조는 `Docs/TECH_ARCHITECTURE.md`를 따른다.

## 8) 확정 사항(P0)

- 전장 수: 3개
- 전장 변경: 프로토타입에서는 전투 내 고정
- Enemy Intent: 완전 공개
- Resolve: 전장 0→1→2 순서대로 판정, 매 판정 후 Morale 즉시 체크
- Retreat:
  - Player Deploy에서만
  - Stability > 0일 때만 가능
  - Retreat 시 전투 종료, 보상 없음, Stability -1, clamp 0
- Mana:
  - 최대 5
  - 전투 시작 시 최대 충전
  - 턴 종료 시 +2 회복
- 쿨다운:
  - 턴 종료 시 -1
- 수치 증감 기본 규칙:
  - 기본은 Attack Result 변화
  - Attack Result 최소 1 / 최대 없음
  - Attack 변경은 P0에서 금지
- 배치 제한:
  - 기본 무제한
  - 명시된 전장만 slotLimit 적용
