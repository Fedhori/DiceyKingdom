# Game Design (Canonical Rules)

**Role:** The single source of truth for implemented gameplay rules and the intended near-term design.

**Last updated:** 2026-02-27

**Terminology:** See `Docs/Game/Glossary.md`.

## 1) One-line identity

Free or Die is a 1v1 arena duel game where both sides deploy Abilities into **three fixed Combat slots**, roll dice-like values, and exchange damage based on the outcome.

## 2) High-level loop

- Choose an opponent.
- Start a Duel.
- Repeat turns until either side reaches **Health <= 0** or the player **Surrenders**.

## 3) Duel structure

### 3.1 Fixed combat slots

- There are always **3** Combat slots.
- Combat slots are not data-defined entities and have **no IDs**.
- Combat slots are referenced by `combatIndex` in the range **0..2**.

### 3.2 Turn phases

The turn proceeds through phases in this order:

1. Reset
2. OpponentSetup
3. PlayerSetup
4. Roll
5. Resolve
6. AfterCombat (internal processing per combat)
7. TurnEnd (internal processing)

```mermaid
flowchart TD
    Reset --> OpponentSetup --> PlayerSetup --> Roll --> Resolve --> AfterCombat --> TurnEnd --> Reset
```

### 3.3 OpponentSetup (enemy deployment)

- The enemy has an `abilityLoadout` entry list:
  - required: `abilityId`, `count`
  - optional overrides: `power`, `cooldown`
- If overrides are omitted, runtime uses values from the referenced `AbilityDef`.
- Enemy and player abilities use the same runtime interaction rules.
- Difference: enemy placement is driven by AI random assignment.
- During OpponentSetup, deployable enemy abilities are assigned to random `combatIndex` in **0..2**.
- “Deployable” means Attack type and `cooldownRemaining == 0`.

### 3.4 PlayerSetup (player deployment)

- The player deploys abilities from Loadout into any Combat slot.
- Each Combat slot has per-side assignment cap equal to visible slot count (currently **6**).
- The same deployability rule is applied: Attack type and `cooldownRemaining == 0`.

### 3.5 Roll and resolution

- Only **Attack** type abilities participate in rolling.
- “Power” means the base roll range input for an Attack.
- “Power Result” is the roll output after modifiers.

Rules:

- `PowerResultMin` is **1**.
- There is no explicit upper bound for Power Result (unless introduced later).

### 3.5.1 Cooldown

- Abilities have cooldown turns.
- Baseline cooldown by type:
  - Attack: **1**
  - Skill: **1**
  - Passive: **0**
- At TurnEnd:
  1. Existing cooldowns tick down by `1`.
  2. Abilities used in the just-resolved turn set `cooldownRemaining = max(0, cooldownTurns - 1)`.

### 3.6 Win/Draw/Lose per slot and damage

For each Combat slot:

- Compare **Total Power** for player vs opponent.
- Result is one of:
  - Victory
  - Draw
  - Defeat

Damage:

- If Victory occurs, the winner deals **Damage = 1** to the loser.
- Draw deals **no damage**.
- Effects may override damage rules:
  - `PreventOutgoingDamageOnWin`: sets outgoing damage to 0
  - `ModifyOutgoingDamageOnWin`: adds to winner outgoing damage for that combat (`base 1 + bonus`, minimum 0)

### 3.6.1 AfterCombat timing

- `AfterCombat` is an effect timing executed per combat, after outcome calculation and before damage application.
- It is outcome-aware (`OutcomeIsVictory`, `OutcomeIsDefeat`, `OutcomeIsDraw`) and supports source-relative evaluation for both sides.

### 3.7 Surrender

Surrender is allowed only if:

- Current phase is **PlayerSetup**
- `Honor > 0`

Effects:

- Duel ends immediately
- **No reward**
- Consume **1 Honor**

## 4) Ability system (current)

- Ability types:
  - **Attack**
  - **Skill**
  - **Passive**

- Attack:
  - Can be deployed into Combat slots
  - Participates in Roll/Resolve

- Skill:
  - Cannot be deployed into Combat slots
  - May trigger on specific timings (e.g., Resolve, Skill, TurnEnd)
  - Exact behavior is driven by effect definitions

- Passive:
  - Cannot be deployed into Combat slots
  - Stays in passive panel and triggers by timing/condition/ops
  - Can have cooldown and follows the same cooldown decrement rule
  - `power` must be `0`

### 4.1 Talent

- Talent system is removed from current plan and rules.

### 4.2 Ability icon data

- Every ability definition must include `iconId`.
- Icon files are resolved by policy: `Data/icons/{iconId}.png`.
- Default icon file must exist at `Data/icons/icon.default.png`.
- In Development mode, missing icon files must fail validation visibly.
- In runtime rendering, if icon lookup/load fails unexpectedly, use default icon and emit error logs.

### 4.3 Ability obtainability field

- Every ability definition must include `isPlayerObtainable`.
- `player.start.startingLoadoutAbilityIds` must reference only abilities with `isPlayerObtainable = true`.
- Enemy loadout may reference abilities regardless of `isPlayerObtainable`.

### 4.4 Effect op side semantics

- For `ModifyHealth` and `ModifyTotalPower`, `ops[].side` is optional.
- If `ops[].side` is omitted, the op is resolved from the source ability owner side (self-side).
- If `ops[].side` is present, only `Player` or `Opponent` is valid (case-sensitive).

### 4.5 Health cap semantics

- `ModifyHealth` is capped by `maxPlayerHealth` / `maxOpponentHealth` stored in `DuelState`.
- This cap applies equally to player and opponent.

### 4.6 Ability localization keys

- Every ability definition must include:
  - `nameLocKey`
  - `descLocKey`
- Key format is fixed:
  - `nameLocKey = <abilityId>.name`
  - `descLocKey = <abilityId>.desc`
- Multi-effect description lines use suffix keys in the same table:
  - first line: `<abilityId>.desc`
  - second line: `<abilityId>.desc.2`
  - third line: `<abilityId>.desc.3`
- Ability UI text resolves from localization table `ability`.
- Missing localization entries must be visible and actionable (error log + explicit missing marker), not silently ignored.

## 5) Enemy data

- `EnemyDef.tier` is required.
- Allowed values: `Normal`, `Elite`, `Boss`.

## 5.1) Data ID naming convention

- IDs use dot-separated domains with snake_case terms.
- Canonical pattern:
  - `ability.<snake_case_slug>`
  - `enemy.<snake_case_slug>`
- Do not use extra dot segmentation for word splitting (e.g., `ability.sharp.scimitar` is invalid).

## 6) Key invariants (must remain true unless explicitly changed)

- Combat slots are always exactly 3.
- `combatIndex` is the only way to reference slots.
- Enemy deployment is loadout-based random assignment.
- Player and enemy ability interaction rules are symmetric (AI random assignment is the only behavior difference).
- Per-side loadout max count is **16**.
- Invalid data is not silently fixed (validation must be visible).
