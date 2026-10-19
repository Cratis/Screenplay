---
id: 0100
title: Move a single declaration between slices, carrying the specifications that cover only it
status: accepted
stage: none
class: product
reversibility: costly
decided: 2026-10-18
decider: woksin (ruling on #582); coverage, obligation and refusal details delegated to the implementing agent's recommendation
applies-to:
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay.Mcp/**
  - Documentation/screenplay/mcp/**
---


## Context

[Decision 0038](0038-move-logical-subtrees-with-identity-continuity.md) deferred moving declarations between slices
because event contracts and productions are slice-owned. [#582](https://github.com/Cratis/Screenplay/issues/582)
ruled: a moved declaration carries its own specifications; references from screen actions are repaired, not moved;
the move is refused when a specification also covers a declaration that stays behind, or when the source slice's
specification obligations ([#390](https://github.com/Cratis/Screenplay/issues/390)) would break. It is MCP tooling
only; a change to the executable semantic model would defer it.

Name resolution of `produces <Event>` and projection `from <Event>` is not slice-scoped: a command may produce, and a
projection may read, an event declared in another slice (StateView projections do this in every sample). Moving a
command therefore never requires its produced events to travel, and they do not travel implicitly.

## Decision

`propose-move` also accepts a command, event, read model, projection, query, reaction or capture as `target` with a
slice as `newParent`. Specifications on their own, screens, constraints, modules, inline `produces event`
declarations and a declaration written in more than one fragment refuse.

A specification covers a declaration when it is a matching specification of one of that declaration's obligations as
`find-specification-obligations` reports them, or when its `when` step names that declaration (command, query,
appended event, redelivered reaction, trigger, capture). Only declarations of the source slice count, and coverage
comes only from obligations and the `when` step: a `given` or `then` step that meets no obligation is a reference,
repaired like any other. (A `then` read-model or result step does meet SPEC006.) A specification that covers only
the moved declaration moves with it. One that covers it and a staying declaration refuses the move, naming both. Any
other specification stays.

Obligations are evaluated for the original and the candidate workspace and keyed by rule, owner kind, owner address
(rewritten through the move's identity migrations) and subject. The move refuses when an obligation met before is not
met after, or when the candidate introduces an unmet obligation. The MCP handler owns this policy because obligations
are an MCP review surface: it computes the carried specifications, passes them to the workspace planner as
`CarriedSpecifications`, and checks obligations on the planner's candidate.

The planner reuses 0038: semantic and event-contract migrations for every assigned address in the moved subtrees
(including inline events of a moved command), unchanged event names and pins, no retired identity, destination
collisions by address, qualified reference repair (screen `execute`, `refresh` and `navigate to` included) and
inherited `authorize`/interaction continuity. Unqualified references are not rewritten: a move that would change what
one resolves to refuses and asks for that reference to be qualified first. The executable model comparison lifts
every slice declaration array out by identity, empty or not, so the moved declarations may change slice and nothing
else may change; reducers and constraints stay with their slice, and a move that changes them refuses by name. The
obligation comparison rewrites owners through every address the planner moves, not only catalog-assigned identities.
The report adds `specificationsCarried`.

## Options considered

- **Coverage by #390 matching plus the `when` subject (chosen).** One definition shared with the obligations tool;
  `when Register then ProjectRegistered` moves with `Register` while the event stays.
- **Coverage by every reference.** Rejected: nearly every command specification names a staying event, so commands
  could never move.
- **Make produced events travel with their command.** Rejected: production resolves across slices, and an implicit
  move would change event-contract ownership the author did not ask for.
- **Obligation check in the workspace planner.** Rejected: obligations live in the MCP review layer; moving them is a
  separate refactoring.
