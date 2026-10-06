---
id: 0025
title: Allocate ESM v7 to generated values and responses, and number later versions at admission
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Source/Screenplay/EventModels/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

Accepted [0023](0023-command-production-model.md) fixes the delivery order of ESM v7–v11 in its *ESM allocation* section: v7 exact numbers (#285), v8 generated values and responses (#300, #303), v9 operations, v10 event sources and streams, v11 reads. Versions are cumulative and ship in that order, so the first slot gates everything after it.

That order now blocks the feature with the most downstream demand. Generated values and responses have an approved design (0023, *Generated values and responses*), shipped authoring (parsing, printing, validation, MCP, editors; [#361](https://github.com/Cratis/Screenplay/pull/361)) and a binder that refuses them until an ESM version admits them ([`SemanticModelBinder.CommandProductions.cs:40-43,50-53`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.CommandProductions.cs)). Stage#175, Scene#53 and Arc#2885 are related downstream work. End-to-end Screenplay response delivery remains blocked; downstream implementation can proceed independently. Exact numbers hold v7, but [0024](0024-exact-numeric-source-mode.md) is still proposed and its Phase B (numeric-mode root field, mode-aware binding and runner, exact capture-guard parsing) is unbuilt. Chronicle#4239, the one concrete consumer of exact numbers named so far, reads syntax, not the ESM. The status quo therefore makes responses wait for a proposal that no ESM consumer is waiting on.

Fixing later slots in advance has the same defect in reverse. Any fixed number makes a feature wait on every feature numbered before it, whether or not the earlier one is ready, and any later withdrawal forces a renumbering sweep through documentation, diagnostics and product strings. Product strings already do this: the binder messages name "ESM v8", "v9" and "v10" for constructs that nothing admits yet ([`CommandProductions.cs:30,37,58`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.CommandProductions.cs)), and [`McpAuthoringReadiness.RequiredVersion`](../Source/DotNET/Screenplay.Mcp/McpAuthoringReadiness.cs) encodes the order as numbers 10, 8 and 9 combined with `Math.Max` (`:73-89`).

[0004](0004-admission-and-governance-of-portable-executable-semantics.md) requires an accepted record for every new ESM version and consumer admission by explicit choice. It does not require the number to be fixed before the feature is ready. [0017](0017-map-declared-decision-reads-to-chronicle-decision-reads.md) already defers its number to an accepted follow-up before merge (`:34`).

Accepting a feature's design and numbering a release are different acts. A design can be accepted while its implementation is months away. A number is a public promise to consumers about what a released package contains. Binding the number to design acceptance is what produced the hostage problem: an accepted but unready design holds a slot nobody can ship.

This record is proposed. It records no acceptance by Einar Ingebrigtsen or Sindre Alstad Wilting.

## Decision

1. **ESM v7 is generated values (#300) plus response blocks (#303).** These are admitted together as one version under 0004. Their contract is [0026](0026-generated-values-and-command-responses-in-esm-v7.md).
2. **Later versions are numbered at admission, not in advance.** A feature record may be accepted for its *semantic design* without a number. A version number is assigned only at a release-ready admission checkpoint, against current `main`:
   - A feature is release-ready when its admission record is accepted in final form and its 0004 gate-2 evidence exists (runtime meaning, canonical form with golden vector, reference execution or typed unsupported outcome, source-backed vector, fail-closed behavior, activation only when used).
   - The number is the **next unused number**: one more than the highest version that is released, claimed or reserved.
   - **v7 is reserved by acceptance of this record.** v7 is neither released nor claimed by a pull request, so the reservation stands in for a claim: it counts as the single unreleased entry and blocks every later numbered claim until v7 is released.
   - **Only one unreleased entry exists at a time.** A claim becomes effective only when its pull request merges. The pull request adds the version to `Versions.cs` and to the *claimed, unreleased* part of the version table, and adds a dated status note naming the number on the feature's admission record. Before merging, the pull request rechecks current `main` for the highest released, claimed or reserved number and for any other unreleased entry, and does not merge if one exists. An open pull request, a branch or a comment holds nothing.
   - The claim ends when the version is released or withdrawn. A **withdrawal** is one change that removes or deactivates the unreleased admission everywhere it was admitted (`Versions.cs`, binding, strict readers, golden and corpus files and the table entry) and adds a dated status note. Only after that change has merged is the number free for the next checkpoint; no number is returned to the pool on a status note alone.
   - A **released** version is never withdrawn, renumbered or reused. A **published** version is treated the same way, including a published prerelease package that exposes the contract: its number is never reused, even if the claim is later withdrawn. A defect in a released version is corrected only by a new version or by a byte-preserving extension that 0004 already permits.
3. **Parallel work uses feature names, never self-chosen public numbers.** Until a feature holds a claim (or, for v7, the reservation), its branches, issues, diagnostics text, documentation, product strings and user-facing messages name the feature (for example "operations") and say that it is not admitted by any supported ESM version. They do not name a version number. Golden and corpus files for an unclaimed feature carry the feature name and take the number when the claim is made. v7 is the one number assigned directly by this record, and that reservation is the single exception to the rule that only features holding a claim may name a number. Features that are neither released nor currently claimed do not name one.
4. **Exact numbers leave the fixed sequence.** Exact mode (#285, 0024) takes the next unused number at its own checkpoint, when 0024 is accepted and Phase B is release-ready. It is a separate version from responses. Choosing separate versions is a conservative allocation choice. 0004 does not prohibit extending a shipped version when no model that bound before changes bytes (code validation joined v3 that way, `0004:47`). A separate version keeps responses and exact mode independently admittable and independently rejectable, so a consumer that admits v7 does not thereby accept exact-mode models.
5. **Numeric mode stays independent of feature version.** The ESM records numeric mode separately from its version, and exact mode is never implied by a later feature. The root field belongs to the version that admits exact mode and to every later version. Versions before it omit the field and their strict readers reject it. 0023's rules that declaration-bearing files opt in together, that mixed documents fail and that existing models keep their numeric behavior and bytes are unchanged.
6. **"Cumulative" means vN includes the contract of vN−1, including every earlier version, and releases only after vN−1. It does not imply admission of future contracts.** It does not mean a consumer must implement every earlier feature before admitting a model that selects vN, and it does not mean a number can be assigned ahead of release. A model selects the lowest version that contains every construct it uses, as before.
7. **A compact authoritative table maps each released version to its contract.** It lives in [`Documentation/screenplay/interoperability.md`](../Documentation/screenplay/interoperability.md), under *Executable semantic model versions*. It has two parts. The *released* part lists, for each released version: the number, the constructs that select it, the `schemaVersion`, the decision record, the golden file and the corpus vector. A separate *claimed, unreleased* part holds at most one entry (v7 until it ships) in the same columns, marked unreleased, and never inside the released list. Features without a claim or reservation appear in prose by name only. [`Versions.cs`](../Source/DotNET/Screenplay/Semantics/Versions.cs) mirrors both parts, and a check keeps the two equal, including that at most one unreleased entry exists.

### Partial supersession of 0023

0025 replaces only the following parts of 0023. It is partial supersession described by section and clause, not by line numbers. 0023 stays `accepted`, and its decision text is not edited.

| 0023 location | What changes |
| --- | --- |
| *ESM allocation*, table rows v7, v8, v9, v10 and v11 | Replaced by decision points 1 to 4 above. The **Unchanged** and **v6** rows stay. |
| *ESM allocation*, "Versions stay cumulative: each admits the earlier versions' features and ships only after the previous one. If an earlier allocation is withdrawn, renumber later allocations through an accepted follow-up before shipping them." | Replaced by points 2 and 6. |
| *ESM allocation*, in the numeric-mode paragraph: "v7 introduces exact numbers as a preamble-selected mode", "A v8–v11 document keeps Double numbers unless its preamble opts into exact numbers; `schemaVersion: 8`, for example, admits v8 features but does not select a numeric mode" and "`returns` requires at least v8 in either mode" | The version numbers are replaced: exact mode arrives with the version that admits it (point 4), `returns` requires v7 or later in either mode, and a document of any version admitting a feature keeps Double numbers unless its preamble opts into exact. The independence of numeric mode from version is **preserved** (point 5), as are all of this paragraph's other sentences. |
| *ESM allocation*, the final paragraph naming v11 as the allocation 0017 requires for #129 | The number is replaced: decision reads are numbered at their admission checkpoint. 0017's requirements (accepted amendment for the required/optional absence rule, 0017's admission and explicit-opt-in guarantees) are unchanged. |
| *Default if unanswered*, "v11 reads cannot ship with the proposed absence rule until 0017 is amended" | Read as "reads cannot ship ...". |
| *Timeline and scope*, "The allocation above fixes delivery order" and "New read behavior requires the allocated v11 admission" | Replaced by points 2 and 3. |
| *Verification*, "Semantic batches follow the allocated order" and "Include v8–v11 Double and exact-mode vectors" | Batches are numbered at admission. Each version admitted after the exact-mode version carries Double and exact-mode vectors for its features; the exact-mode version adds exact-mode vectors for every feature admitted before it. |

**Preserved by 0025:** the v6 row and 0022; numeric mode as an independent preamble-selected mode; cumulative contracts as defined in point 6; the *Unchanged* allocation row; every decision in *Events and identity*, *Generated values and responses*, *Operations and external systems*, *Event sources, streams and concurrency*, *Implementation lifecycle*, *Reads, derived values and provisioning* and *Specification time*; Sindre Alstad Wilting's defaults; 0004's gates for every batch.

### Banner to add when 0025 is accepted

Nothing in 0023 changes while 0025 is proposed, and no banner is added now. A banner that said 0023's allocation "has been superseded" would be false until a decider accepts this record. On acceptance, the following dated banner is inserted directly under 0023's front matter, the status stays `accepted`, and 0023's body is untouched:

> **<acceptance date> — ESM allocation superseded in part by [decision 0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md).** 0025 replaces the v7–v11 rows of the *ESM allocation* table and the allocation-dependent sentences in *ESM allocation*, *Default if unanswered*, *Timeline and scope* and *Verification*. ESM v7 is allocated to generated values and responses; admission remains subject to accepted 0026 and 0004's gates. Later versions, including exact numbers, are numbered at a serialized release-ready admission checkpoint instead of in a fixed order. The v6 row, numeric-mode independence, cumulative contracts as redefined in 0025 and every other section of this record remain in force.

0023 is replaced only in part, so its `status` and front matter do not change. Acceptance also regenerates the README index, and updates the issue status blocks and product strings listed under Verification.

## Options considered

- **Status quo: v7 exact, v8 responses, then operations, streams, reads (0023).** Not taken. It is already recorded and consistent. Its cost is that responses wait on 0024 acceptance and Phase B for a numeric need no ESM consumer has asked for, and any slip renumbers everything after it.
- **Exact last (v7 responses, v8 operations, v9 streams, v10 reads, v11 exact).** Not taken. It frees responses but holds exact behind reads, the least ready feature: it needs a 0017 amendment and unreleased Arc and Chronicle work. Exact is nearly designed and could ship well before reads.
- **Exact second (v7 responses, v8 exact, then operations, streams, reads).** Not taken, though reasonable. It is sound if exact admission is release-ready or has a concrete near-term delivery commitment. Neither holds today: 0024 is proposed, and Phase B is unbuilt. It would make operations and streams wait on it, and every later feature would carry exact-mode vectors.
- **Fixed fallback (v7 responses, v8 operations, v9 streams, v10 exact, v11 reads).** Not taken. It is operationally simpler and no less safe for consumers than late allocation. It still fixes speculative slots for features with uncertain increment sizes, and recreates the hostage problem when any of them slips.
- **Exact without a version of its own: an optional `numericMode` on an existing version.** Not taken. It has the best surface symmetry. Older bytes need not change, so 0004's join rule could permit it. It is rejected as a conservative choice: a consumer that has admitted that version by pin would accept exact-mode models without choosing to. The cost of the separate version is one more number.
- **Exact on a separate admission axis (API opt-in, not a version).** Not taken. It needs an amendment to 0004 and a second admission dimension in every consumer for one feature.
- **Responses join shipped v6 under 0004's join rule.** Not taken. 0004 would permit it only if no model that bound before changes bytes, which can be arranged. It would change the contract of a version already released under 0022 and tracked by consumers for its existing contents (cli#239, StudioIssues#475, Screenplay.Generation#66). A separate version is clearer for them.
- **Number at design acceptance.** Not taken. Design acceptance and release readiness are separate: a second accepted but unready design would claim the same "next free" number, and exact numbers would again be hostage to it.

## Default if unanswered

0023 stands. v7 stays reserved for exact numbers and responses stay at v8 behind it. Responses cannot be admitted until 0024 is accepted and its Phase B ships. End-to-end Screenplay response delivery remains blocked on a feature it does not depend on; downstream implementation can proceed independently. Product strings and readiness logic keep naming numbers for features nothing admits.

## Timeline and scope

Applies from acceptance until superseded. It must be accepted before 0026 is accepted and before any v7 implementation merges to `main`. Slices may be reviewed on an integration branch earlier, at the risk of rework.

In scope: which feature is v7, how later versions and exact numbers are numbered and withdrawn, how parallel work names unnumbered features, numeric mode's independence from version, the meaning of "cumulative", the released-version table, and the exact parts of 0023 that change.

Out of scope: the v7 contract (0026); exact-number semantics (0024, which has been updated to use "the admitting version"; its numbering remains conditional on 0025's acceptance); which feature is numbered next, since the planned order of operations, streams and reads is not binding; 0004's gates and consumer duties; the v6 contract; and any change to v1–v6 bytes, revisions or outcomes.

## Verification

**Done when:**
- `Versions.cs` selects v7 only for models using a generated property, a response, a generated fixture or a return expectation, and a model without them keeps its v1–v6 version, bytes and revision.
- The released-version table in `interoperability.md` lists every released version with its selecting constructs, record, golden file and corpus vector, and a check fails when `Versions.cs` and the table disagree.
- No product string, diagnostic text, documentation page, editor message, sample or readiness logic names a version number for a feature that holds no claim. `McpAuthoringReadiness` no longer encodes feature order as numbers.
- 0023 carries the banner above, 0024 has been updated to use "the admitting version" and its numbering remains conditional on 0025's acceptance, and the status blocks of #285, #300, #301, #302, #303, #308 and #309 describe numbering at admission.

**Verify by:** Search `Source` and `Documentation` for `ESM v(8|9|10|11)` and confirm only decision history matches. Run the golden and corpus specs for v1–v7. Run the check that compares `Versions.cs` with the interoperability table. Read 0023 after acceptance for the banner, and the listed issue status blocks. Check that the v7 pull request carries `Decision: 0004, 0023, 0025, 0026` trailers.

## Consequences

Responses can ship without waiting on exact numbers, and neither waits on reads. No feature holds a slot it cannot use, and withdrawal never forces a renumbering sweep. Consumers get an immutable table of what each released version means instead of speculative promises.

Costs: plans and messages cannot quote a future number and must say a feature is not admitted yet. Each checkpoint needs a deliberate claim, and a second feature that is ready first waits for the unreleased entry to clear. The product-string and readiness sweep is real work across documentation, C#, TypeScript and the managed AI skills (tracked on Cratis/AI#500).

It forecloses fixed multi-version roadmaps in decision records.

## Related issues

Screenplay: [#285](https://github.com/Cratis/Screenplay/issues/285), [#300](https://github.com/Cratis/Screenplay/issues/300), [#301](https://github.com/Cratis/Screenplay/issues/301), [#302](https://github.com/Cratis/Screenplay/issues/302), [#303](https://github.com/Cratis/Screenplay/issues/303), [#308](https://github.com/Cratis/Screenplay/issues/308), [#309](https://github.com/Cratis/Screenplay/issues/309), [#129](https://github.com/Cratis/Screenplay/issues/129). Stage: [#175](https://github.com/Cratis/Stage/issues/175). Scene: [#53](https://github.com/Cratis/Scene/issues/53). Arc: [#2885](https://github.com/Cratis/Arc/issues/2885). Chronicle: [#4239](https://github.com/Cratis/Chronicle/issues/4239).

## Status notes

**Proposed.** No decider and no acceptance date are recorded.
