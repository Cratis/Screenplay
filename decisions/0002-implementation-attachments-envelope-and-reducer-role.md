---
id: 0002
title: "Implementation attachments: envelope first, reducer transitions as the first role"
status: accepted
stage: none
decided: 2026-09-24
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/SemanticImplementationRequirement.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Implementations.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.SliceMembers.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Validations.cs
  - Source/DotNET/Screenplay/Semantics/Execution/**
  - Source/DotNET/Screenplay/Semantics/Serialization/**
---

## Context

[#139](https://github.com/Cratis/Screenplay/issues/139) defines an envelope for code the executable semantic model (ESM) hands off to: an `ImplementationRequirement` (stable identity, owner, role, context/result contract versions, capabilities) and an `ImplementationAttachment` (language, inline or file source, content hash, source map). It has three open choices: which role ships first, the capability model, and where the language service lives. [#213](https://github.com/Cratis/Screenplay/issues/213) waits on it for reducers with a body, and [#209](https://github.com/Cratis/Screenplay/issues/209) waits on it for code validation.

Today every reducer fails binding with `requires a portable reducer contract` ([`SemanticModelBinder.SliceMembers.cs:22,51`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.SliceMembers.cs)), and code validation fails at [`SemanticModelBinder.Validations.cs:184`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Validations.cs). The v4.23.0 inventory ([#236](https://github.com/Cratis/Screenplay/pull/236)) records each attachment, but a file-backed attachment is hashed from its path, not its content ([`SemanticModelBinder.Implementations.cs:47`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Implementations.cs)). That contradicts #139's rule that paths are never identity.

Under [decision 0001](0001-chronicle-runtime-semantic-authority.md), Chronicle defines what a reducer means. Chronicle calls client code with the events and the current state. A null result deletes the instance ([`ReducerPipeline.cs:77-83`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/Observation/Reducers/ReducerPipeline.cs)). A reducer read model "is always keyed by event source id" (`ReducerPipeline.cs:105`). The wire format carries a per-event key expression, but no Chronicle path routes a reducer on it. The only reader is the definition-evolution check, which uses it to plan a replay.

## Decision

The #139 envelope is fixed first. Reducer transitions are its first role, rule predicates are its second, and the other roles follow one at a time.

1. **Envelope fixes.** A file-backed attachment is identified by its content, never its path. Each requirement has a stable identity, a context/result contract version for its role, and a capability field.
2. **Reducer body.** The body is an opaque attachment, not a restricted portable expression. Anything a restricted expression could say is a projection. Screenplay's reference executor does not compute reducer state. Reducer behavior is proven against rendered targets. A specification that depends on a reducer-built read model gets an explicit unsupported (requires-target) outcome, never a silent pass.
3. **Reducer key.** The key is fixed to the event source id, because Chronicle routes reducers only by event source. #213 no longer depends on [#132](https://github.com/Cratis/Screenplay/issues/132).
4. **Capabilities.** A provider declares a capability profile. Screenplay names each role's required capability (for example `pure`). Each rendering provider enforces its own API allowlist. Screenplay cannot reject undeclared APIs itself. A capability mismatch reaches the author as a blocking gap from the provider.
5. **Language service.** Editing support is delegated to the host editor. Screenplay emits wrapper and source-map data only.

## Options considered

- **First role: command handler.** Richest context and the largest effect on the samples. Not taken: its context is `dynamic` today ([#84](https://github.com/Cratis/Screenplay/issues/84)), and designing the envelope around it would fix the least settled contract first.
- **First role: rule predicate.** Smallest context, and it unblocks #209 code validation. Not taken first: Screenplay invented its meaning, and it is split across three roles (`CommandValidation`, `ConceptValidation`, `RulePredicate` in [`SemanticImplementationRequirement.cs:15,18,21`](../Source/DotNET/Screenplay/Semantics/SemanticImplementationRequirement.cs)). It is the second role.
- **Reducer transition first (taken).** Chronicle defines its inputs and result completely, so fixing this shape early meets decision 0001 and invents nothing.
- **Close #213 as covered by #139.** Not taken: #139 would then have no first role to prove its envelope against.
- **Restricted portable transition expression.** It would make reference-execution vectors possible. Not taken: Chronicle has no transition algebra, and anything such an expression could state is already a projection. The body-less reducer message already points there ([`SemanticModelBinder.SliceMembers.cs:50`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.SliceMembers.cs)).
- **Keep the #132 affected-key dependency.** Not taken: it would model a routing choice Chronicle does not offer.
- **Declared-API allowlist or deny-list in Screenplay.** Not taken: Screenplay has no Roslyn runtime dependency and cannot check target code. Stage already compiles inline handlers against a fixed set of core assemblies ([`InlineCommandHandlerAdmission.cs:19-22`](https://github.com/Cratis/Stage/blob/main/Source/Rendering.Cratis/Renderers/InlineCommandHandlerAdmission.cs)).
- **Screenplay-hosted Roslyn virtual documents.** Not taken: this adds a runtime dependency and duplicates what Monaco and VS Code already host for embedded languages.

## Default if unanswered

Every reducer with a body stays rejected, code validation stays blocked, and file attachments keep a path-derived hash that changes on a move and misses content edits. Stage keeps rendering target-specific partial behavior, including `TODO` stubs, with no Screenplay contract to check it against (#139).

## Timeline and scope

Settle this before any #139 role ships, and keep it until superseded. Order: envelope fixes, then the reducer role (#213), then the rule-predicate role (#209 code validation), then later roles.

In scope: the requirement and attachment envelope, the reducer contract (events consumed, key fixed to the event source, initial state absent, result is state or delete), capability naming, the unsupported outcome for reducer-dependent specifications, and wrapper/source-map data.

Out of scope: a universal code IR (a #139 non-goal); executing reducer bodies in Screenplay; enforcing API allowlists in Screenplay; a Screenplay-hosted language service; Stage's `IReducerFor<T>` rendering ([Cratis/Stage#119](https://github.com/Cratis/Stage/issues/119)); command-handler, policy, reaction and screen roles.

## Verification

**Done when:** A reducer with an inline or file-backed body binds to a reducer contract that carries its events, a key fixed to the event source, an absent initial state and a state-or-delete result. Both forms produce the same contract. A file attachment's hash changes when its content changes and stays the same when only its path changes. Each requirement carries a stable identity, a contract version and a capability. A specification that reads a reducer-built read model returns `SemanticUnsupported` ([`SemanticExecutionContracts.cs:300`](../Source/DotNET/Screenplay/Semantics/Execution/SemanticExecutionContracts.cs)) naming the reducer, never a pass.

**Verify by:** Specs under `Source/DotNET/Screenplay` that bind inline and file reducer bodies and compare the results, cite `ReducerPipeline.cs:77-83,105`, move and edit a file attachment and check its hash, and run a reducer-dependent specification through the reference runner. Update the golden vectors in `Semantics/Serialization/Golden` along with the semantic version bump.

## Consequences

#213 unblocks without waiting for the whole of #139, and its contract comes directly from Chronicle. The envelope shape is fixed by a small role, so later roles extend it rather than reshape it. The cost is that reducer behavior can never be proven by Screenplay alone: it needs a rendered target. "Undeclared APIs are rejected" (a #139 criterion) is met only at the provider. Reducers keyed on anything other than the event source are foreclosed until Chronicle routes them. Adding the reducer contract to canonical JSON requires a semantic version bump. Whether a minor bump is enough is not settled here.

## Related issues

Screenplay: [#139](https://github.com/Cratis/Screenplay/issues/139), [#213](https://github.com/Cratis/Screenplay/issues/213), [#209](https://github.com/Cratis/Screenplay/issues/209), [#132](https://github.com/Cratis/Screenplay/issues/132), [#236](https://github.com/Cratis/Screenplay/pull/236), [#84](https://github.com/Cratis/Screenplay/issues/84), [#65](https://github.com/Cratis/Screenplay/issues/65). Stage: [#119](https://github.com/Cratis/Stage/issues/119).
