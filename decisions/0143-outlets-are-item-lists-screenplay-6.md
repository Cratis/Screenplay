---
id: 0143
title: An item list a template offers is an outlet that lower levels inherit, extend or override (Screenplay 6)
status: proposed
stage: designed
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

Screenplay 5 had three unrelated ways to give the screens beneath a template a shared list of items: a layout's
`navigation topbar|sidebar` blocks (with module `inherits|extends|overrides`), a slot's `contributes <Point>` collecting
`contribute to <Point>` entries, and a feature or module `toolbar <name>` adding buttons to its screens. Templates had no way
to offer filters, tabs or figures at all. Decision 0142 freed the word `outlet` for this purpose (Cratis/Screenplay#1178, epic
Cratis/StudioIssues#1327).

## Decision

1. **An outlet is a named list of items a layout, screen template or dialog template offers**:
   `outlet <name> of actions|filters|tabs|stats|navigation [for item|for screen] [in <slot>]`, with `default` items written
   in the kind's item form without the item's keyword. A slot is where content goes; an outlet is a list, and `in <slot>` ties
   it to the slot that shows it.
2. **Item forms per kind**: actions take `button` (existing properties, plus `runs close|submit|refresh`) and `group`;
   filters `filter <id> title "…" [sets <Query>.<param>]`; tabs `tab <id> title "…" [shows <Screen>]`; stats
   `stat <id> title "…" [value from <Query>.<field>]`; navigation `item … [target …]`, nesting. A dialog or drawer template
   declares its buttons as `outlet actions of actions`, using the built-ins `runs close` and `runs submit`.
3. **Lower levels change an outlet, never declare one.** A module, feature or slice screen writes
   `outlet <name> inherits|extends|overrides`, may `hide <id>` an inherited item and place its own `before <id>` or
   `after <id>`. Levels apply in order: template defaults, module, feature and parents, screen. `contribute to <outlet>` adds
   items from any module or feature to any outlet.
4. **The Screenplay 5 forms are errors with new codes** naming the replacement and `screenplay migrate --from 5 --to 6`:
   `PLAY0894` (`navigation topbar|sidebar`), `PLAY0895` (`<slot> contributes <Point>`), `PLAY0896` (feature or module
   `toolbar`). Their old checks are retired (`PLAY0224`, `PLAY0740`, `PLAY0742`, `PLAY0745`-`PLAY0747`, `PLAY0808`). A
   contribution to an outlet nothing declares is now an error (`PLAY0903`), except where a blueprint may declare it.
5. **New checks**: `PLAY0897` malformed or misplaced outlet line, `PLAY0898` malformed item, `PLAY0899` `hide`/`before`/`after`
   naming an item the inherited outlet lacks, `PLAY0900` item of the wrong kind, `PLAY0901` filter on a parameter the query
   lacks, `PLAY0902` stat from a list query, `PLAY0903` unknown outlet, `PLAY0904` outlet declared or changed twice,
   `PLAY0905` repeated item id.
6. **No executable-model version**, as in decision 0142: outlets are screen authoring syntax and deferred from the ESM.

Placement and hooks (`fits`, `opens`, `runs` on non-built-ins, `enabled when`), `choose from query` and
`show … from …selection` are decision 0144.

## Consequences

- The syntax tree gains `OutletSyntax` with `OutletItemSyntax` forms; `LayoutSyntax.Navigation`, `ModuleSyntax.Navigation`,
  `ModuleSyntax.Toolbars`, `FeatureSyntax.Toolbars` and `SlotSyntax.Contributes` are removed. AST JSON readers of those
  members must move to `outlets`.
- Layouts, templates and contributions stay opaque to the TypeScript compiler. It parses lower levels' outlets and items and
  reports `PLAY0894` (module regions), `PLAY0896`-`PLAY0898` and `PLAY0904`; the reference checks are C#-only.
- Samples, the canonical corpus and the documentation are written with outlets; a new Outlets reference page explains them.
