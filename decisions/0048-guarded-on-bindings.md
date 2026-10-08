---
id: 0048
title: Select whole interaction action lists with ordered item conditions
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-08
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/Parsing/InteractionParser.cs
  - Source/DotNET/Screenplay/Syntax/BehaviorSyntax.cs
  - Source/DotNET/Screenplay/Parsing/GuardedAction*.cs
  - Source/DotNET/Screenplay/Printing/ScreenplayPrinter.Interactions.cs
  - Source/DotNET/Screenplay/Workspaces/WorkspaceDiagnosticRepairs.cs
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/interactions.md
  - Documentation/screenplay/screens.md
  - Documentation/screenplay/diagnostics.md
  - Samples/Invoicing/**
---

## Context

[0029](0029-guarded-screen-action-renderer-contract.md) selects a command for one labeled action, but excludes guarded `on` bindings. [#447](https://github.com/Cratis/Screenplay/issues/447) asks for the same choice inside interactions, including row clicks and submit. Interaction `where` currently holds opaque text; event-trigger guards in Invoicing rely on that form. Screens and interactions remain deferred from the executable semantic model (ESM).

## Decision

An `on click`, `on double click` or `on select` binding may select a whole non-empty action list using ordered block-form `when` alternatives and an optional final `otherwise`. Conditions use exactly 0029's strict item-condition grammar and subject rules. Selection happens once against the rendered subject when the gesture occurs, never after a refetch and never again while the selected list runs. First match wins; failures, denials and unavailable commands never fall through. With a subject but no match, the fallback runs if present. With no subject, nothing runs, including `otherwise`. Opaque `where` is deprecated only on these three triggers: a strict item condition produces a Warning and a C# typed repair; other text produces Information without a repair. Event, change and application triggers keep opaque `where` unchanged and cannot carry alternatives.

### Form and conditions

```screenplay
on double click
  when item.status == "failed"
    confirm $strings.retryAttempt
      on success
        execute RetryAttempt
          with attemptId from item.attemptId
  when item.status == "open"
    open dialog AttemptDetails
      with attemptId from item.attemptId
  otherwise
    notify info $strings.attemptClosed
```

A binding contains either plain actions or alternatives, never both; `where` cannot accompany alternatives. At least one `when` is required. `otherwise` occurs at most once, last. Each branch has an indented, non-empty action list with the existing action order, continuations and confirmation short-circuit semantics. The label-headed spelling `when <condition> execute <Command>` is refused inside `on` and has a typed repair to block form.

Conditions compare `item.<path>` with literals and compose with `and`, `or` and parentheses, using the operand restrictions and diagnostics of 0029. Comparisons over missing or null fields do not match, except `== null`, which matches authored null, not missing fields.

### Subject and excluded triggers

- Click or double click on a table or collection data uses the activated row. Other elements use the nearest container's single item or selected row, including 0029's sibling precedence.
- Select uses the newly selected item. An empty selection has no subject and runs nothing, even a fallback.
- A component's `context from` or `selectedItem` does not supply a subject in v1. An unresolved or equally near subject reports the existing guarded-action warning.
- Named behaviors validate conditions at every `uses` site against that site's subject. Layout, module and feature attachments without a subject report that warning at the attachment.
- `on submit` is excluded in v1. This deliberately departs from the issue text: `item` is a form's populated item, not its submitted values; form-value conditions need a separate operand decision.
- The one-line `on row-click navigate to … by …` remains a one-liner without alternatives. Event, change and application triggers have no alternatives.

0029's click-time refresh rule does not carry over: an interaction displays no choice in advance. Commands still enforce authorization, validation and constraints. Command availability follows #335 once admitted, never replacement by another branch. A renderer without this support rejects a binding with alternatives instead of silently running nothing or every branch.

### Deprecation, repair and transport

A strict `where` repair replaces the opaque guard and plain actions with one structured `when` branch and no fallback, preserving otherwise-nothing. This is an AST operation under [0013](0013-equivalence-for-screenplay-code-round-trips.md) and [0014](0014-diagnostic-repairs-are-typed-workspace-proposals.md), not automatic printer migration. Refuse the repair when trailing comments would be lost. `CanFixAll` is false: assigning structured meaning to formerly opaque text requires individual review. Both compilers report deprecation with the same severity; repairs are C#-only, following PLAY0397's precedent. Non-parsing `where` gets Information so authors are not left with an unfixable warning.

The syntax gains ordered alternative and fallback nodes; plain `Actions` is empty when alternatives exist. AST/schema, references, rename, completeness and repair capabilities expose the nested lists. Strict older syntax readers fail closed on new members. No new MCP tool and no ESM admission or byte change are needed; interactions remain PLAY0269.

## Options considered

- **Whole lists, first match (chosen):** row gestures often navigate, open dialogs or ask confirmation rather than only execute commands.
- **Command-only alternatives:** rejected because other choices would still require opaque guards.
- **Several guarded `on` blocks:** rejected because overlapping guards could run several lists with no exclusivity or priority contract.
- **Submit with `item`:** rejected because it tests populated data, not submitted values. A future submitted-values operand is out of scope.
- **One-line alternatives as sugar:** rejected to keep one canonical block spelling; a typed repair helps authors migrate.
- **Blanket `where` deprecation:** rejected because non-item triggers have no structured replacement and Invoicing's event guard must remain warning-free.
- **Breaking removal or opportunistic structural interpretation of `where`:** rejected to preserve source and keep meaning independent of whether opaque text happens to parse.
- **Mandatory fallback:** rejected because an intentional no-action result is useful, as in 0029.

## Default if unanswered

Gestures cannot select by item state. Authors duplicate controls or use opaque guards whose ordering and meaning renderers invent.

## Timeline and scope

Applies from acceptance until superseded. This qualifies only 0029's exclusion of guarded `on` bindings; the rest of 0029 remains in force. All in-repository compiler, printer, validation, repair, reference, editor, documentation and sample surfaces land together. Out of scope: non-item structured conditions, submit alternatives, component subject inference, input-resolution changes, #335 availability admission, renderer implementation and ESM admission.

## Verification

**Done when:** both compilers diagnose the structural forms and deprecation; C# preserves alternatives through parser, printer, AST transport and workspace operations; conditions validate against each trigger and attachment subject; repairs refuse unsafe comment loss; editors and samples teach the new form; ESM bytes remain unchanged.

**Verify by:** parser, printer, syntax JSON/schema and conformance specifications; validator specifications for triggers and `uses` sites; reference/rename/completeness and repair proposal specifications; binder byte comparisons with and without alternatives. Each downstream renderer exercises first-match, absent subject, absent match, empty selection, denial without fall-through and unsupported-feature rejection separately.

## Consequences

One gesture can choose the appropriate action list without opaque guards. Renderers acquire another selection site and must reject unsupported alternatives. Opaque guards survive unchanged on non-item triggers; downstream Stage and the Cratis/AI UI-composition skill need adaptation.
