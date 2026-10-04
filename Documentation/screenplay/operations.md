# Operations and external systems

Describe the work a command asks another system to do without hiding it in a handler. An operation states its inputs, the external system it uses, and optional execution and compensation intent.

> **Experimental, syntax-only authoring.** Systems, operations and their specification steps cannot execute today. Binding reports `PLAY0268` and returns no executable model. ESM v9 admission and provider support are future work; the current reference runner cannot run these specifications, even when code is attached.

## Declare intent

A `system` belongs to the application, including when its file is imported into a module or feature. It names the business system, not a provider interface. It accepts an optional description; abilities are not supported.

An `operation` belongs to a slice. It declares exactly one `uses <System>`, typed inputs, an optional description and at most one `execute` and one `compensate` phase. Inputs support concepts, enums, composites, collections and `optional`. They cannot be `identifier` or `generated`.

````screenplay
system Accounting
  description "The finance department's ledger"
concept ProjectId : Uuid
module Projects
  feature Registration
    slice StateChange Register
      operation NotifyAccounting
        description "Open a provisional cost center"
        uses Accounting
        projectId ProjectId
        execute
          implementation
            hint "Use the existing accounting adapter"
            file Adapters/OpenCostCenter.cs
        compensate
          description "Close the provisional cost center if registration does not commit"
      command RegisterProject
        projectId ProjectId identifier
        produces NotifyAccounting
          projectId = projectId
````

The [complete source fixture](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/fixtures/operations.play) includes inline and standalone declarations, composite input sources, optional collections, failure and compensation specifications, and a cross-slice reference. It is intentionally outside executable `Samples/`: those samples remain runnable until v9 admission. This is the syntax-only exception to the samples policy, not an executable integration example.

## Declare inline or reference a declaration

`produces operation <Name>` declares a slice-owned operation. Each input is a typed mapping, `<input> <Type> = <source>`. Plain `produces <Name>` only references an existing event or operation; a typo never creates one. Referenced operations use untyped target mappings, `<input> = <source>`.

Event and operation names share the slice namespace, including inline declarations. Resolution considers both kinds together. A collision or ambiguous reference is an error, not an event-first guess. Operation productions belong to commands, not reactions, and do not accept event `for` destinations, tags or routing metadata. Required inputs must be mapped; optional inputs may be omitted. Known mapping types must be compatible. Unknown imported shapes are not fabricated.

Keep mixed event and operation productions in one authored sequence. **Future transaction contract:** events are enrolled first, operations execute in authored order before commit, and declared compensation follows commit disposition. This does not specify reverse compensation order or a rollback mechanism. Operations are not post-commit reactions.

### Reuse across slices and files

A standalone operation can be referenced from another slice in the assembled application. Use enough scope to identify it uniquely, for example `produces Register.NotifyAccounting` in the fixture's `Reconcile` slice. Qualification is available **only for explicit operation productions**; it does not broaden event production grammar. A contract import alone is not evidence of an operation declaration. [Quoted imports](imports.md) bring real declarations and preserve placement.

### Promote an inline operation manually

Move the inline declaration body to `operation <Name>` in its owning slice. Remove each mapping RHS from the declaration inputs, and replace the old inline block with `produces <Name>` plus its existing untyped mappings **at the same sequence position**. Retain descriptions, phases, comments and attachments. If you move files, check relative attachment paths and every reference against the new layout. Validate the complete application before accepting the edit.

No automatic operation extraction is available. Event extraction's executable-byte proof cannot establish operation equivalence before v9 admission.

## Attach phase intent or source

Each phase accepts a description and either a direct `file`/tagged fence or an `implementation` wrapper with ordered nonblank `hint` lines and an optional source. A phase owns its sole `File` or `Code`; the wrapper owns hints only. Mixed direct/wrapped sources, duplicate payloads and duplicate phases are rejected. A description-only or hints-only phase is valid **pending** intent, not a missing-source error or an execution guarantee.

Existing fields named `system`, `operation`, `command`, `execute` or `compensate` keep their property meaning. Bare `execute`/`compensate` open phases; typed forms remain inputs. Use `@uses Type` for an input named `uses`, and escape event metadata names in standalone operation inputs. Inline typed mappings are recognized before directives, for example `sequence Int = count`.

## Inspect and edit

Monaco and VS Code provide declaration-aware suggestions, typed input/source assistance, phase/hint hover and attachment navigation. Ambiguous references receive no guessed declaration. The board describes ordered operation and system intent in existing command details; it creates no operation event card, event identity or passing operation assertion state.

[MCP authoring tools](mcp/authoring-tools.md#operation-and-system-intent) expose revision-pinned inventories and typed source handles without executable identities. Use existing typed AST proposals with Authoring validation. Executable validation refuses these constructs with `PLAY0268`; accepted source does not realize or confirm code.

See [operation specifications](specifications.md#operation-specifications-syntax-only), [grammar](grammar.md) and [diagnostics](diagnostics.md#operations-and-external-systems).
