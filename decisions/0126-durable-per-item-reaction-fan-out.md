---
id: 0126
title: Fan a reaction out over a collection with durable per-item completion (at-least-once)
status: proposed
stage: none
class: contract
reversibility: costly
decider: direction by woksin on 2026-10-30; detailed contract proposed
amends: 0006
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/reactions.md
---

## Context

A reaction that invokes a command for every element of an event's collection has no portable meaning today.
[0006](0006-reaction-triggers-declare-reads.md) reserves `for each <View>` for a view-driven trigger and does not admit it.
[0030](0030-reaction-refusals-and-redelivery.md) names collection fan-out (#286) out of scope, together with durable
external-effect deduplication. Cratis/Screenplay#286 asks for it. woksin's direction of 2026-10-30 covered direction only: durable
per-item fan-out, in its own record. The syntax, lifecycle and admission details below are proposals, not woksin-approved
contracts. Both records stay drafts until proven; no accepted contract is amended by this draft.

Facts in the target runtimes at `origin/main` on 2026-10-30:

- Chronicle: `ReactorDelivery` identifies one event delivered to one reactor partition. Recovery and replay re-read the same
  sequence numbers and produce the same `Id` (`ReactorDelivery.cs:40-65`). It states it is not exactly-once and that a
  process dying between effect and record repeats the effect (`:29-35`). It derives idempotency keys per delivery, effect
  epoch and effect name (`IdempotencyKeyFor`, `ReturnedEffectKey(index)`, `:106-118`), for use with
  `IEventSequence.AppendOnce` (`:100-101`).
- Arc: `CommandSideEffectExecutor` runs invoked commands in order and stops at the first failure; a refusal handled by a
  branch ends the remaining invocations (`CommandSideEffectExecutor.cs:87-96`, `58-79`). It keeps no per-element progress.
- Screenplay: reaction execution exists in the reference runner (`Semantics/Execution/SemanticReactionBodyExecution.cs`).
  The 2026-09-27 comment on #286 says reactions did not bind into the ESM; this was not re-verified for the fan-out case.
  There is no `for each` in the parser (`git grep 'for each'` on the parsing folder finds only comments).

The 2026-09-27 design correction on #286 supersedes the earlier proposal: #70 did not ship replay syntax, and nothing may
imply exactly-once external effects.

## Decision

Proposed detailed contract, subject to proof and acceptance:

1. **Guarantee.** Fan-out is resumable at-least-once execution with completion suppression. It is never exactly-once.
   Where a stated "exactly once" would be needed, the author must make the receiver idempotent.
2. **Form.** A reaction invocation may take `for each <name> in <collection>`, where the collection is a property of the
   triggering event (the design on #286 also shows `payloads` read first; exact grammar is fixed at implementation).
   `for each` over a view stays reserved under 0006.
3. **Order.** Elements run one at a time in index order. The collection must be finite and is read once from the
   delivered event; its length does not change during a delivery.
4. **Completion key.** Durable completion is keyed by delivery id × stable invocation id × zero-based element index. The
   delivery id is Chronicle's. The invocation id is derived from the ESM identity of the invocation, not its position
   in the file, so reordering or renaming source does not move completed work.
5. **Receipt order.** A receipt is never written before the effect. If the effect succeeds and the receipt write fails,
   the element runs again. Recovery skips elements with a receipt and retries the first without one.
6. **Local effects.** An appended event and its receipt are one atomic append, using Chronicle's idempotency key per
   delivery, invocation and element, so this path is effectively once.
7. **External effects.** A command with an external effect must carry an idempotency key derived from the same triple, or
   the receiver must support reconciliation. A fan-out whose invoked effect has neither is refused at admission.
8. **"Already done".** A constraint violation acknowledges an element as complete only when structured constraint
   identity proves a durable terminal fact of the same logical operation and no unrelated failure or uncertain commit is
   present. Matching a constraint name or message alone is insufficient. A batch with one duplicate does not prove the
   whole command done. Decision 0030's stop-after-handled-refusal is preserved: acknowledging an invocation's handled
   refusal never continues later invocations. It ends this trigger's remaining invocations, not merely the current
   element. Recovery must preserve that terminal outcome rather than resume the skipped remainder.
9. **Replay.** Replays keep receipt identity and suppress external dispatch by default. Erasure of receipts follows the
   data-subject rules (0008); the receipt holds ids and indexes, not payload.
10. **Failure.** An unhandled refusal or failure at element k stops the fan-out there, leaves receipts for 0..k−1, and the
    delivery retries from k. Transient concurrency conflicts are not refusals (0030).

## Obligations

- **Chronicle** owns durable progress and the claim and completion lifecycle, keyed as in item 4, with an atomic
  receipt-plus-append primitive. It must not mark completion before the effect.
- **Arc** owns returned-command orchestration, the opt-in progress seam in the side-effect executor, and structured
  acknowledgement by constraint identity.
- **Stage** admits a backend only if it declares the progress capability, and renders receipts and checkpoints. Without
  the capability it refuses the ESM version under 0004.
- **Screenplay** binds the portable intent, adds an ESM version numbered at admission under 0025 (none claimed here),
  and extends the reference runner with the same completion key.

## Admission proof

No grammar-only release. Before admission, fault-injection specs prove: a crash before the effect, between effect and
receipt, and after receipt; a crash during atomic receipt-plus-append; a duplicate delivery of the whole event; a replay;
an unrelated failure beside a constraint violation; and an external effect with and without an idempotency key. Each shows
the recovered run's effects and receipts. A handled-refusal vector must prove that later invocations do not run,
including after recovery. Syntax, binding, ESM and Stage land together under 0004. These proofs are prerequisites,
not results claimed by this draft.

## Non-goals

Exactly-once external effects; parallel or batched fan-out; fan-out over unbounded or view-driven collections; cancelling
or reordering in flight; changing 0030's refusal semantics; scheduling.

## Stop conditions

Stop and report if a required primitive is missing in Chronicle or Arc, receipt schema evolution is ambiguous, or an
external effect has neither idempotency nor reconciliation.

## Options considered

- **Durable per-item fan-out with receipts (proposed).** Bounded rework after a crash.
- **Re-run the whole fan-out on recovery.** Rejected: every element repeats its effect.
- **Claim exactly-once.** Rejected: Chronicle itself disclaims it.
- **Match refusals by name or message.** Rejected: not proof of the same operation.

## Default if unanswered

Authors loop in code, with no portable guarantee and no Stage rendering.

## Consequences

Large fan-outs are resumable and bounded in repeated work. Authors must design receivers for idempotency, and Chronicle,
Arc and Stage each carry a prerequisite change.

## Related

Proposes an amendment to 0006. Builds on 0004, 0025 and 0030. Cratis/Screenplay#286, #70.
