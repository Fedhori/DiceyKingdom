# Technical Architecture

**Role:** Explains the current architecture, identifies known structural risks, and defines the target architecture that refactors must move toward.

**Last updated:** 2026-02-25

## 1) Layering model

Intended dependency direction:

- Presentation → Application → Domain
- Infrastructure (data/IO) may be used by Application, but **must not depend on Application**.

### Layer responsibilities

- **Domain**
  - Pure state and computation (e.g., Duel state simulation).
  - Avoid UnityEngine dependencies when feasible.

- **Application**
  - Orchestrates domain use-cases (phases, turn processing, effect execution).

- **Infrastructure**
  - Data loading, parsing, validation, persistence.
  - Defines schema contracts required for validation.

- **Presentation**
  - MonoBehaviours and UI: input, views, animation, and scene bindings.
