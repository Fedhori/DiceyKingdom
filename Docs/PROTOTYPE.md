# PROTOTYPE
> 역할: 프로토타입 목표/확정표/진행도를 관리하는 **실행 계획 문서**입니다.

- 마지막 갱신: `2026-02-22`
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
- 눈(Attack Result) 변화가 **즉시 UI에 반영**되고, 툴팁/로그로 “기본→보정→최종”을 확인할 수 있다.

---

## 2) 범위

### 포함(P0)

- Battle(전투 1회) 전체 루프
- 전장 3개 고정 + Enemy Intent 공개
- Roster Deck(드로우 없음) + Battle Start Triggers(Squad→Support)
- Troop Attack/Attack Result/Total Attack 계산
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
| Attack Result | 최소 1, 최대 없음 |
| 수치 증감 기본 | Attack Result 변화(예외: Reinforce는 Total Attack +2) |
| Base Attack 직접 변경 | P0에서 금지(Modifier 기반 런타임 보정은 허용) |
| 배치 제한 | 기본 무제한, 전장에 slotLimit 명시 시만 제한(초과 배치/이동 불가) |

---

## 4) 작업 계획(작업 단위 분할)

> 원칙
> - **구성 요소를 의존성 순서대로** 구현한다.
> - 각 작업 단위는 “완료 후 즉시 테스트 가능”해야 한다.
> - 과도한 폴리싱 금지(연출/애니/아트는 기능 검증 후).
> - 별도 디버그 씬은 만들지 않는다. 필요 검증은 `GameScene`에 단계적으로 붙인다.

각 작업 단위는 아래 형식으로 정의한다.
- 산출물: 무엇이 생기나(코드/데이터/씬/툴)
- 수동 테스트: 사람이 직접 확인하는 체크리스트
- 자동 테스트(가능하면): EditMode 테스트/검증 커맨드

---

### 작업 1: Domain 상태 모델 고정(BattleState 계층)

**목표:** 전투 규칙이 올라갈 최소 상태 모델을 먼저 확정한다.

- 산출물
  - `Game.Domain.Battle`
    - `BattleState`
    - `BattlefieldState`
    - `TroopInstance`
  - 상태 최소 필드
    - Morale / Mana / Turn / Cooldown / Camp / Battlefields / EnemyIntent 참조
    - Troop의 `baseRoll`, `modifiers`, `attackResult`

- 수동 테스트
  - 전투 시작/턴 전환 시 상태 객체가 null 없이 유지되는가
  - 전장 3개 상태가 항상 초기화되는가
  - Troop에 `base/mod/final` 기록 슬롯이 유지되는가

- 자동 테스트(가능하면)
  - 상태 생성 테스트(기본값/필수 컬렉션 null 방지)
  - 전장 개수 강제(3) 초기화 테스트

**완료 기준(DoD):** 이후 규칙 구현에서 재사용 가능한 Domain 상태 모델이 고정됨

#### 작업 1 서브테스크(구현 순서)

- [x] `T1-01` 상태 타입 계약 고정
  - `BattleState`, `BattlefieldState`, `TroopInstance`의 최소 필드명/자료형을 먼저 확정한다.
  - 기준: `Docs/GAME_STRUCTURE.md`의 전투 규칙 + `Docs/TECH_ARCHITECTURE.md`의 상태 모델.

- [x] `T1-02` 파일/네임스페이스 스켈레톤 생성
  - 경로: `Assets/Scripts/Game/Domain/Battle`
  - 네임스페이스: `Game.Domain.Battle`
  - 클래스 3종 생성: `BattleState`, `BattlefieldState`, `TroopInstance`

- [x] `T1-03` `BattleState` 초기화 정책 구현
  - 필수 컬렉션(`cooldowns`, `campTroops`, `battlefields`)이 null이 되지 않도록 기본 생성자를 구현한다.
  - 전장 상태는 기본 3개로 초기화한다.
  - 턴/리소스 기본값(예: `turnIndex=0`)을 명시한다.

- [x] `T1-04` `BattlefieldState` 최소 구조 구현
  - `playerTroops`, `enemyTroops`, `totalAttackBonusPlayer`, `totalAttackBonusEnemy`, `slotLimit` 필드를 구현한다.
  - 컬렉션 null 방지 초기화를 적용한다.

- [x] `T1-05` `TroopInstance` 굴림 추적 필드 구현
  - `troopDefId`, `Attack`, `baseRoll`, `attackResult`, `modifiers`, `tags` 필드를 구현한다.
  - `modifiers`는 이후 로그/툴팁 확장을 고려해 리스트 구조로 고정한다.

- [x] `T1-06` 상태 정합성 보조 메서드 추가
  - 최소 1개 메서드(예: `EnsureInitialized`)로 런타임 중 null 복구/개수 보정을 제공한다.
  - 책임은 “규칙 계산”이 아니라 “상태 안전성 보장”에 한정한다.

- [x] `T1-07` EditMode 테스트 추가
  - `BattleState` 생성 시 필수 컬렉션 null 아님
  - `battlefields.Count == 3`
  - `TroopInstance`의 `base/mod/final` 필드가 생성 직후 접근 가능

- [x] `T1-08` 컴파일/기본 검증
  - Assembly 컴파일 에러 0 확인
  - 테스트가 있으면 최소 작업 1 테스트 통과 확인
  - 실패 시 원인과 수정 범위를 즉시 기록한다.

---

### 작업 2: Phase 오케스트레이션 고정(BattlePhaseRunner)

**목표:** 페이즈 진행 순서와 전환 규칙을 Application 계층에서 고정한다.

- 산출물
  - `Game.Application.Battle.BattlePhaseRunner`
  - `Phase enum`
    - Recall / EnemyDeploy / PlayerDeploy / Roll / Tactics / Resolve
  - 전환 API
    - `StartBattle`
    - `AdvanceToNextPhase`
    - `TryRetreat` (PlayerDeploy + Stability 조건 강제)

- 수동 테스트
  - 페이즈 순서가 확정 규칙대로만 진행되는가
  - 허용되지 않은 시점의 Retreat 요청이 거부되는가
  - Resolve 이후 Turn End 내부 처리로 돌아가는가

- 자동 테스트
  - 페이즈 전이 테스트(정상/비정상 전이)
  - Retreat 조건 테스트(Stability > 0, PlayerDeploy 한정)

**완료 기준(DoD):** UI 없이도 턴 루프 순서를 코드로 강제 가능

#### 작업 2 서브테스크(구현 순서)

- [x] `T2-01` 페이즈 타입 고정
  - `BattlePhase` enum을 고정한다.
  - 순서: `Recall -> EnemyDeploy -> PlayerDeploy -> Roll -> Tactics -> Resolve`

- [x] `T2-02` 실패 사유 타입 고정
  - `BattlePhaseFailureReason` enum을 고정한다.
  - 최소 항목: `None`, `NotStarted`, `InvalidPhase`, `StabilityInsufficient`, `AlreadyEnded`

- [x] `T2-03` Runner 스켈레톤 생성
  - 경로: `Assets/Scripts/Game/Application/Battle/BattlePhaseRunner.cs`
  - 네임스페이스: `Game.Application.Battle`
  - 순수 C# 클래스로 구현(MonoBehaviour 금지)
  - 상태는 `BattleState`를 직접 수정(in-place)한다.

- [x] `T2-04` 공개 API 고정
  - `StartBattle()`
  - `AdvanceToNextPhase()`
  - `TryRetreat()`
  - 반환은 `bool`로 단순화하고, 실패 사유는 `LastFailureReason` 프로퍼티로 노출한다.

- [x] `T2-05` StartBattle 구현
  - 시작 시 현재 페이즈를 `Recall`로 둔다.
  - 이미 종료 상태면 실패 처리한다(`AlreadyEnded`).

- [x] `T2-06` AdvanceToNextPhase 구현
  - 확정 순서대로만 전이한다.
  - `Resolve` 다음에는 내부 Turn End 처리 후 `Recall`로 복귀한다.
  - Turn End 최소 처리: `turnIndex` 증가.

- [x] `T2-07` TryRetreat 구현
  - `PlayerDeploy`에서만 허용한다.
  - `stability > 0`일 때만 성공한다.
  - 성공 시 `stability -= 1` (최소 0), `isBattleEnded = true`.

- [x] `T2-08` 거부/보정 시 Warning 정책 적용
  - 잘못된 전이/잘못된 Retreat 요청은 상태 변경 없이 거부한다.
  - 조용한 수정 없이 `Debug.LogWarning`으로 원인을 남긴다.

- [x] `T2-09` EditMode 테스트 추가
  - 정상 페이즈 전이 테스트
  - `Resolve -> Recall` 복귀 + `turnIndex` 증가 테스트
  - Retreat 성공/실패(페이즈 오류, stability 부족) 테스트

- [x] `T2-10` 검증 및 체크리스트 반영
  - Unity EditMode 테스트 통과 확인
  - 작업 2 서브테스크 체크 상태 갱신

---

### 작업 3: 전투 계산 커널(주사위/전투력/판정) 구현

**목표:** Resolve까지의 핵심 계산 규칙을 Domain에 고정한다.

- 산출물
  - Roll 계산(기본 굴림, FaceValue 최소 1)
  - Total Attack 계산
  - Outcome 판정
    - Great Victory / Victory / Draw / Defeat / Great Defeat
  - Resolve 순서 처리(0→1→2 + 매 전장 후 Morale 즉시 체크)

- 수동 테스트
  - Roll 후 `base -> modifiers -> final` 값이 일관되게 갱신되는가
  - Great Victory 조건(`winner >= loser * 2`)이 맞게 동작하는가
  - Resolve 중간에 Morale <= 0이면 즉시 종료되는가

- 자동 테스트
  - 판정식 테스트
  - FaceValue 하한 테스트
  - Resolve 조기 종료 테스트

**완료 기준(DoD):** 전투 결과 계산이 UI와 분리된 순수 로직으로 검증 가능

#### 작업 3 서브테스크(구현 순서)

- [x] `T3-01` 계산 타입/진입점 고정
  - `BattleOutcome` enum 추가
  - `BattleSimulator` 정적 진입점(`RollTroop`, `ApplyRollFinalization`, `ComputeTotalAttack`, `ComputeOutcome`, `ResolveBattlefieldsInOrder`) 고정

- [x] `T3-02` Roll/최종값 계산 구현
  - `baseRoll` 범위: `1..Attack` (attack이 1 미만이면 warning 후 1로 clamp)
  - 계산식: `(base + add합) * (1 + percent합)` 후 `Floor` 적용
  - 최종 Attack Result 최소 1 clamp

- [x] `T3-03` 전장 전투력 계산 구현
  - 전장 내 Troop `attackResult` 합 + 전장 보너스 합산
  - 누락 troopId/null 항목은 warning 후 무시

- [x] `T3-04` Outcome 판정 구현
  - Draw: 동일 전투력
  - Great Victory / Great Defeat: `winner >= loser * 2`
  - loser가 0일 때 winner가 양수면 Great 판정

- [x] `T3-05` Resolve 1개 전장 처리 구현
  - 전장 전투력 계산 -> Outcome 판정 -> Morale 반영
  - Morale `<= 0`이면 전투 종료 처리

- [x] `T3-06` Resolve 순차 처리 구현
  - 전장 0 -> 1 -> 2 순서 처리
  - 중간에 Morale `<= 0`이면 즉시 중단

- [x] `T3-07` EditMode 테스트 추가
  - 수학 테스트: add/percent/floor/min clamp
  - 전장 테스트: Total Attack/Outcome
  - 전투 테스트: Resolve 순서 + 중간 종료

- [x] `T3-08` 검증 및 체크리스트 반영
  - Unity EditMode 테스트 통과 확인
  - 작업 3 진행도 체크 반영

---

### 작업 4: Effect 시스템(opcode) 최소 세트 구현

**목표:** “효과 확장”의 핵심 구조를 P0 범위에서 완성한다.

- 산출물
  - `EffectResolver` + `OpCode 핸들러(Dictionary<OpCode, IOpHandler>)`
  - P0 opcode 최소:
    - ModifyAttackResult(Add/PercentBonus)
    - MoveTroop(keepAttackResult=true)
    - MoveEnemyTroop(keepAttackResult=true)
    - ModifyTotalAttack(+2)
    - TransformOutcome(Risky/Safe)
    - ModifyMorale
    - AddAttackModifier(layer=Battle/Permanent)
  - 스킬 5종이 호출하는 효과 경로를 opcode 핸들러로 통일

- 수동 테스트
  - Risky: 플레이어 Victory → Great Victory 변환이 동작하는가
  - Safe: 플레이어 Great Defeat → Defeat 변환이 동작하는가
  - Reinforce: Attack Result 변화 없이 Total Attack만 +2 되는지(UI/로그로 확인)

- 자동 테스트
  - opcode 단위 테스트(예: ModifyAttackResult 적용 순서/최소 1)

**완료 기준(DoD):** 전투 효과 적용이 하드코딩 분기 대신 opcode 핸들러 경로로 동작

---

### 작업 5: JSON 데이터 파이프라인(typed DB) + 검증 커맨드

**목표:** 하드코딩 의존을 제거하고 데이터 기반 전투로 전환한다.

- 산출물
  - `DataIndex.json` + Def JSON 묶음
  - `GameDatabase`(typed repository)
  - `Tools/Validate Game Data` 또는 `validate_data` DevCommand

- 수동 테스트
  - JSON 변경 시 전투 동작이 실제로 바뀌는가
  - 참조 오류/금지 op가 즉시 검증 에러로 노출되는가

- 자동 테스트
  - 데이터 로드/참조 해결/검증 테스트(EditMode)

**완료 기준(DoD):** Battle 실행이 코드 하드코딩이 아닌 Def 데이터로 구동됨

- 진행 현황(2026-02-22)
  - `DataIndex.json` + 샘플 Def 세트 추가
  - `GameDatabaseLoader` / `GameDataValidator` / `validate_data` 경로 추가
  - EditMode 데이터 로드/검증 테스트 추가 및 통과
  - Battle 런타임에 Def를 실제 주입하는 연결 단계는 후속 작업으로 유지

---

### 작업 6: GameScene 통합(UI 최소) + BattleDebugPanel 연결

**목표:** 별도 디버그 씬 없이 `GameScene`에서 전투 흐름을 수동 검증 가능하게 만든다.

- 산출물
  - `Game.Presentation.Debug.BattleDebugPanel`
    - StartBattle / EnemyDeploy / PlayerDeploy / Roll / Resolve / Retreat 콜백
  - 최소 UI
    - Morale / Stability / Mana / Phase / 전장별 전투력 텍스트
  - `BattlePhaseRunner`와 연결된 표시 갱신

- 수동 테스트
  - 버튼 클릭으로 페이즈 진행 및 상태 변화가 즉시 반영되는가
  - Retreat가 규칙대로 동작하고 전투 종료가 표시되는가
  - Resolve 결과가 전장 순서대로 반영되는가

- 자동 테스트
  - (가능 시) Panel의 상태 포맷터 단위 테스트

**완료 기준(DoD):** `GameScene` 하나로 P0 전투 루프 수동 검증 가능

#### 작업 6 서브테스크(구현 순서)

- [x] `T6-01` `BattleDebugPanel` 스켈레톤 생성
  - 경로: `Assets/Scripts/Game/Presentation/Debug/BattleDebugPanel.cs`
  - 네임스페이스: `Game.Presentation.Debug`
  - TMP_Text/Button 참조 필드와 콜백 시그니처를 먼저 고정한다.

- [x] `T6-02` 패널 상태 컨텍스트 고정
  - 패널 내부에 `BattleState`, `BattlePhaseRunner`, `selectedTroopId`, `selectedBattlefieldIndex`를 유지한다.
  - 상태 리셋/재시작 시 null이 남지 않게 초기화 경로를 고정한다.

- [x] `T6-03` `StartBattle` 구현(데이터 주입)
  - `GameDataRuntime.CurrentDatabase`를 사용한다.
  - Encounter는 `enc_debug_01` 고정으로 로드한다.
  - 시작 시 전투 상태를 새로 만들고 기본 UI를 즉시 갱신한다.

- [x] `T6-04` Enemy 자동 배치 구현
  - `StartBattle` 직후 Enemy Intent(`enc_debug_01.plans`)를 전장에 자동 배치한다.
  - 배치 결과를 전장 텍스트와 로그에 남긴다.

- [ ] `T6-05` PlayerDeploy 수동 배치 구현
  - 흐름: 병력 선택 -> 전장 선택 -> `DeploySelected` 확정
  - `PlayerDeploy` 페이즈에서만 성공, 나머지 페이즈는 거부 + warning 로그

- [ ] `T6-06` 페이즈별 버튼 활성/비활성 규칙 적용
  - `DeploySelected`: `PlayerDeploy` + 선택 2종 완료 시만 활성
  - `Roll`: `Roll` 페이즈에서만 활성
  - `Resolve`: `Resolve` 페이즈에서만 활성
  - `Retreat`: `PlayerDeploy` + `stability > 0`일 때만 활성

- [ ] `T6-07` `Roll` 일괄 굴림 구현
  - 현재 전장에 배치된 병력 전체를 한 번에 굴린다.
  - 굴림 직후 Attack Result/UI/로그를 즉시 갱신한다.

- [ ] `T6-08` `Resolve` 일괄 처리 구현
  - 전장 0 -> 1 -> 2 순서로 처리한다.
  - 중간에 종료 조건 충족 시 즉시 중단하고 종료 상태를 UI에 반영한다.

- [ ] `T6-09` `Retreat` 처리 고정
  - 성공 시 전투 종료 + `stability -= 1`(최소 0)
  - Retreat 후에는 진행 버튼 잠금, `StartBattle`만 재허용

- [ ] `T6-10` 최소 UI 갱신 루프 완성
  - 표기: `Phase`, `Mana`, `Stability`, `Player/Enemy Morale`, 전장별 Total Attack
  - 선택 상태(`selectedTroopId`, `selectedBattlefieldIndex`)를 항상 화면에 표시한다.

- [ ] `T6-11` 실패 피드백 정책 적용
  - 거부/실패 케이스는 무반응 금지
  - 원인을 로그 패널 한 줄 메시지로 즉시 노출한다.

- [ ] `T6-12` 상태 포맷터 분리 + EditMode 테스트
  - 패널 문자열 포맷을 별도 포맷터 클래스로 분리한다.
  - 포맷터 단위 EditMode 테스트를 추가해 출력 안정성을 검증한다.

---

### 작업 7: 최소 메타 루프(보상 → 정비) + Roster Deck 편집

**목표:** 전투 단일 테스트를 넘어 런 루프를 최소 연결한다.

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
  - 선택한 카드가 다음 전투 Battle Start Triggers에 반영되는가

- 자동 테스트
  - RunState 직렬화/역직렬화(최소)

**완료 기준(DoD):** “전투-보상-정비-전투” 루프 플레이테스트 가능

---

### 작업 8: Localization 정착 + 하드코딩 문자열 제거(최소)

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

- [x] 작업 1 완료
- [x] 작업 2 완료
- [x] 작업 3 완료
- [x] 작업 4 완료
- [ ] 작업 5 완료
- [ ] 작업 6 완료
- [ ] 작업 7 완료
- [ ] 작업 8 완료

---

## 6) “테스트 가능한 작업”을 위한 공통 장치

- 별도 `BattleDebugScene`은 만들지 않는다.
- `GameScene`의 `BattleDebugPanel`을 고정 테스트 베드로 사용한다.
- DevCommand(또는 에디터 메뉴)로 최소 2개 제공
  - `validate_data`
  - `dump_last_battle_log` (최근 전투 로그 덤프)
- 시드 고정 옵션 제공
  - 같은 입력이면 같은 결과가 나와야 디버깅이 쉬움
