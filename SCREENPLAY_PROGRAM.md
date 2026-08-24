<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Screenplay delivery program

This program implements [the Screenplay architecture](./SCREENPLAY_ARCHITECTURE.md) in independently releasable increments.

## North star and Program v1 boundary

One serialized semantic model and one specification corpus must produce equivalent observable backend outcomes through:

- the Screenplay reference evaluator;
- direct Stage execution;
- a Cratis-rendered backend;
- the named second target: a TypeScript/Node.js backend runtime and renderer.

For every admitted recovery capability, rendering that model to a supported target and recovering the generated result must preserve the same semantic identities, contracts, and observable specification outcomes. Hand-written code may contribute only evidence and explicitly reviewed proposals; it never becomes semantic authority by being discovered.

Code is an artifact. Unsupported behavior fails closed. No product owns a competing semantic model. Program v1 is backend-only: existing Screenplay UI and Stage Scene compatibility is preserved, while frontend and deployment profiles remain excluded until separately approved.

Before the first public milestone, publish and freeze the Program v1 capability set, language version, semantic version, compatibility matrix, report-envelope version, and artifact-manifest version. Later additions require the semantic admission rule and a new capability/version decision.

## Increment 0 — parallel closeout lane

Increment 0 records and closes already bounded work in parallel. It is **not** a semantic-kernel prerequisite, and Increment 1 must not depend on its completion.

- Screenplay PR #123 and PR #124 are completed.
- Screenplay #69 is retained; #130 is closed and must not replace #69.
- Stage PR #51 is closed as superseded rather than merged as a duplicate model expansion.
- Existing reconciled Stage, Studio, and Prologue work remains authoritative input to successor issues; it is not reopened or duplicated.

This lane may finish before, during, or after Increment 1 without changing ESM semantics.

## Increment 1 — minimum semantic kernel and evaluator

Screenplay releases the minimum ESM walking skeleton:

- explicit language and semantic versions;
- immutable ESM v1, canonical serialization, and deterministic revision;
- `ApplicationSyntax → ESM` binding without silently changing legacy meaning;
- one-file/document-set compilation with deterministic document ordering;
- Screenplay document identity, source spans, source/semantic maps, and diagnostics;
- stable semantic IDs with documented provisional-ID bootstrap limits;
- minimum portable execution-plan contracts;
- a deterministic in-memory evaluator with injected clock and ID allocation;
- compatibility-preserving syntax, visitor, UI, and Scene-facing APIs.

The initial document-set compiler does not claim full CST/trivia preservation, transactional workspace edits, durable Studio authoring IDs, or byte-identical workspace round trips. Those belong to Increment 7.

### Minimum event contract

ESM v1 includes:

- a stable event-contract ID;
- initial revision `1`;
- deterministic derivation for a legacy event declaration;
- persistence of that derived mapping during explicit migration;
- preservation of the contract ID across type/display rename.

Add vectors for new identity, legacy derivation, rename preservation, and ambiguous-bootstrap rejection. Full predecessor lineage, transformations, and later revisions remain in existing Screenplay #71; Increment 1 does not duplicate that evolution work.

### Initial behavioral capability and shared vector

The minimum vertical capability is:

- module, feature, and slice;
- concepts and composite types;
- one command with declarative validation;
- one produced event;
- one projection into a read model with an affected-instance key;
- one deterministic query;
- executable success and rejection specifications.

Use one portable fixture end to end: a valid `RegisterProject` command produces `ProjectRegistered`, projects `ProjectSummary`, and makes `ProjectById` return it; an invalid name is rejected, appends no event, changes no projection, and leaves the query empty. The same vector format is reused by the evaluator, Stage, and rendered targets as those surfaces become required. Increment 1 passes it in the reference evaluator; complete Stage parity remains Increment 6.

### Compatibility gate

Before Increment 1 closes:

- every current `ApplicationSyntax` construct is listed in [`SCREENPLAY_SYNTAX_DISPOSITIONS.md`](./SCREENPLAY_SYNTAX_DISPOSITIONS.md);
- each row is `bind`, `preserve legacy`, `report-only`, `block`, `migrate`, or `defer`;
- existing `reads` keeps its legacy semantics unless the model opts into or migrates to the new semantic version;
- existing `ConcurrencySyntax` is classified as preserved legacy/report-only realization metadata, not as implicit decision consistency;
- no current syntax is silently dropped, strengthened, or reinterpreted.

## Increment 2 — Stage `ArtifactRenderPlan` and Cratis planner

Stage issue #56 delivers:

- additive application/module/feature/slice artifact-planning requests;
- additive `IArtifactRenderPlanner`;
- immutable semantic snapshot, execution plan, and profile inputs;
- pure in-memory `ArtifactRenderPlan` output;
- typed capability diagnostics;
- deterministic Cratis backend scaffold and artifacts;
- generated specifications;
- artifact paths, hashes, schema version, and renderer identity.

`IArtifactRenderPlanner` performs no file, process, network, clock, or ambient-environment I/O. The name does not collide with the existing Scene `RenderPlan`.

Existing `IRenderer` remains source/binary compatible through an adapter. Deprecation starts only after equivalent behavior and tests exist, and removal follows normal major-version policy. Compilation failure or unsupported semantics produces no write plan.

## Increment 3 — crash-recoverable root `cratis render`

CLI issue #101 releases:

```text
cratis render [PATH]
  --target cratis
  --destination ./out
  --name MyApplication
  --force
```

Rules:

- one folder is one logical Screenplay application;
- renderer targets come from a static reviewed target roster;
- source adapters come from a separate static reviewed source roster;
- plan fully before writing;
- reject absolute, traversing, duplicate, and case-colliding artifacts;
- protect unmanaged and user-modified files;
- remove only unchanged stale managed files;
- stage files and persist a journal containing intended operations and prior manifest state;
- resume or roll back deterministically after a crash;
- publish `.cratis-render.json` last;
- emit deterministic text/JSON envelopes and artifact bytes.

The contract is staged, journaled, crash-recoverable publication—not absolute multi-file filesystem atomicity. Manifest and identity-schema upgrades require explicit migration vectors, including interrupted-upgrade recovery.

The generated backend builds and its shared success/rejection specifications pass. This is the first public Screenplay vertical milestone.

## Increment 4 — rendering completeness, then semantic waves

Complete **existing portable syntax rendering** first. Every construct classified `bind` in the compatibility matrix must render or produce a justified blocking diagnostic before any newly admitted semantics are used to claim renderer completeness.

After that gate, deliver separate waves with reference and Cratis conformance vectors:

1. complete query selection, ordering, paging, live semantics, and query specifications;
2. constrained implementation contracts and C#/TypeScript/SQL virtual-document tooling;
3. portable policy evaluation and caller specifications;
4. opted-in decision consistency and multi-read aliases;
5. view/todo-driven automation and business due time;
6. affected-view identity and zero/one/many cardinality;
7. data-subject lineage/capability semantics, followed only by separately modeled erasure/export behavior;
8. full portable event-contract evolution under Screenplay #71;
9. complete specification context: source, event source, compliance subject, caller, execution scope, keys, clocks, IDs, outcomes, effects, and ordering;
10. independently reusable PDL/CDL conformance.

The constrained-implementation wave is delivered role by role, starting from one real rendering gap rather than designing every possible language and body at once. Its first release includes the common requirement/attachment envelope, one role-specific context/result contract, one target provider, virtual-document diagnostics, blocking planner gaps, and accepted-attachment rendering. Additional roles and languages reuse the envelope only after a concrete product need. AI candidate generation remains optional orchestration outside the pure planner; revision-checked acceptance joins the Studio/AI work in Increment 7.

PDL and CDL parsers/compilers remain usable without constructing the full ESM. The ESM may reference their versioned portable compiled plans. Workbench adoption is explicitly gated on standalone package conformance, existing API compatibility, and shared policy/constraint vectors.

A capability closes only when all ledger cells declared `Required` pass. Studio and AI cells are not automatically required for these early waves; they become required only when the capability issue explicitly declares them.

## Increment 5 — source recovery and atomic adapters

Generation issues #24–#26 own the current source-identity, evidence, admission/derivation, atomic-adapter, and composable-report delivery lane. Its release must provide:

1. bounded authored-source/compiler helpers;
2. descriptors, probes, frozen admission, and explicit dispositions;
3. granular facts and fixed-snapshot derivation lineage aligned with the ESM boundary;
4. neutral validation and specification facts;
5. shared root/feature/module placement derivation;
6. lockstep release of affected Generation packages;
7. downstream atomic adapters and compatibility-facade parity;
8. deterministic Generation-owned realization-report fragments;
9. shared render → recover conformance vectors for every admitted bidirectional capability.

Source adapter and renderer target rosters remain separate. Vogen is independent, executes at most once, belongs only to the source adapter roster, and has no renderer-target or Critter/Arc ownership.

## Increment 6 — Stage execution parity

Build and freeze reference vectors first. Stage then passes the required shared vectors for:

- validation and requirements;
- policy/authorization semantics tracked by Stage #57;
- decision inputs and consistency;
- event destinations and atomic commit outcomes;
- real query semantics tracked by Stage #58;
- projections and reducers;
- reactions and translations;
- logical time and external triggers;
- compliance subjects and separately admitted compliance behaviors;
- behavioral specifications.

Stage retires its narrow competing `EventModel` through an explicit compatibility path. It never silently accepts semantics it does not perform. Existing Stage Scene `RenderPlan` and reconciled runtime work remain intact.

## Increment 7 — full workspace, Studio, Prologue, and AI

Screenplay adds the later workspace capabilities deliberately excluded from Increment 1:

- full-fidelity CST and trivia;
- immutable original bytes;
- semantic/source/workspace losslessness levels;
- durable document, semantic, and authoring identity mappings;
- semantic snapshots and revision hashes;
- typed transactional patches and conflicts;
- deterministic flatten/expand/write plans;
- compiler-authored repairs.

StudioIssues #260 and #261 integrate the existing import/export, loss-warning, editing, and AI-job work with Screenplay identities and typed patches. Studio preserves valid semantics it cannot visualize, stores canvas layout separately, and previews changes before applying them transactionally.

Prologue #22 preserves existing candidate/evidence/review history, keeps candidate identity separate, materializes only accepted or corrected candidates, and emits revision-bound semantic patch proposals.

No reconciled Studio or Prologue capability is replaced by a parallel implementation.

## Increment 8 — named portability proof

Implement the named second target: a **TypeScript/Node.js backend runtime and renderer**. For the frozen portability subset, the same ESM and specification corpus must produce equivalent normalized outcomes through:

- the reference evaluator;
- Stage runtime;
- the Cratis renderer/backend;
- the TypeScript/Node.js target.

Frontend and deployment generation remain excluded. Either requires a separate profile decision, security/capability review, ledger scope, and milestone.

## Composable report ownership

The deterministic realization report is assembled from versioned fragments:

- Screenplay anchors semantic identity/revision, language/semantic versions, required capabilities, and specification hashes;
- Generation contributes source identity, evidence, provenance, derivation, admission, uncertainty, and conflicts;
- Stage and each renderer contribute capability dispositions, renderer identity, artifact findings, hashes, and diagnostics;
- CLI owns the envelope, selected source/target rosters, serialization, safe paths, publication journal result, and deterministic assembly.

No fragment may change ESM meaning, and no producer may overwrite another producer's owned fields.

## Current GitHub operation map

Use these current operations; superseded numbers from earlier drafts are not program authority:

| Repository   | Current operation   | Program use/status                              |
| ------------ | ------------------- | ----------------------------------------------- |
| Screenplay   | #69                 | Retained                                        |
| Screenplay   | #130                | Closed; does not replace #69                    |
| Screenplay   | #135–#142           | Current semantic delivery issue set             |
| Screenplay   | #146                | Release-complete documentation and grammar      |
| Screenplay   | #148                | Bidirectional render → recover fidelity          |
| Screenplay   | #71                 | Existing owner of full event-contract evolution |
| Screenplay   | PR #123 and PR #124 | Completed                                       |
| Generation   | #24–#26             | Current source-recovery/adapter/report lane     |
| Stage        | #56                 | `ArtifactRenderPlan` child                      |
| Stage        | #57                 | Policy child                                    |
| Stage        | #58                 | Query child                                     |
| Stage        | PR #51              | Closed as superseded                            |
| CLI          | #101                | Root render and safe artifact publication       |
| Prologue     | #22                 | Candidate-to-semantic-patch integration         |
| StudioIssues | #260 and #261       | Workspace/identity/patch integration            |

When an issue or PR is closed as duplicate, replaced, or superseded, its closing comment must link the exact successor issue or PR, and the successor body must link back to the exact replaced item. A broad epic, repository name, or unlinked issue number is not sufficient. This document records the discipline but does not authorize GitHub edits.

## Conformance ledger

Maintain one matrix per semantic capability:

| Capability | Reference evaluator | Cratis rendered | Stage executed | TypeScript/Node.js | Studio round-trip | AI patchable |
| ---------- | ------------------- | --------------- | -------------- | ------------------ | ----------------- | ------------ |

Every cell contains exactly one state:

- `Required` — in scope but not yet demonstrated;
- `Passed` — required evidence is recorded and green;
- `Deferred` — named future milestone and owner are recorded;
- `N/A` — a rationale shows why the surface cannot apply;
- `Blocked` — required work cannot proceed and the blocker is linked.

A capability cannot close with `Required` or `Blocked` cells. `Deferred` and `N/A` are explicit, never blank. Studio and AI default to `Deferred` or `N/A` in early backend increments unless the owning capability declares them `Required`.

## First public milestone stop condition

Stop and release the first public vertical milestone when:

- the Program v1 capability set, language version, and semantic version are frozen;
- every existing `ApplicationSyntax` construct is classified in the compatibility disposition matrix;
- every ledger cell is explicit, including deferred and N/A Studio/AI, Stage-parity, and second-target cells;
- released Screenplay semantic-kernel packages include the deterministic evaluator;
- the shared command → event → projection → query success/rejection vector passes in the evaluator and generated Cratis backend;
- event-contract ID, initial revision, legacy derivation, rename preservation, and ambiguous-bootstrap vectors pass;
- released Stage #56 `ArtifactRenderPlan`/Cratis planner consumes the kernel without changing Scene `RenderPlan`;
- released CLI #101 exposes safe root `cratis render`;
- file and folder forms render identically;
- the generated backend builds and generated specifications pass;
- rerender is byte-identical for unchanged inputs and protects user files;
- staged publication, crash recovery, identity migration, and artifact-manifest migration vectors pass;
- unsupported syntax blocks with typed diagnostics;
- the TypeScript/Node.js second target is named and explicitly deferred to Increment 8;
- frontend and deployment profiles are explicitly excluded.

Do not delay this milestone for complete Stage runtime, full workspace/CST, Studio, AI, source recovery, the delivered second target, frontend, or deployment generation.

## Program completion condition

The program is complete when:

- the frozen Program v1 ESM, workspace, identity, and artifact architecture is released;
- all existing syntax has a final bind/preserve/report/block/migrate/defer disposition;
- all required identity and artifact migrations have executable bootstrap, rename, upgrade, interruption, and ambiguity vectors;
- admitted semantic capabilities pass their shared behavioral vectors on every ledger surface marked `Required`;
- Cratis rendering and Stage execution pass the required shared corpus;
- `cratis render` is publicly verified with crash-recovery evidence;
- source recovery is released with independent Vogen handling, composable report evidence, and render → recover semantic conformance for admitted capabilities;
- standalone PDL/CDL and the explicit Workbench gate pass;
- Studio performs its declared lossless round trips using separate document, semantic, and authoring IDs;
- Prologue and AI use reviewable revision-bound patches without conflating candidate identity;
- the TypeScript/Node.js backend proves portability;
- every `Deferred` or `N/A` ledger cell has its explicit owner/rationale and no `Required` or `Blocked` cell remains;
- all program issues are closed or assigned to a named future milestone using exact replacement links;
- frontend and deployment remain explicit exclusions unless separately approved;
- final cross-repository handover records versions, hashes, gates, limitations, migrations, and continuation points.

At that point, stop. New syntax, frontend/deployment generation, or framework breadth requires a new product decision and must pass the semantic admission rule.
