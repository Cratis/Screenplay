---
id: 0095
title: Compare declared event relationships with a host-supplied observation snapshot
status: proposed
stage: none
decider: Sindre Wilting (delegated to the L0 coordinator)
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Observations/**
  - Documentation/screenplay/observation-comparison.md
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

[Screenplay#294](https://github.com/Cratis/Screenplay/issues/294) asks whether a running namespace holds the event
relationships a model declares. Structural model comparison does not answer that: observations have bounded coverage,
counts, attribution and subscription progress, not another set of authored declarations. A pinned rename can preserve
its portable contract identity while a rendered application registers the wrong stored name. A producer can append an
event the model never says it produces. Both the CLI and Chronicle MCP need the same findings without duplicating rules.

## Decision

Provide a pure `Cratis.Screenplay.Observations` library in Screenplay over an immutable, host-supplied snapshot of one
store, namespace and sequence. Screenplay compares the successful semantic compilation with that evidence and optional
identity overrides; hosts own IO, store-specific causation decoding and render-target identity conventions. This adds no
language construct, ESM member, evaluator behavior, editor feature or MCP tool, respecting [0092](0092-the-language-is-frozen-at-esm-v28.md).

The proposed contract is:

1. `ObservationSnapshot.Create` sorts registrations, appends, attributed event counts, unattributed counts and consumers.
   It refuses duplicate keys, negative or overflowing totals, inconsistent ranges and attribution exceeding appends.
   Coverage records the captured tail, inclusive examined bounds, skipped ranges and optional informational times.
   No payload, event source identity or causation properties enter the snapshot.
2. `ObservationIdentityMap.FromModel` maps events by `StoredName ?? Name`, never by `ContractId`, following
   [0075](0075-carry-the-stored-event-type-name-in-the-executable-model.md). Current revision means generation; declared
   prior revisions remain recognized. Commands map by unique name. Duplicate names are ambiguous, not guessed.
   Reactions remain unmapped until the caller supplies producer and consumer identifiers with immutable `With` overrides.
3. `ObservationComparison.Compare` returns an `ObservationReport` containing typed findings, coverage, per-slice
   evidence and explicit limits. Known disagreement is `Drift`, bounded non-drift evidence is `Informational`,
   insufficient evidence is `Unknown`, and foreign contracts or platform consumers are `Excluded`.
4. `HasDrift` is true for any known drift, false only with complete coverage and no Unknown findings, and null otherwise.
   `Complete` requires both full scan coverage and no Unknown findings. Empty sequences count as fully examined.
   Partial coverage weakens absence to Unknown, not presence. A missing event registration is still drift because the
   registry is supplied whole. Undeclared appended events are drift; undeclared registrations alone are informational.
5. A missing declared edge is drift only when full coverage shows that an unambiguously mapped producer appended other
   events, the expected type has no unattributed appends, and production is unconditional. A described command handler,
   non-event or conditional reaction trigger, opaque requirement, missing mapping or lagging consumer gives a typed
   Unknown instead. No attributed appends gives `NoMatchingAppendsWithinCoverage`, never proof that a producer never ran.
6. Event-triggered reactions have type-level application-owned subscription checks without identity mapping. Explicitly
   mapped consumers additionally compare their subscriptions and progress through the captured tail. Projection and
   reducer consumer semantics remain Unknown `ConsumerKindNotCompared`; platform ownership is excluded, never guessed.
7. Counts always mean events, not invocations. Owning slices hold append counts; producing slices show only attributed
   edge counts. Report totals come once from the snapshot, never from summing slices.
8. Findings follow model position, finding kind and ordinal observed identities; undeclared evidence comes last.
   `ObservationReportJson` owns the deterministic result encoding: camelCase fields, string enums, fixed field order,
   null omission and identical human statements. `NotEstablished` is the finding kind for Unknown evidence without a
   comparable declaration or edge. A null verdict is omitted, never written as false.

## Options considered

- **Put comparison in each adapter.** Rejected: the CLI and Chronicle MCP would disagree about absence, coverage and
  drift, and every consumer would need its own interpretation of the model.
- **Read Chronicle from Screenplay.** Rejected: introduces IO and a Chronicle dependency into the compiler package,
  prevents synthetic comparison tests and imposes one render target's names on every host.
- **Generate an observed `.play` and reuse structural comparison.** Rejected: loses counts, attribution, progress and
  the distinction between a missing declaration and missing evidence.
- **Assume missing evidence means no drift.** Rejected: an incomplete scan or unmapped reaction would silently pass.
- **Compare projection and reducer consumers now.** Deferred: their platform ownership and registration semantics are
  not established; matching names alone risks false drift.

## Default if unanswered

No shared comparison contract is established. The CLI and Chronicle MCP either remain blocked or implement independent
rules, with inconsistent findings and exit-code decisions. Existing structural comparison remains available but cannot
establish relationships with observed appends.

## Timeline and scope

The proposal covers the first library slice needed by [cli#200](https://github.com/Cratis/cli/issues/200) and
[Chronicle.Mcp#41](https://github.com/Cratis/Chronicle.Mcp/issues/41), within the ESM v28 freeze, and holds until superseded
if accepted. Acceptance belongs to the named decider; this record remains proposed.

In scope: immutable neutral snapshots, mapping overrides, event/generation/producer/reaction-consumer findings, explicit
coverage and limits, per-slice views, result JSON, specs and documentation.
Out of scope: Chronicle reads or mutations, payload/schema agreement, specification execution, multiple sequences in one
snapshot, projection/reducer consumer comparison, default target-specific reactor IDs, snapshot JSON, a CLI verb, MCP
exposure, TypeScript twins and generated applications. A CLI `conform` command already covers model-versus-code comparison;
choosing a distinct verb for observations belongs to the CLI, not this library.

## Verification

**Done when** synthetic snapshots prove every documented finding and Unknown boundary, partial coverage preserves known
drift without inventing absence, stored-name pins and prior generations match correctly, shared events are counted once,
reordered inputs give identical output, composed folder models compare, and reviewed result JSON contains no event
contents while preserving the typed statements and verdicts.

**Verify by** a clean Release build of `Source/DotNET/Screenplay/Screenplay.csproj`, the `Observations/for_*` specifications
and existing `Comparison/for_ModelComparison` regressions in Debug, and `Observations/for_ObservationReportJson` against
its reviewed real-output golden. Adapter integration is verified separately in the consuming repositories.

## Consequences

Adapters can share typed findings and JSON without sharing a Chronicle client. Hosts must capture honest bounds,
registrations and attribution, and provide generated producer/consumer identities instead of guessing them. Unknown
findings are expected where those identities or progress are unavailable; this is not behavioral conformance or release
approval. A consumer cannot treat a false or null-free JSON default as a successful complete comparison.

Cross-repository work remains with the owners: CLI snapshot acquisition and command naming; Chronicle MCP packaging;
Stage/Arc publication of generated identity mappings; and a Cratis/AI skill update describing evidence limits and the
library API. No managed skill is edited in Screenplay.
