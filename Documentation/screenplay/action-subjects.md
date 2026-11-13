---
title: "Screenplay action subjects: screen or selected item"
description: "Use for item for actions on selected data and for screen for screen-wide actions. Screenplay can infer subjects and bind inputs from declared types."
---

# Action subjects

An action's subject says whether it acts on a selected data item or on the screen as a whole. Use `for item` for an action that needs the displayed record or a selected row, and `for screen` for an action that does not depend on selection.

Here, retrying uses an item's identity, while adding an item stays available independently of row selection:

```play
action RetryItem for item label "Retry"
  with itemId from item.rowId
  with note from $context.note

action AddItem for screen

toolbar actions
  item retry action RetryItem for item
  item add action AddItem for screen
```

Place the clause after the command or guarded label, before an inline `label`. In a toolbar, only action items accept it; navigation and dialog items do not. `for item` and `for screen` are contextual words, not reserved declaration or property names.

The `with` lines explicitly supply the retry command's inputs. Plain actions keep those lines in authored order. Guarded alternatives keep their arguments, and toolbar items keep their UI-binding parameters.

## Inference and inputs

`for item` needs one nearest `data` declaration: a record, or a collection from which the user selects an item. Local sibling data takes precedence over inherited data, including in sections, filled slots and component slots. Source order does not break a tie between equally near declarations; missing or ambiguous data needs to be resolved explicitly.

A screen often shows a header record next to a table of rows. An authored plain or toolbar `for item` action then acts on the one collection `data` that a `table` in scope shows, so the header does not make it ambiguous:

```play
data Header via query HeaderOf
data Row[] via query Rows
table Row
action RetryItem for item
```

The narrowing needs exactly one collection data shown by a table in scope; a table over a record or two tables over two collections leave the action ambiguous, and PLAY0346 names the candidates. Guarded actions and inferred subjects are unchanged.

Once the subject is known, inputs are supplied in this order:

1. Explicit input bindings win.
2. Remaining non-generated inputs use compatible, case-sensitive same-name fields.
3. A required, non-generated, non-collection identifier can use one key field with the same declared nominal concept.
4. Remaining inputs can use an existing command form or renderer input.

A nominal concept is a declared type that gives an identifier its meaning, not just its underlying value type. Matching primitives, similar names, optional identifiers and unmarked foreign keys do not prove that an input identifies the subject. If several declared keys match, the compiler warns and you must write the binding. It never chooses the first row or first key.

`for screen` keeps plain and toolbar actions independent of selection and does not fill inputs from a row. A guarded action can inspect a single record even with `for screen`; collection-valued data warns, stays unresolved, and never chooses the first row.

When you omit the clause, a plain or toolbar action infers `Item` only when there is exactly one collection data context, exactly one required non-generated identifier, and one matching declared key. Otherwise `Screen` remains the omitted legacy default. Guarded actions retain their single-subject behavior. In new documents, write the subject explicitly so readers can see whether selection matters.

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

Native `ScreenActionSubjects.Resolve(application)` and TypeScript `resolveActionSubjects(application)` return a separate effective application and diagnostics without mutating authored syntax. Resolving the effective application again does not change it. Consumers needing subject behavior opt in; raw compilation, canonical JSON and printing preserve the source form. Empty inputs and omitted subjects remain absent in legacy raw JSON. The additive native properties do not alter positional record constructors or deconstruction arity.

The paired native/TypeScript fixture packs are under `Source/Screenplay/Compiler/Conformance/action-subjects`: authored source, raw syntax, effective syntax, diagnostics, and context/location sidecars. Each context includes the complete authored declaration tree and canonical-path source locations, so consumers can verify nominal identity without inventing it. Native/TS packs preserve their existing modeled syntax surfaces and assert common-member parity separately.

Scene owns runtime execution and rendering. The compiler fixtures establish how subjects and inputs resolve; they do not establish renderer support or change the executable semantic model.

## Corpus migration gate

If you are integrating inferred subjects into a Scene release, first compile each real application workspace with its imports and collect PLAY0818 and PLAY0819 by source path, line and command. Treat an unresolvable declaration as insufficient evidence, not a successful migration. For every PLAY0819, review the previous row-name bindings alongside the proposed effective inputs; either state `for screen`, state `for item` with the intended explicit inputs, or retain the previous bindings explicitly. Apply a proposed repair transactionally and compile the complete workspace again. Do not bulk-accept a binding based only on its spelling.

Track that real-corpus scan and disposition review as a follow-up to Screenplay#1052, before the consuming Scene release. The repository samples and paired fixture packs are the compiler regression gate, not evidence that an external corpus has been migrated. Scene must separately verify selection, form/renderer inputs and guarded single-record behavior against the effective pack; it must not reinterpret the raw authored pack as resolved execution metadata.

With the subject and inputs chosen, use [guarded actions](screens.md#one-action-several-commands) when item state determines which command to offer. Use [interactions](interactions.md) when a gesture needs to run a sequence of actions.
