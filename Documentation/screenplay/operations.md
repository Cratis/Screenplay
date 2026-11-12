---
title: "Screenplay operations: describe external-system work"
description: "Describe external-system work with Screenplay operations, including inputs and compensation intent. Declarations are syntax-only and cannot execute."
---

# Operations and external systems

An operation in Cratis Screenplay describes work a command asks an external system to do, with its inputs and optional execution and compensation intent. Use it to make that dependency visible in the model, not to run an integration.

Operations and their specification steps are syntax-only, and so is a system that declares no ability. Binding reports `PLAY0268` and returns no executable model: no supported executable semantic model (ESM) version admits them. A system that declares [abilities](systems.md) is admitted in ESM v50; an operation that `uses` it is still refused. Attaching code does not change that boundary, and the reference runner cannot execute their specifications.

For example, order fulfilment may need work from a carrier system. An operation can describe that work, but it does not provide a running carrier adapter or a durable wait. Use a [workflow](workflows.md) to describe the longer process and a [reaction](reactions.md) for event-driven follow-up. Neither makes an operation executable.

## Declare intent

A `system` belongs to the application, including when its file is imported into a module or feature. It names the business system, not a provider interface. It accepts an optional description and `ability` blocks, which say what the system can be asked and are described in [Systems and abilities](systems.md). An operation's `uses` names a system whether or not that system declares abilities, and it does not call an ability.

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

Here, `RegisterProject` describes a request to open a provisional cost center in `Accounting`. The `execute` phase points to the intended adapter; `compensate` describes closing the cost center if registration does not commit. These are declarations of intent, not evidence that either action runs.

The [complete source fixture](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/fixtures/operations.play) covers inline and standalone operations, composite input sources, optional collections, failure and compensation specifications, and a cross-slice reference. It is outside `Samples/` because these constructs prevent an executable model from being produced. Invoicing is also a syntax showcase with individual binding dispositions, not a runnable model. The samples policy lists operations among its [syntax-only constructs](https://github.com/Cratis/Screenplay/blob/main/.cratis/ai/rules/project/samples.md#preview-constructs); neither example demonstrates an executable integration.

## Invoke an ability from a phase

A phase of an operation can name the ability it calls with `invokes <System>.<Ability>`, with its inputs. Operation phases do not handle answers, so a branch there is `PLAY1016`. They are authoring intent: binding reports `PLAY0268` naming #1252. Use a reaction, workflow action or state entry to [invoke an ability](systems.md#invoke-an-ability) that runs.

## Declare inline or reference a declaration

`produces operation <Name>` declares a slice-owned operation. Each input is a typed mapping, `<input> <Type> = <source>`. Plain `produces <Name>` only references an existing event or operation; a typo never creates one. Referenced operations use untyped target mappings, `<input> = <source>`.

Event and operation names share the slice namespace, including inline declarations. Resolution considers both kinds together. A collision or ambiguous reference is an error, not an event-first guess. Operation productions belong to commands, not reactions, and do not accept event `for` destinations, tags or routing metadata. Required inputs must be mapped; optional inputs may be omitted. Known mapping types must be compatible. Unknown imported shapes are not fabricated.

Keep mixed event and operation productions in one authored sequence. The intended transaction contract enrolls events first, executes operations in authored order before commit, and makes declared compensation follow the commit disposition. That is not implemented execution support, and it specifies neither reverse compensation order nor a rollback mechanism. Operations describe pre-commit work, not post-commit reactions.

### Reuse across slices and files

A standalone operation can be referenced from another slice in the assembled application. Use enough scope to identify it uniquely, for example `produces Register.NotifyAccounting` in the fixture's `Reconcile` slice. Qualification is available **only for explicit operation productions**; it does not broaden event production grammar. A contract import alone is not evidence of an operation declaration. [Quoted imports](imports.md) bring real declarations and preserve placement.

### Promote an inline operation manually

Move the inline declaration body to `operation <Name>` in its owning slice. Remove each mapping RHS from the declaration inputs, and replace the old inline block with `produces <Name>` plus its existing untyped mappings **at the same sequence position**. Retain descriptions, phases, comments and attachments. If you move files, check relative attachment paths and every reference against the new layout. Validate the complete application before accepting the edit.

No automatic operation extraction is available. Event extraction's executable-byte proof cannot establish operation equivalence while operations are not admitted by any supported executable model (ESM) version yet.

## Attach phase intent or source

Use a phase description when you know what the external work should do but have not attached its implementation. A description-only or hints-only phase is valid intent; it is neither a missing-source error nor an execution guarantee.

Each phase accepts a description and either a direct `file`/tagged fence or an `implementation` wrapper with ordered nonblank `hint` lines and an optional source. The phase owns its sole `File` or `Code`; the wrapper owns hints only. Mixed direct/wrapped sources, duplicate payloads and duplicate phases are rejected.

Existing fields named `system`, `operation`, `command`, `execute` or `compensate` keep their property meaning. Bare `execute`/`compensate` open phases; typed forms remain inputs. Use `@uses Type` for an input named `uses`, and escape event metadata names in standalone operation inputs. Inline typed mappings are recognized before directives, for example `sequence Int = count`.

## Inspect and edit

Monaco and VS Code provide declaration-aware suggestions, typed input/source assistance, phase/hint hover and attachment navigation. Ambiguous references receive no guessed declaration. The board describes ordered operation and system intent in existing command details; it creates no operation event card, event identity or passing operation assertion state.

[MCP authoring tools](mcp/authoring-tools.md#operation-and-system-intent) expose revision-pinned inventories and typed source handles without executable identities. Use existing typed AST proposals with Authoring validation. Executable validation refuses these constructs with `PLAY0268`; accepted source does not realize or confirm code.

Continue with [operation specifications](specifications.md#operation-specifications-syntax-only) to describe intended success, failure and compensation outcomes. They remain syntax-only. Use the [grammar](grammar.md) for accepted forms and [diagnostics](diagnostics.md#operations-and-external-systems) to interpret authoring errors.
