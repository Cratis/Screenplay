---
id: 0012
title: A language-neutral typed-context descriptor, then command handlers as the next role
status: accepted
stage: none
decided: 2026-09-24
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Contexts/**
  - Source/DotNET/Screenplay/Semantics/SemanticImplementationRequirement.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Implementations.cs
  - Source/DotNET/Screenplay/Semantics/Serialization/**
  - Source/DotNET/Screenplay.Mcp/**
  - Documentation/screenplay/context.md
---

## Context

[Decision 0002](0002-implementation-attachments-envelope-and-reducer-role.md) delegated editing support to the host editor and said Screenplay emits wrapper and source-map data only. Exact source maps shipped in v4.30.0 ([#252](https://github.com/Cratis/Screenplay/pull/252)). The wrapper data has no defined shape yet: an editor or renderer cannot learn, from Screenplay, what an implementation body can see and with which types.

The code contexts are still `dynamic` where they carry model data: `CommandContext.Command`, `QueryContext.Arguments`, `RuleContext.Artifact` and `Value`, `PolicyContext.Artifact`, `ReducerContext.State` and `Event` ([`CommandContext.cs:25`](../Source/DotNET/Screenplay/Contexts/CommandContext.cs), [`QueryContext.cs:26`](../Source/DotNET/Screenplay/Contexts/QueryContext.cs), [`RuleContext.cs:38-39`](../Source/DotNET/Screenplay/Contexts/RuleContext.cs), [`PolicyContext.cs:33`](../Source/DotNET/Screenplay/Contexts/PolicyContext.cs), [`ReducerContext.cs:29-30`](../Source/DotNET/Screenplay/Contexts/ReducerContext.cs)). v4.21.0 added typed accessors beside them (option 2 of [#84](https://github.com/Cratis/Screenplay/issues/84)), but the consumer names the type. Option 1, a context typed per artifact, is what #84 still asks for.

Screenplay already has one precedent for publishing a catalog as data: `EventContextCatalog` restates Chronicle's event context once, and specs hold the editor surfaces and documentation to it ([`EventContextCatalog.cs`](../Source/DotNET/Screenplay/Syntax/EventContextCatalog.cs), `Syntax/for_EventContextCatalog`).

[#139](https://github.com/Cratis/Screenplay/issues/139) lists later roles: command handlers, query performers and reaction effects. Policy predicates were the third role, shipped in v4.29.0 ([decision 0005](0005-policy-predicates-as-an-implementation-attachment-role.md)).

## Decision

1. **Descriptor.** For each implementation role and each artifact that needs code, Screenplay publishes a language-neutral typed-context descriptor: the members the body can see, their types, and the model property each member comes from. It is data, versioned with the role's context contract, and pinned by shared vectors like the event-context catalog.
2. **Language providers generate wrappers.** A language provider turns the descriptor into typed code; for C#, that is Stage ([Cratis/Stage#151](https://github.com/Cratis/Stage/issues/151)). Screenplay stays free of Roslyn, as decision 0002 requires.
3. **#84.** Option 1 of #84 is realized as this descriptor plus Stage's generation.
4. **Next role.** Command handlers are the next implementation role after policy predicates. They are implemented after the descriptor, because a handler's context is the richest and is `dynamic` today.
5. **Later roles.** Query performers wait on [decision 0010](0010-query-paging-ordering-and-live-delivery.md). Reaction effects wait on [decision 0006](0006-reaction-triggers-declare-reads.md), [decision 0009](0009-external-event-origin-and-translation-slices.md), and the deferred due-time work.

## Options considered

- **Language-neutral descriptor (taken).** One shape serves every language provider and every editor, and Screenplay keeps no compiler dependency.
- **Wrapper text per language from Screenplay.** Not taken: Screenplay would have to know C# and every later language, and decision 0002 already keeps language services in the host.
- **Keep typed accessors only.** Not taken as the answer: the consumer still names the type, so completion has nothing the document declares to work from.
- **Next role: query performer.** Not taken: its contract depends on selection, paging and live delivery (decision 0010).
- **Next role: reaction effect.** Not taken: reactions do not bind in the executable semantic model, and their occurrence and effect semantics are still being settled (decisions 0006 and 0009).

## Default if unanswered

Hosts keep seeing `dynamic` contexts, each renderer or editor derives its own idea of what a body can see, and command handlers stay unbound with nothing to type their context against.

## Timeline and scope

Settle before the command-handler role or any wrapper generation in Stage, and keep it until superseded. Order: the descriptor, then the command-handler role.

In scope: the descriptor shape and its vectors, one descriptor per role for the roles already bound (reducer transitions, rule predicates, policy predicates) and for command handlers, exposure through the compilation result and MCP, and the role order.

Out of scope: wrapper generation in any language (owned by providers, Stage#151 for C#); language-service hosting (decision 0002); removing the `dynamic` members; the command-handler contract beyond its context, which gets its own record if it changes the envelope.

## Verification

**Done when:** Every bound implementation requirement exposes a typed-context descriptor listing each member, its type and its model source. A shared vector pins the descriptor for a command, a rule, a policy and a reducer, and a spec fails when the context contract changes without the vector. Command handlers then bind as the next role with their descriptor.

**Verify by:** Descriptor specs per role, vector specs in the style of `for_EventContextCatalog`, an MCP view spec, and Stage#151 generating a typed C# context from the vector.

## Consequences

Editors and renderers get one source of truth for what a body can see, and #84's option 1 becomes possible without Screenplay learning C#. Completion for inline code becomes possible in hosts. Query performers and reaction effects are explicitly sequenced behind other records, so they will not ship first.

## Status notes

**2026-09-25 — partially implemented, not verified. Shipped in v4.41.0.** Revision-1 typed context descriptors are published as compilation/MCP sidecars for bound reducer, rule and policy requirements and resolvable but unbound command handlers. Command-handler binding and provider-generated wrappers remain pending. Six shape choices were decided under the maintainer's delegation: sidecar (no canonical ESM byte changes), portable type tokens, policy descriptors per use site, derived booleans but no accessor methods as data, context-only unbound handler descriptors, and an independently versioned descriptor contract without changing attachment manifest semantics.

## Related issues

Screenplay: [#139](https://github.com/Cratis/Screenplay/issues/139), [#84](https://github.com/Cratis/Screenplay/issues/84), [#65](https://github.com/Cratis/Screenplay/issues/65), [#252](https://github.com/Cratis/Screenplay/pull/252). Stage: [#151](https://github.com/Cratis/Stage/issues/151), [#119](https://github.com/Cratis/Stage/issues/119).
