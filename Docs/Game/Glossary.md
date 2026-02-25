# Glossary

**Role:** Canonical terminology for docs, code, and data. Use these terms consistently.

**Last updated:** 2026-02-25

## Usage rules

- Use one term per concept. Do not invent synonyms.
- In new code/data/docs, prefer the **English** terms below.
- Deprecated terms must not appear in new writing.

## Canonical terms

| Category | Term (EN) | Term (KR) | Meaning / Notes |
|---|---|---|---|
| Project | Free or Die | Free or Die | Project name |
| Combat | Duel | 결투 | One complete battle instance |
| Combat | Phase | 페이즈 | A step within a turn (e.g., Setup, Roll, Resolve) |
| Combat | Combat (slot) | 전투 지점 | One of 3 fixed combat slots |
| Combat | combatIndex | 전투 인덱스 | Slot index: 0, 1, 2 |
| Abilities | Ability | 어빌리티 | The unified concept (attacks + skills) |
| Abilities | Attack | 공격 | An ability that rolls and contributes to combat resolution |
| Abilities | Skill | 스킬 | An ability that triggers by timing (design supported) |
| Meta | Loadout | 로드아웃 | Un-deployed ability storage during a duel |
| State | Health | 체력 | Duel ends when Health <= 0 |
| State | Honor | 명예 | Determines whether Surrender is allowed |
| Meta | Capacity | 용량 | Build-time capacity limit (pre-duel) |
| Action | Surrender | 항복 | Immediate duel end; no reward; consumes 1 Honor |

## Deprecated terms (do not use)

- Clash → Combat
- Intent → not used
- Pattern → not used (enemy placement is based on enemy loadout)
- Bag → Loadout
- Passive (Ability type) → Talent (separate system concept; not implemented)
