# Engineering Guidelines (Unity + C#)

**Role:** Repository-wide engineering rules and conventions (coding style, safety rules, and change-management).

**Last updated:** 2026-02-25

## 1) Code style

- Indentation: **4 spaces**
- Braces: **Allman style**
- Avoid excessive inline comments; prefer clear naming and small functions.

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

## 7) UI rules

- Prefer configuring layout in the editor.
- Only compute positions/sizes in code when it is unavoidable and documented.
- Keep UI controllers thin; push business logic out of `MonoBehaviour`s.

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
