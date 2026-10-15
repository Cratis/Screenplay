---
id: 0085
title: Admit protected command reads in the next ESM version through an explicit preamble
status: accepted
stage: none
decided: 2026-10-15
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
amends: 0017
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Diagnostics/**
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

[0017](0017-map-declared-decision-reads-to-chronicle-decision-reads.md) maps a declared `reads` to a Chronicle protected decision read, but it allocates no version, leaves absence open and keeps `require` over reads deferred. [0023](0023-command-production-model.md) (*Reads, derived values and provisioning*) proposes that a missing required read rejects the command, and makes that rule depend on an accepted 0017 amendment. [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) numbers versions at admission, and [0004](0004-admission-and-governance-of-portable-executable-semantics.md) requires an accepted record for each new version. [#129](https://github.com/Cratis/Screenplay/issues/129) waits on these choices.

Today the binder refuses every command `reads` and `concurrency` with PLAY0271 (`SemanticModelBinder.Commands.cs:29-43`). It refuses a read-model path in a condition operand, left or right, with PLAY0268 (`SemanticModelBinder.Conditions.cs:31,53`), and refuses a path in a produces mapping with PLAY0268 (`Documentation/screenplay/commands.md:97`). `SemanticCommand` has no reads member (`Semantics/SemanticBehaviors.cs:351-370`). The evaluator already declares `SemanticExecutionOutcomeKind.Conflict` and `SemanticConflictCategory.DecisionStateChanged` (`SemanticExecutionContracts.cs:31,42-53`), but a command executes against an immutable world and commits once (`SemanticCommandExecution.cs:327`), so there is no point at which a competing write can interleave.

KeldaKraft (ee76807) declares 36 reads in 29 command files. `RevokePlantAccess.play` maps `memberId = PlantAccessGrant.memberId` from a read, and `KeepPlantWindow.play` maps `keepDays = RetentionPolicy.keepDays`, so those models need read paths in mappings as well as in `require`. The decision was settled by woksin through the coordinator on 2026-10-15; the six rulings below are all accepted recommendations.

## Decision

**This record amends 0017; it does not supersede it.** It adds the version, absence and delivery rules 0017 left open, and 0017's admission rules, protection semantics and out-of-scope list stand. Protected command reads are admitted in the next ESM version, expected to be **v24** (v22 is #769, released in v4.181.0; v23 is expected for #782). The number is fixed at the admission checkpoint under 0025, not by this record.

1. **Opt-in.** A root preamble `command reads protected` selects the mode. The ESM carries it as the final application member `commandReadMode: "protected"`, written only with that value, as 0078 does for `reactionProductionMode`. Binding selects the admitting version only when the preamble exists. A model without it keeps its bytes, revision and the PLAY0271/PLAY0268 refusals. Writing `reads` never confers protection by itself, and no model is upgraded automatically. New-model scaffolding and samples declare the preamble by default, as [#771](https://github.com/Cratis/Screenplay/issues/771) does for 0078.
2. **Absence.** The order is 0023's: authorization, then property rules, then reads (a required read whose instance is absent rejects here), then derive/provide, then requirements, then productions and responses. Absence therefore rejects before any derived value, provided value, requirement or mapping is computed. The protected dependency still covers the absent state. `optional` stays refused by PLAY0481 and `advisory` by PLAY0175, as today, and both stay with [#308](https://github.com/Cratis/Screenplay/issues/308).
3. **Races.** The reference evaluator splits into a decide step and a commit step. Decide captures the unfiltered log length as the boundary and the dependency set, which is each read's key plus its view's handled event types; reads of one key merge. Commit returns the existing `SemanticConflict` with category `DecisionStateChanged` and no facts when a fact after the boundary matches the set. An irrelevant fact does not conflict: commit then settles against the commit-time world, not the world captured at decide. It runs constraint checks (including uniqueness), projection and publication preparation against the latest world and preserves every intervening fact and its state, so an intervening fact that makes a uniqueness claim fail still rejects the command. A command that produces no facts is still validated. Specifications get no construct for races; evaluator conformance vectors cover them.
4. **Admission.** 0017's strict shape is kept as written. Chronicle's stream-scoped reads and reducer decision reads are tracked in [#805](https://github.com/Cratis/Screenplay/issues/805) and are not admitted here.
5. **Read paths.** A read path is admitted as a `require` operand (either side of a comparison) and as a produces mapping source. This closes [#802](https://github.com/Cratis/Screenplay/issues/802).
6. **Reactions.** [0006](0006-reaction-triggers-declare-reads.md) is unchanged: a reaction that reads before a direct produce stays refused (PLAY0268), and a reaction that only invokes commands keeps PLAY0270.

### What the ESM records

`SemanticCommand.Reads` holds one entry per declared read:

- `name`: the alias, or the view name when there is no alias.
- `readModel`: the semantic id of the view.
- `key`: the semantic id of a command property, or the declared identifier when `by` is omitted.
- `presence`: `required`, the only value in the admitting version.

A condition operand and a mapping source gain a read-path form: a read name plus a property id of the view. No sequence number, boundary or event-type list is stored. The guarded set derives from the admitted projection, which keeps the model renderer-neutral and prevents drift from projection data. Earlier schemas and programmatic validators reject `commandReadMode` and `reads`. Exact member spellings are pinned by the v24 golden file.

### Admission rules and refusals

Under the preamble, a read is admitted only when all of these hold. Each failure refuses with the reason, never a fallback to an unguarded read:

- The view is keyed on the event-source identity over the event log, with a finite, non-empty set of handled event types that includes removals.
- The key is a string, a string concept or a canonical GUID. The key is a required, scalar, non-generated input property of the command, or the declared identifier when `by` is omitted, which must meet the same conditions. Reads run before generation, so a generated, optional or collection key is refused.
- A second read of one view carries an alias (0003 and 0017).

Refused: a reducer, a join (including a removal join), children or nested scopes, an open or all-event subscription, a derivative, a payload-property key, `ByParts`, a view whose `every` is not limited to its declared event types, a singleton or unkeyed view, an omitted `by` with no declared identifier, any other key type, `optional`, `advisory`, `via query`, and a command that declares both `reads` and `concurrency` (blocking ambiguity). A read path must name a declared read or alias. A view name shared by two reads must be written as the alias.

### Diagnostics needed

Names only; codes are allocated separately.

- `UnprotectableDecisionRead` (carries the reason: reducer, join, children, subscription, derivative, payload key, parts, singleton, key type)
- `DecisionReadWithoutIdentifier`
- `DecisionReadsWithConcurrency`
- `UnknownReadPath`
- `AmbiguousReadPath`
- `InvalidCommandReadsDeclaration` (duplicate, conflicting or non-root preamble)
- `LegacyCommandReadsRepairAvailable` (information, offers the preamble repair)

PLAY0271 and PLAY0268 keep their wording for models without the preamble, except that the PLAY0268 message at `SemanticModelBinder.Conditions.cs:31` points to the preamble.

### Migration

Legacy documents are unchanged. A typed workspace repair, following [0014](0014-diagnostic-repairs-are-typed-workspace-proposals.md) (typed proposal, revision check, preview, explicit acceptance), adds `command reads protected` only when every read in the model is admissible. Otherwise it lists each refused read with its reason. Nothing upgrades a model automatically.

## Options considered

- **Root preamble and application member (chosen).** One explicit intent that survives serialization and source recovery, and no unchanged model moves. 0078 set the precedent.
- **A keyword on each read.** Rejected: it conflicts with 0023's rule that protection is the default and `advisory` opts out, and it lets one document mix two meanings of `reads`.
- **Select the version from the features used.** Rejected: 0017 requires explicit opt-in, and a later unrelated feature would change old commands.
- **Ship `optional` in the same version.** Rejected by ruling: it stays with #308.
- **A specification construct for a write between read and commit.** Rejected by ruling: the evaluator's commit step is the contract, and conformance vectors prove it.
- **Admit stream-scoped and reducer reads now.** Rejected by ruling: 0017's strict shape holds and #805 tracks the rest.
- **Limit the admitting version to `require` and keep mapping paths refused.** Rejected by ruling: KeldaKraft's `RevokePlantAccess` needs mapping paths.

## Default if unanswered

0017 stays accepted but unimplemented. The binder keeps refusing command `reads` and read-model paths, `require` over reads stays unsupported, and Arc#3298 stays in authoring mode. Commands that decide from a read model have no portable consistency guarantee, and KeldaKraft's read-path `require` lines and read-sourced mappings stay unbindable.

## Timeline and scope

This record holds until superseded. It does not allocate a version. The implementing pull request takes the next unused ESM version under 0025 at its admission checkpoint, with release-ready gate-2 evidence, after verifying on current `main` that no other version is claimed but unreleased; if #782's v23 is still unreleased then, it waits. That pull request adds the version to `Versions.cs` and to the claimed-unreleased table. A branch holds nothing.

In scope: the preamble and member, the reads entries, the read-path operand and mapping source, the absence rule, the decide/commit split, admission and diagnostics, the repair, scaffolding defaults, all in-repository surfaces and consumer tracking.

Not in scope: stream-scoped and reducer reads (#805), `optional`, `advisory` and `via query` reads (#308), reaction reads (0006), validators over read-backed rules (Arc#2831 is open), and a specification construct for races.

## Verification

**Done when:** a model with the preamble binds admitted reads to the admitting version and writes `commandReadMode: "protected"`; the same source without it keeps its version, bytes, revision and refusals; v1 to v22 and v23 (once released) goldens are byte-identical; refused shapes fail with their reason; a required read that is absent rejects before requirements and mappings; a fact after the boundary that matches the dependency set returns `Conflict`/`DecisionStateChanged` with no facts; read paths bind in `require` and in mappings; the repair adds the preamble only when every read is admissible; each consumer admits v24 or rejects it.

**Verify by:** a golden for the admitting version (expected `full-esm-v24.json`) and a source-backed corpus for that version with an identical-source legacy control, in file and folder forms. Parser, binder and strict-reader specifications in both compilers for every admitted and refused shape, the ambiguity and alias cases, and the preamble (duplicate, conflict, import). Evaluator vectors for absence in an empty and a non-empty log, absence after removal and recreation, a present-state race, a cross-source read, a removal after the boundary, an irrelevant fact, a same-key merge, a no-fact command, a rejected command, an irrelevant intervening fact that is preserved in the committed world and its projections, and an intervening fact that makes the command's uniqueness claim fail at commit. Precedence vectors show absence rejects before derive/provide and requirements run. Binder and strict-reader refusals cover generated, optional and collection keys. Repair tests for preview, refusal and stale revisions. The version-table check, and a `Decision: 0004, 0017, 0025, 0085` trailer on the implementing commit. Generated vectors are regenerated and reviewed, never hand-edited.

## Consequences

Authors get decision-consistent commands that bind, execute and conflict deterministically, and read-backed `require` and mappings stop being refused. Some views stay refused, so KeldaKraft reads over joins, children or singleton views must be re-modeled. The preamble costs plumbing in every surface, and each target must realize protected reads or reject the mode. The split evaluator changes how command execution is structured. The decision forecloses silent promotion of legacy `reads` and a per-read protection keyword.

Consumer follow-ups, filed with the implementation under 0004:

- **Stage:** admit the admitting version and render to `[ProtectedDecision]` and `IDecisionReads`; reject any target that cannot protect a read.
- **Arc#3298:** promote the generator's reads and requirements from authoring mode to executable output.
- **Studio:** preamble in the new-application template; board and editor support.
- **AI corpus (Cratis/AI):** glossary, the "`reads` treated as protection" anti-pattern, command-surface and streams-and-consistency skills.
- **KeldaKraft:** re-model the refused reads and adopt the preamble.
- Also in this repository: Monaco, VS Code, MCP syntax-schema notes, `commands.md` and the `ReadsSyntax` XML documentation, as 0017's consequences require.

## Related issues

Screenplay: [#129](https://github.com/Cratis/Screenplay/issues/129), [#308](https://github.com/Cratis/Screenplay/issues/308), [#771](https://github.com/Cratis/Screenplay/issues/771), [#802](https://github.com/Cratis/Screenplay/issues/802), [#805](https://github.com/Cratis/Screenplay/issues/805). Arc: [#3298](https://github.com/Cratis/Arc/issues/3298), [#2831](https://github.com/Cratis/Arc/issues/2831). Decisions: [0003](0003-decision-consistency-for-command-reads.md), [0004](0004-admission-and-governance-of-portable-executable-semantics.md), [0006](0006-reaction-triggers-declare-reads.md), [0017](0017-map-declared-decision-reads-to-chronicle-decision-reads.md), [0023](0023-command-production-model.md), [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md), [0078](0078-opt-into-atomic-reaction-production-batches.md).
