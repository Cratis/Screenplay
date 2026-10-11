---
id: 0067
title: Report every explicitly selected event property evolution in one generations array
status: accepted
stage: none
decided: 2026-10-11
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Screenplay.Contracts/**
  - Documentation/screenplay/mcp/**
  - Documentation/screenplay/events.md
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

[0044](0044-evolve-event-properties-through-mcp.md) allows explicitly selected properties throughout a resolved mapping chain. One command input can feed two event contracts, and `alsoRename` or `alsoRemove` can select properties on both. Their starting generations can differ. The singular `property.generation` result described in 0044 cannot disclose every selected event-contract change.

Released Screenplay v4.138.0, at `a3ab319087614f918f51b162edb57dca6946dcd7`, contains the property-reference index but no `propose-property` implementation or singular generation result. This is a correction to the unimplemented result design, not a migration of a shipped tool. No independent audit of external implementations established universal third-party compatibility.

`WorkspaceTransactions.EventRevisionAdvancements` is transaction input, not a public report. It can carry several advancements but does not report the previous revision or an in-place never-persisted edit. Reusing that input shape as the result would broaden the transaction contract unnecessarily.

Sindre Alstad Wilting approved the researched clarification on 2026-10-11: report every selected event evolution, preserve explicit selection, and derive the report and transaction inputs from one validated plan.

## Decision

`propose-property` reports event-contract edits in one required `property.generations` array. It has no singular `property.generation` alias. Every entry contains the canonical EventContract address as `event`, that event's own `from` and `to` revisions, and the explicit `mode`, either `newGeneration` or `eventNeverPersisted`. Entries are deduplicated by the owning selected event contract and stably ordered with `WorkspaceSemanticAddressComparer`.

### Amendment to 0044

This record replaces only 0044's singular generation-report shape and clarifies its selection rule. 0044 stays accepted; its original decision text is preserved with a dated pointer to this amendment. Its revision checks, Safe reference policy, retained-proposal review, journaled apply, atomic refusal, historical-generation immutability and identity guarantees remain unchanged.

- No accepted planned event-contract edits produce `generations: []`. An empty array is not evidence that the overall proposal succeeded or that requested edits were absent.
- `newGeneration` reports each event's current revision and exactly one succeeding revision. Two selected properties on the same event produce one evolution entry and one advancement, not two generations.
- `eventNeverPersisted` reports each edited event with equal `from` and `to` revisions. In-place editing is visible rather than looking like no contract change.
- Two events with different starting revisions retain their own revision pairs; no shared revision number is inferred.
- Only the target and explicitly selected `alsoRename` or `alsoRemove` members are edited. Membership of the mapping chain does not authorize extra edits. Renaming a command input rewrites its mappings without automatically renaming the connected event properties.
- The two existing flags choose one mode for the explicitly selected event properties. They do not select additional events. A mixed per-event mode request is outside this contract.
- The public report and internal advancement inputs are derived from the same validated plan. The existing transaction machinery is reused; this amendment introduces no transaction redesign or duplicate public advancement report.

The MCP result, retained `read-proposal` view and public C# property report carry the same ordered array. The report uses existing canonical address objects, not display-name strings. Request-schema goldens do not replace result-JSON specifications.

## Options considered

- **One required ordered array, correcting the unshipped design (chosen).** It covers zero, one and several selected events without concealing changes or inventing compatibility behavior for an unpublished result.
- **Keep a singular alias, null for several events.** Rejected: null can be mistaken for no event evolution, and no released singular implementation was found that requires the alias.
- **Report only the first selected event.** Rejected: it hides other requested stored-contract changes.
- **Refuse requests selecting several events.** Rejected: it unnecessarily narrows 0044's accepted mapping-chain scope to fit a report shape.
- **Expose transaction advancements as the result.** Rejected: the input lacks the previous revision and never-persisted mode; changing it for presentation couples reporting to transaction execution.
- **Add per-event mode inputs.** Rejected for this amendment: fixing report cardinality does not require another request concept or a mixed-mode contract.

## Default if unanswered

Implementation of 0044's planner remains blocked because one result cannot safely disclose several valid selected event evolutions. Selecting or dropping an event to fit the singular result is not an acceptable default.

## Timeline and scope

In force from acceptance on 2026-10-11 until superseded. It applies before the first publication of `propose-property`, across 0044's rename, remove and add increments.

In scope: event-report cardinality, explicit modes and revision pairs, ordering, event-owner deduplication, selected-set intent, retained report projections and their verification.

Out of scope: changing the event-evolution choices, implicitly selecting chain members, mixed per-event modes, transaction redesign, the transformation construct, grammar or ESM changes, and new Studio or TypeScript compiler consumers.

## Verification

**Done when:** the C# property report, MCP proposal result and retained report use only the required ordered `generations` array; all accepted selected event edits are disclosed; report and advancement inputs agree; and 0044 links to this amendment without rewriting its original decision text.

**Verify by:** result-JSON snapshots and workspace/MCP specifications for zero, one and two events with unequal starting generations; several selected properties on one event; reversed selection order; never-persisted `from == to`; historical refusal; atomic failure; and retained `read-proposal`/`apply` identity continuity. Confirm that an unselected connected event property is not renamed or removed. Regenerate the machine-readable tool contract under 0039 and separately verify result JSON.

## Consequences

Every selected event-contract change is visible in one report, including in-place changes. Callers need only one cardinality rule. The new planner does not inherit a misleading singular compatibility alias. Explicit selection and one validated plan continue to govern both reporting and mutation.

Tracked in [Screenplay#389](https://github.com/Cratis/Screenplay/issues/389); corpus adaptation is tracked in [Cratis/AI#597](https://github.com/Cratis/AI/issues/597).
