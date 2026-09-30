---
id: 0020
title: Admit keyed read-model absence assertions in ESM v5
status: accepted
stage: none
decided: 2026-09-26
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Syntax/Specifications/**
  - Source/DotNET/Screenplay/Parsing/SpecificationParser.cs
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Workspaces/WorkspaceAbsenceKey*.cs
  - Source/DotNET/Screenplay/Workspaces/WorkspaceEditProvenance.cs
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Documentation/screenplay/specifications.md
---

## Context

`then readmodel` asserts present keyed state. Reducers may delete an instance by returning null, and projections can remove it, but the language cannot state that the particular instance is absent. An empty read model or query is not equivalent to a missing keyed instance.

## Decision

`then no readmodel <View> for <key>` asserts the absence of precisely that view/key pair. The key is mandatory and type-checked against the view identifier. A present and absent assertion for the same pair contradict each other. Projection-backed assertions run in the reference evaluator; reducer-dependent assertions return typed Unsupported and require a target. Models using this assertion select ESM v5 (language/semantic 5.0, `schemaVersion` 5), retaining v4's generation history and transition-cardinality rules. Existing v1–v4 canonical bytes and revisions do not change.

## Options considered

- Add an explicit keyed negative assertion in v5 (chosen): exact, portable, independently testable.
- Interpret an empty query result as absence: rejected because an empty result and a missing read-model instance are distinct.
- Treat null properties as deletion: rejected because null is a value in an existing instance.
- Reuse ESM v4 and append a field: rejected because changing serialized bytes requires explicit version admission under decision 0004.

## Default if unanswered

Providers keep target-only deletion tests with no source-backed specification and may claim a deletion passed after checking the wrong instance.

## Timeline and scope

Applies from ESM v5 until superseded. In scope: parser, syntax, binding, evaluator, strict serialization, vectors, grammar and docs. Out of scope: executing opaque reducers, automatic downstream consumer admission, or changing v1–v4 bytes.

## Verification

**Done when:** The two-key example asserts one missing instance and one surviving instance; malformed and wrongly typed keys and contradictions fail closed; projection absence runs while reducer absence is Unsupported; a v5 golden and source-backed corpus pin canonical bytes, revision and outcomes; old vectors remain identical.

**Verify by:** Run parser/printer/walker/rename, binder/model/runner, strict-reader, canonical vector/corpus and MCP round-trip specs; build both configurations and check explicit Stage, CLI, Studio and Generation admission before downstream use.

## Consequences

The assertion carries a precise portable meaning and consumers need explicit v5 admission. A provider that cannot check deletion must reject the specification instead of silently passing it. Typed workspace authoring binds absence keys in their own pass, separate from generic reference correspondence, so v1–v4 reference behaviour is unchanged. Each key member either binds to a property of the read model's identifier type or is an explicit unresolved obligation. Correspondence between the original and the proposed workspace comes only from the operations in the proposal, never from a matching physical path or collection index. Safe rejects new unresolved key members, keeps proven unchanged debt and accepts explicit repairs; Draft reports new debt; both refuse unexplained rebinding and ambiguous correspondence without a write plan. A rename rewrites only the key members bound to the renamed declaration and verifies the complete candidate before it returns a write plan. A rename is refused while an absence key in the workspace is unresolved.
