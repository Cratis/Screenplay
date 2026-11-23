---
id: 0168
title: Compare explicit agent allowlist membership using bound operation identities
status: proposed
stage: none
decided: 2026-11-23
decider: Pending - records the comparison boundary for Cratis/Screenplay#1165; a maintainer accepts or amends it
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Comparison/**
  - Source/DotNET/Screenplay.Mcp/McpAgentExposureDiff.cs
  - Source/DotNET/Screenplay.Mcp/McpSemanticDiff.cs
  - Source/DotNET/Tool/DiffText.cs
  - Documentation/screenplay/model-comparison.md
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

[0053](0053-public-structural-model-comparison.md) freezes the default MCP structural comparison contract.
[0155](0155-explicit-agent-exposure-default-deny.md) establishes explicit exposure but excludes its comparison.
[Screenplay #1165](https://github.com/Cratis/Screenplay/issues/1165) also needs findings for newly exposed operations.
The semantic binder already records each allowlisted operation's resolved identity.

## Decision

Newly exposed means newly entering the explicit allowlist, not merely being declared.
Compare bound kind and operation identity when both catalogs are authoritative and belong to the same application.
Otherwise compare kind and canonical qualified address, without publishing IDs or inferring rename continuity.
Report additions, removals, scope widening/narrowing and description changes separately.
A write-to-read requirement widens token access; it does not establish effective authorization.

Require successful whole-model semantic compilation on both sides. Successful absence is known empty;
failed compilation is explicitly unavailable, with no guessed delta and an unknown exposure verdict.
Exposure completeness and verdict are independent of structural completeness and verdict.

Add a non-positional library result property and an additive CLI JSON `difference.agentExposure` field.
Keep default MCP response bytes unchanged. Opt-in proposal and revision views use `agent-exposure-diff/1`
and existing revision-pinned, byte-bounded paging and recovery admission.
Comparison findings, not a PLAY diagnostic code, satisfy the issue's diagnostic-specification criterion
for newly exposed operations: a history-dependent result is not a single-model compilation diagnostic.

## Options considered

- **Bound explicit membership (chosen):** uses the compiler's resolved references without a second classifier.
- **Syntax/reference-index fingerprints (rejected):** approximate navigation does not prove exposure or continuity.
- **Effective authorization or transitive capability comparison (excluded):** needs a separately designed contract;
  this result says nothing about handlers, dependencies, runtime equivalence or authorization effects.
- **Change default MCP shape (rejected):** violates 0053's frozen contract; opt-in views preserve existing consumers.

## Default if unanswered

Keep this record proposed with a pending decider. Existing structural consumers retain their contract;
maintainers review the explicit allowlist directly until accepting or amending this boundary.
The implementation request approves this bounded work but does not accept the ADR on a maintainer's behalf.

## Timeline and scope

This boundary holds for the #1165 implementation and until superseded. In scope: .NET comparison, CLI,
opt-in MCP reporting and documentation. Out of scope: language changes, PLAY codes, ESM/manifest bytes,
TypeScript comparison APIs, authorization, transitive dependencies, handlers and runtime equivalence.

## Verification

**Done when:** additions are individually reported, identity-preserving edits and reference aliases do not fabricate
exposure, invalid models remain unknown, and opt-in MCP views preserve revision/recovery and paging guarantees.

**Verify by:** focused comparison, CLI and MCP specs, frozen default MCP contracts, full CI-equivalent local gates
and unchanged canonical vectors. No integration or container suite is required by this bounded comparison change.
