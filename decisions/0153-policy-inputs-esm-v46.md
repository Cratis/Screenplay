---
id: 0153
title: Policies declare typed inputs that an authorizing command or query maps in ESM v46
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
A policy read takes its key from a property of the authorized command or query by name, so a policy is coupled to the spelling of every artifact that uses it ([Screenplay#1231](https://github.com/Cratis/Screenplay/issues/1231), epic [StudioIssues#1387](https://github.com/Cratis/StudioIssues/issues/1387)).
## Decision
- A policy may declare typed inputs, one per line, `asset AssetId`. A `reads ... by <input>` takes its key from an input.
- `authorize <Policy>` may carry a mapping block, one `input = property` line each, where `property` is an input of the authorized command or query. A key that names no declared input still resolves against the artifact property of that name, so decision 0151 models keep working unchanged.
- A mapping block belongs to a single policy reference; it is refused on a logical `authorize`.
- A declared default source (`asset AssetId from $context.asset`) parses, prints and round-trips, and is refused when bound until a trusted context carries it ([Screenplay#1231](https://github.com/Cratis/Screenplay/issues/1231) records the open point).
- Only a model that declares inputs or a mapping selects ESM v46; `inputs` and `mappings` are omitted from the bytes when absent, so every earlier model keeps its bytes and revision.
## Consequences
- Stage renders a mapped input as the lookup key of the authorization policy ([Stage#1048](https://github.com/Cratis/Stage/issues/1048)).
