---
id: 0080
title: Treat the first record of a key in a record capture as added only, as Chronicle does
status: accepted
stage: none
decided: 2026-10-15
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
supersedes-in-part: 0022
applies-to:
  - Source/DotNET/Screenplay/Semantics/Execution/SemanticCaptureEvaluation.cs
  - Source/DotNET/Screenplay/Semantics/Execution/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Documentation/screenplay/captures.md
  - Documentation/screenplay/specifications.md
  - Samples/**
---

## Context

The reference evaluator treats the first record it sees for a key in a record capture (`source api`, polls and other
outside sources) as changed in every field it carries (`SemanticCaptureEvaluation.Changed`, documented with decision
[0022](0022-esm-v6-time-triggers-captures-and-reactions-in-specifications.md)'s capture rules). `when <field>`
and `and`/`or` over field changes therefore hold on a first record (transitions already require an earlier record). Chronicle does the opposite, with one engine for record and
event captures (v19.68.0: `CaptureChangeDetector.cs:22-28`, `WhenClauseEvaluator.cs:19-24,37-40`, `Capturer.cs:164-168`):
an unknown key is `Added` with no previous state; field changes, `and`/`or` and transitions require a `Modified` change.
[Decision 0077](0077-present-consumed-public-events-to-captures-over-events.md) already follows Chronicle for captures
over events and left record captures for [#767](https://github.com/Cratis/Screenplay/issues/767). Today a record-capture
specification can pass in the reference and behave differently when Stage renders it to Chronicle; two capture families
disagree on the same question.

## Decision

1. **A record with no previous state is `added`, not changed.** Previous state is what the reference already uses: the
   last `given capture` of the same capture with the same typed key (a specification has one action, so there is no
   earlier `when capture`; a list in a record is a child collection, not a sequence of observations). When there is no
   previous state, field-change conditions (`when <field>`, and `and`/`or` combinations of them) do not hold; `when added`
   holds as today. Transitions already require previous state and are unchanged. When there is previous state, comparison
   is exactly as today (a field the earlier record lacked has changed when the new one has it).
2. **Children and nested scopes follow the same principle at their own level.** A child whose identity has no
   counterpart in its parent's previous collection, including every child of a root with no previous state, has no
   previous state; a nested record whose field had no previous value has none either. Field-change conditions do not
   hold on them; `when added` does.
3. **Expression guards, `map` and unguarded appends keep their current meaning**, as 0077 kept them for captures over
   events. Chronicle refuses expressions, maps, children and nested scopes in captures today, so these are reference-only
   behaviors that Stage must refuse or realize; this decision does not change them.
4. **Same rule for both capture families.** Record captures and captures over events now share the first-observation
   rule; the reference keeps one code path for it.
5. **No grammar, ESM or binding change.** Bytes, goldens and versions are unchanged. The reference outcome is corrected
   across every ESM version that admits captures (v6 and later), as precedents 0054 item 11, 0071, 0072 and 0077 did.
   Frozen canonical vectors stay byte- and outcome-identical (none encodes a first-record field change: the v6 corpus
   and the serialization goldens seed their baselines); a new regression vector pins the corrected outcome.
6. **Migration.** A specification that relied on the old rule now fails with the events it expected but did not get.
   If the append should also run when a key first appears, add a separate append with `when added` and keep the
   field-guarded append for later updates; if the scenario meant an update, seed the key's earlier state with
   `given capture` so the presented record is a modification. The release note (minor) names the change, why
   (Chronicle behaves this way), and both repairs. No compiler diagnostic is added: whether a scenario's record is the
   first for its key is a property of a specification run, not of the model.
7. **Surfaces and consumers.** Reference evaluator and its specifications (the runner spec that pinned the old rule is
   rewritten to pin the new one, plus specs for children and nested scopes), a new regression vector, `captures.md` and
   `specifications.md`, and samples whose specifications depend on first-record behavior (the TimeTracking absence
   imports already expect `added`-only outcomes and start passing). The TypeScript compiler has no reference evaluator,
   so it is unchanged. The implementing change files a Cratis/Stage issue (its capture fixtures and conformance must
   expect `added`-only first observations) and a Cratis/AI issue (skills that show first-observation capture examples).

## Options considered

- **Keep 0022's rule for record captures.** Not taken: specifications pass in the reference and diverge when rendered.
- **Ask Chronicle to change.** Not taken: Chronicle's rule is consistent for both feeds and deliberate (no previous
  state means no change to describe); changing it would alter running systems.
- **A diagnostic for specifications that present a first record to a field-guarded append.** Not taken now: it is a
  run-time property; the runner's failure already shows the missing events.

## Default if unanswered

Record-capture specifications keep passing in the reference where Chronicle would append nothing, and the two capture
families keep different first-record rules.

## Timeline and scope

Holds until superseded. In scope: the first-observation rule for record captures (root, children, nested) in the reference
evaluator, a regression vector, docs, samples and the consumer issues. Out of scope: Stage realization, Chronicle's
per-cycle snapshot (a key absent from the last snapshot becomes `added` again; the reference keeps 0022's immutable
scenario baseline), expressions, maps, and any grammar or ESM change.

## Verification

**Done when:** with no previous state, `when added` holds and field-change conditions do not, at root, child and nested
level; expression guards and unguarded appends behave as before; a `given capture` for the same key makes the presented
record a modification and one for a different key does not; frozen vectors are unchanged and a new vector plus the
rewritten runner spec pin the rule; samples compile and their specifications pass. **Verify by:** runner specifications, the canonical vector specs, and a golden
diff against `main` showing unchanged bytes.

## Consequences

Record captures specify what Chronicle does. Existing models whose specifications relied on the old rule fail until they
add `when added` or seed state, which the release note explains. Decision 0022's first-record wording is superseded in
part; 0022 gets a dated banner pointing here.

## Related

Decisions 0022, 0077; #767; #483. Consumers: Cratis/Stage#421, Cratis/AI#716.
