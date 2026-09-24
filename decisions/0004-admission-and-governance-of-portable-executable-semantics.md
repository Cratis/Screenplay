---
id: 0004
title: Admission and governance of portable executable semantics
status: accepted
stage: none
decided: 2026-09-24
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Documentation/screenplay/interoperability.md
  - Documentation/screenplay/projections/semantic-model.md
---

## Context

[#128](https://github.com/Cratis/Screenplay/issues/128) makes the executable semantic model (ESM) the portable authority that Stage, renderers, specifications, Studio and AI consume. It states an admission rule and non-goals, then says architecture and delivery decisions live in `SCREENPLAY_PLUS_ARCHITECTURE.md` and `SCREENPLAY_PLUS_PROGRAM.md`. Neither file is tracked in this repository, and a code search of the Cratis organization finds neither, so the rule that decides what may enter the ESM cannot be read, cited or superseded. The v4.20.0 status comment on #128 names governance as one of the remaining criteria.

In practice the bar already exists. It was set by two version activations. ESM v2 shipped in v4.20.0 for typed state-change destinations, event-source context and specification event sources. ESM v3 shipped in v4.26.0 for bodied reducers and was extended in v4.27.0 to code validation. v4.28.0 identified file attachments by content. Each construct got a mirrored runtime meaning, canonical bytes pinned by golden vectors, reference execution or a typed unsupported outcome, and a version that only models using it select ([`interoperability.md:98,100`](../Documentation/screenplay/interoperability.md), [`Versions.cs:213`](../Source/DotNET/Screenplay/Semantics/Versions.cs)).

How v2 and v3 met that bar:

| Requirement | ESM v2 (v4.20.0) | ESM v3 (v4.26.0, v4.27.0) |
| --- | --- | --- |
| Runtime meaning | Chronicle event context and event source | Chronicle `ReducerPipeline` ([decision 0002](0002-implementation-attachments-envelope-and-reducer-role.md)) |
| Canonical form | `full-esm-v2.json` | `full-esm-v3.json` |
| Reference execution | Executed | `SemanticUnsupported` for reducer state and code validation |
| Source-backed corpus vector | `RegisterProject/v2` | None yet |
| Activation | Only models using a v2 construct | Only models with an implementation attachment |

Consumers pin `Cratis.Screenplay` independently in their `Directory.Packages.props`. When this record was written, Chronicle pinned 4.16.0 and Studio 4.19.0, both before v2. Stage and Generation pinned 4.24.1, before v3. The CLI sets its version through a property. No rule says when a consumer must follow a new ESM version or how it learns that one exists.

## Decision

A language construct enters the ESM only when it passes both gates below. A new ESM version, or any change to existing canonical bytes, requires an accepted decision record. Consumers follow new versions through explicit admission, never by accident.

1. **Why it belongs (from #128).** The construct changes portable observable behavior, execution, specifications, Studio/AI reasoning or equivalent generated backends. Existing building blocks cannot already express it. It has deterministic, framework-neutral semantics. Adapter diagnostics or framework API frequency alone never justify syntax.
2. **What it takes to enter (from ESM v2 and v3).**
   - **Runtime meaning.** Where Chronicle has a runtime meaning, the ESM mirrors it and its specs cite the Chronicle source ([decision 0001](0001-chronicle-runtime-semantic-authority.md)). Where Chronicle has none, the proposal states the meaning deterministically.
   - **Canonical serialized form.** The construct has canonical JSON, and a golden vector in [`Semantics/Serialization/Golden`](../Source/DotNET/Screenplay/Semantics/Serialization/Golden/README.md) pins it.
   - **Reference execution or typed unsupported.** The reference evaluator executes it, or it returns `SemanticUnsupported` naming the construct ([`SemanticExecutionContracts.cs:300`](../Source/DotNET/Screenplay/Semantics/Execution/SemanticExecutionContracts.cs)). Such a specification never passes, and unrelated specifications still run, as for reducers (v4.26.0) and code validation (v4.27.0).
   - **Conformance vectors.** A source-backed vector in `Cratis.Screenplay.CanonicalCorpus` pins ESM bytes, semantic revision and normalized outcomes across file and folder forms ([`interoperability.md:106-112`](../Documentation/screenplay/interoperability.md)).
   - **Fail closed.** A form the ESM does not admit fails binding with a diagnostic. It is never bound and ignored ([`semantic-model.md:69`](../Documentation/screenplay/projections/semantic-model.md)). A target that cannot realize it rejects it ([`interoperability.md:104`](../Documentation/screenplay/interoperability.md)).
   - **Versioning activated only when used.** A model selects the new language/semantic version and `schemaVersion` only when it uses the construct. Existing models keep their bytes and revisions. Strict readers reject versions they do not declare ([`interoperability.md:98,100`](../Documentation/screenplay/interoperability.md)). A construct that previously failed binding may join the highest version only if no model that bound before changes bytes, as code validation joined v3 in v4.27.0.
3. **Who governs.** No single owner. Changes follow this process: an issue answers gate 1. A decision record is required for a new version, a change to existing canonical bytes, or a change to the outcome kinds. It is accepted by a named decider through the [decision-record procedure](README.md). The implementation pull request shows evidence for each item in gate 2 and is reviewed before merge. The release notes name the version, what selects it, and what consumers must do, as v4.20.0 and v4.26.0 did.
4. **Keeping consumers in step.** `Cratis.Screenplay` and `Cratis.Screenplay.CanonicalCorpus` release together ([Golden README](../Source/DotNET/Screenplay/Semantics/Serialization/Golden/README.md)). A release that adds an ESM version opens a tracking issue in each consumer that reads the ESM: Stage, CLI, Studio and Generation. Each consumer admits the version explicitly and rejects it until then. A construct with a Chronicle runtime meaning also gets Chronicle conformance coverage under decision 0001 ([Cratis/Chronicle#4130](https://github.com/Cratis/Chronicle/issues/4130)). Grammar changes are checked against Chronicle's pinned version.

## Options considered

- **Record in `decisions/` (proposed).** It can be consulted through `applies-to`, cited and superseded, and it lives in the repository.
- **Track the two `SCREENPLAY_PLUS_*` files.** Not taken: they mix durable rules with a dated delivery order, which belongs on issues, and they have no supersession mechanism.
- **Keep the rule in #128's body and comments.** Not taken: issue text is edited and closed, and the consult step never matches it against paths.
- **Gate 1 only.** Not taken: v2 and v3 show that gate 2 is what protects consumers. Byte stability, typed unsupported outcomes and vectors are what let a pinned consumer reject a model safely.
- **Admit on adapter evidence or framework frequency.** Rejected by #128. It would turn realization vocabulary into core semantics.
- **Bump the ESM version on every release.** Not taken: every existing model would change bytes and revision. v4.20.0 chose per-model activation instead.
- **Lockstep releases across consumers.** Not taken: it couples five release trains. Explicit rejection by pinned consumers is already safe.
- **A named semantics owner.** Not taken: acceptance names a person per record. The standing rule is a process, so it outlives any one person.

## Default if unanswered

The bar stays implicit, and #128 keeps citing files nobody can open. The next construct can enter with a weaker bar, such as no corpus vector or a silent fallback, and consumers find out only when they bump a pin. v3 already has a golden vector but no source-backed corpus vector. Chronicle, Studio, Stage and Generation all pin versions that predate an admitted ESM version, and no issue tracks their catch-up.

## Timeline and scope

Accept before any new ESM version or construct admission, and keep it until superseded. #128 cannot close without it.

In scope: the two admission gates, the versioning rule, the change process, and the consumer notification and admission duty.

Out of scope: #128's non-goals (HTTP routes, database CRUD APIs, message-broker dispatch modes, projection daemons, framework Saga classes, package versions and deployment topology). Also out of scope: which constructs to admit next (owned by the child issues), consumer code changes, Chronicle runtime changes, choosing the second target named in #128's acceptance criteria, and syntax that never reaches the ESM.

## Verification

**Done when:** #128 cites this record instead of the untracked files. Every semantic version in `EsmSchemaV3Support.SemanticVersions` has a golden vector and a source-backed `Cratis.Screenplay.CanonicalCorpus` vector, including v3. Stage, CLI, Studio and Generation each have a tracking issue or an admitted pin for v2 and v3. The next pull request that adds an ESM version cites an accepted record and shows gate-2 evidence.

**Verify by:** Read #128's body. List `Semantics/Serialization/Golden` and `Screenplay.CanonicalCorpus/Corpus` against the versions in [`Versions.cs`](../Source/DotNET/Screenplay/Semantics/Versions.cs). Check each consumer's `Directory.Packages.props` and issue tracker. Check the `Decision: 0004` trailer on the next version-adding commit.

## Consequences

Adding a construct costs more: vectors, an unsupported path and a record when a version changes. In return, a pinned consumer can always reject what it does not understand rather than misread it. Consumer catch-up becomes visible work instead of a surprise. Realization-shaped vocabulary stays out of the core unless a record overturns #128's non-goals.

## Related issues

Screenplay: [#128](https://github.com/Cratis/Screenplay/issues/128), [#135](https://github.com/Cratis/Screenplay/issues/135), [#136](https://github.com/Cratis/Screenplay/issues/136), [#139](https://github.com/Cratis/Screenplay/issues/139), [#167](https://github.com/Cratis/Screenplay/issues/167), [#218](https://github.com/Cratis/Screenplay/issues/218). Chronicle: [#4130](https://github.com/Cratis/Chronicle/issues/4130).

## Status notes

**2026-09-24 — accepted.** Accepted as written, including the two rules this record inferred from v2 and v3 rather than from #128: a construct that previously failed binding may join the highest existing ESM version when no model that bound before changes bytes, and each release that adds an ESM version opens a tracking issue in Stage, CLI, Studio and Generation. The decision text is unchanged. It stays at `stage: none` because its *Done when* is not met: ESM v3 has no source-backed `Cratis.Screenplay.CanonicalCorpus` vector yet.
