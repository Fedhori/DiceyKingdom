# DATA_SCHEMA
> 역할: P0에서 사용할 JSON 데이터 구조(스키마) 정의 문서입니다.

- 마지막 갱신: `2026-02-22`
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
    "Data/duel.config.json",
    "Data/run.config.json"
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
    "Data/actions/action.ratkin.json"
  ],
  "cards": [
    "Data/cards/card.squad.reserves.json",
    "Data/cards/card.squad.miko.json",
    "Data/cards/card.squad.cannon.json",
    "Data/cards/card.squad.ratkin.json",
    "Data/cards/card.support.latest.gear.json"
  ],
  "skills": [
    "Data/skills/skill.redeploy.json",
    "Data/skills/skill.decoy.json",
    "Data/skills/skill.risky.json",
    "Data/skills/skill.safe.json",
    "Data/skills/skill.reinforce.json"
  ],
  "encounters": [
    "Data/encounters/encounter.debug.01.json"
  ]
}
```

---

## 3) Config

### DuelConfig(예)

```json
{
  "schemaVersion": 1,
  "id": "duel.config",

  "clashCount": 3,
  "focusMax": 5,
  "focusRegenPerTurn": 2,
  "cooldownTickPerTurn": -1,

  "attackResultMin": 1,
  "greatVictoryMultiplier": 2,

  "p0Rules": {
    "disallowBaseAttackMutation": true,
    "defaultSlotLimit": null
  }
}
```

---

## 4) ClashDef

- `slotLimit`은 선택(optional)이며, 없으면 무제한
- `outcomeEffects`는 Outcome별 EffectBlock 리스트

```json
{
  "schemaVersion": 1,
  "id": "clash.peak",

  "slotLimit": 1,
  "tags": ["peak"],

  "nameLocKey": "clash.peak_name",
  "descLocKey": "clash.peak_desc",

  "outcomeEffects": {
    "GreatVictory": [
      { "ops": [ { "op": "ModifyHealth", "side": "Opponent", "delta": -2, "textLocKey": "effect_health_minus" } ] }
    ],
    "Victory": [
      { "ops": [ { "op": "ModifyHealth", "side": "Opponent", "delta": -1, "textLocKey": "effect_health_minus" } ] }
    ],
    "Draw": [],
    "Defeat": [
      { "ops": [ { "op": "ModifyHealth", "side": "Player", "delta": -1, "textLocKey": "effect_health_minus" } ] }
    ],
    "GreatDefeat": [
      { "ops": [ { "op": "ModifyHealth", "side": "Player", "delta": -2, "textLocKey": "effect_health_minus" } ] }
    ]
  }
}
```

---

## 5) ActionDef

```json
{
  "schemaVersion": 1,
  "id": "action.reservist",

  "attack": 2,
  "tags": ["reserve"],

  "nameLocKey": "action.reservist_name",
  "descLocKey": "action.reservist_desc",

  "effects": [
    {
      "timing": "TurnEnd",
      "condition": { "type": "IsInActionHolder" },
      "ops": [
        { "op": "AddAttackModifier", "target": "Attack", "layer": "Duel", "mode": "Add", "value": 2, "textLocKey": "effect_attack_plus" }
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
- `duelStart`: 전투 시작 트리거 블록

### Squad 예시

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

### Support 예시

```json
{
  "schemaVersion": 1,
  "id": "card.support.latest.gear",

  "type": "Support",
  "supplyCost": 1,

  "nameLocKey": "card_latest_gear_name",
  "descLocKey": "card_latest_gear_desc",

  "duelStart": {
    "ops": [
      {
        "op": "ModifyAttackResult",
        "side": "Player",
        "scope": "AllActions",
        "mode": "Add",
        "value": 1,
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
  "id": "skill.redeploy",

  "focusCost": 2,
  "cooldown": 2,
  "timing": "Skill",

  "target": { "type": "AllyAction", "count": 1 },

  "nameLocKey": "skill.redeploy_name",
  "descLocKey": "skill.redeploy_desc",

  "ops": [
    { "op": "MoveAction", "keepAttackResult": true, "textLocKey": "effect_move_keep_face" }
  ]
}
```

---

## 8) EncounterDef(Opponent Intent)

> 프로토타입은 “의도 완전 공개”이므로, EncounterDef는 UI에 그대로 보여줄 구조를 가진다.

```json
{
  "schemaVersion": 1,
  "id": "encounter.debug.01",

  "opponentHealth": 10,
  "plans": [
    { "clashIndex": 0, "actions": [ { "actionId": "action.miko.assassin", "count": 1 } ] },
    { "clashIndex": 1, "actions": [ { "actionId": "action.ratkin", "count": 2 } ] },
    { "clashIndex": 2, "actions": [ { "actionId": "action.ratkin", "count": 1 } ] }
  ]
}
```

---

## 9) Effect 스펙(P0)

### Timing

- DuelStart / Deploy / Roll / Skill / ClashResolve / TurnEnd
- (권장) RollFinalize: 굴림 후 눈 보정 적용 단계

### Condition(type)

- Always
- OpponentCountEquals
  - `value` 또는 `count`로 비교값 지정(둘 다 없으면 1로 처리)
- IsInActionHolder
- HasTag
  - `tag` 필드로 검사할 태그 지정

### OpCode(op)

- ModifyAttackResult
- MoveAction
- MoveOpponentAction
- ModifyTotalAttack
- TransformOutcome
- ModifyHealth
- AddAttackModifier

> P0 금지: ModifyPower 류 op

- `AddAttackModifier.layer`는 `Duel` / `Permanent`만 허용(대소문자 구분).
- 수치 입력은 `value` / `amount` / `delta` 중 최소 1개가 필요하다.
