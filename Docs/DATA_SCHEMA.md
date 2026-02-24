# DATA_SCHEMA
> ??븷: ?꾩옱 援ы쁽 湲곗? JSON ?ㅽ궎留??붿빟.

- 留덉?留?媛깆떊: `2026-02-24`
- 吏곷젹?? `Newtonsoft.Json` (`JsonUtility` 湲덉?)

---

## 1) 怨듯넻 洹쒖튃

- 紐⑤뱺 Def 猷⑦듃??`schemaVersion` + `id`瑜?媛吏꾨떎.
- 李몄“???뚯씪 寃쎈줈媛 ?꾨땲??ID 臾몄옄?대줈 ?쒕떎.
- ID????`.`) ?쒓린踰뺤쓣 ?ъ슜?쒕떎. ?? `ability.slash.sword`

---

## 2) DataIndex.json

?꾨뱶:
- `configs`
- `abilities`
- `encounters`

```json
{
  "schemaVersion": 2,
  "configs": [
    "Data/duel.config.json",
    "Data/run.config.json",
    "Data/player.start.json"
  ],
  "abilities": [
    "Data/abilities/ability.slash.sword.json",
    "Data/abilities/ability.slash.sword.json"
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
  "schemaVersion": 2,
  "id": "duel.config",
  "cooldownTickPerTurn": -1,
  "powerResultMin": 1,
  "p0Rules": {
    "disallowBasePowerMutation": true,
    "defaultSlotLimit": null
  }
}
```

## run.config

```json
{
  "schemaVersion": 2,
  "id": "run.config",
  "startingHonor": 3,
  "capacity": 6
}
```

## player.start

```json
{
  "schemaVersion": 2,
  "id": "player.start",
  "startingHonor": 3,
  "startingPlayerHealth": 10,
  "startingLoadoutAbilityIds": [
    "ability.slash.sword",
    "ability.shield.up"
  ]
}
```

---

## 4) AbilityDef

?꾨뱶:
- `type`: `Attack` / `Skill` / `Passive`
- `buildCost`: ?몄꽦 鍮꾩슜
- `cooldown`: ???⑥쐞 荑⑤떎??- `power`:
  - Attack ??? `> 0`
  - Skill/Passive ??? `0`

```json
{
  "schemaVersion": 2,
  "id": "ability.slash.sword",
  "type": "Attack",
  "buildCost": 0,
  "cooldown": 0,
  "power": 4,
  "tags": ["assassin"],
  "nameLocKey": "ability.slash.sword_name",
  "descLocKey": "ability.slash.sword_desc",
  "effects": []
}
```

---

## 5) EncounterDef

?꾩옱 ?쒖?:
- `enemy.id`
- `enemy.health`
- `enemy.startPatternId`
- `enemy.patterns[]`
  - `patternId`
  - `clashes[]`
    - `clashId`
    - `maxPlayerAssignments` (optional)
    - `abilityLoadout[]` (`abilityId`, `count`)
  - `nextPatterns[]`
    - `patternId`
    - `probability` (?⑷퀎 1.0)

```json
{
  "schemaVersion": 2,
  "id": "encounter.debug.01",
  "enemy": {
    "id": "enemy.debug.01",
    "health": 10,
    "startPatternId": "pattern.opening",
    "patterns": [
      {
        "patternId": "pattern.opening",
        "clashes": [
          {
            "clashId": "clash.peak",
            "maxPlayerAssignments": 1,
            "abilityLoadout": [
              { "abilityId": "ability.slash.sword", "count": 2 }
            ]
          }
        ],
        "nextPatterns": [
          { "patternId": "pattern.opening", "probability": 1.0 }
        ]
      }
    ]
  }
}
```

---

## 6) Effect OpCode (P0)

- `ModifyPowerResult`
- `MoveAbility`
- `MoveOpponentAbility`
- `ModifyTotalPower`
- `ModifyHealth`
- `AddPowerModifier`

`AddPowerModifier.target`:
- `Power`
- `PowerResult`

議곌굔 ???P0):
- `Always`
- `IsInLoadout`
- `OpponentCountEquals`
- `HasTag`



