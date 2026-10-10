---
title: Completeness checks
description: Select structural warnings for missing connections in a valid Screenplay model.
---

Use opt-in structural checks to find missing connections in an otherwise valid model:

```bash
screenplay Samples/Library --check data-bindings,input-surfaces,field-origins,query-keys --warnaserror
screenplay model --scope Billing.Invoices --check all
```

Repeat `--check` to combine selections. Names and diagnostic codes are case-sensitive; an unknown or empty selection exits `2`. A code selects its whole check. Findings are warnings or information; `--warnaserror` fails on warnings in the reported set. Ordinary compilation does not run these checks.

Checks require the **whole application** to have no source errors, even when checking one scope. Otherwise the CLI prints `completeness checks skipped: the model has N error(s)`. Fix source errors before treating an empty completeness result as evidence. Source warnings do not prevent checks.

MCP `diagnostics` accepts the same comma-separated selection as `checks`. Findings share its severity summary, revision-bound pages, document filter and scoped declaration selection. `completenessStatus` reports skipped checks; `completenessCoverage` says `structure only; a finding is a prompt to look`.

## Rules

| Check | Codes | Rule and exemptions |
|---|---|---|
| `data-bindings` | PLAY0530, PLAY0531 | Bindings visible along one screen container chain must agree on cardinality, resolved query and `by`. Identical rebinding and sibling sections are allowed. A binding's read model and cardinality must match its query's return; observable and optional qualifiers do not change this check. Unknown or ambiguous shapes are skipped. |
| `input-surfaces` | PLAY0532, PLAY0533 | An action needs an issuing screen in the command's own slice or a module-level command-bound form. Post-action `navigate to` does not supply input, and a title-only screen in the command's slice does not issue the command. Commands whose every property is generated need no typed input. Command properties currently have no context-source declaration; `$context` in an event mapping does not exempt a caller-supplied input. A StateChange command needs a used screen action or attached behavior `execute`; a form alone is not an issuer. Attached navigation or `open dialog` can reach an issuing screen, but an unattached named behavior cannot. Without declared navigation roots, authored screens are inspected; once roots exist, disconnected screens do not count. The navigation check separately diagnoses missing entry points; reaction-invoked commands and Automation/Translate commands are exempt. API-only invocation is not inferred. |
| `field-origins` | PLAY0534 | Each top-level field of an explicitly shaped projection-built read model needs an explicit key target, mapping (including `$eventSourceId`), compatible AutoMap, children/nested target or counter. The owning slice's unambiguous keyed-query property counts as the identity. Each variant is checked separately, including AutoMap from its entering events. A view with no builder or performer is reported once. Reducers, opaque code, performer-served views, imports and unknown coverage are exempt. |
| `query-keys` | PLAY0535 | Without a performer, queries over event-built views need `by` types compatible with the view identity or key parts, and `filter` names held by compatible view properties or key parts. `$context.tenant` parameters and unknown identity types are skipped. Identity is the property named by the owning slice's single distinct keyed-query `by` name, or a structurally resolved event/from key (a projection-level key is ignored by the binder); read models do not declare `identifier` properties. An optional filter may narrow a required field; omission does not require the field to be nullable. This is structural: it does not infer periods or historical retention from vocabulary. |
| `event-consumers` | PLAY0536 | Events need a projection, reducer, reaction, constraint or interaction consumer. Specifications do not count. Imports are exempt. Terminal facts and externally consumed events can legitimately trigger this check; there is no acknowledgement mechanism yet. |
| `navigation` | PLAY0537 | Screens must be reachable from contributions or shell-level behaviors. Edges include actions, row clicks, screen behaviors and discovered forms' submit navigation. Forms are discovered at screen actions and attached behavior `execute` sites, including parameterized command arguments. `open dialog` reaches screens filling that dialog template. Cycles without an entry point are unreachable. Unattached named behaviors and unused screen/dialog templates are not entry points. Template behaviors are edges from the screens using them. With no roots, one unlocated application finding states that no screen is reachable and is retained in every scope. Deep links and opaque code are not inferred. |
| `personas` | PLAY0574, PLAY0575, PLAY0576 | Warns when none of a persona's policies gates a command, query or inherited screen scope; warns when an effective command or query gate definitely denies every synthesized persona. Unsynthesizable personas and undecidable claim targets count as unknown, not denial. Reports unpinned `or` choices with multiple buildable alternatives as information, naming an unchosen alternative and suggesting a policy that pins it. No findings without personas. |

| `privilege` | PLAY0652 | Checks each producer of the trigger event against an elevated reaction's exact roles. Warning for unprivileged producers; Information for opaque gates. Clock and application triggers are silent. |
| `purposes` | PLAY0602–PLAY0606 | [Processing purposes](purposes.md): prompts for uncovered personal data, missing special-category conditions or criminal authorization, missing bases and unused purposes. Coverage is the union of references on slices and their ancestors; composite types are traversed. Findings do not assess lawfulness or enforce retention. |

## Elevated reaction paths

Select `--check privilege` (or `PLAY0652`) to inspect an event-triggered reaction's `runs as` roles. Each producer of its trigger event must require **every** declared role in its effective command gate, including module and feature gates. An alternative such as `role "Automation" or role "User"` does not require the automation role.

Ungated commands, captures and events produced by other reactions count as unprivileged producers and receive Warning. Opaque gates receive Information stating that they cannot be compared. Clock and application triggers are silent because they have no producer. An identity with no roles adds no role requirement. This check prompts you to review the trusted path; it neither executes a reaction nor proves that external effects are safe.

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
