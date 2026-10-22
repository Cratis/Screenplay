---
id: NNNN
title: Actions name their subject explicitly, and selection is separate from visibility
status: proposed
stage: none
class: contract
reversibility: costly
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

A command button next to a list usually acts on the selected row. Screenplay has no way to say so for a plain action.

- A plain action is `ScreenActionSyntax(Command, Label, Navigate, ...)` (`ScreenSyntax.cs:80`). It has no subject and no
  `with` input bindings.
- A guarded action has both. Its subject rule: `item` is the nearest container's `data` item, or the selected element
  when that data is a collection; without an item the action is always hidden. Inputs resolve from explicit `with`, then
  subject fields of the same name, then the form (`screens.md:80`, `:92-97`). Interactions use the same subject rules
  (0048; `interactions.md:103`). Guarded alternatives are selected in authored order (0029).
- The Acme walkthrough treats a plain action under a `toolbar` as an action on the selected row
  (`acme-commerce-walkthrough.md:453`). The normative `screens.md` says nothing of the kind.
- Stage fills the gap at run time. It scans the rows of every query on the screen for a property named like the
  identifier (`StageCommandForm.tsx:321-366`, Stage #670, #693) and copies `id` to `<word>Id` aliases built from words in
  the read-model name (`:325-335`). That is a name heuristic. The toolbar path sets `rowAction: true` and shows "Select
  a row first" (`stageComponents.tsx:342`, `:364`, `:369`). The rendered application's scene does not set `rowAction`
  (Stage #745).
- Stage resolves a guarded action's selection across all selected rows and accepts exactly one, rather than the
  declared source (`StageGuardedAction.tsx:43-67`). Two tables on one screen therefore have a binding problem.
- Scene has no native subject. `core:action` carries untyped properties Stage invents (`alternatives`, `otherwise`,
  `rowAction`, `itemSelection`, `itemQuery`). `InteractionBinding(Trigger, Actions, Condition?)` has one guard and no
  first match; `ExecuteCommandAction(Command)` takes arguments only
  (`InteractionBinding.cs:8-17`; `ExecuteCommandAction.cs:6-13`). Its engine can already read a selection as an
  argument (`component.<id>.selectedItem.id`, `resolveArgumentSource.ts:14-16`). Scene #68 is open.
- Screenplay #684 (selection-required toolbar items) was closed on 2026-10-22 with no comment or commit, and the
  syntax is not on main. The closure is not delivery.
- Screen constructs are authoring syntax; the executable model does not carry them (`screens.md`, toolbar section). New
  members follow the AST rules in `ast-compatibility.md:28`: init-only, left out of JSON when absent.

This record is allowed by the record that lifts the freeze (0105 on branch `screenplay-1035-typed-capture-key`).

## Decision

1. **An optional subject clause.** A plain `action`, a toolbar `item ... action` and a guarded action may say what
   they act on:
   - `for item`: the action acts on the data item, by the guarded-action subject rule word for word (`screens.md:80`).
   - `for screen`: the action never binds to a selection.
   ```screenplay
   action ArchiveOrganization for item
   action MoveOpportunityToStage for item label "Move to stage"
     with stageId
   action RegisterOrganization for screen
   toolbar main
     item archive action ArchiveOrganization for item label "Archive"
   ```
2. **Inputs.** A plain action gains `with <input> from <binding>` lines, as guarded alternatives have. For `for item`,
   an identifier input binds in this order:
   1. an explicit `with <input> from item.<path>`;
   2. a subject field of the same name;
   3. the subject read model's key, when the input's concept equals the key's concept.

   If the third step finds more than one candidate, the compiler reports an ambiguity and binds nothing. Names are never
   matched by words.
3. **Selection and visibility are different.** A `for item` action with no item is **disabled**, with the text "Select a
   row first". It is not hidden. A guarded action keeps hiding when it has no item (0029). A hiding guard is for
   business conditions; it is not a way to ask for a selection.
4. **The default when nothing is written.** The compiler decides, from types only. The action is item-scoped when all of
   these hold:
   - the nearest `data` is a collection;
   - the command has a required `identifier` input that is not `generated`;
   - the collection's read model has a key, or a property, of that input's concept.

   Otherwise it is screen-scoped. The compiler reports what it inferred, as information, with a repair that writes `for
   item` or `for screen`. Existing guarded actions are unchanged.
5. **Boards.** On a board, the selected card is the `item`, with the same rule.
6. **Click time.** The subject is the item displayed when the action is clicked (`screens.md:99`). A guard hides or
   disables the control; it is never authorization.
7. **Scene gets a typed subject.** Scene #68 is re-scoped: an `ActionSubject` on `core:action` and on
   `InteractionBinding`, naming its source (a data binding, or a component's selected item) and what happens when it is
   absent (disabled or hidden), ordered alternatives with a condition, a command and arguments, an `otherwise`, and a
   stable action identity. Members are init-only on the existing records. Identifier arguments are binding expressions
   through the existing `component.<id>.selectedItem.<key>` source. Scene implements the current null rule
   (`== null` matches an explicitly null field, a missing field is different; `screens.md:84`), not older issue text.
8. **Stage** emits the typed subject instead of `rowAction` and `itemSelection`, drops its row scanning and its name
   aliasing, and keeps reading the old properties only until Scene ships the contract.
9. **No ESM change.** Screen syntax is outside the executable model.

## Options considered

- **A separate `row action` keyword.** Rejected: it duplicates guarded and plain actions and cannot say "data item of a
  record page".
- **`requires selection` as a modifier (#684's wording).** Rejected: it covers enabled and disabled only. `for item`
  also fixes input binding.
- **A guard such as `item.id != null` to require selection.** Rejected: it mixes availability with business conditions
  and forces a fake condition on commands that have none.
- **Keep the inference in Stage at run time (#745 as written).** Rejected: it is name-based, scans every query, and
  needs the live Host and the rendered application kept in step by hand.
- **Infer the target from any selected row.** Rejected: ambiguous with two tables.
- **Replace guarded actions with row actions.** Rejected: choosing between commands by state stays useful.
- **Bulk execution over several selected rows.** Not part of this record; it needs its own cardinality and failure rules.

## Consequences

- **Screenplay:** parser, printer, syntax JSON (`ScreenActionSyntax.Subject` and `Inputs`), validation, the inference
  diagnostic and repair, `screens.md` and the walkthrough.
- **Scene:** model, wire schema, TypeScript model, runtime evaluation of `core:action`, conformance fixtures.
- **Stage:** `ScreenDirectiveConverter`, `ScreenplaySceneVisitor`, `StageSceneDocument`, `StageCommandForm`,
  `stageComponents`. Fixes #693 and the parity gap in #745.
- **Arc, Chronicle, CLI:** none beyond bundled versions. **AI corpus:** teaches `for item` and `for screen`.
- Existing `.play` files keep working: no clause means the compiler-inferred default, which matches the documented
  Acme behaviour without the false positives of #693.
- A list whose read model reuses another entity's key concept as a foreign key (an opportunity row carrying an
  organization id) is the main risk. The subject read model's own key wins; a remaining tie is reported.

## Open points

1. Whether `for item` may name a specific table when a screen has several (`for item of <data>`). Today the nearest
   container decides.
2. The wording of the disabled state, and whether it is localizable.
3. Whether the bridge (Stage #745) ships before the Screenplay clause.

## Related

Builds on 0029 and 0048. Screenplay #684; Stage #670, #693, #745; Scene #68.
