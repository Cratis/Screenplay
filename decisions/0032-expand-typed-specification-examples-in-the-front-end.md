---
id: 0032
title: Expand typed specification examples in the front end
status: accepted
stage: none
decided: 2026-10-07
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Printing/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

Specifications repeat the same command inputs, event payloads and read-model states while varying one value. [#427](https://github.com/Cratis/Screenplay/issues/427) needs reusable, possibly partial fixtures without hiding whole scenarios or changing matching. Syntax consumers, including Stage's specification converter, need complete effective steps even though the authored syntax preserves example references.

[0004](0004-admission-and-governance-of-portable-executable-semantics.md) protects executable semantic model (ESM) bytes and admission. [0011](0011-event-generations.md) and [0015](0015-event-generations-in-the-executable-model.md) distinguish current event shapes from historical generations. [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) and [0026](0026-generated-values-and-command-responses-in-esm-v7.md) govern generated fixtures. Examples must respect those contracts rather than introduce a new execution feature.

## Decision

> **2026-10-07 — routes and effective-syntax consumers clarification.** Route lines from 0031 (`stream`, `streamId`, `no stream`) are not allowed in examples in this version. An example stating one is rejected with a clear invalid-example-body diagnostic; routes are stated on the step. [#491](https://github.com/Cratis/Screenplay/issues/491) tracks route lines in examples. The effective-syntax API is .NET-only; named consumers are Stage ([Stage#209](https://github.com/Cratis/Stage/issues/209)) and Studio's importer ([StudioIssues#534](https://github.com/Cratis/StudioIssues/issues/534)). Implicit fixture defaults remain undecided; [#492](https://github.com/Cratis/Screenplay/issues/492) tracks production-model measurement. This record was renumbered from 0028 when main allocated 0028 to module and feature dependencies.

Typed examples expand in the front end and never reach the ESM. `example <Name> : <EventOrCommandOrReadModel>` names one typed instance, with ordinary property assignments, `for`, command `generated` fixtures and an optional description. Examples are declared at slice, feature, module or top level, including specification-only documents. They share the type namespace: collision with an event, command, read model, type or concept is an error. A unified resolver resolves the underlying type from the example's declaration scope. Examples always mean the current generation; historical-only fields are errors. There is no example-to-example inheritance in v1.

An example name may occupy the existing type slot in `given`, `given readmodel`, `when`, `when append`, `then` and `then readmodel [exactly]`. The kind keyword remains explicit; a mismatch is an error with a suggested corrected step. Step assignments override the example's values. Overrides may be indented or one inline assignment after the name, including structured values, without `with`. Repeating a property within the same step or example, including across header and body, is an error; overriding an example's property once is intentional and allowed.

Matching is unchanged: expected events have exact shape and equality; `then readmodel` matches the effective values as a subset unless `exactly` is written. Given events and read models, command inputs, appended events and expected events remain complete at binding. A missing property produces a precise diagnostic at its step, naming the example when used. There are **no implicit fixture defaults in v1**. Whether defaults should exist remains open, to be decided using production-model measurements; this record does not reject them permanently or introduce type-level defaults.

A public effective-syntax API returns expanded specifications and provenance identifying authored, example and override values. Syntax consumers use that API rather than independently inventing expansion. Tooling exposes effective values and origins. Binding the expanded syntax produces ESM bytes and revision identical to the hand-expanded spelling, with no new ESM version or fields; models without examples remain unchanged.

## Options considered

- **Typed examples with front-end expansion (chosen).** Removes repeated values in given, when and then while keeping every fact and action visible.
- **Setup-only examples or named multi-step setups.** Not taken in v1: hide an entire world, do not remove repeated when inputs, and overlap persona and storyline work.
- **Example inheritance.** Not taken in v1: introduces chains, cycles and additional hidden precedence without evidence that one typed instance plus overrides is insufficient.
- **Implicit fixture or type defaults now.** Not taken in v1: invented values become exact event assertions and may trigger validation unexpectedly. Production-model measurements must first establish the need and safe scope.
- **New ESM fixture references or partial matching.** Not taken: execution already expresses complete values, and changing matching would break the meaning of existing specifications.
- **Syntax consumers expand independently.** Not taken: duplicates name resolution and provenance rules and risks divergent interpretations.

## Default if unanswered

Specifications continue restating every instance. Incomplete exact-shape fixtures fail through the generic semantic-contract diagnostic, and syntax consumers cannot rely on a shared effective view.

## Timeline and scope

Applies to v1 of typed specification examples from acceptance until superseded. In scope: declarations, references, overrides, scoped resolution, current-generation validation, binding diagnostics, effective-syntax API, provenance, workspace references and rename, editors, samples and documentation.

Out of scope: named setups, scenario outlines, composite-value examples, examples in query results or `then result`, trigger or capture examples, runtime defaults, and any change to ESM contracts or matching. Named setups are reconsidered after persona and storyline work; defaults are reconsidered against measured production-model repetition.

## Verification

**Done when:** Examples parse, print and round-trip on both compiler surfaces; all supported step forms expand with scoped resolution and origin metadata; collisions, kind mismatches, duplicate assignments, historical-only fields and missing exact-shape properties fail precisely; equivalent authored and hand-expanded models have identical ESM bytes and revisions; existing goldens and corpus outcomes remain unchanged; MCP and editor views expose effective values, and workspace renames preserve references.

**Verify by:** Run parser, printer, syntax JSON, conformance, expansion, binder equivalence, golden, corpus, MCP, workspace rename and editor specifications, then the repository's .NET and TypeScript gates. Compare samples with Monaco's bundled Invoicing document. A downstream Stage follow-up adopts the effective-syntax API.

## Consequences

Authors declare a small cast once and state scenario-relevant differences explicitly. The ESM and reference runner need no fixture construct. Editing an example changes every referencing specification, so effective hover, fixture queries and reference edges make that impact inspectable. Syntax consumers must use the effective API to obtain complete steps; authored syntax deliberately keeps references.

## Technical clarification — 2026-10-07

Unused examples are not an admission escape hatch. Semantic binding validates every stated value with the ordinary fixture normalizer and semantic type validator, including scalar/null/UUID values, generated fixtures, nested object completeness and `for` identities. Top-level partial examples remain partial; the pass does not supply omitted properties, execute business validations, promote an ESM version merely because an unused fixture exists, or change design-mode fixture checks. An unavailable or ambiguous destination contract refuses admission. An override cannot hide an invalid example value.

Failure provenance belongs to the source compilation, not the ESM. A compilation-bound runner overload admits the compilation's own model and appends effective values and authored/example/override origins to failed comparisons. Callers cannot pair an arbitrary provenance sidecar with another plan. ESM-only execution retains existing failure messages and does not infer lost source origins. Failed capability admission remains a failed, unsupported result. The model's canonical bytes and revision remain identical to hand-expanded syntax.

## Status notes

**2026-10-07 — accepted.** Accepted by Sindre Alstad Wilting on 2026-10-07 after reviewing the amended text. Acceptance covers typed examples, inline overrides, no inheritance, no implicit defaults in v1, unchanged matching and the public effective-syntax API. Implementation and verification remain pending.
