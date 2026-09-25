---
id: 0007
title: Which read-model instances an event affects follows Chronicle's keys and joins
status: accepted
stage: none
decided: 2026-09-25
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/SemanticBehaviors.cs
  - Source/DotNET/Screenplay/Semantics/ExecutableSemanticModel.cs
  - Source/DotNET/Screenplay/Semantics/SemanticProjectionScopes.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Projections.cs
  - Source/DotNET/Screenplay/Semantics/Execution/SemanticExecutionPlan.cs
  - Source/DotNET/Screenplay/Semantics/Execution/SemanticScopedProjection.cs
  - Source/DotNET/Screenplay/Semantics/Serialization/**
---

## Context

[#132](https://github.com/Cratis/Screenplay/issues/132) asks which read-model instance, or instances, an event affects, and suggests a key-list form such as `on CustomersShared keys customerIds`. The executable semantic model (ESM) already carries `SemanticAffectedInstance` with a cardinality of `One`, `ZeroOrOne` or `Many` ([`SemanticBehaviors.cs:328`](../Source/DotNET/Screenplay/Semantics/SemanticBehaviors.cs)). The validator accepts all three, reading `Many` as a collection-typed key and `ZeroOrOne` as an optional key ([`ExecutableSemanticModel.cs:640-645`](../Source/DotNET/Screenplay/Semantics/ExecutableSemanticModel.cs)); the binder only ever produces `One` ([`SemanticModelBinder.Projections.cs:107`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Projections.cs)); the evaluator rejects anything else ([`SemanticExecutionPlan.cs:169-172`](../Source/DotNET/Screenplay/Semantics/Execution/SemanticExecutionPlan.cs)). Both values are pinned in the golden vectors.

Under [decision 0001](0001-chronicle-runtime-semantic-authority.md), Chronicle defines the meaning:

- A key resolver returns a resolved key, a deferred key (retried once the parent exists) or an unresolvable key. There is no list-of-keys outcome ([`KeyResolverResult.cs:12-35`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/Projections/Engine/KeyResolverResult.cs)).
- "Many" exists only structurally. A root join updates every existing document whose join property equals the value, with `UpdateMany` and no upsert ([`ChangesetConverter.cs:354-401`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Storage.MongoDB/Sinks/ChangesetConverter.cs), [`Sink.cs:164-172`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Storage.MongoDB/Sinks/Sink.cs)). A child join matches children across parents. Remove-via-join pulls the child from every document ([`Sink.cs:662-674`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Storage.MongoDB/Sinks/Sink.cs)).

## Decision

Affected instances mirror Chronicle. No new syntax is added.

1. **`from` affects one instance.** A `from` transition affects exactly one instance, possibly deferred until its parent exists. A key never resolves to a list, so a key-list syntax is rejected.
2. **"Many" is structural.** It is expressed only by the scoped shapes the ESM already binds (`SemanticProjectionJoin`, `SemanticProjectionJoinRemoval` in [`SemanticProjectionScopes.cs`](../Source/DotNET/Screenplay/Semantics/SemanticProjectionScopes.cs)):
   - a root join updates every existing instance whose join property equals the event source id, and never creates one;
   - a child join updates matching children across parents;
   - remove-via-join pulls the child from every document.
3. **Transition cardinality.** `ZeroOrOne` and `Many` on a projection transition have no Chronicle meaning. They get a deprecation diagnostic now, and are removed from canonical bytes in the next ESM version, following [decision 0004](0004-admission-and-governance-of-portable-executable-semantics.md). Query cardinality (`SemanticQueryCardinality`) is unaffected.
4. **Derived view for Studio.** Screenplay derives a read-only affected-instance view per (event, block) from the projection scope: one by key (`from`, removal); one per event source (`all`); many where a property equals the event source id (root join); many where the child identity equals the key, across parents (child join, remove-via-join). It is derived, not authored, and adds nothing to canonical bytes.
5. **Reducers.** Reducers are keyed by the event source only, as [decision 0002](0002-implementation-attachments-envelope-and-reducer-role.md) settled.

## Known differences to resolve

- **Chronicle, root join key.** A root join resolves its key as the event source id and ignores the join's key expression ([`ProjectionFactory.cs:969-981`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/Projections/Engine/ProjectionFactory.cs)), although the .NET client's variant support sets one. Tracked in [Cratis/Chronicle#4165](https://github.com/Cratis/Chronicle/issues/4165).
- **Chronicle, in-memory sink.** It applies a root join to the one document it resolved ([`InMemorySink.cs:430-432`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Storage.InMemory/Sinks/InMemorySink.cs)) instead of fanning out as the MongoDB and SQL sinks do. Tracked in Chronicle#4165.
- **Reference evaluator.** It matches Chronicle's join fan-out ([`SemanticScopedProjection.cs:144-163`](../Source/DotNET/Screenplay/Semantics/Execution/SemanticScopedProjection.cs)) except that it honors the key of a variant join ([`SemanticModelBinder.Variants.cs:108-110`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Variants.cs)) where Chronicle uses the event source id. The affected-instance view reports a root `remove via join` as `Unverified`: Chronicle wires a child pull with an empty collection path, not a root deletion. Without an application schema, the projection-only view reports an unset child identity as `Unverified`; the application-aware overload resolves the fallback.

Neither Chronicle difference was reproduced at runtime; both come from reading the code.

## Options considered

- **Mirror Chronicle; many is structural (taken).** Every construct has a runtime counterpart, and the ESM stops accepting states the evaluator and Chronicle cannot run.
- **Key-list syntax (`keys customerIds`).** Not taken: Chronicle has no construct that resolves one event to a list of keys, so it would be invented semantics, which decision 0001 rules out.
- **Keep `ZeroOrOne`/`Many` as reachable transition cardinalities.** Not taken: the validator accepts shapes nothing can execute. `ZeroOrOne` only corresponds to deferral, which is operational and not something a modeler writes.
- **Remove them from canonical bytes immediately.** Not taken: it changes existing bytes, which decision 0004 allows only in a new ESM version.

## Default if unanswered

The validator keeps accepting cardinalities the evaluator rejects, #132 keeps inviting a key-list syntax with no runtime, and Studio has no data to show which instances an event touches.

## Timeline and scope

Settle before any #132 syntax or Studio visualization work, and keep it until superseded. The diagnostic and the derived view ship first; byte removal ships with the next ESM version.

In scope: the deprecation diagnostic, removal in the next ESM version, the derived affected-instance view, the reducer keying statement, and specs for the known differences.

Out of scope: new projection syntax; Chronicle runtime changes (owned by Chronicle#4165); Studio's rendering of the view; reducers keyed other than by event source.

## Verification

**Done when:** A model whose transition carries `ZeroOrOne` or `Many` gets a deprecation diagnostic, and the next ESM version's golden vectors no longer contain them on transitions. The derived view returns the four shapes above for a model with `from`, `all`, a root join, a child join and a remove-via-join. A spec shows one child-join event updating children under two parents.

**Verify by:** `ExecutableSemanticModel.DeprecationDiagnostics` specs for the model-level diagnostic (the binder cannot author legacy cardinalities); specs for the derived view per shape; the child-join spec against `SemanticScopedProjection`; golden-vector diff on the version bump, with a `Decision: 0004` and `Decision: 0007` trailer.

## Consequences

The ESM says only what Chronicle can do, and "many" is visible through the derived view without new syntax. Authors who expect a key list must model a join. The byte removal costs a version bump. The variant join difference remains open until Chronicle#4165 is resolved.

## Related issues

Screenplay: [#132](https://github.com/Cratis/Screenplay/issues/132), [#128](https://github.com/Cratis/Screenplay/issues/128). Chronicle: [#4165](https://github.com/Cratis/Chronicle/issues/4165).

## Status notes

**2026-09-25 — partially implemented.** Release version: TBD. The model-level deprecation warning and read-only projection affected-instance view ship first. The source binder cannot express `ZeroOrOne` or `Many` transition cardinality, so source compilation has no such warning; ESM created or read from canonical bytes reports it. Canonical bytes and golden vectors remain unchanged. `stage: none` remains until the next ESM version removes legacy cardinality bytes and the *Done when* criteria are satisfied. Chronicle's variant join-key discrepancy remains tracked in Chronicle#4165; reference child-event deferral is modeled with a per-projection pending queue reconstructed from given history before the when phase. A root remove-via-join remains unverified as a root deletion.
