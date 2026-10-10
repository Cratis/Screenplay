---
title: Editor diagnostic support
description: Find which Screenplay diagnostics the editors share and which require the C# compiler.
---

A clean editor buffer is not a compiler verdict. Monaco and the VS Code extension use the [TypeScript compiler](typescript-compiler.md) for local syntax and selected authoring checks. The [C# compiler and CLI](tool.md) remain the authority for whole-application validation and executable readiness.

The diagnostic catalogues share the C# names and codes. A catalogue entry does not promise that the TypeScript compiler runs that check.

## Shared checks

| Codes | TypeScript and editor support |
|---|---|
| PLAY0633–PLAY0646 | Identity declaration, source, duplicate-name, built-in-name and keyed-query/type/authorization checks. Both editors forward these checks from the assembled TypeScript model; caller detail paths are completed and validated alongside token built-ins. Executable detail reads still require the C# binder's PLAY0268 verdict (#600). |
| PLAY0514, PLAY0515 | Projection target and personal identifier checks. Both editors preserve compiler diagnostics. |
| PLAY0518, PLAY0519 | Typed-example syntax and duplicate assignments. Both editors preserve compiler diagnostics. Example type resolution and semantic values remain C# checks. |
| PLAY0341–PLAY0344 | Guarded-action syntax. Both editors preserve compiler diagnostics. |
| PLAY0345–PLAY0348 | Scoped guarded-action validation: nearest data subject, nested field paths, command argument properties and collection cardinality, and provable shadowing with a 64-disjunct expansion cap. Unknown or ambiguous shapes remain undecided. Monaco forwards these checks for buffers; VS Code also resolves declarations across workspace files compiled together. |
| PLAY0391 | A unique constraint names a missing direct event field. Checks declared events across all generations, including inline events and merged files. Unknown/imported events, dotted paths and removed-generation fields remain undecided here. |
| PLAY0478 | Information advising an explicit destination for plain event productions when there is exactly one required scalar command identifier. Operations, inline events, conditional productions and explicit destinations are excluded. Advice does not change routing. |
| PLAY0453 | Malformed read-model absence assertions in `numbers exact` mode. Legacy numeric mode deliberately skips malformed `then no readmodel` assertions without a diagnostic, preserving its existing parser behavior. |
| PLAY0607–PLAY0620 | Public/private event boundaries on the assembled model: scoped operational inputs and outputs, imports, seeds, direction, exactly one local public output type, event-target projections and reducers, and `source events` captures. Both editors forward these diagnostics. Unresolved and ambiguous references remain unclassified; legacy Translate slices without public metadata retain inbound behavior. Event-target projections and `source events` captures are checked like any other operational edge. These checks do not admit public events for execution. |
| PLAY0538–PLAY0545 | Refusal-branch and no-event assertion syntax, scoped constraint resolution, selector coverage, refusal-value scope and types, and redelivery observer/occurrence matching. Monaco forwards these diagnostics for buffers; VS Code also reports them for files compiled together in a workspace folder. These checks do not admit the features for execution. |
| PLAY0621, PLAY0625 | Malformed `exposure` and `instance` declarations. Both compilers report them. |
| PLAY0622–PLAY0624, PLAY0626–PLAY0632 | Screen-composition references on the assembled model: exposure owners, re-exposures, instances, exposed values, template content slots, navigation outlets, template scopes and component packages. C# only; the TypeScript compiler parses the syntax but does not resolve these references. |

Shared invalid-source cases live in `Source/Screenplay/Compiler/Conformance/diagnostics.json`; both compilers are checked against their codes, lines and order.

## C#-only checks

These checks are not computed by the TypeScript compiler or by the editors' ordinary validation:

| Codes | Why the C# tool is required |
|---|---|
| PLAY0530–PLAY0537 | Opt-in [completeness checks](completeness.md), selected through CLI `--check` or MCP `checks`, after error-free whole-application compilation. TypeScript has no completeness-check API. They are not ordinary parser warnings. |
| PLAY0546 | The semantic binder checks negated claim targets for nullable, missing or non-string values. TypeScript has no executable semantic binder. |

The opt-in C# repair bridge is not a general C# validation service. Run the CLI or MCP checks for C#-only verdicts. Public-event repair capabilities and ESM, Stage and Chronicle support remain required before the full feature ships.

This boundary follows the TypeScript compiler's syntax/authoring scope, not executable admission: a check may be C#-only even when C# reports it before binding. See [Diagnostics](diagnostics.md) for conditions and severities, [guarded actions](screens.md) for their meaning, and [reaction refusals](reactions.md) for the syntax-only admission boundary.
