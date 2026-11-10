---
id: 0158
title: Screen element availability is derived from command and query authorization and reported, not enforced
status: accepted
stage: designed
class: contract
reversibility: cheap
decided: 2026-11-10
decider: Einar Ingebrigtsen (direct request to deliver Cratis/StudioIssues#1387)
applies-to:
  - Source/DotNET/Screenplay/Semantics/Execution/SemanticScreenAvailability.cs
  - Documentation/screenplay/policies.md
---

## Context

A screen declares data through a query and actions through commands, and authorization lives on those commands and queries. Nothing said which elements of a screen a given caller is offered. With policies that decide from recorded state (decisions 0151 to 0154, 0157) the answer also depends on the data the screen is showing, such as the plan of one asset (Cratis/Screenplay#335).

## Decision

Availability is derived, never authored. An action is available to a caller exactly when the caller satisfies the authorization of its command, and data exactly when the caller satisfies the authorization of its query. The author writes nothing extra, and the single source of truth stays on the command or query.

`SemanticScreenAvailability.Evaluate` reports, for every element of every screen (or one screen), `Available`, `Unavailable` or `Undecided`, with the policies that gate the element and the reason. It takes the caller, the values of the data the screen is showing by property name, and the recorded read-model instances, and uses the same evaluator as command execution, so a state-reading policy follows the same fail-closed rule: a missing instance denies. An opaque predicate, or an artifact outside the executable model, is `Undecided`, never `Available`.

The report is a presentation of an authorization fact. It never replaces enforcement: the command or query still decides when it runs. It adds no ESM member and changes no canonical bytes. Explicit narrowing (`visible to`), persona reachability diagnostics and persona-scoped screen specifications are not part of this decision.

## Consequences

Studio and the generated applications can show or hide an element for a chosen caller and state from one evaluator. Admitting availability into the executable model would reopen #254 and needs its own decision.
