---
id: 0074
title: Admit explicit no-event assertions and redelivery before reaction refusal branches
status: accepted
stage: none
decided: 2026-10-13
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Documentation/screenplay/**
---

## Context

[0030](0030-reaction-refusals-and-redelivery.md) accepted three specification and reaction features for [#433](https://github.com/Cratis/Screenplay/issues/433): ordered `on refused` branches on `invokes`, `when redelivered <Event> to <Reaction>`, and `then no events`. It requires all three to be admitted **together** in one new executable model (ESM) version, and it makes [Arc#3058](https://github.com/Cratis/Arc/issues/3058) (reactor code can tell why an invoked command was refused) and [Stage#214](https://github.com/Cratis/Stage/issues/214) (rendering refusal branches) prerequisites of that admission.

Those prerequisites only concern refusal branches. Arc#3058 has not started, so the joint admission holds back two features that need nothing from Arc:

- `then no events` asserts that an action produced no new facts. [#699](https://github.com/Cratis/Screenplay/issues/699) needs it now: the `EventRoutes/observer-filters` corpus asserts that a reaction did **not** fire, and today that check is vacuous because a specification without `then` events never compares facts. Stage's three-way conformance for reaction filters ([Stage#297](https://github.com/Cratis/Stage/issues/297)) inherits the gap.
- `when redelivered` delivers an existing given fact to one reaction again. A runtime already does this on recovery, so it needs nothing from Arc#3058. Consumers still have to run it: Stage renders and executes redelivery specifications under its own issue ([Stage#371](https://github.com/Cratis/Stage/issues/371)), separate from refusal branches (Stage#214). 0030's other redelivery condition is met: [#490](https://github.com/Cratis/Screenplay/issues/490) is closed, so locators can state a stream or `no stream` (decision 0037, ESM v8 specification routes).

0030 also rejects `then no events` after `when append` "in this increment". Since ESM v6 the facts that follow `when append` are what reactions appended (`specifications.md`, "An appended event is the action itself"), which is exactly what #699 must assert is empty.

## Decision

Admit `then no events` and `when redelivered` in the next ESM version, numbered at admission under [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) and frozen on publication under [0066](0066-published-esm-versions-are-frozen.md). Refusal branches and `$refusal` values stay refused with `PLAY0268` and are admitted later in their own version, once Arc#3058 lands; 0030's prerequisites apply to that later version only. Once the first version is admitted, the `PLAY0268` refusal splits: it names only reaction refusal branches and `$refusal` values, and no longer redelivery or explicit no-event assertions, which bind in the new version and stay refused below it.

In the first version:

- `then no events` asserts that the action produced no new facts: no command-produced facts, and no facts appended by the reactions it set off. After `when append` it asserts that no reaction appended anything; the appended fact itself is the action, not an outcome. It counts as the success outcome an action specification must have. It still conflicts with event, event-order, error and denial expectations (`PLAY0545`), and may accompany read-model, query and response assertions.
- `when redelivered` keeps 0030's semantics and canonical form unchanged: the locator selects exactly one given fact, `whenRedelivered: {"reaction": "<id>", "given": <index>}`, the reference fires only the named reaction against that fact and settles the ordinary cascade, never appending the fact again. A refused invocation during redelivery ends the scenario as any unhandled refusal does today and is assertable with `then error`.
- Canonical members are written only when used, so models without the features keep their bytes, revisions and outcomes. A strict reader below the new version rejects both members.

Everything else in 0030 stands: branch semantics, `$refusal` values, stop-after-handled, the redelivery locator rules and the reasons recorded there.

## Options considered

- **Admit no-event assertions and redelivery first; refusal branches later (proposed).** Unblocks #699 and redelivery specifications now and keeps refusal semantics exactly as accepted. Cost: two version bumps instead of one, and consumers admit #433 in two steps.
- **Keep the joint admission (0030 as is).** Not proposed: `then no events` and redelivery wait on Arc work they do not use, and #699's corpus stays vacuous for an unknown time.
- **Admit only `then no events` now.** Smallest change and the only one with a consumer waiting today. Not proposed because redelivery has no external prerequisite either, and splitting it from no-event assertions would cost a third version. If the decider prefers it, redelivery joins refusal branches instead.
- **Make omitted `then` events mean "no events" in existing versions.** Rejected, as in 0030: it changes the accepted contract of released versions.

## Default if unanswered

0030 applies unchanged: nothing from #433 is admitted until Arc#3058 and Stage#214 allow refusal branches, #699's "reaction did not fire" specifications stay vacuous, and Stage's reaction-filter conformance keeps a gap.

## Timeline and scope

Holds until both #433 versions are admitted. In scope: splitting the admission, `then no events` after `when append`, and the first version's canonical and reference behavior. Out of scope: refusal branch semantics (0030), Arc's refusal classification, Stage rendering, and any change to released versions.

## Verification

**Done when:** a new ESM version binds `then no events` (including after `when append`) and `when redelivered`; the reference fails a no-event specification when a command or reaction appends a fact and passes when none does; redelivery fires only the named reaction against the located given fact; refusal branches and `$refusal` values still fail binding with `PLAY0268`; prior-version golden and corpus bytes are unchanged; the observer-filters corpus asserts its no-reaction cases with `then no events`.

**Verify by:** binder, strict-reader and canonical round-trip specifications; reference-runner specifications for both features; a new golden and source-backed corpus vectors for the version; the interoperability version table check; existing golden and corpus comparisons.

## Status notes

**2026-10-13 — accepted with changes.** Accepted by Sindre Alstad Wilting (delegated through the coordinator after an independent review) with three changes: #490's closure is recorded, the redelivery consumer work is named (Stage#371), and the `PLAY0268` message split is stated. Redelivery stays bundled with `then no events`.

**2026-10-13 — refusal-branch prerequisites amended.** Arc#3058 is released (Arc 22.88.0) and Stage#214 is no longer an admission prerequisite but the consumer issue of the refusal-branch version (see the dated note in [0030](0030-reaction-refusals-and-redelivery.md)). Refusal branches follow this record's version as the next admission. Accepted by Sindre Alstad Wilting (delegated through the coordinator) on 2026-10-13.

## Consequences

Authors can state "nothing followed" and recovery redelivery as executable specifications without waiting for Arc. Consumers (Stage, Studio, Arc's generator) admit #433 in two versions. Refusal branches keep 0030's guarantees and prerequisites.
