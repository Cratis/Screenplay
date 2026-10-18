---
title: Editor diagnostic support
description: Find which Screenplay diagnostics the editors share and which require the C# compiler.
---

A clean editor buffer is not a compiler verdict. Monaco and the VS Code extension use the [TypeScript compiler](typescript-compiler.md) for local syntax and selected authoring checks. The [C# compiler and CLI](tool.md) remain the authority for whole-application validation and executable readiness.

The diagnostic catalogues share the C# names and codes. A catalogue entry does not promise that the TypeScript compiler runs that check.

## Shared checks

| Codes | TypeScript and editor support |
|---|---|
| PLAY0530, PLAY0531 | Opt-in data-binding completeness (`data-bindings`, `PLAY0530` or `PLAY0531`): conflicting visible screen bindings and query return-type/cardinality mismatches. The TypeScript `completenessDiagnostics` API computes these after error-free compilation. Both editors preserve supplied findings; ordinary validation does not request completeness checks. |
| PLAY0574–PLAY0576, PLAY0602–PLAY0606, PLAY0652 | Opt-in persona (`personas`), processing-purpose (`purposes`) and reaction-privilege (`privilege`) completeness. The TypeScript `completenessDiagnostics` API computes these after error-free compilation; each diagnostic-code alias selects its whole check, and `all` includes them. Both editors preserve supplied warnings and Information findings; ordinary validation does not request completeness checks. |
| PLAY0667–PLAY0671 | Enum numbering syntax, uniqueness and int32 bounds. PLAY0671 is an opt-in completeness Information hint for unnumbered enums used by event properties; editors preserve it when supplied with completeness diagnostics, but ordinary validation does not report it. Numbering is opt-in through a refactoring, not an automatic correctness repair. |
| PLAY0663–PLAY0665 | Command-only production routes, redundant overrides, and observer filter syntax, source/stream resolution and event-only triggers. |
| PLAY0660, PLAY0662 (shape checks) | Read-model key modifiers and key-part types; `by` block arity and duplicate names. Scoped lookup resolution remains a C# check. |
| PLAY0647–PLAY0649 | Reaction `runs as` syntax, gated invocations without identity and unused identity. Both editors forward the TypeScript checks; declared identity suppresses PLAY0557. PLAY0650–PLAY0651 are C# bound-gate analyses, and PLAY0652 is the opt-in `privilege` completeness finding. Supplied C# findings are preserved. |
| PLAY0633–PLAY0646 | Identity declaration, source, duplicate-name, built-in-name and keyed-query/type/authorization checks. Both editors forward these checks from the assembled TypeScript model; caller detail paths are completed and validated alongside token built-ins. Executable detail reads still require the C# binder's PLAY0268 verdict (#600). |
| PLAY0514, PLAY0515 | Projection target and personal identifier checks. Both editors preserve compiler diagnostics. |
| PLAY0518, PLAY0519, PLAY0520, PLAY0526 | Typed-example syntax, duplicate assignments, type resolution and non-event example routes. The TypeScript compiler emits the same declared arguments as C# for these checks. Example kind mismatches and semantic example values remain C# checks. |
| PLAY0341–PLAY0344 | Guarded-action syntax. Both editors preserve compiler diagnostics. |
| PLAY0345–PLAY0348 | Scoped validation of guarded screen actions and guarded interaction alternatives (`on click`, `double click` and `select` with `when` branches, including named behaviors at each `uses` site): nearest data subject, nested field paths, command argument properties and collection cardinality, and provable shadowing with a 64-disjunct expansion cap. Unknown or ambiguous shapes remain undecided. Monaco forwards these checks for buffers; VS Code also resolves declarations across workspace files compiled together. |
| PLAY0391 | A unique constraint names a missing direct event field. Checks declared events across all generations, including inline events and merged files. Unknown/imported events, dotted paths and removed-generation fields remain undecided here. |
| PLAY0478 | Information advising an explicit destination for plain event productions when there is exactly one required scalar command identifier. Operations, inline events, conditional productions and explicit destinations are excluded. Advice does not change routing. |
| PLAY0465 (query-error shape) | Exactly one actionless query and one error, without results, no result, denial or other outcomes. Both editors forward the shared shape check; ESM v18 admission and concept-rule execution remain native C# checks. |
| PLAY0453 | Malformed read-model absence assertions in `numbers exact` mode. Legacy numeric mode deliberately skips malformed `then no readmodel` assertions without a diagnostic, preserving its existing parser behavior. |
| PLAY0607–PLAY0620, PLAY0687, PLAY0694 | Public/private event boundaries on the assembled model: scoped operational inputs and outputs, imports, seeds, direction, exactly one local public output type, event-target projections and reducers, and `source events` captures, including captures whose events come from several origins. Both editors forward these diagnostics. Unresolved and ambiguous references remain unclassified; legacy Translate slices without public metadata retain inbound behavior, and PLAY0694 warns when such a slice reacts to an event its own capture appends. Event-target projections and `source events` captures are checked like any other operational edge. These checks do not admit public events for execution. |
| PLAY0538–PLAY0545 | Refusal-branch and no-event assertion syntax, scoped constraint resolution, selector coverage, refusal-value scope and types, and redelivery observer/occurrence matching. Monaco forwards these diagnostics for buffers; VS Code also reports them for files compiled together in a workspace folder. Redelivery and explicit no-event assertions execute from ESM v17; refusal branches and `$refusal` values execute from ESM v19. |
| PLAY0621, PLAY0625 | Malformed `exposure` and `instance` declarations. Both compilers report them. |
| PLAY0622–PLAY0624, PLAY0626–PLAY0632, PLAY0654–PLAY0656 | Screen-composition references on the assembled model: exposure owners, re-exposures, instances, exposed values, template content slots and nesting cycles, screen contribution points, navigation outlets and destinations, template scopes and component packages. C# only; the TypeScript compiler parses the syntax but does not resolve these references. |

Compiler diagnostics expose the [declared argument contract](diagnostics.md#for-tool-authors). The Monaco marker API and VS Code diagnostic API have no general diagnostic `data` field; their displayed message and code stay unchanged.

Shared invalid-source cases live in `Source/Screenplay/Compiler/Conformance/diagnostics.json`; both compilers are checked against their codes, lines and order.

## C#-only checks

These checks are not computed by the TypeScript compiler or by the editors' ordinary validation:

| Codes | Why the C# tool is required |
|---|---|
| PLAY0521–PLAY0525, PLAY0351 | Example name collisions, kind mismatches, value admission, complete step fixtures and missing read-model identifiers. PLAY0524 includes the authored specification path, step keyword, authored step index, missing property and effective type, plus example and case names when applicable. These arguments are available through C# and MCP, not ordinary local editor validation. |
| PLAY0666 | Warns when a reaction or reducer filter excludes every statically known producer's effective route. Handlers and foreign origins remain unknown. |
| PLAY0690 | Compares known nominal producer destinations with reducer-backed instance queries, or warns about several identity domains in one reducer. Unknown provenance suppresses the comparison. Both editors preserve supplied native warnings; local TypeScript validation does not compute them. |
| PLAY0661, PLAY0662 (scoped checks) | Resolve declared read-model keys and require complete compatible single-instance lookups across reads, queries, screens, forms and absence fixtures. |
| PLAY0532–PLAY0537 | Opt-in [completeness checks](completeness.md), selected through CLI `--check` or MCP `checks`, after error-free whole-application compilation. The TypeScript completeness API supports data bindings (PLAY0530/PLAY0531), personas (PLAY0574–PLAY0576), purposes (PLAY0602–PLAY0606), privilege (PLAY0652) and enum pinning (PLAY0671), including their diagnostic-code aliases and `all`; the remaining checks are C#-only. PLAY0536 is blocked on TypeScript interaction/behavior syntax: `Syntax/Screens.ts` retains only `on`/`uses` markers, without event or behavior names. These are not ordinary parser warnings. |
| PLAY0546 | The semantic binder checks negated claim targets for nullable, missing or non-string values. TypeScript has no executable semantic binder. |
| PLAY0686 | Retired by ESM v16, which carries event `id` pins as stored names. Both editors still preserve warnings supplied by earlier native compilers; ordinary TypeScript validation does not bind the ESM. |

The opt-in C# repair bridge is not a general C# validation service. Run the CLI or MCP checks for C#-only verdicts. Public-event repair capabilities and ESM, Stage and Chronicle support remain required before the full feature ships.

This boundary follows the TypeScript compiler's syntax/authoring scope, not executable admission: a check may be C#-only even when C# reports it before binding. See [Diagnostics](diagnostics.md) for conditions and severities, [guarded actions](screens.md) for their meaning, and [reaction refusals](reactions.md) for their executable admission boundary.
