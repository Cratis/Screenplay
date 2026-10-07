---
title: Completeness checks
description: Select structural warnings for missing connections in a valid Screenplay model.
---

Use opt-in structural checks to find missing connections in an otherwise valid model:

```bash
screenplay Samples/Library --check data-bindings,input-surfaces,field-origins,query-keys --warnaserror
screenplay model --scope Billing.Invoices --check all
```

Repeat `--check` to combine selections. Names and diagnostic codes are case-sensitive; an unknown or empty selection exits `2`. A code selects its whole check. Findings are warnings, so `--warnaserror` fails on findings in the reported set. Ordinary compilation does not run these checks.

Checks require the **whole application** to have no source errors, even when checking one scope. Otherwise the CLI prints `completeness checks skipped: the model has N error(s)`. Fix source errors before treating an empty completeness result as evidence. Source warnings do not prevent checks.

MCP `diagnostics` accepts the same comma-separated selection as `checks`. Findings share its severity summary, revision-bound pages, document filter and scoped declaration selection. `completenessStatus` reports skipped checks; `completenessCoverage` says `structure only; a finding is a prompt to look`.

## Rules

| Check | Codes | Rule and exemptions |
|---|---|---|
| `data-bindings` | PLAY0530, PLAY0531 | Bindings visible along one screen container chain must agree on cardinality, resolved query and `by`. Identical rebinding and sibling sections are allowed. A binding's read model and cardinality must match its query's return; observable and optional qualifiers do not change this check. Unknown or ambiguous shapes are skipped. |
| `input-surfaces` | PLAY0532, PLAY0533 | An action needs the command's own slice screen, a command-bound form, or navigation to the command's slice or another screen invoking it. Commands whose every property is generated need no typed input. Command properties currently have no context-source declaration; `$context` in an event mapping does not exempt a caller-supplied input. A StateChange command needs an action, form or behavior `execute`; reaction-invoked commands and Automation/Translate commands are exempt. API-only invocation is not inferred. |
| `field-origins` | PLAY0534 | Each top-level field of an explicitly shaped projection-built read model needs identity/key coverage, mapping, compatible AutoMap, children/nested target or counter. Each variant is checked separately. A view with no builder or performer is reported once. Reducers, opaque code, performer-served views, imports and unknown coverage are exempt. |
| `query-keys` | PLAY0535 | Without a performer, queries over event-built views need `by` types compatible with the view identity or key parts, and `filter` names held by compatible view properties or key parts. `$context` parameters and unknown identity types are skipped. This is structural: it does not infer periods or historical retention from vocabulary. |
| `event-consumers` | PLAY0536 | Events need a projection, reducer, reaction, constraint or interaction consumer. Specifications do not count. Imports are exempt. Terminal facts and externally consumed events can legitimately trigger this check; there is no acknowledgement mechanism yet. |
| `navigation` | PLAY0537 | Screens must be reachable from contributions or shell-level behaviors. Edges include actions, row clicks, screen behaviors and discovered forms' submit navigation. `open dialog` reaches screens filling that dialog template. Cycles without an entry point are unreachable; no roots means every screen is reported. Deep links and opaque code are not inferred. |

## Library API

```csharp
using Cratis.Screenplay.Completeness;

if (CompletenessChecks.TryParse("data-bindings,query-keys", out var checks))
{
    var findings = ModelCompleteness.Check(compilation.Result, checks);
}
```

The compilation-result overload returns no findings on failed source compilation. The syntax-tree overload requires an error-free, merged application supplied by the caller. Neither API binds or executes the model, proves business completeness, nor inspects hand-written code.

See [Diagnostics](diagnostics.md) for stable codes and [scoped checking](tool.md#check-one-part-of-the-application) for the reported-set contract.
