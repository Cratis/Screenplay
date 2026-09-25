---
id: 0015
title: Event generations in the executable model
status: accepted
stage: implemented
decided: 2026-09-25
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/SemanticBehaviors.cs
  - Source/DotNET/Screenplay/Semantics/SemanticAddress.cs
  - Source/DotNET/Screenplay/Semantics/ExecutableSemanticModel.cs
  - Source/DotNET/Screenplay/Semantics/SemanticIdentityCatalog.cs
  - Source/DotNET/Screenplay/Semantics/SemanticCompilation.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder*.cs
  - Source/DotNET/Screenplay/Semantics/Versions.cs
  - Source/DotNET/Screenplay/Semantics/Serialization/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Documentation/screenplay/events.md
  - Documentation/screenplay/interoperability.md
---

## Context

[Decision 0011](0011-event-generations.md) point 4 maps a generation to the event contract revision, with each revision recording its predecessor. It does not say how revisions sit in the executable semantic model (ESM), which identities their properties get, or what a reference to an evolved event means; an unmerged implementation of 0011 stops at ESM binding on exactly these questions. This record implements 0011 point 4. It refines 0011 and does not supersede it.

The ESM has one shape per event today:

- `SemanticEventContract` carries one `ContractId`, one `Revision` and one property list ([`SemanticBehaviors.cs:223-237`](../Source/DotNET/Screenplay/Semantics/SemanticBehaviors.cs)). Behaviors reference the declaration's `SemanticId`. The model rejects a non-initial revision and a duplicated contract id ([`ExecutableSemanticModel.cs:319-330`](../Source/DotNET/Screenplay/Semantics/ExecutableSemanticModel.cs)), and so does the catalog ([`SemanticIdentityCatalog.cs:594-598`](../Source/DotNET/Screenplay/Semantics/SemanticIdentityCatalog.cs)).
- A property address is owner plus member name, with no generation ([`SemanticAddress.cs:349-357`](../Source/DotNET/Screenplay/Semantics/SemanticAddress.cs)), and binding derives the property identity from it ([`SemanticModelBinder.Identity.cs:13-18`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Identity.cs)). Two generations that both declare `name` would get one address.
- The catalog persists each event's revision ([`SemanticIdentityCatalog.cs:53-57`](../Source/DotNET/Screenplay/Semantics/SemanticIdentityCatalog.cs)). Migration planning keeps an unchanged assignment with its old revision (lines 387-413), and compilation fails when catalog and ESM revisions disagree ([`SemanticCompilation.cs:67-74`](../Source/DotNET/Screenplay/Semantics/SemanticCompilation.cs)).
- The canonical writer still emits the transition cardinalities that [decision 0007](0007-affected-read-model-instances.md) point 3 removes in the next ESM version ([`SemanticModelCanonicalJson.cs:315-326,710-716`](../Source/DotNET/Screenplay/Semantics/Serialization/SemanticModelCanonicalJson.cs)).

Under [decision 0001](0001-chronicle-runtime-semantic-authority.md), Chronicle defines the meaning:

- One event type definition holds all its generations, each with its own schema ([`EventTypeDefinition.cs:14-19`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Concepts/Events/EventTypeDefinition.cs), [`EventTypeGenerationDefinition.cs:13`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Concepts/Events/EventTypeGenerationDefinition.cs)). MongoDB stores it as one document with a schema per generation ([`EventType.cs:19-25`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Storage.MongoDB/EventTypes/EventType.cs)).
- Chronicle consumes more than the current generation. A prior-generation type resolves to the current type's id with its own generation ([`EventTypeExtensions.cs:109-124`](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Events/EventTypeExtensions.cs)), a migration declares an upcast and a downcast ([`EventTypeMigration.cs:58-64`](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Events/Migrations/EventTypeMigration.cs)), the kernel materializes content for every generation ([`EventTypeMigrations.cs:25-102`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/EventSequences/Migrations/EventTypeMigrations.cs)), and a reactor receives the generation its handler declares ([`Reactors.cs:465-479`](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Reactors/Reactors.cs)).
- Registering a later generation requires a migration chain from generation 1 ([`EventTypeRegistrar.cs:63-69,197-215`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/EventTypes/EventTypeRegistrar.cs)), and an incompatible change to a registered generation's schema is refused (lines 368-402).

[#168](https://github.com/Cratis/Screenplay/issues/168)'s v2 vector is still partial. It maps `projectId` into the payload, declares it on the event, asserts it on the expected event and keys the projection on it ([`RegisterProject.play:13,16,24,47`](../Source/DotNET/Screenplay.CanonicalCorpus/Corpus/RegisterProject/v2/source/RegisterProject.play)).

## Decision

Generations form one aggregate per event contract, as in Chronicle. Ordinary references see only the current generation in this increment.

1. **One aggregate.** An event contract is one ESM entry per contract id. Its current revision is the highest declared generation. It keeps today's location and shape, and every ordinary reference resolves to it. Prior revisions are listed beneath it in numeric order `1..N−1`, each with its revision, its predecessor and its complete property list. The current revision also records its predecessor; generation 1 has none. A historical revision is addressed internally by `(ContractId, Revision)`.
2. **Revision-scoped property identity.** A property identity belongs to a property in one revision. An event with one generation keeps today's addresses. Once an event declares more than one generation, the property addresses of every revision are generation-qualified. Generation-1 identities move to their generation-1 addresses through explicit catalog continuity, as persisted assignments, the way a rename carries an identity today ([`SemanticIdentityCatalog.cs:375-380`](../Source/DotNET/Screenplay/Semantics/SemanticIdentityCatalog.cs)). They survive when generation 1 becomes historical. Each later generation gets its own identities, even for a same-named property, and keeps them when a newer generation arrives. No correspondence between same-named properties is claimed until the transformation construct is designed. Command, read-model, query and specification identities do not change.
3. **References.** A bare `X` means the current generation. A reference that needs an older shape fails with an unsupported diagnostic naming the event and revision, because Screenplay has not yet admitted revision-qualified consumption or historical execution; Chronicle itself supports both. A current-generation mapping that names a property the current generation no longer has fails binding. Nothing falls back to a historical shape. Specifications that use only current facts still execute.
4. **Deployment.** A generation-2 model without transformations is not automatically deployable to Chronicle, because the registrar requires migration coverage. A target that cannot realize the evolution rejects it, following the fail-closed gate of [decision 0004](0004-admission-and-governance-of-portable-executable-semantics.md). Historical replay never passes through a guessed conversion.
5. **Catalog advancement.** The catalog advances a persisted event-contract revision explicitly and only forward. Source that declares generations `1..N` against a persisted revision below N produces an advancement that keeps the contract id and is checked against the expected catalog revision, like any other catalog change. Source that declares fewer generations than persisted fails. A persisted revision-1 assignment is never silently kept when source declares revision 2. A fresh bootstrap assigns the contract id as today, records revision N, and assigns property identities for every revision at their generation-qualified addresses.
6. **Immutability stays open.** A complete property list does not prove a historical schema unchanged, because properties refer to concepts and composite types ([`SemanticModelBinder.Identity.cs:20-31`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Identity.cs)) that can change underneath it. This record does not meet [#71](https://github.com/Cratis/Screenplay/issues/71)'s immutability or replay criteria. Protecting a revision's resolved schema against an admitted baseline stays open under #71.
7. **ESM v4.** A model in which any event declares more than one generation selects ESM v4 (language and semantic version 4.0, `schemaVersion` 4), under decision 0004. A v4 model may also use every v2 and v3 construct. A v4 model without such an event is invalid, as v2 and v3 require their constructs ([`ExecutableSemanticModel.cs:106-122`](../Source/DotNET/Screenplay/Semantics/ExecutableSemanticModel.cs)). A lone `generation 1` marker, with no other generation of that event, binds exactly as an unmarked declaration and does not select v4. Revision history is part of the canonical bytes and therefore of `SemanticRevision` (lines 83-84), which the strict reader checks along with the exact bytes ([`SemanticModelSerializer.Reader.cs:59-69`](../Source/DotNET/Screenplay/Semantics/Serialization/SemanticModelSerializer.Reader.cs)). v1–v3 bytes do not change. Tags stay with the revision that declares them: a historical revision keeps its tags in its record, and appends use the current revision's tags.
8. **Transition cardinality in v4.** v4 implements decision 0007 point 3: its canonical bytes never carry `zeroOrOne` or `many` on a transition. A v4 model with a `ZeroOrOne` or `Many` transition is rejected on creation and on reading; it is never read as `One`. v1–v3 bytes and the deprecation warning ([`ExecutableSemanticModel.cs:56-61`](../Source/DotNET/Screenplay/Semantics/ExecutableSemanticModel.cs)) stay as they are, and query cardinality is unchanged.
9. **#168's v2 corpus.** It follows [#167](https://github.com/Cratis/Screenplay/issues/167) and makes no new choice. `ProjectRegistered` generation 1 keeps `projectId` and `name`; generation 2 has only `name`. The command still `produces ProjectRegistered for projectId`, without `projectId = projectId`. The projection keys by event source: `from ProjectRegistered` without a key binds to the event-source identity ([`SemanticModelBinder.ProjectionValues.cs:17-22`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.ProjectionValues.cs)) and sets the read-model identifier ([`a_from_transition_keyed_on_the_event_source.cs:17`](../Source/DotNET/Screenplay/Semantics/Execution/for_SemanticEvaluator/when_projecting_scoped_projections/a_from_transition_keyed_on_the_event_source.cs)). The expected event keeps its `for` and drops the payload `projectId`; the read model, query and their assertions keep `projectId`. Corpus "v2" names the scenario; ESM v4 is the schema. The corrected vector's bytes and revision are new and reviewed, not byte-compatible with today's `esm-v2.json`.

## Options considered

- **One aggregate per contract, current at today's location (taken).** It mirrors Chronicle's event-type aggregate and keeps every existing reference pointing at one entry.
- **Separate revision entries sharing a contract id.** Not taken: it forces revision-qualified references and wider identity and index changes before historical consumption has a designed meaning.
- **Reuse a property identity across revisions by name.** Not taken: it claims a correspondence nobody declared and puts one identity on two properties. Explicit correspondence belongs to the transformation design.
- **Fall back to a historical shape when the current one lacks a property.** Not taken: it silently reinterprets stored facts, a #71 non-goal.
- **Let a lone `generation 1` marker select v4.** Not taken: it changes the bytes and revision of a model whose meaning did not change.
- **Remove transition cardinality in a later version.** Not taken: decision 0007 ties the removal to the next ESM version, and a separate bump would cost consumers another admission.

## Default if unanswered

The marker parses but fails at ESM binding, so no document can say an event evolved. #168's v2 vector cannot advance its revision. The first implementation picks a layout ad hoc and may move generation-1 property identities onto generation 2, which is costly to undo once catalogs persist it.

## Timeline and scope

Settle before ESM generation binding merges, and keep it until superseded.

In scope: the aggregate shape, revision-scoped property identity and generation-qualified addresses, the reference rule, catalog advancement and fresh bootstrap, ESM v4 activation and bytes, decision 0007's removal in v4, and the #168 v2 correction.

Out of scope: the transformation construct (#71); revision-qualified references and historical execution; resolved-schema immutability against an admitted baseline (#71); rendering migrations for Chronicle; consumer admission of v4 (decision 0004 point 4); changes to Chronicle.

## Verification

**Done when:** A two-generation event binds to one ESM entry with the current revision at today's location, and revision 1 beneath it; revision 2 names revision 1 as predecessor and revision 1 names none. Generation-1 property identities are unchanged after advancing 1→2, and generation-2 identities are unchanged after 2→3. A current mapping that names a removed property fails binding, and a reference to an older shape fails with the unsupported diagnostic. A multi-generation model selects v4, a lone `generation 1` model does not, and a v4 model with a `ZeroOrOne` or `Many` transition is rejected. v1–v3 golden vectors are byte-identical. #168's v2 vector pins generation 2 without `projectId`.

**Verify by:** Identity-catalog specs for advancement, refusal of a lower declared revision, and fresh bootstrap; binder specs for identity continuity across 1→2→3, removed-property mapping failure and older-shape reference failure; version-selection specs for v4 activation and the lone marker; ESM specs that reject v4 transition cardinalities on creation and reading; a `full-esm-v4.json` golden vector with a two-generation event; the corrected `RegisterProject/v2` corpus vector with its own catalog in each source form. Check the `Decision: 0015`, `Decision: 0011` and `Decision: 0007` trailers.

## Consequences

Evolution reaches the ESM without changing existing references or v1–v3 bytes, in the same aggregate shape Chronicle uses. Property identities never move between revisions, at the cost of generation-qualified addresses and a catalog advancement path. Models that declare generations cannot run historical facts or deploy to Chronicle until transformations exist. Evolution changes the contract, so round-trip equivalence under [decision 0013](0013-equivalence-for-screenplay-code-round-trips.md) is tested within each version, not between them.

## Related issues

Screenplay: [#71](https://github.com/Cratis/Screenplay/issues/71), [#167](https://github.com/Cratis/Screenplay/issues/167), [#168](https://github.com/Cratis/Screenplay/issues/168), [#132](https://github.com/Cratis/Screenplay/issues/132), [#128](https://github.com/Cratis/Screenplay/issues/128). Decisions: [0001](0001-chronicle-runtime-semantic-authority.md), [0004](0004-admission-and-governance-of-portable-executable-semantics.md), [0007](0007-affected-read-model-instances.md), [0011](0011-event-generations.md), [0013](0013-equivalence-for-screenplay-code-round-trips.md).

## Status notes

**2026-09-25 — accepted.** Sindre Alstad Wilting delegated this choice to the orchestrating agent. The option was chosen under that delegation after an independent review of the Screenplay (v4.33.0) and Chronicle source.

**2026-09-25 — implemented, not verified. Release version: TBD.** ESM v4 carries current and prior event revisions, revision-qualified property identities and explicit catalog advancement. The v4 golden and corrected RegisterProject/v2 corpus pin new bytes and revisions; v1–v3 bytes are unchanged. Historical replay, schema immutability against an admitted baseline and Chronicle migration rendering remain #71 work. Verification across consumers and realizations is pending.

**2026-09-25 — review fixes.** A source-built mixed v4 golden pins ordinary events alongside a three-generation event with prior-revision tags. Fresh workspaces record declared revisions; persisted revisions advance only through a revision-bound catalog plan or explicit workspace transaction advancement. Strict-reader, binder, corpus-derivation and MCP export specs cover these contracts. The v4 serializer golden revision is `rev1:8e7c31552c528d080a8617d1fe44b6a1dff4a636d98a00709baa93ba985283b2`; the RegisterProject/v2 corpus revision remains `rev1:c2bef8cf2ba598b564f67d9dc96a5555f4246ac33c6937cb83409b71acc2b3dd`. Consumer admission and live Chronicle conformance remain unverified.
