# DATA_SCHEMA
> 역할: P0에서 사용할 JSON 데이터 구조(스키마) 정의 문서입니다.

- 마지막 갱신: `2026-02-21`
- 직렬화: `Newtonsoft.Json`

---

## 1) 공통 규칙

- 모든 Def(JSON 루트)는 `schemaVersion`(int)와 `id`(string)를 가진다.
- 다른 데이터를 참조할 때는 경로가 아니라 **id로만 참조**한다.
- 필드명은 `camelCase`를 사용한다.

---

## 2) DataIndex.json

### 목적

- `StaticDataService` 인스펙터에 파일을 잔뜩 등록하지 않기 위해,
  DataIndex 하나로 모든 JSON 파일 목록을 관리한다.

### 예시

```json
{
  "schemaVersion": 1,
  "configs": [
    "Data/battle_config.json",
    "Data/run_config.json"
  ],
  "battlefields": [
    "Data/battlefields/bf_peak.json",
    "Data/battlefields/bf_ruins.json",
    "Data/battlefields/bf_forest.json"
  ],
  "troops": [
    "Data/troops/troop_reservist.json",
    "Data/troops/troop_miko_assassin.json",
    "Data/troops/troop_dwarf_cannon.json",
    "Data/troops/troop_ratkin.json"
  ],
  "cards": [
    "Data/cards/card_squad_reserves.json",
    "Data/cards/card_squad_miko.json",
    "Data/cards/card_squad_cannon.json",
    "Data/cards/card_squad_ratkin.json",
    "Data/cards/card_support_latest_gear.json"
  ],
  "skills": [
    "Data/skills/skill_redeploy.json",
    "Data/skills/skill_decoy.json",
    "Data/skills/skill_risky.json",
    "Data/skills/skill_safe.json",
    "Data/skills/skill_reinforce.json"
  ],
  "encounters": [
    "Data/encounters/enc_debug_01.json"
  ]
}
```

---

## 3) Config

### BattleConfig(예)

```json
{
  "schemaVersion": 1,
  "id": "battle_config",

  "battlefieldCount": 3,
  "manaMax": 5,
  "manaRegenPerTurn": 2,
  "cooldownTickPerTurn": -1,

  "faceValueMin": 1,
  "greatVictoryMultiplier": 2,

  "p0Rules": {
    "disallowPowerChange": true,
    "defaultSlotLimit": null
  }
}
```

---

## 4) BattlefieldDef

- `slotLimit`은 선택(optional)이며, 없으면 무제한
- `outcomeEffects`는 Outcome별 EffectBlock 리스트

```json
{
  "schemaVersion": 1,
  "id": "bf_peak",

  "slotLimit": 1,
  "tags": ["peak"],

  "nameLocKey": "bf_peak_name",
  "descLocKey": "bf_peak_desc",

  "outcomeEffects": {
    "GreatVictory": [
      { "ops": [ { "op": "ModifyMorale", "side": "Enemy", "delta": -2, "textLocKey": "effect_morale_minus" } ] }
    ],
    "Victory": [
      { "ops": [ { "op": "ModifyMorale", "side": "Enemy", "delta": -1, "textLocKey": "effect_morale_minus" } ] }
    ],
    "Draw": [],
    "Defeat": [
      { "ops": [ { "op": "ModifyMorale", "side": "Player", "delta": -1, "textLocKey": "effect_morale_minus" } ] }
    ],
    "GreatDefeat": [
      { "ops": [ { "op": "ModifyMorale", "side": "Player", "delta": -2, "textLocKey": "effect_morale_minus" } ] }
    ]
  }
}
```

---

## 5) TroopDef

```json
{
  "schemaVersion": 1,
  "id": "troop_reservist",

  "Attack": 2,
  "tags": ["reserve"],

  "nameLocKey": "troop_reservist_name",
  "descLocKey": "troop_reservist_desc",

  "effects": [
    {
      "timing": "TurnEnd",
      "condition": { "type": "IsInCamp" },
      "ops": [
        { "op": "AddAttackModifier", "target": "Attack", "layer": "Battle", "mode": "Add", "value": 2, "textLocKey": "effect_attack_plus" }
      ]
    }
  ]
}
```

---

## 6) CardDef(Squad/Support)

### 공통 필드

- `type`: `Squad` 또는 `Support`
- `supplyCost`
- `battleStart`: 전투 시작 트리거 블록

### Squad 예시

```json
{
  "schemaVersion": 1,
  "id": "card_squad_reserves",

  "type": "Squad",
  "supplyCost": 1,

  "nameLocKey": "card_reserves_name",
  "descLocKey": "card_reserves_desc",

  "battleStart": {
    "summonTroops": [
      { "troopId": "troop_reservist", "count": 2 }
    ],
    "ops": []
  }
}
```

### Support 예시

```json
{
  "schemaVersion": 1,
  "id": "card_support_latest_gear",

  "type": "Support",
  "supplyCost": 1,

  "nameLocKey": "card_latest_gear_name",
  "descLocKey": "card_latest_gear_desc",

  "battleStart": {
    "ops": [
      {
        "op": "ModifyAttackResult",
        "side": "Player",
        "scope": "AllTroops",
        "mode": "Add",
        "value": 1,
        "min": 1,
        "textLocKey": "effect_face_plus"
      }
    ]
  }
}
```

---

## 7) SkillDef

```json
{
  "schemaVersion": 1,
  "id": "skill_redeploy",

  "manaCost": 2,
  "cooldown": 2,
  "timing": "Tactics",

  "target": { "type": "AllyTroop", "count": 1 },

  "nameLocKey": "skill_redeploy_name",
  "descLocKey": "skill_redeploy_desc",

  "ops": [
    { "op": "MoveTroop", "keepAttackResult": true, "textLocKey": "effect_move_keep_face" }
  ]
}
```

---

## 8) EncounterDef(Enemy Intent)

> 프로토타입은 “의도 완전 공개”이므로, EncounterDef는 UI에 그대로 보여줄 구조를 가진다.

```json
{
  "schemaVersion": 1,
  "id": "enc_debug_01",

  "enemyMorale": 10,
  "plans": [
    { "battlefieldIndex": 0, "troops": [ { "troopId": "troop_miko_assassin", "count": 1 } ] },
    { "battlefieldIndex": 1, "troops": [ { "troopId": "troop_ratkin", "count": 2 } ] },
    { "battlefieldIndex": 2, "troops": [ { "troopId": "troop_ratkin", "count": 1 } ] }
  ]
}
```

---

## 9) Effect 스펙(P0)

### Timing

- BattleStart / Deploy / Roll / Tactics / Resolve / TurnEnd
- (권장) RollFinalize: 굴림 후 눈 보정 적용 단계

### Condition(type)

- Always
- EnemyCountEquals
- IsInCamp
- HasTag

### OpCode(op)

- ModifyAttackResult
- MoveTroop
- MoveEnemyTroop
- ModifyTotalAttack
- TransformOutcome
- ModifyMorale
- AddAttackModifier

> P0 금지: ModifyPower 류 op
