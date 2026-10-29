---
id: 0119
title: Live performers bind a whole-collection stream (ResultVersion 2)
status: accepted
stage: implemented
class: contract
reversibility: reversible
decided: 2026-10-29
decider: woksin (Q-136, delegated through L0)
amends: 0113
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Documentation/screenplay/**
---

## Context

Decision 0113 items 3 and 4 let a host render a performer body at a documented version pair, and gap any other pair. They
left the live (`observable`) performer without a contract. The binder stamps `ContextVersion 1` and `ResultVersion 1` on
every attached requirement (`SemanticModelBinder.Implementations.cs`), so a live body had no version that told a host
which contract it was written for. Stage gaps every live performer by its delivery before it reads versions
(`SemanticImplementationAdmission.cs:65-68` at Stage `3409f564`), which keeps its last demo stub alive (Stage#831,
Stage#834).

Evidence revisions: Screenplay `b198ff43a`, Stage `3409f564`; Arc v22.131.0 and Chronicle 19.99.1 as cited in the
accepted proposal on Cratis/Screenplay#1077.

## Decision

1. **Version pair.** A performer for an observable query whose cardinality is `Many` carries `ContextVersion 1` and
   `ResultVersion 2`. Every other performer keeps `(1, 1)`. `RequirementId`, `ContentHash` and the ESM bytes do not
   change. A live performer at `ResultVersion 1` (One or ZeroOrOne) has no live contract and stays a host gap under 0113
   item 4.
2. **Body.** Inline and `file` bodies are a method body, as in the snapshot form. In scope are `readModels`, of type
   `LiveReadModels<T>` for the declared read model `T`; `context`, the same performer context as the snapshot form; and the
   caller arguments by their camelCase names. The body returns `IObservable<IEnumerable<T>>`. Each emission is the complete
   answer, never a delta. `System.Linq` and `System.Reactive.Linq` are in scope. The method signature is the host's to
   generate; Screenplay does not prescribe one.
3. **Live source.** `LiveReadModels<T>` implements `IObservable<IEnumerable<T>>` and adds no members. On subscribe it emits
   the current collection, then the whole collection after each change, with no deduplication guarantee. The host declares
   the type next to the query, as it already does for its context record. No package declares it.
4. **Reads.** The body reads only the declared read model, through `readModels`, with no other read models and no
   services. A host that cannot observe the read model renders a gap (for example a read model with no projection or
   reducer). The binder does not refuse it.
5. **Lifetime.** The host subscribes once per client subscription and disposes it on unsubscribe, disconnect or shutdown.
   Disposal is the only cancellation: `context` has no cancellation member and `ContextVersion` stays 1. The host adds no
   retry, deduplication, throttling or clock refresh.
6. **Errors.** An exception while building the stream fails the query, as for a snapshot. `OnError` and `OnCompleted` end
   that subscription. The host does not resubscribe.
7. **Context capture.** `context` is captured once per subscription, so `ReceivedAt` does not advance. Bodies must not read
   ambient tenant or user state. There is no paging.
8. **ZeroOrOne and One are deferred.** A later decision gives them their own result version.

## Options considered

- `IAsyncEnumerable<T>` result: rejected, the host stream is `IObservable` end to end.
- `Current()` plus `Changes`: rejected, two members for what one subscription expresses.
- A plain `IObservable` named `readModels`: rejected, it would not state that each emission is the whole collection.
- A `uses` list of read models: rejected, one declared read model is all Stage#834 needs.
- DI services in the body: rejected, bodies stay pure over their inputs.
- A package-declared `LiveReadModels<T>`: rejected, it adds a runtime dependency and a release for no gain.
- `context.Cancellation` with `ContextVersion 2`: rejected, disposal already ends the stream.
- Binder refusal without a projection: rejected, a performer's read model may have none; observability is a host fact.
- `ResultVersion 2` on every live performer: rejected, a version number must never name an undefined shape.

## Consequences

- Screenplay: the binder stamp, specs and documentation. No ESM, corpus, MCP or Tool change; the MCP
  `implementation-requirements` view already reports the versions.
- Stage#834: render live bodies at `(1, 2)`; generate `LiveReadModels<T>` and the `ISubject<IEnumerable<T>>` wrapper; gap
  any other version with a warning naming both versions. Stage also copies the requirement's version onto the gap instead
  of the hard-coded `1, 1` (`SemanticQueryContextRuntime.cs:83`, `ArtifactImplementationGap.cs:58`), per 0113 item 3.
- Arc, Chronicle and the CLI: none.

## Related

Amends 0113. Cratis/Screenplay#1077; Stage#831, Stage#834.
