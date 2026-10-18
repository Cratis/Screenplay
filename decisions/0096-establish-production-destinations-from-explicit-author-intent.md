---
id: 0096
title: Establish production destinations from explicit author intent
status: accepted
stage: none
decided: 2026-10-18
decider: Sindre Wilting (delegated to the L0 coordinator)
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

PLAY0470 requires explicit destinations across a command's event productions when defaults are mixed.
Editing one production at a time can affect sibling defaults. Unlike PLAY0478's preservation-proven repair,
this model has no executable baseline from which to prove routing preservation. A valid candidate establishes
routing the author selected; binding alone proves neither business correctness nor target execution support.

L0 accepted this contract on 2026-10-18 after independent review of the original draft and its attachment/write-overlap revision, authorizing implementation of Screenplay #397. It complements [0014](0014-diagnostic-repairs-are-typed-workspace-proposals.md)
and preserves [0092](0092-the-language-is-frozen-at-esm-v28.md). Decision 0095, accepted in the independent
observation-comparison work (PR #903), does not change this repair contract.

## Decision

Provide a compiler-owned, intent-parameterized PLAY0470 repair through the existing typed workspace/MCP
proposal contract. It fills only missing `for` expressions, validates the whole workspace, retains a routing
report for review, and writes only on explicit application of that exact proposal. It never guesses intent,
retargets explicit destinations, or offers fix-all.

### Complete intent, bounded edits

- Address the containing command through an actual PLAY0470 occurrence and revision-bound handles.
  Require exactly one selection for every event-production occurrence, including conditional productions
  and already-explicit destinations. Repeated references to the same event remain distinct occurrences.
- Each selection supplies a non-null expression through the existing typed expression schema. There is no
  automatic/default selection, sibling-copy fallback, or invented allocation expression. An agent needs
  authorization to choose destinations; diagnostic discovery does not provide that authorization.
- Confirm existing explicit `for` expressions by structural equality. Generate operations only for missing
  `For` members. Preserve mappings, conditions, tags, event declarations, order and route syntax; do not
  modify operation productions. Retargeting is a separate authoring operation.
- Keep event-source identity (`for`) distinct from named source/stream routing. Every selection confirms
  either legacy routing or the exact, uniquely resolved named source and stream using declaration handles,
  including inherited command routing. Refuse a mismatch or ambiguity; never add or change a named route.

### Additive request, discovery and report contract

Extend `propose-repair` with optional, strictly typed `destinationIntent`, version 1, required for PLAY0470
and rejected on unrelated repairs. It contains the complete occurrence selections, their expressions and
route confirmations. Existing requests, deterministic repair discovery, no-input capabilities and clients
retain their behavior. PLAY0470 must not enter the existing no-input action list.

Expose separate, bounded, paged intent discovery through `read-workspace` (`repair-intents`) and an additive
`intentActions` capability. A descriptor identifies the command, diagnostic occurrences, every production,
existing expression, condition, authored/effective route context, schema version and formatting requirement.
It declares executable validation and individual review mandatory, no executable baseline, `canFixAll: false`
and `pinRepairEvidence: false`. Unsupported clients continue to receive diagnostics, not an automatic action.
Pinned repair evidence v1 remains limited to its existing repairs; do not extend its promise by implication.

Retain an additive typed `ProductionDestinationReport` with the proposal and expose it through bounded
`read-proposal` pages without changing existing repair reports. It includes:

- `baselineExecutableAvailable: false` and `preservationProven: false`, candidate language/semantic versions,
  binding result, submitted intent, exact changed occurrences, and authoring/executable diagnostics;
- the candidate command default and route, plus one row per event production in authored order: occurrence,
  event, condition, original explicit expression or omission category, selected expression and resolved
  property/type, candidate effective destination, and named source/stream and stream-id mappings if present;
- classifications `MadeExplicit`, `ExplicitUnchanged` or `EstablishedFromInvalidModel`, distinguishing the
  syntax edit from the establishment of effective routing in a previously invalid model;
- newly enrolled identities and confirmation that existing assigned identities survive. Other commands'
  authored routing is unchanged; their executable behavior is not claimed preserved.

For omissions, describe syntax only: inline identifier default, or plain legacy omission whose allocation/
inheritance depends on binding. Never invent an effective before-destination. Candidate admission to an
existing ESM version is separate from target capability and specification results.

### Proposal, stale preview and application

Check expected source/workspace and catalog revisions before resolving intent, and verify exact saved disk
state. Resolve the complete production set and route evidence, then run one coordinated typed authoring
transaction with `Executable` validation and the physical MCP attachment loader. Require resolved imports,
round-trip fidelity, every comment preserved exactly once, identity continuity, whole-workspace binding,
removal of the selected command's PLAY0470, and candidate destinations matching all submitted selections.
Unrelated executable blockers refuse the proposal too. No authoring-only downgrade is permitted.

Discovery and proposal do not write. A refusal returns structured conflicts and useful candidate diagnostics,
not an applyable partial candidate. Reject stale/foreign/duplicate/missing handles, incomplete physical
inputs, unsupported or incompatible expressions, route changes, explicit retargeting, missing formatting
consent, failed round trips, pinned-evidence requests and fix-all. Do not cache intent-dependent validation
under a key that omits intent; verify each proposed selection set afresh.

Apply only the retained proposal the author reviewed. Immediately before installation, reject changed
source/workspace or catalog revisions, exact disk bytes/file membership, and relevant attachment evidence
that supported candidate binding (referenced attachment set, availability, content and diagnostics).
Refresh/recompare that evidence, including candidate references, rather than treating revision equality
or a prior preview as sufficient. Drift requires a new proposal and review, never silent re-planning at apply.
This attachment guarantee is a requirement of this new path, not a claim that ordinary apply already pins it.

Before retaining the proposal, and again immediately before installation, independently reject overlap between
any attachment evidence path and the source-document, identity-state, journal, staging or backup write paths.
Compare physical aliases as well as exact paths; refuse uncertain physical resolution rather than assuming
separation. This prevents installation itself from invalidating evidence that passed preflight. Reuse the
write-conflict safeguards where applicable without requiring pinned evidence or changing existing unpinned
repair behavior.

Proposal and application are all-or-nothing outcomes: no subset is accepted as success. Reuse the current
journaled writer: stage files and durable identity state, recheck before installation, back up originals,
install, then verify the resulting disk state before success. `McpDisk.Apply` attempts rollback on a caught
installation failure. A verified restoration reports failure/`RolledBack`; failed restoration reports
`RecoveryRequired` and preserves the journal/backups. A pending journal blocks ordinary operations.

These are recoverable coordinated writes under the existing exclusive-writer assumption, not a kernel
transaction or crash-atomic multi-file visibility. A crash or failed rollback can leave intermediate files;
never label that partial state successful. On an unknown outcome, inspect `workspace-state` first; do not
retry or recover automatically. Recovery remains an explicitly requested operation.

## Options considered

- **Intent-parameterized MCP proposal (chosen):** one complete selection set and one reviewable contract.
- **Infer identifiers, copy siblings, or enumerate combinations:** rejected; none establishes author intent.
- **Guidance/manual typed edits only:** safe fallback, but leaves completeness and routing disclosure to callers.
- **One-click editor repair or fix-all:** rejected; neither collects intent nor provides individual review.
- **New grammar, runtime semantics or ESM version:** rejected; existing expressions and admission remain authoritative.

## Default if unanswered

Do not implement this repair. Retain diagnostics and coordinated manual typed authoring. Authors bear the
cost of checking completeness and interpreting routing without the dedicated report.

## Timeline and scope

Settle before implementing #397; hold the contract through delivery and consumer adaptation until superseded.
In scope: typed repair planning, additive MCP discovery/request/report transport, preview/apply safeguards,
regression coverage and documentation. Acceptance does not establish implementation correctness.
Out of scope: grammar, runtime or ESM changes, new CLI commands, route retargeting, allocation syntax,
Monaco/VS Code one-click actions, fix-all and changes to PLAY0478's preservation proof. The v28 freeze stands.

Consumer obligations for delivery are explicit:

- **Cratis/AI:** teach complete selections, no executable before-baseline and no preservation claim, retained
  preview/review followed by explicit apply, and refusals. Update the owning corpus, not managed local copies.
- **Cratis/cli:** bundle and expose this MCP contract and its capability discovery; add no command or grammar.
- **Studio:** feature-detect the intent capability; never offer a one-click automatic action. Collect every
  entry, preview the retained report, and apply only that exact reviewed proposal. Without support, show guidance.

Track AI adaptation in [Cratis/AI#784](https://github.com/Cratis/AI/issues/784) and CLI adaptation in
[Cratis/cli#398](https://github.com/Cratis/cli/issues/398). Studio adaptation is tracked in its owning tracker,
[Cratis/StudioIssues#828](https://github.com/Cratis/StudioIssues/issues/828); do not present consumer adaptation
as complete until its obligations are delivered.

## Verification

**Done when:** Complete explicit intent yields one retained, whole-workspace-binding proposal and routing
report; review then explicit apply writes exactly it. Every refusal leaves no applyable subset, stale previews
cannot apply, and failures expose rollback/recovery outcomes without partial success. Existing repairs and
clients retain their contracts; consumer obligations and their adaptation issues are accounted for.

**Verify by:** Workspace/MCP specs covering repeated/conditional occurrences, explicit equality, route
confirmation, unrelated binding blockers, comments/identities, retained paged reports, no discovery/proposal
writes, source/catalog/exact-disk/attachment drift at apply, exact-path and physical-alias attachment/write
overlap refusal both before retaining and before installing, unchanged non-overlapping attachment success,
rollback and pending-journal refusal. Run existing
repair/client regressions and language/conformance gates to prove no grammar/ESM/runtime change. Inspect AI/CLI
adaptation evidence and Studio capability/collection/preview/apply behavior before claiming consumer delivery.

## Consequences

Authors can establish destinations without pretending an invalid model had executable behavior to preserve.
The complete-selection and whole-workspace gates cost more interaction and can refuse a locally plausible fix.
Hosts share one contract rather than implementing routing guesses. Journaled recovery remains necessary;
a successful preview is neither permission to apply nor a guarantee against later drift.
