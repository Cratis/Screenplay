---
id: 0155
title: Keep explicit agent exposure default-deny and compare it separately with fail-closed coverage
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Agents/**
  - Source/DotNET/Screenplay/Comparison/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Tool/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

An agent must not infer permission to discover or call an operation merely because the model declares it.
An explicit allowlist describes the desired host contract without creating endpoints, issuing tokens or granting
application authorization. A comparison that silently loses an invalid block can incorrectly describe an exposure
change as safe. At the same time, existing comparison responses and published ESM versions are contracts.

[0053](0053-public-structural-model-comparison.md) keeps comparison structural and typed, and
[0094](0094-report-enum-numeric-identity-changes-in-model-comparison.md) adds comparison detail without replacing
existing transport layouts. [0066](0066-published-esm-versions-are-frozen.md),
[0149](0149-screens-in-the-executable-model.md) and [0150](0150-projection-parity-esm-v43.md) reserve the published
v42/v43 contracts. Decisions 0151–0154 reserve v44–v47 for state policies, text and membership conditions,
policy inputs and query narrowing. Agent exposure therefore takes the next version, v48, rather than extending
any published baseline.

## Decision

An application-root `agents` block is the complete, default-deny allowlist of existing queries and commands.
Absent and explicitly empty blocks expose nothing. Exposure never substitutes for effective `authorize` clauses.
A separate typed exposure comparison and opt-in MCP `agent-exposure` view classify allowlist changes with
fail-closed coverage, while legacy structural views explicitly disclose that they do not compare exposure.

### Structural classification

Rows match by operation kind and resolved qualified name and have deterministic kind/name order. Bare and qualified
references to the same target match. A newly exposed target is `Added`; a removed target is `Removed`. Higher scope
in the declared `read < write` order is `Widened`; lower scope alone is `Narrowed`. A changed exposed operation
signature or referenced contract is `Widened`, even when scope is unchanged. Changed descriptions with the same
resolved target and exposure contract are metadata-only `Unchanged` rows. These classifications are review signals,
not claims about runtime behavior or an authorization lattice.

Loosening accepted tokens must never appear unchanged or narrowed. An admitted change of token contract is
conservatively widened. Today only personal tokens are admitted: an attempt to add OAuth or another unsupported
kind is a validation error and follows the Unknown rule below, not a partially compared token surface.
Added and widened exposure use the comparison warning codes `PLAY0978` and `PLAY0979` respectively.

### Source errors are distinct from executable-binding refusal

For exposure comparison, **compile errors** mean error-severity diagnostics from parsing and validation of the
authoring source. This is the stage at which an `agents` block, reference, scope or token declaration could be lost,
misread or rejected. If either side has such errors, every available exposure row is `Unknown`, coverage is
`Incomplete`, and `HasWidening` is null. This also applies when neither side retains a block or any rows: an empty
result with incomplete coverage is never a safe no-widening conclusion. Unknown is reserved for these source
errors, not warnings or an unresolved referenced contract in otherwise valid authoring source. Without source
errors, an exposed contract change is Widened (including at identical scope); a contract that cannot be uniquely
resolved is conservatively Widened for review, never Unknown or a safe unchanged result.

Executable binding is a later, separate stage. A preview construct such as `system`/`operation` can parse and
validate with no diagnostics, then be refused by binding with `PLAY0268` and `executable*Available=false`.
That refusal alone does **not** trigger the source-error rule: the allowlist remains intact in authoring syntax,
which is what this comparison reads. Binding refusal without an agents block therefore adds no exposure limit and
preserves the frozen incomplete-comparison response byte-for-byte. With an intact block, exposure coverage is
computed from its resolved authoring contracts independently of executable availability. This distinction avoids
both permissive fallback after source loss and unnecessary loss of reliable syntax evidence.

### Legacy compatibility and explicit opt-in

Legacy CLI diff, MCP `semantic-diff` and `read-proposal` structural responses retain their existing shapes. They do
not insert exposure rows. They append exactly one stable limit statement when either side has an agents block
(including empty), or either side has parsing/validation errors even if a block disappeared:

> Agent exposure was not compared; use the agent-exposure view.

Clean sources without agents add nothing, including sources whose executable binding alone refuses.
`ModelDifference.AgentExposure` exposes the typed aggregate separately and is excluded from legacy JSON
serialization. MCP callers opt into `view: "agent-exposure"` with `version: 2` on `semantic-diff` or `read-proposal`.
Every page must request version 2 and answers `version: 2`, the source revision pin and coverage, including empty,
incomplete and byte-bounded pages. Missing or mismatched versions refuse; clients must check requested version equals
answered version on every page and must not fall back to legacy completeness. This view uses the existing row and
serialized-byte page bounds, revision-pinned continuation, root containment and pending-recovery guards. It does not
add a tool, relax source admission or change writes.

## Options considered

- **Explicit allowlist, typed comparison and opt-in transport (chosen).** Makes widening reviewable without
  redefining legacy rows or treating missing data as safe.
- **Expose all declared operations by default.** Rejected: a new domain declaration would silently become an agent
  capability without an explicit exposure decision.
- **Add exposure rows to default structural JSON.** Rejected: changes frozen CLI/MCP bytes and mixes separate coverage.
- **Treat every executable-binding refusal as source failure.** Rejected: intact preview syntax remains reliable
  evidence and existing error-side transport contracts must remain unchanged.
- **Compare only the valid subset after parsing/validation errors.** Rejected: a missing or misread entry could
  conceal widened exposure, and an empty row set would incorrectly look safe.

## Default if unanswered

Keep existing comparison bytes and do not infer safe exposure from incomplete source. Without a separate explicit
exposure result, reviewers must inspect the allowlist manually; legacy structural completeness is not an exposure
verdict. No runtime agent surface is created by this record.

## Timeline and scope

The contract applies to the v48 agent metadata admission and its structural review surfaces. Existing versions,
goldens and schemas remain frozen. Scope includes C# typed comparison, MCP opt-in transport, legacy limit disclosure,
compiler/editor authoring support and documentation. TypeScript authoring validates the block but has no model
comparison API to extend.

Token issuance, OAuth implementation, approval workflows, generated endpoints, Arc hosting and live agent execution
are out of scope. This is a desired host contract, not a shipped runtime implementation.

## Verification

**Done when:** default-deny admission and v48 round trips are pinned; added/removed, scope, signature and description
changes have focused specs; source errors on either side make every row Unknown with incomplete coverage; preview
binding refusal alone preserves the existing incomplete-response golden (`with_incomplete_json_contract.cs`);
opt-in pages are bounded, deterministic, version-checked and revision-pinned with coverage on every page;
root/recovery protections remain enforced; and existing no-agent transport/golden bytes are unchanged.

**Verify by:** focused native agent/comparison/serialization specs, MCP semantic/revision/proposal comparison specs
including frozen JSON contracts, native-generated TypeScript transport checks and TypeScript compiler/editor tests,
then the repository's Release solution build and required lint/non-integration test matrix. Generation of the new
v48 artifacts must leave v44–v47 and all older published artifacts unchanged.

## Consequences

Reviewers can distinguish known widening, known narrowing and unknown exposure without relying on executable binding.
Legacy callers must opt in for exposure rows and heed the explicit exclusion statement. New token kinds or richer
contracts must extend the comparison deliberately; unsupported source is never silently admitted as an empty surface.
The portable allowlist remains separate from application policy and runtime endpoint/token implementation.
