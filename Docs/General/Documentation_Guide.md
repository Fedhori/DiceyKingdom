# Documentation Guide

**Role:** Standards for how this repository’s documentation is written and maintained so that both humans and tools (e.g., CODEX CLI) can reliably use it.

**Last updated:** 2026-02-25

## 1) Folder ownership

- `Docs/General/` contains reusable rules and standards (not tied to this specific game).
- `Docs/Game/` contains game-specific details (rules, schema, architecture, paths).

If a document mixes both, split it.

## 2) Document roles must be explicit

Every Markdown file must start with:

- A **Role** statement (what this document is for, and what it is not for)
- **Last updated** date
- Links to the canonical docs if it depends on them

## 3) Avoid duplicated sources of truth

If two documents overlap, do one of the following:

- Merge them into one canonical document, or
- Keep one canonical document and convert the other into a short pointer.

Never keep two conflicting “truths”.

## 4) Write for execution (tool-friendly)

Assume CODEX CLI (or another agent) will follow these docs:

- Prefer concrete rules over vague guidance.
- Use checklists and step-by-step procedures for risky operations.
- When listing files, use repository paths.
- When describing invariants, state how to verify them (tests, validation steps).

## 5) When to split a document

Split when any of the following happens:

- It becomes hard to navigate (too long, too many unrelated sections)
- It mixes canonical rules with brainstorming
- It contains both general standards and game-specific details

## 6) Markdown conventions

- Use ATX headings (`#`, `##`, `###`).
- Prefer short paragraphs.
- For lists of rules, use bullets with strong verbs (“Must”, “Must not”, “Do”, “Do not”).
- Mermaid diagrams are allowed, but must be correct and minimal.

## 7) Change discipline

- Update docs in the same change set as the code/data change.
- If a refactor changes names/paths, update all doc links.
- If something is not implemented yet, label it clearly as **Target** or **Planned** (never present it as current reality).

## 8) Recommended template

```md
# <TITLE>

**Role:** <What this document is for and what it is not for.>

**Last updated:** YYYY-MM-DD

## <Section>
...
```
