# DATA_SCHEMA
> 역할: 현재 확정 기획 기준 JSON 스키마 요약.

- 마지막 갱신: `2026-02-24`
- 직렬화: `Newtonsoft.Json` (`JsonUtility` 금지)

---

## 1) 공통 규칙

- 모든 Def 루트는 `schemaVersion` + `id`를 가진다.
- 참조는 파일 경로가 아니라 ID 문자열로 한다.
- ID는 점(`.`) 표기법을 사용한다. 예: `ability.slash.sword`

---

## 2) DataIndex.json

필드:
- `configs`
- `abilities`
- `enemies`

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
    "Data/abilities/ability.shield.up.json"
  ],
  "enemies": [
    "Data/enemies/enemy.northern.footman.json"
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
    "ability.slash.sword",
    "ability.slash.sword",
    "ability.shield.up"
  ]
}
```

---

## 4) AbilityDef

필드:
- `type`: `Attack` / `Skill` / `Passive`
- `buildCost`: 편성 비용
- `cooldown`: 턴 단위 쿨다운
- `power`:
  - Attack 타입: `> 0`
  - Skill/Passive 타입: `0` 가능

```json
{
  "schemaVersion": 2,
  "id": "ability.shield.up",
  "type": "Attack",
  "buildCost": 1,
  "cooldown": 0,
  "power": 10,
  "nameLocKey": "ability.shield.up_name",
  "descLocKey": "ability.shield.up_desc",
  "effects": [
    {
      "timing": "Resolve",
      "condition": { "type": "Always" },
      "ops": [
        { "op": "PreventOutgoingDamageOnWin", "scope": "Self" }
      ]
    }
  ]
}
```

---

## 5) EnemyDef

현재 표준:
- `id`
- `health`
- `abilityLoadout[]` (`abilityId`, `count`)

```json
{
  "schemaVersion": 2,
  "id": "enemy.northern.footman",
  "health": 10,
  "abilityLoadout": [
    { "abilityId": "ability.slash.sword", "count": 3 }
  ]
}
```

---

## 6) Combat 규칙(데이터 외 규칙)

- Combat은 데이터 ID로 관리하지 않는다.
- 런타임에서 항상 3개 고정 생성한다. (`combatIndex: 0,1,2`)
- 적 Ability는 `OpponentSetup`마다 각 Combat에 무작위 배치한다.

---

## 7) Effect OpCode (P0)

- `ModifyPowerResult`
- `MoveAbility`
- `MoveOpponentAbility`
- `ModifyTotalPower`
- `ModifyHealth`
- `AddPowerModifier`
- `PreventOutgoingDamageOnWin`

`AddPowerModifier.target`:
- `Power`
- `PowerResult`

조건 타입(P0):
- `Always`
- `IsInLoadout`
- `OpponentCountEquals`
