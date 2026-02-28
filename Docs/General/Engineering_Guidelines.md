# Engineering Guidelines (Unity + C#)

**Role:** Repository-wide engineering rules and conventions (coding style, safety rules, and change-management).

**Last updated:** 2026-02-28

## Code style

- Indentation: 4 spaces (no tabs).
- Braces: Allman style for block statements.
- Enforce via `.editorconfig` and formatting tools. Do not rely on manual consistency.
- Keep methods small and single-purpose; prefer extracting helpers over deep nesting.

## 2) Naming and file structure

- Public types and namespaces: **PascalCase**
- Parameters, locals, private fields: **camelCase**
- Prefer **one public type per file**. Minimize exceptions.

## 3) Architecture boundaries

### 3.1 Layering dependency rule

- Follow this dependency direction: Presentation -> Application -> Domain.
- Infrastructure (data/IO) may be used by Application, but **must not depend on Application**.

### 3.2 Layer responsibilities

- **Domain**
  - Keep pure state and computation logic.
  - Avoid UnityEngine dependencies when feasible.

- **Application**
  - Orchestrate use-cases and domain flow (phase progression, rule application, effect execution).

- **Infrastructure**
  - Handle data loading, parsing, validation, and persistence.
  - Own schema contracts required for validation.

- **Presentation**
  - Keep MonoBehaviour/UI concerns: input, binding, animation, and scene wiring.

## 4) Unity safety rules

### 4.1 Namespace changes can break Unity serialization

Changing the namespace of a `MonoBehaviour` / `ScriptableObject` can break references in scenes and prefabs.

**Required procedure for namespace moves:**

- Do it in an isolated change set.
- Open all relevant scenes/prefabs and fix any missing scripts.
- Do not merge until the Console has **0 Missing Script** issues.

### 4.2 Initialization order

Avoid designs that silently depend on Unity execution order. If order matters, make it explicit through:

- A single bootstrap entry point
- Clear “stage” logging (start/success/failure)
- Explicit failure policy (stop vs continue)

### 4.3 Async in Unity lifecycle methods

`async void` may be unavoidable in Unity event-style methods (e.g., `Awake`), but it is risk-prone.

Minimum rule:

- Always wrap awaited logic in `try/catch`.
- Log the stage that failed.
- Define whether the application should stop or continue after a failure.

## 5) Data and randomness

- JSON serialization/deserialization: **Newtonsoft.Json** (do not use `JsonUtility`).
- Randomness: use **System.Random** (do not use `UnityEngine.Random`), and allow seed injection for reproducibility when feasible.

## 6) Data validation

- Do not silently fix invalid data.
- If auto-correction is unavoidable, emit at least a warning and keep the correction auditable.
- Validation failures should be visible and actionable.

### 6.1 Fail-fast policy (Ensure/Cache patterns)

- Do not use `Ensure*`, `Cache*IfNeeded`, or similar patterns to silently repair invalid state at runtime.
- Validation code must not assign missing references or recreate null collections behind the scenes.
- For invalid state, emit an **error log** with clear context and fail fast (e.g., stop initialization or throw) instead of continuing with hidden corrections.
- Runtime read/write methods (e.g., increment/add/apply/update) must not hide missing state with fallbacks such as `x ?? new ...`; missing prerequisites must fail explicitly.
- State creation is allowed only at explicit lifecycle entry points (`Initialize*`, `Create*`, `Build*`) and must be visible in call flow.
- Lazy creation is an exception-only pattern. If absolutely required:
  - method name must explicitly use `GetOrCreate*`
  - call sites must intentionally choose that method (no implicit fallback in unrelated methods)
  - behavior and reason must be documented in the same change set

## 7) UI rules

- Prefer configuring layout in the editor.
- Only compute positions/sizes in code when it is unavoidable and documented.
- Keep UI controllers thin; push business logic out of `MonoBehaviour`s.

### 7.1 Observable UI state (mandatory)

- Any UI value/state that must be reflected immediately when data changes **must** use `ObservableValue` / `IReadOnlyObservableValue` subscription.
- Do not rely on ad-hoc/manual full refresh calls as the primary update mechanism for those subscribed values.
- This rule applies to scalar values and list-like UI states that require real-time reflection (use observable revision signals if needed).
- If an exception is unavoidable, the implementer must notify the user first with:
  - Why the exception is necessary
  - Scope and risk
  - Temporary workaround and follow-up plan
- Without explicit user approval, exception handling must not be merged as final behavior.

## 8) Tests and verification

Minimum expectation for refactors:

- Project compiles.
- Automated tests (EditMode) pass.
- Any data validation tooling still succeeds (if available).

## 9) Change-management rules (for refactors)

- Make small, reviewable steps; avoid mixing unrelated changes.
- Keep “Current vs Target” clearly distinguished in docs and code comments.
- When moving types between folders/namespaces/assemblies:
  - Update all references
  - Verify build and runtime behavior
  - Update documentation paths and examples

## Layer dependency matrix (must)

- Presentation may depend on: Application, Domain (read-only models), Common.
- Application may depend on: Domain, Infrastructure, Common.
- Infrastructure may depend on: Domain/Contracts, Common. Must not depend on Application/Presentation.
- Domain may depend on: Common only (avoid UnityEngine when feasible).
