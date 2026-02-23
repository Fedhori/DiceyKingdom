# DATA_SCHEMA
> 역할: 현재 구현 기준 JSON 스키마 요약.

- 마지막 갱신: `2026-02-23`
- 직렬화: `Newtonsoft.Json` (`JsonUtility` 금지)

---

## 1) 공통 규칙

- 모든 Def 루트는 `schemaVersion` + `id`를 가진다.
- 참조는 파일 경로가 아니라 ID 문자열로 한다.
- ID는 점(`.`) 표기법을 사용한다. 예: `ability.miko.assassin`

---

## 2) DataIndex.json

필드:
- `configs`
- `clashes`
- `abilities`
- `encounters`

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
  "abilities": [
    "Data/abilities/ability.reservist.json",
    "Data/abilities/ability.miko.assassin.json"
  ],
  "encounters": [
    "Data/encounters/encounter.debug.01.json"
  ]
}
```

---

## 3) Config

## duel.config

```json
{
  "schemaVersion": 1,
  "id": "duel.config",
  "clashCount": 3,
  "cooldownTickPerTurn": -1,
  "attackResultMin": 1,
  "p0Rules": {
    "disallowBaseAttackMutation": true,
    "defaultSlotLimit": null
  }
}
```

## run.config

```json
{
  "schemaVersion": 1,
  "id": "run.config",
  "startingHonor": 3,
  "capacity": 6
}
```

## player.start

```json
{
  "schemaVersion": 1,
  "id": "player.start",
  "startingHonor": 3,
  "startingPlayerHealth": 10,
  "startingBagAbilityIds": [
    "ability.miko.assassin",
    "ability.dwarf.cannon"
  ]
}
```

---

## 4) ClashDef

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

## 5) AbilityDef

필드:
- `type`: `Attack` / `Skill` / `Passive`
- `buildCost`: 편성 비용
- `cooldown`: 턴 단위 쿨다운
- `damage`:
  - Attack 타입: `> 0`
  - Skill/Passive 타입: `0`

```json
{
  "schemaVersion": 1,
  "id": "ability.miko.assassin",
  "type": "Attack",
  "buildCost": 0,
  "cooldown": 0,
  "damage": 4,
  "tags": ["assassin"],
  "nameLocKey": "ability.miko.assassin_name",
  "descLocKey": "ability.miko.assassin_desc",
  "effects": []
}
```

---

## 6) EncounterDef

현재 표준:
- `enemy.id`
- `enemy.health`
- `enemy.clashes[]`
  - `clashId`
  - `abilityLoadout[]` (`abilityId`, `count`)

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
          { "abilityId": "ability.test6", "count": 2 }
        ]
      }
    ]
  }
}
```

---

## 7) Effect OpCode (P0)

- `ModifyAttackResult`
- `MoveAbility`
- `MoveOpponentAbility`
- `ModifyTotalAttack`
- `ModifyHealth`
- `AddAttackModifier`

