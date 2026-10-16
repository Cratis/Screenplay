---
id: 0089
title: Expose the reference evaluator's decide/commit split to hosts and publish protected-read race vectors
status: accepted
stage: none
decided: 2026-10-16
decider: Sindre Wilting (delegated to the L0 coordinator)
class: contract
reversibility: costly
amends: 0085
applies-to:
  - Source/DotNET/Screenplay/Semantics/Execution/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Documentation/screenplay/**
---

## Context

[0085](0085-admit-protected-command-reads-through-an-explicit-preamble.md) §3 says a command that read protected state
conflicts only when a later fact matches its dependency set, and that an irrelevant later fact settles against the
commit-time world. The reference evaluator already has that split: `SemanticEvaluator.Decide` captures the boundary and
the dependencies, and `SemanticDecision.Commit` checks them. `Execute` is `Decide(...).Commit(world)`
(`SemanticEvaluator.cs:24`, `:178`). `SemanticEvaluator` is public, but `Decide`, `SemanticDecision` and
`SemanticDecisionDependencies` are `internal`; `InternalsVisibleTo` lists only the MCP project and the canonical
vector specs (`Screenplay.csproj:15-16`). Commands invoked by reactions run through `evaluator.Execute` at one world
(`SemanticReactionLoop.cs:310`), so they decide and commit with no window between.

A host that appends to a real store has a window between deciding and committing, and facts written by someone else can
land in it. It cannot use the split, so it cannot apply 0085 §3. Cratis/Stage#440 is the first such host. It faults the
whole session on any outside append (`SemanticRuntime.cs:93-97`), and a spec pins that, even for an unrelated fact
(`should_fault_on_even_an_irrelevant_outside_append`). Stage's plan marks this as interim until #833 ships. The
documentation says the same: "Appending facts outside a command has no public path today"
(`Documentation/screenplay/reactions.md:361`).

0085 also says "evaluator conformance vectors cover" races, but the tests that do are internal specs. A second target
cannot run them, so the rule has no shared proof.

## Decision

1. **Public surface.** On the already-public `SemanticEvaluator` class, not on `ISemanticEvaluator`:
   - `SemanticDecision Decide(SemanticExecutionPlan plan, SemanticWorld world, SemanticExecutionRequest request)`
     changes from internal to public.
   - `SemanticExecutionResult ExtendWorld(SemanticExecutionPlan plan, SemanticWorld world, ImmutableArray<SemanticFact> facts)`
     is new.
   - `SemanticDecision` (internal today) becomes public with `int Boundary`, `SemanticDecisionDependencies Dependencies` and
     `SemanticExecutionResult Commit(SemanticWorld world)`. Its constructor and `Refused()` stay internal.
   - `SemanticDecisionDependencies` (internal today) becomes public with `Empty`, `ImmutableArray<Entry> Entries`,
     and `bool Matches(SemanticFact fact)`. `Entry` carries `SemanticValue Key` and `ImmutableHashSet<SemanticId> EventContracts`
     (renamed from `Handled`). Its constructor and `Capture()` stay internal.
   - `SemanticConflict`, `SemanticConflictCategory`, `SemanticWorld` and `SemanticFact` are already public. Nothing else
     is exposed.
2. **`ExtendWorld` semantics.** It validates the new facts as `EstablishWorld(publicReplay: true)` does, then projects
   only the new facts from the world's read models and pending children, passing the world's facts as the prior facts.
   It adds no constraint checks and no publications, because the facts are already in the log. A reached reducer
   returns `Unsupported`. The law: for a successfully established prefix `a` on the same plan, the world of
   `ExtendWorld(Establish(a), b)` equals the world of `Establish(a + b)`, comparing facts, read models and pending
   children. Results are not compared whole, because `Accepted.Facts` holds only `b`. On failure the input world is
   unchanged. A spec pins the law.
3. **`Commit` is pure and retriable.** It does not change the decision or the world it receives. The same decision may
   be committed against several worlds in turn: accepted against one, conflicting against a longer one. Item 8 says when a host may
   retry this way after a guarded append loses the race.
4. **Read-only requests.** `Decide` refuses a read-only request with a `Contract` rejection. Today it falls through to
   "Command '' is not in the execution plan".
5. **`ISemanticEvaluator` is unchanged.** Implementers and fakes keep compiling. Reactions and cascades keep calling
   `Execute`, because inside settlement the decide and commit worlds are the same and no race window exists.
6. **Target-neutral conflict contract.** The vectors and this record state it without Chronicle or C# types:
   - The boundary is the count of facts in the decided world.
   - A dependency is a key value plus a set of event contract ids (the view's from-events and removals, merged per key).
   - A conflict is any fact after the boundary whose event contract is in the set and whose destination equals the key,
     compared ordinally.
   - The outcome is `Conflict` with category `DecisionStateChanged` and no facts. Otherwise the command settles
     against the later log, as 0085 §3 says.
   - Keys are `SemanticValue`. Composite protected keys stay refused by `SemanticDecisionReadAdmission` and 0085;
     this representation is what a later composite-key decision (0088) can use, not an admission.
7. **Two windows, two rules.**
   1. *Decide to commit.* The host extends the decided world with every fact up to the tail it reads just before
      persisting, then calls `Commit` with that extended world. Only a matching fact conflicts (0085 §3); irrelevant
      facts settle against that later world. The host then runs `SemanticReactionSettlement.Settle` on the accepted
      result. Invoked commands decide and commit at one world through `Execute`, so they have no race window.
   2. *Settle to persist.* The host persists the complete settled batch (the root command's facts and the reactions'
      facts) atomically, guarded on the **durable pre-batch world**: the extended world passed to `Commit`, before any
      tentative root or cascade facts. The log must hold exactly those facts (the fact-count boundary). A successful
      command with no facts makes the same guarded write, because 0017 says it still validates its dependencies.
8. **Retry after a lost guard.** If the guarded write is definitively not written because the tail moved:
   - When settlement invoked no commands, the host MAY go back to window 1 with the same `SemanticDecision`. This is
     sound because `Commit` is pure and the decision's generated values are fixed when `Decide` runs (generation is
     part of production preparation, before the decision exists, and `Commit` only reads those prepared facts).
   - When settlement invoked any command, the operation fails with `Conflict` and `DecisionStateChanged`. It is never
     retried automatically (0017): re-running would silently re-decide the invoked commands against a new world.
   - After an unknown write outcome the host neither retries nor reports success; it reads the log to find out.
   The wording uses no store sequence numbers. The window-2 rules are host obligations, verified by host specs
   (Stage#440), not by corpus vectors.
9. **Race vectors.** `Cratis.Screenplay.CanonicalCorpus` publishes `ProtectedCommandReadRaceVectors` (`Version`,
   `Sha256`, `Bytes`) from `Corpus/ProtectedCommandReads/races-v1/`: a source-backed `races.play` with the
   `command reads protected` preamble, its `esm-v25.json`, revision and catalog, and `races.v1.json`. The cases:
   `present-state-race-recorded`, `present-state-race-removal`, `same-event-other-key`, `other-view-same-key`,
   `merged-keys`, `no-fact-command-conflict`, `no-fact-command-accept`, `irrelevant-fact-preserved`,
   `intervening-publication`, `absent-then-created`, `rejected-decision`, `late-uniqueness-claim`, `no-later-facts`, `repeat-commit-accept-then-conflict`,
   `repeat-commit-late-constraint`, `repeat-commit-stale-generated-source`,
   `reconstructed-nonprefix-history`, `publication-dependency-conflict` and `unchanged-publication-suppressed`. Each gives the decided facts, the request, the expected dependencies, the later facts and the
   expected outcome. The specs run them only through the public `EstablishWorld` → `Decide` → `ExtendWorld` → `Commit`, with an explicit second `Commit` step for the repeat-commit cases,
   and pin the bytes by SHA-256. Non-canonical UUID key casing is not a vector; the public path rejects it.
10. **No new ESM version.** The vectors use the existing v25 model and the existing corpus stays frozen; no member is
   added to the ESM.
11. **Out of scope.**
   - Running reactions for outside facts: whoever wrote them settles them.
   - Clock and application triggers.
   - Refreshing worlds on the query path.
   - A race construct in specifications (0085 refuses one).
   - Stream-scoped and reducer reads (#805).
   - Changes to Arc or Chronicle (0017 stands).

## Options considered

- **Public `Decide`, a value-typed decision and `ExtendWorld` (chosen).** The host owns the loop, `Commit` is pure, and
  the dependencies are visible so a target can map them onto its own guards.
- **Overload `Execute(plan, decidedWorld, laterWorld, request)`.** Rejected: the host must know the later world before
  the call, so it cannot re-commit after its own append races, and the dependencies stay hidden.
- **A world callback or world-source interface.** Rejected: it puts I/O inside a pure, synchronous evaluator, forces
  sync-over-async onto store tail reads and hides the retry loop.
- **Add `Decide` to `ISemanticEvaluator`.** Rejected: it breaks Stage's `ReactionEvaluator` and every fake.
- **Each host reimplements matching.** Rejected: the dependency rules would drift from the evaluator's.
- **`EstablishWorld(all facts)` for every race instead of `ExtendWorld`.** Rejected: it is O(n) per race, and
  `Commit` checks the prefix by value (Occurred, Tags, Context, Route), which facts may not keep across a store round
  trip.

## Default if unanswered

0085 §3 stays correct in the reference evaluator and unreachable from any host. Stage keeps faulting the session on
every outside append, including an irrelevant one, and #440 cannot meet 0085. Race behavior stays proven only by
internal specs.

## Timeline and scope

This record holds until superseded. It adds no ESM version and changes no reading of 0085 except its verification, as
amended below. In scope: the public API in item 1, `ExtendWorld`, the read-only refusal, the conflict contract, the
two-window rules (host obligations), the vectors and their specs, and documentation: the host API in `commands.md`, the decide, extend, commit, settle and
guarded-append loop in `reactions.md` (replacing the line quoted above), `ExtendWorld` in `semantic-model.md`, and a
race-vector section in `interoperability.md`. Consumer follow-up: Cratis/Stage#440 switches from the fault path,
inverts its spec and runs the vectors through its host.

## Verification

**Amends 0085 §3 verification:** "evaluator conformance vectors" means the published race vectors, executed through
the public API, in addition to the evaluator specs.

**Done when:** a host can run decide, extend, commit and settle using only public types; `ExtendWorld(Establish(a), b)`
and `Establish(a + b)` give equal worlds; one decision commits to accepted against one world and to a conflict against a longer one;
a read-only request is refused with Contract; the vectors and their SHA-256 are published; the repeat-commit vectors show one decision accepted against one world and rejected or conflicting against a longer one; `ISemanticEvaluator` and
the v1 to v25 goldens are byte-identical.

**Verify by:** the existing protected-read specs moved onto the public `ExtendWorld`; new specs for repeated commits,
exposed dependencies, the read-only refusal and each `ExtendWorld` case (world equivalence, malformed fact, reached reducer,
no publications, default facts array, prefix references); `for_ProtectedCommandReadRaceVectors` specs that load the
source, pin the ESM, revision and catalog, and run every vector through the public API. The whole-solution Debug test
command and the Release build on net8, net9 and net10. A `Decision: 0085, 0089` trailer on the implementing commit.
Generated vector files are regenerated and reviewed, never hand-edited.

## Consequences

Hosts can honor 0085 §3 without forking the evaluator, and a second target can prove the same conflict rule against
shared vectors. The library now commits to four public types and a vector file format, so renaming or reshaping them is
a breaking change. Hosts take on the append, retry and bounding loop. The decision forecloses a world callback inside
the evaluator and a host-specific copy of the matching rule.

## Related issues

Screenplay [#833](https://github.com/Cratis/Screenplay/issues/833), [#805](https://github.com/Cratis/Screenplay/issues/805);
Cratis/Stage#440. Decisions: [0017](0017-map-declared-decision-reads-to-chronicle-decision-reads.md),
[0053](0053-public-structural-model-comparison.md) (public library contract precedent),
[0085](0085-admit-protected-command-reads-through-an-explicit-preamble.md), 0088 (composite keys; a later fit, not admitted here).
