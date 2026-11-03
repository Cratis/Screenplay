---
id: 0135
title: Passive read models are an explicit realization choice outside portable ESM
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-11-02
decider: Accepted Screenplay#1129 implementation plan (delegated)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
---

## Context

A command may need private decision state without a materialized query surface. Inferring passivity from current consumers would silently change realization when a query is added or removed. Putting Chronicle sink choices into portable ESM would also mix execution semantics with a deployment choice. The accepted [Screenplay#1129](https://github.com/Cratis/Screenplay/issues/1129) plan selects explicit metadata instead.

## Decision

A bare `passive` line in a `readmodel` body opts in. Omission means active; passivity is never inferred. It may appear anywhere among body directives and properties. It is not a header modifier, has no arguments, and does not reserve a property named `passive`. Repeating it is idempotent with Warning `PLAY0858`.

Only a command `reads` dependency consumes passive decision state. C# rejects a query or screen data surface over it with Error `PLAY0859`, regardless of collection, optional or observable query shape. C# warns `PLAY0860` when no command reads it. Usage resolves scoped declaration identities, not matching short names. Projections and specification fixtures do not make it observed or command-read. The TypeScript parser retains the flag and reports repeats; it does not add usage heuristics.

`ReadModelSyntax.IsPassive` is a default-false init property, preserving the public constructor and Deconstruct signatures. Syntax JSON emits only true and defaults missing to false. The binder reports Information `PLAY0270` for the directive and exposes immutable canonical identities on `SemanticCompilation.PassiveReadModels`. Sidecar-copy operations preserve it. There is no `SemanticReadModel.IsPassive`, ESM/version change, or changed canonical semantic byte/revision.

Renderers must include the flag in hashed render input (the `AuthoringMetadataInput` precedent), since toggling it leaves the ESM revision unchanged. Runtime consumers must carry it through `ApplicationRealization`; a portable ESM alone cannot recover it. Stage#208 is the first consumer, mapping this choice to Chronicle sink `None`, `IsActive=false` and `[Passive]`. Screenplay does not implement Chronicle realization.

## Alternatives and consequences

Inference from command-only usage was rejected because adding a consumer must not silently change deployment. Adding a portable ESM member was rejected because this is realization metadata, not execution semantics. Making the syntax record positional was rejected to preserve binary compatibility. Editors can parse the choice without claiming semantic validation. Existing active models and their transport bytes stay unchanged.

The parser adds one ordinal keyword comparison beside the existing directive checks, with no allocation, regex, lookahead rescan or extra pass for active models. Managed read-surface skill updates belong in Cratis/AI, not consuming corpus copies.

## Downgrade path

Older tools reject syntax JSON that uses `passive`: their `SyntaxJsonReader` refuses unknown members. Before downgrading, remove the directive or re-export the model with a version that omits it. Portable ESM bytes are unchanged, so only the syntax JSON and `.play` source carry the incompatibility.
