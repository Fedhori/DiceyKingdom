# TECH_ARCHITECTURE
> 역할: **현재 Unity 템플릿 코드베이스(Scripts)** 위에, 본 게임의 P0 시스템을 어떻게 얹을지에 대한 구현 기준 문서입니다.

- 마지막 갱신: `2026-02-21`
- 기준 문서: `Docs/GAME_STRUCTURE.md`, `Docs/GLOSSARY.md`

---

## 1) 핵심 결론(최종 권장 구조)

- 기존 템플릿의 강점(1인 개발 친화적인 Composition Root)을 유지한다.
  - `GameApp`(싱글턴) + 인스펙터 wiring + `AppServices/RunServices`
- 새 게임 로직은 신규 루트 네임스페이스 `Game.*` 아래에만 추가한다.
- 전투 규칙/효과는 **Domain(순수 C#)** 로 분리하고, Unity(MonoBehaviour/UI)는 Presentation에서만 다룬다.
- 효과 확장은 “스크립팅 언어/복잡한 룰 엔진” 대신
  - **opcode + handler 딕셔너리(EffectResolver)** 로 해결한다.
- 데이터는 JSON(`Newtonsoft.Json`) + DataIndex(manifest) 기반.
  - StaticDataService 인스펙터에 JSON을 수십 개 등록하지 않기 위해, `DataIndex.json` 1개만 등록하는 구조를 권장.
- 텍스트는 **Unity Localization 키+args** 파이프라인으로 end-to-end를 고정한다.

---

## 2) 기존 템플릿과의 접점(변경 최소)

### 2.1 유지

- `Bootstrap` / `GameApp` / `AppServices`
- `SaCache`(StreamingAssets 캐시)
- `StaticDataService`(부팅 시 정적 JSON 로드)
- `LocalizationUtil`(LocalizedString 래핑)
- `DevCommandService`, `TooltipService`, `Modal/Toast` 등 UI 공용 서비스

### 2.2 추가(권장)

- `GameDatabaseService` (MonoBehaviour)
  - `GameApp` 인스펙터에 추가하여 AppServices로 접근 가능하게
  - 내부에 `GameDatabase`(typed def cache) 보유

- `BattleService` 또는 `BattleController` (MonoBehaviour)
  - Battle Debug Panel / Battle Scene에서 전투 흐름을 구동

- `Game/` 폴더 하위 신규 코드

---

## 3) 폴더/네임스페이스(권장)

> 기존 템플릿 폴더를 억지로 옮기지 않는다.
> 신규 코드는 `Assets/Scripts/Game/*` 아래로만 추가한다.

```
Assets/Scripts/
  Game/
    Domain/
      Battle/
      Dice/
      Effects/
      Logging/
    Application/
      Battle/
      Run/
    Infrastructure/
      Data/
      Validation/
    Presentation/
      Battle/
      Debug/
      Tooltip/
```

- 네임스페이스는 파일 경로와 일치하도록:
  - `Game.Domain.*`
  - `Game.Application.*`
  - `Game.Infrastructure.*`
  - `Game.Presentation.*`

---

## 4) 데이터 파이프라인(JSON)

### 4.1 핵심 아이디어: DataIndex.json

문제: `StaticDataService`의 entries 인스펙터 등록 방식은 데이터가 늘수록 유지보수가 힘들다.

해결:
- `StaticDataService`에는 **단 1개 엔트리**만 등록한다.
  - key: `data_index`
  - relativePath: `Data/DataIndex.json`
- `GameDatabase`는 DataIndex를 읽고, 거기에 나열된 JSON들을 SaCache로 로딩해 typed def로 파싱한다.

### 4.2 권장 파일 배치

```
Assets/StreamingAssets/
  Data/
    DataIndex.json
    battle_config.json
    run_config.json
    battlefields/*.json
    troops/*.json
    cards/*.json
    skills/*.json
    encounters/*.json
```

### 4.3 typed DB 구조

- `GameDatabase`
  - `Dictionary<string, BattlefieldDef>`
  - `Dictionary<string, TroopDef>`
  - `Dictionary<string, CardDef>` (Squad/Support 공통)
  - `Dictionary<string, SkillDef>`
  - `Dictionary<string, EncounterDef>`
  - `BattleConfigDef`, `RunConfigDef`

로드 흐름(권장):
1) Parse pass: 모든 Def를 일단 파싱해서 dict에 넣는다.
2) Resolve pass: ID 참조를 실제 포인터로 매핑/검증한다.
3) Validation pass: P0 금지 룰(예: Base Attack 직접 변경 op)을 검사한다.

### 4.4 Validation(필수)

- 부팅 시 Validation 실패하면:
  - 개발 빌드: 에러 로그 + 초기화 중단(게임 시작 막기)
  - 릴리스: 안전한 실패(메뉴로 복귀) 정책 중 택1

최소 검증 항목:
- ID 중복
- 참조 누락(없는 troopId/skillId 등)
- `slotLimit < 1` 같은 말이 안 되는 값
- P0 금지 op(`ModifyPower` 등)가 포함되어 있는지

---

## 5) 전투 엔진 구조

### 5.1 런타임 상태 모델(최소)

- `RunState`
  - seed
  - supplyLimit
  - rosterDeck: `List<string cardId>`
  - reserves: `List<string cardId>`
  - stability

- `BattleState`
  - turnIndex
  - playerMorale / enemyMorale
  - mana
  - cooldowns: `Dictionary<string skillId, int>`
  - battlefields: `List<BattlefieldState>` (3개)
  - camp: 플레이어 병력 리스트
  - enemyIntent: 전장별 적 배치 계획
  - logs: `List<BattleLogEvent>`

- `BattlefieldState`
  - playerTroops: `List<TroopInstance>`
  - enemyTroops: `List<TroopInstance>`
  - totalAttackBonusPlayer / totalAttackBonusEnemy
  - slotLimit: nullable(int)

- `TroopInstance`
  - troopDefId
  - Attack
  - baseRoll
  - modifiers(list)
  - attackResult
  - tags

> UI/툴팁 요구사항 때문에 **baseRoll/modifiers/attackResult를 항상 보관**하는 구조를 강제한다.

### 5.2 PhaseRunner(Application)

- `BattlePhaseRunner`가 다음을 담당:
  - 페이즈 순서 호출
  - Player 입력(배치/스킬/후퇴) 반영
  - Domain 호출(roll/resolve)

Domain 쪽은 “규칙 계산”만 맡는다:
- `BattleSimulator`:
  - Roll(기본 굴림)
  - ApplyRollFinalization(눈 보정 반영)
  - ComputeTotalAttack
  - ResolveBattlefield(i)

### 5.3 Resolve 순서(확정)

- 전장 인덱스 순서대로 처리
- 전장 하나 Resolve할 때마다 outcomeEffects 적용 후 즉시 Morale 체크
- 중간 종료 가능

---

## 6) Effect 시스템(확장성 핵심)

### 6.1 원칙

- 효과는 가급적 “데이터 추가”로 확장한다.
- 그러나 1인 개발/직관성 우선이므로, 과도한 DSL/스크립팅 언어는 도입하지 않는다.
- 대신 다음 구조를 사용한다:
  - `EffectResolver` + `Dictionary<OpCode, IOpHandler>`

### 6.2 P0 opcode 최소 세트

- ModifyAttackResult(Add/PercentBonus, min=1)
- MoveTroop(keepAttackResult=true)
- MoveEnemyTroop(keepAttackResult=true)
- ModifyTotalAttack(+2)
- TransformOutcome(Risky/Safe)
- ModifyMorale
- AddAttackModifier(layer=Battle/Permanent)

> Base Attack 직접 변경 op는 P0에서 금지(Validation 단계에서 차단).
> Modifier를 통한 런타임 Attack 보정은 허용한다.

### 6.3 Timing(타이밍)

- BattleStart
- Deploy
- Roll
- Tactics
- Resolve
- TurnEnd

권장 추가(구현 단순화를 위해):
- `RollFinalize` (기본 굴림 후, 모든 “눈 변화” 적용을 한 번에 처리)

### 6.4 Condition(조건) 최소

- Always
- EnemyCountEquals
- IsInCamp
- HasTag

---

## 7) 텍스트/로컬라이징 파이프라인

### 7.1 원칙

- “문자열 하드코딩” 금지(툴팁/로그 포함)
- 데이터(JSON)는 문자열 자체가 아니라
  - `nameLocKey`
  - `descLocKey`
  - `textLocKey`
  - `args`
  만 제공한다.

### 7.2 구조화 로그 이벤트

- 전투 로그는 문자열이 아니라 아래 형태로 저장한다.

```
BattleLogEvent {
  timing/turn/phase,
  locTable,
  locKey,
  args,
  numericBefore,
  numericAfter,
  sourceId,
  targetRef
}
```

UI는 `LocalizationUtil`로 렌더링한다.

### 7.3 UI 피드백(확정 요구)

- Attack Result 변화가 발생하면 주사위 UI에 즉시 반영되어야 한다.
- Troop 툴팁에 아래를 표준으로 표시:
  - Base Roll
  - Modifier 목록(원인/수치)
  - Final Attack Result
- Reinforce는 “Total Attack 보너스”이므로
  - 주사위 눈이 아니라 **전장 UI의 보너스 배지**로 표시

---

## 8) 테스트 전략(현실적 최소)

- Domain 로직은 EditMode 테스트로 검증 가능해야 한다.
- 최소 테스트 항목
  - Great Victory 판정
  - Attack Result 최소 1 / 최대 없음
  - Resolve 순서(0→1→2) + 중간 종료
  - slotLimit 초과 배치/이동 불가
  - Retreat 규칙(Stability > 0)

---

## 9) CODEX CLI 사용 가이드(구현 시작 단위)

- 원칙: “한 번에 큰 기능”이 아니라, `PROTOTYPE.md`의 작업 단위를 그대로 쪼개서 진행
- 매 작업 단위마다
  - 컴파일 성공
  - Battle Debug Panel로 수동 테스트
  - 가능하면 EditMode 테스트 1개 추가

추천 시작 순서:
1) `Assets/Scripts/Game` 폴더/네임스페이스 스켈레톤 생성
2) Domain BattleState/Simulator + 최소 테스트
3) Battle Debug Panel(표시/버튼) 붙여서 수동 테스트
4) DataIndex + GameDatabase 로더 도입
