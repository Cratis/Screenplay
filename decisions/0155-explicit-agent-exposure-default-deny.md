---
id: 0155
title: Keep explicit agent exposure default-deny and portable in ESM v48
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Agents/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

An agent must not infer permission to discover or call an operation merely because the model declares it.
An explicit allowlist describes the desired host contract without creating endpoints, issuing tokens or granting
application authorization. Published ESM versions are contracts:
[0066](0066-published-esm-versions-are-frozen.md), [0149](0149-screens-in-the-executable-model.md) and
[0150](0150-projection-parity-esm-v43.md) reserve the published v42/v43 contracts. Decisions 0151–0154 reserve
v44–v47 for state policies, text and membership conditions, policy inputs and query narrowing. Agent exposure
therefore takes the next version, v48, rather than extending any published baseline.

## Decision

An application-root `agents` block is the complete, default-deny allowlist of existing queries and commands.
Absent and explicitly empty blocks expose nothing. Exposure never substitutes for effective `authorize` clauses.
A model with a block selects language/semantic `48.0` and ESM `schemaVersion: 48`; models without one retain their
earlier version and canonical bytes. Only personal tokens are admitted. An exposed operation must resolve to an
existing executable operation identity; global queries and performed or graph-backed queries without a portable
identity are refused rather than partially exposed.

Comparing agent exposure between two models is **not** part of this decision. Existing comparison responses do
not report agent exposure, and no result of them is a verdict on whether exposure widened. A separate comparison
needs its own accepted decision; reviewers inspect the allowlist directly until then.

## Options considered

- **Explicit allowlist with a new ESM version (chosen).** A new domain declaration never silently becomes an agent
  capability, and older consumers reject v48 rather than discard the metadata.
- **Expose all declared operations by default.** Rejected: exposure would happen without an explicit decision.
- **Extend a published ESM version.** Rejected: published versions are frozen.

## Default if unanswered

Expose nothing. No runtime agent surface is created by this record.

## Timeline and scope

The contract applies to the v48 agent metadata admission, compiler and editor authoring support, and
documentation. Existing versions, goldens and schemas remain frozen. Token issuance, OAuth implementation,
approval workflows, generated endpoints, Arc hosting, live agent execution and exposure comparison are out of
scope. This is a desired host contract, not a shipped runtime implementation.

## Verification

**Done when:** default-deny admission and v48 round trips are pinned; invalid exposure is rejected with focused
specs; the generated v48 artifacts leave v44–v47 and all older published artifacts unchanged.

## Consequences

The portable allowlist remains separate from application policy and runtime endpoint/token implementation.
Changes to exposure between models are reviewed manually until a comparison decision is accepted.
