---
title: "Action subjects"
description: "Explicit action scope, identifier-aware inputs, and the authored/effective syntax boundary."
---

# Action subjects

State intent explicitly (decision 0110):

```play
action RetryItem for item label "Retry"
  with itemId from item.rowId
  with note from $context.note

action AddItem for screen

toolbar actions
  item retry action RetryItem for item
  item add action AddItem for screen
```

The clause follows the command or guarded label, before an inline `label`. Only action toolbar items accept it; navigation and dialog items do not. These are contextual words, not new reserved declaration or property names. Plain-action `with <input> from <binding>` entries remain ordered authored syntax. Guarded alternatives retain their arguments; toolbar items retain their UI-binding parameters.

## Inference and inputs

`for item` needs one nearest `data` declaration: a record, or a selected collection item. Local sibling data takes precedence over inherited data regardless of source order, including sections, filled slots and outlets. Missing or equally near data is not guessed.

1. Explicit input bindings win.
2. Remaining non-generated inputs use compatible, case-sensitive same-name fields.
3. A required, non-generated, non-collection identifier can use one key field with the same declared nominal concept.
4. Remaining inputs can use an existing command form or renderer input.

Primitive equality, coincidentally similar names, optional identifiers and unmarked foreign keys are not identity proof. Multiple matching keys warn and need an explicit binding. There is no first-row or first-key fallback.

`for screen` keeps plain and toolbar actions independent of selection and does not fill inputs from a row. A guarded action can inspect a single record even with `for screen`; collection-valued data warns, stays unresolved, and never chooses the first row.

Without a clause, a plain or toolbar action infers Item only for exactly one collection data context, exactly one required non-generated identifier, and one matching declared key. Otherwise Screen remains the omitted legacy default. Guarded actions retain their existing single-subject behavior.

## Diagnostics and migration

| Code | Severity | Meaning |
|---|---|---|
| PLAY0817 | Error | Malformed, duplicate or misplaced subject clause. |
| PLAY0818 | Information | Explains an omitted plain/toolbar subject's inference. |
| PLAY0819 | Warning | Effective row binding differs from legacy case-insensitive name matching. |
| PLAY0820 | Warning | Multiple nominal keys could bind an input; write an explicit binding. |
| PLAY0821 | Warning | Guarded `for screen` has collection-valued data and cannot choose one item. |

PLAY0319 still covers malformed/duplicate input syntax, PLAY0345 still covers unknown subject fields, and PLAY0346 still covers a missing or ambiguous guarded subject. Fixes can make inference explicit or retain legacy row behavior using explicit bindings when declaration proof is unambiguous. No fix silently rewrites source or guesses an imported declaration.

## Authored and effective syntax

Native `ScreenActionSubjects.Resolve(application)` and TypeScript `resolveActionSubjects(application)` return a separate effective application and diagnostics without mutating authored syntax. Resolution is idempotent. Consumers needing subject behavior opt in; raw compilation, canonical JSON and printing preserve the source form. Empty inputs and omitted subjects remain absent in legacy raw JSON. The additive native properties do not alter positional record constructors or deconstruction arity.

The paired native/TypeScript fixture packs are under `Source/Screenplay/Compiler/Conformance/action-subjects`: authored source, raw syntax, effective syntax, diagnostics, and context/location sidecars. Each context includes the complete authored declaration tree and canonical-path source locations, so consumers can verify nominal identity without inventing it. Native/TS packs preserve their existing modeled syntax surfaces and assert common-member parity separately.

Runtime execution and rendering proof are Scene-owned. Compiler fixtures and editor assistance do not claim renderer support or change the executable semantic model.

## Corpus migration gate

Before making inference part of a Scene release, compile each real application workspace with its imports and collect PLAY0818 and PLAY0819 by source path, line and command. Treat an unresolvable declaration as insufficient evidence, not a successful migration. For every PLAY0819, review the previous row-name bindings alongside the proposed effective inputs; either state `for screen`, state `for item` with the intended explicit inputs, or retain the previous bindings explicitly. Apply a proposed repair transactionally and compile the complete workspace again. Do not bulk-accept a binding based only on its spelling.

Track that real-corpus scan and disposition review as a follow-up to Screenplay#1052, before the consuming Scene release. The repository samples and paired fixture packs are the compiler regression gate, not evidence that an external corpus has been migrated. Scene must separately verify selection, form/renderer inputs and guarded single-record behavior against the effective pack; it must not reinterpret the raw authored pack as resolved execution metadata.
