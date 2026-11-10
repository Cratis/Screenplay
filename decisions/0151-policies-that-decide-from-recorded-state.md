---
id: 0151
title: Policies decide from recorded state with reads and the shared condition operand
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
A policy decides from the caller (role, claim) and from the arguments of the command or query. It cannot decide from state the application has recorded, for example whether the asset's plan includes a feature, or whether the caller is the recorded author ([Screenplay#1230](https://github.com/Cratis/Screenplay/issues/1230), epic [StudioIssues#1387](https://github.com/Cratis/StudioIssues/issues/1387), [Screenplay#1207](https://github.com/Cratis/Screenplay/issues/1207)).
## Decision
- A `policy` accepts `reads <Model> [as <alias>] [by <property>]` lines with the command grammar. `by` resolves against the artifact being authorized.
- `require` accepts the shared condition operand (`<path> <operator> <value>`) beside `authenticated`, `role` and `claim`. Paths start at a read alias, an artifact property or `$identity`. It is a new `StateConditionSyntax` that wraps the shared `ComparisonConditionSyntax`.
- A read that finds nothing denies (fail closed), as keyed read-model absence does in [0020](0020-keyed-read-model-absence-in-esm-v5.md) where it applies.
- The admission read is advisory: it does not enrol in decision reads and nothing revokes access already granted. This matches `Provide()` and Arc's `StateAwareAuthorizationPolicy` ([Arc#3764](https://github.com/Cratis/Arc/issues/3764)). A rule that must hold under concurrent writers belongs in the command's protected reads.
- The runtime meaning stays with Arc and Chronicle ([0001](0001-chronicle-runtime-semantic-authority.md)); screens keep following "the guard controls what is offered, not what the system accepts".
- A policy without `reads` serializes with the same bytes as before: an empty `reads` is omitted from the wire form, in both compilers.
## Consequences
- Both compilers, the printer, the walkers, the syntax JSON kinds and transport schema change together; the executable semantic model refuses a state policy until [Screenplay#1233](https://github.com/Cratis/Screenplay/issues/1233) admits it, so no ESM version changes here.
- Persona caller synthesis refuses a state condition as a non-literal claim, since a fixture cannot be derived from recorded state.
