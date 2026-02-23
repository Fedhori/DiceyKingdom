# DATA_SCHEMA
> 역할: 현재 구현 기준 JSON 데이터 구조(스키마) 정의 문서입니다.

- 마지막 갱신: `2026-02-23`
- 직렬화: `Newtonsoft.Json` (`JsonUtility` 금지)

---

## 1) 공통 규칙

- 모든 Def(JSON 루트)는 `schemaVersion`(int) + `id`(string)를 가진다.
- 참조는 파일 경로가 아니라 **id 문자열**로만 한다.
- ID 네이밍은 점(`.`) 표기법을 사용한다.

---

## 2) DataIndex.json

### 목적

- 로딩 대상 JSON 목록을 단일 파일에서 관리한다.

### 필드

- `configs`: config Def 경로 목록
- `clashes`: clash Def 경로 목록
- `actions`: action(=ability) Def 경로 목록
- `cards`: card Def 경로 목록
- `encounters`: encounter Def 경로 목록

### 예시

```json
{
  "schemaVersion": 1,
  "configs": [
    "Data/duel.config.json",
    "Data/run.config.json",
    "Data/player.start.json"
  ],
  "clashes": [
    "Data/clashes/clash.peak.json",
    "Data/clashes/clash.ruins.json",
    "Data/clashes/clash.forest.json"
  ],
  "actions": [
    "Data/actions/action.reservist.json",
    "Data/actions/action.miko.assassin.json",
    "Data/actions/action.dwarf.cannon.json",
    "Data/actions/action.ratkin.json",
    "Data/actions/action.test6.json"
  ],
  "cards": [
    "Data/cards/card.squad.reserves.json",
    "Data/cards/card.squad.miko.json",
    "Data/cards/card.squad.cannon.json",
    "Data/cards/card.squad.ratkin.json",
    "Data/cards/card.support.latest.gear.json"
  ],
  "encounters": [
    "Data/encounters/encounter.debug.01.json"
  ]
}
```

---

## 3) Config

### DuelConfigDef

```json
{
  "schemaVersion": 1,
  "id": "duel.config",
  "clashCount": 3,
  "focusMax": 5,
  "focusRegenPerTurn": 2,
  "cooldownTickPerTurn": -1,
  "attackResultMin": 1,
  "p0Rules": {
    "disallowBaseAttackMutation": true,
    "defaultSlotLimit": null
  }
}
```

---

## 4) ClashDef

- `damage`는 필수이며 `>= 1`.
- 현재 Clash 판정 후 체력 반영은 `damage`를 사용한다.
- `outcomeEffects`는 선택 필드(확장/호환 목적)다.

```json
{
  "schemaVersion": 1,
  "id": "clash.peak",
  "slotLimit": 1,
  "damage": 2,
  "tags": ["peak"],
  "nameLocKey": "clash.peak_name",
  "descLocKey": "clash.peak_desc",
  "outcomeEffects": {
    "Victory": [],
    "Draw": [],
    "Defeat": []
  }
}
```

---

## 5) ActionDef (Ability Def)

- `type`: `Attack` / `Skill` / `Passive`
- `buildCost`: 편성 비용
- `cooldown`: 턴 단위 쿨다운
- `damage`:
  - `Attack` 타입이면 `> 0` 필수
  - `Skill/Passive`는 `0`이어야 함
- `attack` 필드는 레거시 호환용(신규 데이터에서는 사용 금지)

```json
{
  "schemaVersion": 1,
  "id": "action.miko.assassin",
  "type": "Attack",
  "buildCost": 0,
  "cooldown": 0,
  "damage": 4,
  "tags": ["assassin"],
  "nameLocKey": "action.miko.assassin_name",
  "descLocKey": "action.miko.assassin_desc",
  "effects": [
    {
      "timing": "Roll",
      "condition": { "type": "OpponentCountEquals", "value": 1 },
      "ops": [
        { "op": "ModifyAttackResult", "scope": "Self", "mode": "PercentBonus", "value": 100 }
      ]
    }
  ]
}
```

---

## 6) CardDef (Squad / Support)

- `type`: `Squad` 또는 `Support`
- `supplyCost`
- `duelStart.summonActions`: 시작 시 생성할 action 목록

```json
{
  "schemaVersion": 1,
  "id": "card.squad.reserves",
  "type": "Squad",
  "supplyCost": 1,
  "nameLocKey": "card_reserves_name",
  "descLocKey": "card_reserves_desc",
  "duelStart": {
    "summonActions": [
      { "actionId": "action.reservist", "count": 2 }
    ],
    "ops": []
  }
}
```

---

## 7) EncounterDef (Enemy + Clash Loadout)

### 현재 표준 구조

- `enemy.id`
- `enemy.health`
- `enemy.clashes[]`
  - `clashId`
  - `abilityLoadout[]` (`actionId`, `count`)

```json
{
  "schemaVersion": 1,
  "id": "encounter.debug.01",
  "enemy": {
    "id": "enemy.debug.01",
    "health": 10,
    "clashes": [
      {
        "clashId": "clash.peak",
        "abilityLoadout": [
          { "actionId": "action.test6", "count": 2 }
        ]
      },
      {
        "clashId": "clash.ruins",
        "abilityLoadout": [
          { "actionId": "action.test6", "count": 1 }
        ]
      },
      {
        "clashId": "clash.forest",
        "abilityLoadout": []
      }
    ]
  }
}
```

### 레거시 호환

- `opponentHealth`, `plans`도 로더/세션빌더에서 fallback으로 지원한다.
- 신규 데이터는 `enemy` 구조를 사용한다.

---

## 8) Effect 스펙(현재 허용 OpCode)

### Timing

- `DuelStart`
- `Deploy`
- `Roll`
- `Skill`
- `ClashResolve`
- `TurnEnd`

### Condition(type)

- `Always`
- `OpponentCountEquals`
- `IsInActionHolder`
- `HasTag`

### OpCode(op)

- `ModifyAttackResult`
- `MoveAction`
- `MoveOpponentAction`
- `ModifyTotalAttack`
- `ModifyHealth`
- `AddAttackModifier`

### 제약

- `AddAttackModifier.layer`: `Duel` / `Permanent`만 허용(대소문자 구분).
- 수치 입력은 `value` / `amount` / `delta` 중 최소 1개 필요.
- `TransformOutcome`는 현재 스키마에서 사용하지 않는다.
