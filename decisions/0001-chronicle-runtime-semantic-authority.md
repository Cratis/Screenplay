---
id: 0001
title: Use Chronicle's runtime meaning for portable executable semantics
status: accepted
stage: none
decided: 2026-09-24
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/for_ScreenplayCompiler/**
---

## Context

Chronicle pins `Cratis.Screenplay` in [`Directory.Packages.props`](https://github.com/Cratis/Chronicle/blob/main/Directory.Packages.props) and hosts Screenplay's compiler: [`LanguageService.cs`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/Projections/Engine/DefinitionLanguage/LanguageService.cs) calls `ScreenplayCompiler.CompileProjection`, then `ProjectionValidator` and `ProjectionDefinitionSyntaxVisitor.Visit`. The visitor lowers Screenplay syntax to Chronicle projection definitions; Chronicle's validator and engine determine execution ([#218](https://github.com/Cratis/Screenplay/issues/218)). Screenplay's [FAQ](../Documentation/screenplay/faq.md) says its portable semantic authority does not copy framework implementation vocabulary. That rules out framework-named ESM types, not learning the observable meaning from Chronicle: Chronicle decides **what** the semantics are; Screenplay decides **how to name** them portably ([#128](https://github.com/Cratis/Screenplay/issues/128), [#218](https://github.com/Cratis/Screenplay/issues/218)).

## Decision

Chronicle's `ProjectionDefinitionSyntaxVisitor`, `ProjectionValidator`, and engine define the runtime meaning of projections, keys, constraints, reducers, and event context. Screenplay owns the grammar and syntax tree; its executable semantic model (ESM) mirrors Chronicle's meaning in portable names. Stage's renderer is never authoritative ([#218](https://github.com/Cratis/Screenplay/issues/218), [#128](https://github.com/Cratis/Screenplay/issues/128)).

## Options considered

- **Chronicle semantics, portable ESM names (proposed):** follows the compiler/lowering boundary without importing Chronicle's implementation vocabulary ([#218](https://github.com/Cratis/Screenplay/issues/218), [FAQ](../Documentation/screenplay/faq.md)).
- **Screenplay-only semantics:** permits the ESM to disagree with the runtime; the projection-level key fallback illustrates the risk ([#218](https://github.com/Cratis/Screenplay/issues/218)).
- **Stage as reference:** its renderer is a consumer, not the runtime lowering; it has known differences from Chronicle ([#211](https://github.com/Cratis/Screenplay/issues/211), [#218](https://github.com/Cratis/Screenplay/issues/218)).
- **Reuse Chronicle's parser:** impossible: Chronicle uses `ScreenplayCompiler` and has no separate projection parser ([`LanguageService.cs`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/Projections/Engine/DefinitionLanguage/LanguageService.cs), [#218](https://github.com/Cratis/Screenplay/issues/218)).

## Default if unanswered

The current implicit boundary remains: ESM and Chronicle can diverge without a conformance check, including on key routing ([#218](https://github.com/Cratis/Screenplay/issues/218)).

## Timeline and scope

Settle the authority boundary before extending ESM projection and related semantics under [#211](https://github.com/Cratis/Screenplay/issues/211), [#212](https://github.com/Cratis/Screenplay/issues/212), and [#217](https://github.com/Cratis/Screenplay/issues/217); keep it until superseded. In scope: semantic contracts, grammar compatibility, and conformance. Out of scope: changing Chronicle's runtime, choosing each individual ESM contract shape, or adopting Chronicle implementation names ([#128](https://github.com/Cratis/Screenplay/issues/128), [#218](https://github.com/Cratis/Screenplay/issues/218)).

## Verification

**Done when:** A projection-declaration corpus, including constructs from [`Documentation/screenplay/projections/`](../Documentation/screenplay/projections/), checks that ESM lowering is equivalent to `ProjectionDefinitionSyntaxVisitor.Visit` for events, keys, cardinality, child/nested scopes, removal, `all`/`every` flags, and mapping kinds ([#218](https://github.com/Cratis/Screenplay/issues/218)).

**Verify by:** Run an executable corpus-driven conformance test in Chronicle (which already references Screenplay) against the visitor; retain Chronicle-cited expectation specs in Screenplay. Check grammar changes against Chronicle's pinned `Cratis.Screenplay` version ([`LanguageService.cs`](https://github.com/Cratis/Chronicle/blob/main/Source/Kernel/Core/Projections/Engine/DefinitionLanguage/LanguageService.cs), [`Directory.Packages.props`](https://github.com/Cratis/Chronicle/blob/main/Directory.Packages.props), [#218](https://github.com/Cratis/Screenplay/issues/218)).

## Consequences

ESM changes cite Chronicle source; divergences become issues on the side whose behavior is wrong rather than silent reinterpretations. Grammar changes must preserve Chronicle's lowering and be checked against its pinned package version. Screenplay retains portable terminology, while conformance work crosses repository boundaries ([#128](https://github.com/Cratis/Screenplay/issues/128), [#218](https://github.com/Cratis/Screenplay/issues/218)).

## Related issues

Screenplay: [#218](https://github.com/Cratis/Screenplay/issues/218), [#128](https://github.com/Cratis/Screenplay/issues/128), [#211](https://github.com/Cratis/Screenplay/issues/211), [#212](https://github.com/Cratis/Screenplay/issues/212), [#217](https://github.com/Cratis/Screenplay/issues/217). Chronicle: [#4109](https://github.com/Cratis/Chronicle/issues/4109), [#4116](https://github.com/Cratis/Chronicle/issues/4116), [#4117](https://github.com/Cratis/Chronicle/issues/4117), [#4118](https://github.com/Cratis/Chronicle/issues/4118), [#4119](https://github.com/Cratis/Chronicle/issues/4119), [#4122](https://github.com/Cratis/Chronicle/issues/4122), [#4123](https://github.com/Cratis/Chronicle/issues/4123), [#4124](https://github.com/Cratis/Chronicle/issues/4124), [#4125](https://github.com/Cratis/Chronicle/issues/4125).
