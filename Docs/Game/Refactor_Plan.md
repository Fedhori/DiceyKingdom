# Refactor Plan (CODEX-Ready)

**Role:** An actionable, prioritized refactor plan aligned with the current codebase risks. Includes prompts suitable for automated execution (e.g., CODEX CLI).

**Last updated:** 2026-02-25

## 0) Global rule for automated execution

Before doing any of the work below, an automated agent must follow `Docs/General/Agent_Interaction_Policy.md` (plan first, ask questions, wait for approval).

## 1) Priorities (highest impact first)

1. Consolidate Run lifecycle ownership (remove duplication).
2. Fix layer inversion: Infrastructure must not depend on Application (effect schema contracts).
3. Standardize namespaces and folder boundaries (reduce global namespace sprawl).
4. Split oversized battle presentation controller into orchestrator/view/state.
5. Secondary cleanup: file-per-type, naming, explicit bootstrap stages.

## 2) Milestone A — Consolidate Run lifecycle

**Goal:** Ensure Run is created/disposed from exactly one place.

**Target direction:**
- Make `GameSceneInstaller` the only Run installer/owner.
- Deprecate and remove `GameService` if it duplicates Run lifecycle behavior.

### CODEX prompt

```text
Goal: Consolidate Run lifecycle into a single component.

Files:
- Assets/Scripts/GameService.cs
- Assets/Scripts/App/GameSceneInstaller.cs
- Assets/Scripts/App/GameApp.cs

Requirements:
1) Merge the responsibilities of GameService (EnsureRunStarted/seed/fixedSeed/sceneRefs) into GameSceneInstaller.
2) Remove all uses of GameService across the project (search scenes/prefabs and code references).
3) Ownership (ownsRun) rules:
   - If GameApp.Run is null: GameSceneInstaller calls BeginRun(sceneRefs) and records ownsRun=true.
   - If a Run already exists: ownsRun=false; OnDestroy must NOT call EndRun.
4) Keep logs useful but reduce duplicate logging.
5) The project must compile and EditMode tests must pass.

Output:
- A code diff equivalent set of changes
- A list of removed/moved files
```

## 3) Milestone B — Fix layer inversion (Effect schema contracts)

**Goal:** Remove Infrastructure → Application dependency by relocating effect contract types.

**Target direction (fast stabilization):**
- Move schema/contract types into `Game.Infrastructure.Data.Effects`.
- Keep runtime logic in `Game.Application.Duel.Effects`.

Contract types include:
- OpCode
- Timing
- Command/operation DTOs
- Target/scope enums

### CODEX prompt

```text
Goal: Remove the layer inversion where Infrastructure depends on Application.

Files:
- Assets/Scripts/Game/Application/Duel/Effects/DuelEffectOpCode.cs
- Assets/Scripts/Game/Application/Duel/Effects/DuelEffectTiming.cs
- Assets/Scripts/Game/Application/Duel/Effects/DuelEffectCommand.cs
- Assets/Scripts/Game/Application/Duel/Effects/DuelModifierTarget.cs
- Assets/Scripts/Game/Infrastructure/Data/GameDataValidator.cs
- All references/usings across the codebase

Instructions:
1) Move the schema/contract files into:
   Assets/Scripts/Game/Infrastructure/Data/Effects/   (new folder)
2) Rename namespaces to:
   Game.Infrastructure.Data.Effects
3) Update Application code (timed effect runner, combat resolver, handlers) to reference the new contract namespace.
4) Update GameDataValidator to reference the Infrastructure contract types (allowed opcodes etc).
5) Keep compilation stable as the top priority (do not split large files yet).
6) Ensure EditMode tests still pass.

Deliverables:
- git mv file move list
- updated code references
```

## 4) Milestone C — Namespace and folder boundary standardization

**Goal:** Reduce global namespace sprawl and align folder ↔ namespace.

**Target mapping (example):**
- `Assets/Scripts/App/*` → `Game.App`
- `Assets/Scripts/Framework/*` → `Game.Framework`
- `Assets/Scripts/Services/*` → `Game.Services`
- `Assets/Scripts/UI/*` → `Game.UI`
- `Assets/Scripts/Tooltip/*` → `Game.UI.Tooltip`
- `Assets/Scripts/Save/*` → `Game.Save`

**Critical Unity warning:** Namespace changes can break serialized references.

### CODEX prompt

```text
Goal: Align namespaces with folder layout and remove global namespaces.

Requirements:
1) Apply `namespace Game.UI Ellipsis` to all files under Assets/Scripts/UI.
2) Apply `namespace Game.UI.Tooltip Ellipsis` to Assets/Scripts/Tooltip.
3) Apply `namespace Game.Save Ellipsis` to Assets/Scripts/Save.
4) Apply `namespace Game.Framework Ellipsis` to Assets/Scripts/Common (or the new Framework folder if already moved).
5) Apply `namespace Game.App Ellipsis` to Assets/Scripts/App.
6) Update all using statements and references so compilation succeeds.
7) Add a prominent warning comment where Unity serialized references might break (MonoBehaviours / ScriptableObjects).

Output:
- A per-folder list of namespace changes
- A list of potential Unity serialization risks discovered
```

## 5) Milestone D — Battle screen separation

**Goal:** Split battle logic into orchestrator/view/state to reduce regression risk and improve testability.

**Target:**
- `BattleScreenController` (MonoBehaviour): wiring only
- `BattleSessionOrchestrator` (plain C#): duel flow
- `BattleScreenView`: UI updates
- `BattleSelectionState`: selection/input state

### CODEX prompt

```text
Goal: Split BattleScreenController responsibilities.

Files:
- Assets/Scripts/Game/Presentation/Battle/BattleScreenController.cs
- Related battle view components (e.g., BattleAbilityCardView, BattleCombatZoneView)

Requirements:
1) Extract duel flow orchestration into `BattleSessionOrchestrator` (non-MonoBehaviour).
2) Extract UI binding/update code into `BattleScreenView`.
3) Keep BattleScreenController as the entry point:
   - keeps SerializeField scene references
   - wires orchestrator and view
   - forwards button events to orchestrator
4) Preserve behavior (no feature removal).
5) Add at least one EditMode test for orchestrator behavior (success/failure + at least 3 minimal cases).

Deliverables:
- New files created
- BattleScreenController reduced substantially
- Tests added and passing
```

## 6) Milestone E — Bootstrap clarity

**Goal:** Make bootstrap stages explicit and enforce a clear failure policy.

Recommended approach:
- Introduce a `BootstrapStage` enum.
- Log stage start/success/failure.
- Decide a clear policy for cache/config/data load failures.

## 7) Definition of Done (for each milestone)

- Compiles cleanly.
- EditMode tests pass.
- Data validation tooling (if present) works.
- Manual smoke test succeeds (boot → duel → one full turn → surrender).
