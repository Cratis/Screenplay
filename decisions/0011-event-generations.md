---
id: 0011
title: Event generations are declared in full, each succeeding the one before
status: accepted
stage: implemented
decided: 2026-09-24
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Syntax/EventSyntax.cs
  - Source/DotNET/Screenplay/Parsing/EventParser.cs
  - Source/DotNET/Screenplay/Parsing/ScreenplayValidator.cs
  - Source/DotNET/Screenplay/Semantics/ExecutableSemanticModel.cs
  - Source/DotNET/Screenplay/Semantics/SemanticIdentityCatalog.cs
  - Source/DotNET/Screenplay/Semantics/Serialization/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Documentation/screenplay/events.md
---

> **2026-09-25 — refined.** [Decision 0015](0015-event-generations-in-the-executable-model.md) settles how point 4 is represented in the executable model: one aggregate per event contract with prior revisions beneath the current one, revision-scoped property identities, current-generation references, explicit catalog advancement, and ESM v4. The initial-revision guard cited below was replaced by lineage validation in decision 0015.

## Context

[#71](https://github.com/Cratis/Screenplay/issues/71) asks for portable event evolution: immutable generations, predecessor lineage and deterministic transformation. Identity already survives renames and moves, but nothing links a revision to its predecessor. The executable semantic model (ESM) rejects any event contract revision other than the initial one ([`ExecutableSemanticModel.cs:297-300`](../Source/DotNET/Screenplay/Semantics/ExecutableSemanticModel.cs), [`SemanticIdentityCatalog.cs:594-598`](../Source/DotNET/Screenplay/Semantics/SemanticIdentityCatalog.cs)).

The open question in #71 is how a document names the prior shape, since today it declares only the current one. The sweep comment offered three answers: declare prior generations in full, declare only the properties a migration touches, or leave the source untyped.

[#168](https://github.com/Cratis/Screenplay/issues/168) waits on this. Its v2 criterion needs `ProjectRegistered` to advance its revision and drop `projectId` from the payload, but the v2 vector still declares `projectId` ([`RegisterProject.play:15-17`](../Source/DotNET/Screenplay.CanonicalCorpus/Corpus/RegisterProject/v2/source/RegisterProject.play)) at contract revision 1.

Under [decision 0001](0001-chronicle-runtime-semantic-authority.md), Chronicle defines the meaning:

- A prior generation is a full type, marked with `[EventTypeGenerationFor<T>(N)]` ([`EventTypeGenerationForAttribute.cs`](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Events/EventTypeGenerationForAttribute.cs)), and every generation resolves to the same event type id ([`MigrationGenerationsMustShareEventTypeId.cs`](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Events/Migrations/MigrationGenerationsMustShareEventTypeId.cs)).
- A migration must go from generation N−1 to N; a gap throws ([`InvalidMigrationGenerationGap.cs`](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Events/Migrations/InvalidMigrationGenerationGap.cs), raised in [`EventTypeMigration.cs:44`](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Events/Migrations/EventTypeMigration.cs)).

## Decision

Event generations mirror Chronicle.

1. **Marker.** `event X generation N` declares generation N of event X. Without the marker, a declaration is generation 1, so existing documents are unchanged.
2. **Prior generations in full.** Each earlier generation is declared as a complete event declaration with its own generation number. All generations of X share one event contract identity.
3. **Predecessor.** The predecessor of generation N is always N−1. Generations are consecutive from 1; a gap or a duplicate number is a compile error.
4. **ESM.** A generation maps to the event contract revision, and each revision records its predecessor. Admission follows [decision 0004](0004-admission-and-governance-of-portable-executable-semantics.md).

## Options considered

- **Prior generations in full with a marker (taken).** It matches Chronicle, where every generation is a real type, and the document shows exactly what can be replayed.
- **Diff-style declarations (only what changed, or only what a migration touches).** Not taken: the prior shape could not be read on its own, and it has no Chronicle counterpart, which keeps each generation a full type.
- **Marker only, with no prior shape.** Not taken: a migration would map from a shape nobody wrote down, which #71 calls "a comment with syntax".
- **Explicit predecessor numbers that may skip.** Not taken: Chronicle rejects migrations that are not consecutive.

## Default if unanswered

Documents cannot say an event has evolved, the ESM keeps rejecting non-initial revisions, and #168's v2 vector cannot advance its revision or drop `projectId` without a silent change that keeps the initial revision, which #71 rules out.

## Timeline and scope

Settle before any generation syntax, and keep it until superseded. The marker and full prior declarations ship first. This unblocks the representation #168's v2 vector needs: `ProjectRegistered` generation 1 with `projectId` and generation 2 without it, at revision 2 with revision 1 as predecessor.

In scope: the `generation` marker, full prior declarations, the consecutive-numbering rule, shared contract identity across generations, predecessor lineage in the ESM, and lifting the initial-revision guards.

Out of scope: the transformation (migration) construct that maps generation N−1 to N, which stays a design question under #71; tombstone and compensation markers; changes to Chronicle.

## Verification

**Done when:** `event X generation 2` parses, prints and round-trips, and an event without the marker prints unchanged. A document that declares generations 1 and 3 of X without 2, or two generation-2 declarations, fails compilation. All generations of X share one event contract id in the ESM, and revision N names N−1 as predecessor. The ESM no longer rejects non-initial revisions that satisfy these rules.

**Verify by:** Parser, printer and validator specs for each case; identity-catalog and ESM specs for shared identity and predecessor lineage; a golden vector for a two-generation event; the #168 v2 corpus vector, once updated, pinning revision 2 without `projectId`.

## Consequences

Evolution becomes visible and checkable in the document, and matches what a Chronicle renderer emits. A document that has evolved an event five times carries five shapes of it, which is heavy but true. The migration construct still needs its own design; this record gives it typed shapes to map between.

## Related issues

Screenplay: [#71](https://github.com/Cratis/Screenplay/issues/71), [#168](https://github.com/Cratis/Screenplay/issues/168), [#167](https://github.com/Cratis/Screenplay/issues/167), [#128](https://github.com/Cratis/Screenplay/issues/128).

## Status notes

**2026-09-25 — partially implemented. Shipped in v4.34.0.** Grammar implemented: The optional marker, full prior declarations, and consecutive-number validation are implemented; unmarked declarations remain unchanged. X is matched by name within its owning module, feature path and slice. The event-contract address (and therefore its catalog identity assignment) has no generation component, so those declarations resolve to the same `EventContractId`; a matching name in another slice is not another generation. The legacy ID derived from application and event name collides across slices, and the current ESM binder also rejects same-named cross-slice references as ambiguous.

At this grammar-only milestone, ESM predecessor lineage and #168's v2 vector were pending. Models using marked generations then failed binding with `PLAY0449` rather than dropping historical shapes; decision 0015 subsequently replaced that guard. This was the shipped grammar increment, not ESM lineage; it was not yet `verified` at that time.

**2026-09-25 — implemented, not verified. Shipped in v4.37.0.** Decision 0015 completes the ESM lineage and the corrected #168 v2 corpus at ESM v4. `PLAY0449` now guards historical-shape references and catalog revision mismatches rather than rejecting every marked event. Cross-target migration and replay conformance remain open under #71.
