# PROTOTYPE
> 역할: 프로토타입 목표/확정표/진행도를 관리하는 **실행 계획 문서**입니다.

- 마지막 갱신: `2026-02-21`
- 기준 문서: `Docs/GAME_STRUCTURE.md`, `Docs/TECH_ARCHITECTURE.md`

---

## 1) 프로토타입 목표(P0)

### P0 한 줄 목표

**플레이어가 직접 조작**하여,
- 3개 전장에 병력을 배치하고
- 주사위를 굴리고(눈 변화 UI 확인)
- 스킬로 전장을 조정한 다음
- 전장을 순서대로 Resolve 하여 승/패/후퇴/게임오버까지 이어지는

최소 전투 루프를 **플레이테스트 가능한 형태**로 완성한다.

### P0 성공 기준(DoD 핵심)

- 플레이어가 “배치 → 굴림 → 조정 → Resolve → 전투 종료”를 최소 2턴 이상 수행할 수 있다.
- Retreat가 Stability 규칙대로 동작한다.
- 전투 패배(플레이어 Morale `<= 0`)가 즉시 게임오버로 처리된다.
- 눈(Face Value) 변화가 **즉시 UI에 반영**되고, 툴팁/로그로 “기본→보정→최종”을 확인할 수 있다.

---

## 2) 범위

### 포함(P0)

- Battle(전투 1회) 전체 루프
- 전장 3개 고정 + Enemy Intent 공개
- Roster Deck(드로우 없음) + Battle Start Triggers(Squad→Support)
- Troop Power/Face Value/Combat Strength 계산
- Outcome 판정 + 전장 outcomeEffects(최소 Morale 피해)
- 스킬 5종: Redeploy / Decoy / Risky / Safe / Reinforce
- Retreat(Stability 규칙 포함)
- JSON 데이터 로딩/검증(최소 스키마)
- Unity Localization 기반 텍스트 출력(키+args)
- 디버그 로그 패널(구조화 로그 기반)

### 제외(P0)

- 전장 랜덤 변경
- 상점/이벤트/유물/강화/소모품
- 장기 런 맵/분기
- 완전한 카드 밸런스/아트/연출 폴리싱

---

## 3) P0 확정표

| 항목 | 값 |
|---|---|
| 전장 수 | 3개 |
| 전장 변경 | 전투 내 고정 |
| Enemy Intent | 완전 공개 |
| Resolve | 전장 0→1→2 순서대로 처리, 매 전장 처리 후 Morale 즉시 체크 |
| Retreat 조건 | Player Deploy에서만, Stability > 0 |
| Retreat 결과 | 전투 종료, 보상 없음, Stability -1, clamp 0 |
| 게임오버 | 전투 패배(플레이어 Morale <= 0) |
| Mana | Max 5, 전투 시작 시 Max, 턴 종료 +2 |
| Cooldown | 턴 종료 -1 |
| Face Value | 최소 1, 최대 없음 |
| 수치 증감 기본 | Face Value 변화(예외: Reinforce는 Combat Strength +2) |
| Power 변경 | P0에서 금지 |
| 배치 제한 | 기본 무제한, 전장에 slotLimit 명시 시만 제한(초과 배치/이동 불가) |

---

## 4) 작업 계획(작업 단위 분할)

> 원칙
> - **가장 빠르게 플레이테스트 가능한 상태**에 도달한다.
> - 각 작업 단위는 “완료 후 즉시 테스트 가능”해야 한다.
> - 과도한 폴리싱 금지(연출/애니/아트는 기능 검증 후).

각 작업 단위는 아래 형식으로 정의한다.
- 산출물: 무엇이 생기나(코드/데이터/씬/툴)
- 수동 테스트: 사람이 직접 확인하는 체크리스트
- 자동 테스트(가능하면): EditMode 테스트/검증 커맨드

---

### 작업 0: 전투 디버그 씬(수직 슬라이스 최소) 만들기

**목표:** "클릭으로 전투 1턴"이 가능한 상태를 만든다. (아직 JSON 데이터 없이 하드코딩도 허용)

- 산출물
  - `BattleDebugScene` 또는 기존 GameScene에 `BattleDebugPanel`
  - 버튼:
    - Start Battle
    - Enemy Deploy (또는 Next Phase)
    - Player Deploy (간단 UI로 배치)
    - Roll
    - Resolve
    - Retreat
  - 화면에 항상 표시:
    - 플레이어/적 Morale
    - Stability
    - Mana + 쿨다운
    - 전장별 배치 목록 + Combat Strength

- 수동 테스트
  - 전투 시작 시 Battle Start Triggers가 1회 실행되는가(Squad→Support)
  - Player Deploy에서 병력 배치가 가능한가
  - Roll 후 Face Value가 표시되는가
  - Resolve 후 Morale이 변하는가
  - Resolve가 전장 0→1→2 순서로 처리되는가(로그로 확인)

- 자동 테스트(가능하면)
  - Great Victory 판정식(winner >= loser*2)
  - Face Value 최소 1

**완료 기준(DoD):** 사용자가 직접 버튼/간단 배치 UI로 전투 1턴을 끝까지 진행 가능

---

### 작업 1: Domain 전투 엔진 + 구조화 로그(Event Log) 고정

**목표:** UI가 아니라도 “같은 입력이면 같은 결과”가 나오는 전투 엔진을 만든다.

- 산출물
  - `Game/Domain/Battle/*` : BattleState, BattlefieldState, TroopInstance 등
  - `Game/Application/Battle/BattlePhaseRunner` : 페이즈 진행 오케스트레이션
  - 구조화 로그:
    - 문자열이 아니라 `locKey + args + before/after` 형태로 저장
    - 로그 패널은 이 이벤트를 렌더링

- 수동 테스트
  - Roll 결과가 `base → modifiers → final` 형태로 로그에 남는가
  - 이동(재배치/유인책) 후에도 Face Value가 유지되는가

- 자동 테스트
  - 시드 고정 + 입력 타임라인으로 결과 재현(리플레이 최소)

**완료 기준(DoD):** “로그만 보고도 전투가 올바르게 진행되는지” 검증 가능

---

### 작업 2: JSON 데이터 파이프라인(typed DB) + 데이터 검증 커맨드

**목표:** 하드코딩을 걷어내고, 데이터를 바꾸면 게임이 바뀌는 상태로 전환한다.

- 산출물
  - `DataIndex.json`(파일 목록/경로)
  - Def JSON:
    - BattleConfig/RunConfig
    - BattlefieldDef
    - TroopDef
    - Squad/Support/Skill Def
    - EncounterDef(Enemy Intent)
  - `GameDatabase`(typed repository)
  - `Tools/Validate Game Data` 메뉴 또는 DevCommand `validate_data`

- 수동 테스트
  - JSON 수정 후 실행하면 실제 동작이 바뀌는가
  - 참조 오류(존재하지 않는 id)가 있으면 즉시 에러로 잡히는가

- 자동 테스트
  - 데이터 전체 로드/참조 검증 테스트(EditMode)

**완료 기준(DoD):** P0 전투가 전부 JSON 기반으로 돌아감

---

### 작업 3: Effect 시스템(opcode) 최소 세트 + 스킬 5종 구현

**목표:** “효과 확장”의 핵심 구조를 P0 범위에서 완성한다.

- 산출물
  - `EffectResolver` + `OpCode 핸들러(Dictionary<OpCode, IOpHandler>)`
  - P0 opcode 최소:
    - ModifyFaceValue(Add/Mul)
    - MoveTroop(keepFaceValue=true)
    - MoveEnemyTroop(keepFaceValue=true)
    - ModifyCombatStrength(+2)
    - TransformOutcome(Risky/Safe)
    - ModifyMorale
    - AddNextRollFaceBonus(예비군)
  - 스킬 5종이 모두 데이터 기반으로 작동

- 수동 테스트
  - Risky: 플레이어 Victory → Great Victory 변환이 동작하는가
  - Safe: 플레이어 Great Victory → Victory 변환이 동작하는가
  - Reinforce: Face Value 변화 없이 Combat Strength만 +2 되는지(UI/로그로 확인)

- 자동 테스트
  - opcode 단위 테스트(예: ModifyFaceValue 적용 순서/최소 1)

**완료 기준(DoD):** “새 효과 추가”가 op 조합/데이터 추가 중심으로 가능

---

### 작업 4: 최소 메타 루프(보상 → 정비) + Roster Deck 편집

**목표:** 전투 1회로 끝나지 않고, “편성 변경”을 테스트할 수 있게 한다.

- 산출물
  - Reward 화면(카드 3장 중 1장 선택)
  - Maintenance 화면
    - Roster Deck 편집
    - Reserves 이동
    - Supply Limit 초과 시 저장 불가
  - 전투 승리 → Reward → Maintenance → 다음 전투 진입
  - Retreat → (보상 없이) Maintenance

- 수동 테스트
  - 덱 편집이 Supply Limit을 준수하는가
  - 선택한 카드가 다음 전투의 Battle Start Triggers에 반영되는가

- 자동 테스트
  - RunState 직렬화/역직렬화(최소)

**완료 기준(DoD):** “전투-보상-정비-전투” 루프 플레이테스트 가능

---

### 작업 5: Localization 정착 + 하드코딩 문자열 제거(최소)

**목표:** 데이터/로그/툴팁 텍스트가 전부 Localization Key 기반으로 나오게 한다.

- 산출물
  - String Table(ko/en)
  - Def에 name/desc/effect locKey 채움
  - 로그/툴팁이 locKey+args로 출력

- 수동 테스트
  - 언어 변경 시 텍스트가 정상 갱신되는가

**완료 기준(DoD):** P0 플레이 중 사용자에게 보이는 핵심 텍스트가 하드코딩이 아님

---

## 5) 진행도 체크리스트

- [ ] 작업 0 완료
- [ ] 작업 1 완료
- [ ] 작업 2 완료
- [ ] 작업 3 완료
- [ ] 작업 4 완료
- [ ] 작업 5 완료

---

## 6) “테스트 가능한 작업”을 위한 공통 장치

- **Battle Debug Panel**을 항상 유지한다.
  - 신규 기능을 붙일 때마다 즉시 눈으로 검증할 수 있는 “고정 테스트 베드”로 사용
- DevCommand(또는 에디터 메뉴)로 최소 2개 제공
  - `validate_data`
  - `dump_last_battle_log` (최근 전투 로그 덤프)
- 시드 고정 옵션 제공
  - 같은 입력이면 같은 결과가 나와야 디버깅이 쉬움
