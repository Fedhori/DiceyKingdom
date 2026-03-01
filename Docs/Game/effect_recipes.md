# Effect Recipes

**Role:** Ability 효과를 데이터(`effects`)로 추가할 때 반복 입력을 줄이기 위한 표준 레시피 문서. 구현 코드 변경 없이 데이터만으로 조합 가능한 범위를 정의한다.

**Last updated:** 2026-03-01

**Canonical refs:** `Docs/Game/Game_Design.md`, `Docs/Game/Glossary.md`

## 목적

- 신규 Ability 추가 시, 효과 설계를 빠르게 합의한다.
- `timing / condition / ops` 조합을 일관된 형태로 유지한다.
- 검증 실패를 사전에 줄인다.

## 스키마 요약

`AbilityDef.effects`의 각 항목은 아래 구조를 사용한다.

```json
{
  "timing": "TurnEnd",
  "condition": { "type": "Always" },
  "ops": [
    {
      "op": "ModifyHealth",
      "scope": "Self",
      "value": 1
    }
  ]
}
```

## 허용 값(현재 검증 기준)

- timing:
  - `DuelStart`, `Deploy`, `Formation`, `Roll`, `Skill`, `Resolve`, `AfterCombat`, `TurnEnd`, `HealthLost`
- condition.type:
  - `Always`, `IsInLoadout`, `OpponentCountEquals`, `OutcomeIsVictory`, `OutcomeIsDefeat`, `OutcomeIsDraw`
  - `OpponentCountGreaterThanSelf`
- op:
  - `ModifyPowerResult`
  - `MoveAbility`
  - `MoveOpponentAbility`
  - `ModifyTotalPower`
  - `ModifyHealth`
  - `AddPowerModifier`
  - `PreventOutgoingDamageOnWin`
  - `DestroyAbility`
  - `ModifyOutgoingDamageOnWin`
  - `PowerMinPercent`

## 레시피

### 1) 턴 종료 시 체력 회복(재생)

```json
{
  "timing": "TurnEnd",
  "condition": { "type": "Always" },
  "ops": [
    {
      "op": "ModifyHealth",
      "scope": "Self",
      "value": 1
    }
  ]
}
```

### 2) 승리 시 추가 피해

```json
{
  "timing": "AfterCombat",
  "condition": { "type": "OutcomeIsVictory" },
  "ops": [
    {
      "op": "ModifyOutgoingDamageOnWin",
      "scope": "Self",
      "value": 1
    }
  ]
}
```

### 3) 패배 시 파괴

```json
{
  "timing": "AfterCombat",
  "condition": { "type": "OutcomeIsDefeat" },
  "ops": [
    {
      "op": "DestroyAbility",
      "scope": "Self"
    }
  ]
}
```

### 4) 이번 결투 동안 파워 영구 증가

```json
{
  "timing": "TurnEnd",
  "condition": { "type": "Always" },
  "ops": [
    {
      "op": "AddPowerModifier",
      "target": "Power",
      "layer": "Duel",
      "mode": "Add",
      "value": 1
    }
  ]
}
```

### 5) 롤 하한선 비율 지정(균등 분포)

```json
{
  "timing": "Roll",
  "condition": { "type": "Always" },
  "ops": [
    {
      "op": "PowerMinPercent",
      "scope": "Self",
      "value": 50
    }
  ]
}
```

### 6) 체력 감소 이벤트 1회당 파워 증가

```json
{
  "timing": "HealthLost",
  "condition": { "type": "Always" },
  "ops": [
    {
      "op": "AddPowerModifier",
      "scope": "Self",
      "target": "Power",
      "layer": "Duel",
      "mode": "Add",
      "value": 3
    }
  ]
}
```

### 7) 상대 수가 더 많을 때 파워 배율 증가

```json
{
  "timing": "Formation",
  "condition": { "type": "OpponentCountGreaterThanSelf" },
  "ops": [
    {
      "op": "AddPowerModifier",
      "scope": "Self",
      "target": "Power",
      "layer": "Duel",
      "mode": "PercentBonus",
      "value": 100
    }
  ]
}
```

## 작성 체크리스트

- Ability ID는 `ability.<snake_case>` 형식인가?
- `nameLocKey`, `descLocKey`가 `<abilityId>.name`, `<abilityId>.desc`와 정확히 일치하는가?
- Attack/Skill/Passive 타입별 `power`, `cooldown` 제약을 만족하는가?
- 아이콘 파일(`Data/icons/{iconId}.png`)이 존재하는가?
- 효과 문구가 가변 인자를 사용하면 Smart Format이 켜져 있는가?

## 검증

신규 Ability 추가 후에는 별도 스크립트 대신 기존 검증 루트를 사용한다.

- Unity 메뉴: `Tools/Validate Game Data`
- 또는 프로젝트 내 데이터 검증 명령 루트

검증 실패는 자동 수정하지 말고 원인 데이터를 직접 고친다.
