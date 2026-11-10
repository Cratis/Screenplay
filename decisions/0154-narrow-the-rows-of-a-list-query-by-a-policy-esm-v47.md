---
id: 0154
title: A collection query narrows its rows by a policy in ESM v47
status: accepted
stage: designed
class: contract
reversibility: costly
decided: 2026-11-09
decider: Einar Ingebrigtsen (direct request to deliver Cratis/StudioIssues#1387)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/policies.md
---
## Context
`authorize` on a list query admits or refuses the whole call. A plan-gated fleet list needs the policy to narrow the rows instead ([Screenplay#1234](https://github.com/Cratis/Screenplay/issues/1234), epic [StudioIssues#1387](https://github.com/Cratis/StudioIssues/issues/1387)).
## Decision
- A collection query may declare `narrow <Policy>`. The policy is evaluated once per row of the read model, with the row's own properties as the artifact, so `reads VesselPlan as plan by vesselId` takes its key from the row's `vesselId`. This is a policy evaluated per row, not a filter that is supplied; the policy already says what it needs to read.
- A denied row is dropped, never refused: the caller sees only the rows the recorded state allows. A row whose state is absent is dropped (absence denies, decision 0151). `authorize` still refuses the whole call and composes with `narrow`.
- The reference evaluator applies the narrowing before the query result is compared, so `then query X` with `result` rows states exactly the surviving rows, and a query with no `result` states that none survive.
- Only a collection result can be narrowed. A policy with an opaque predicate cannot narrow (it needs a target provider), and a read whose key is a policy input, not a property of the row, is refused at bind.
- A query declares at most one `narrow`; conditions are combined in the policy.
- Only a model that declares `narrow` selects ESM v47; `narrowing` is omitted from the bytes when absent, so every earlier model keeps its bytes and revision.
## Consequences
- A runtime narrows before paging, sorting and counting, so a page and its total count describe the allowed rows only ([Arc#3765](https://github.com/Cratis/Arc/issues/3765)).
- Stage renders the narrowing as a policy-narrowed query ([Stage#1049](https://github.com/Cratis/Stage/issues/1049)).
