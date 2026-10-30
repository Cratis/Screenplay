---
id: 0123
title: Store-backed sequences through a Chronicle allocator (design only, no gapless guarantee)
status: proposed
stage: none
class: contract
reversibility: costly
decider: proposed by L73 for woksin's review (Q-168)
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/Screenplay/Compiler/**
  - Documentation/screenplay/generated-values/**
---

## Context

Businesses ask for readable, per-period numbers such as invoice `2026-000123` (Cratis/Screenplay#619, epic #614).
Decision [0054](0054-concept-generation-strategies-and-freshness.md) reserved `sequence`: `generate sequence` reports
binding error `PLAY0682` (`ReservedSequenceGeneration`, `Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Generation.cs:37`,
`Documentation/screenplay/diagnostics.md:964`, `generated-values.md:103`, `generated-values/strategies.md:23`).

The existing strategies cannot produce such numbers. `random` and `pattern` draw values independently, so they are not
ordered and can collide. `generate rule` has no store (0054, "Options considered": "a rule has no store"). Teams will
fake a sequence by reading the highest number in a read model or in `Handle` and writing n+1. Two concurrent commands
then read the same n and both write n+1, which gives a duplicate. A command that fails after reserving a number in a
separate step leaves a gap.

On 2026-10-29 woksin ruled on #619 that sequences get a **design-only decision**. The decision is conditional on
atomic allocation, retry-scope revalidation and freshness evidence, and **no gapless guarantee is claimed**. The
reason: "The guarantee cannot be stated until it is verified."

What exists today:

- **Multi-event batches.** Chronicle validates a `BeforeFirst` scope in a multi-event, multi-source batch against the
  state before the batch, and each source independently (Cratis/Chronicle#4712, closed 2026-10-13).
- **Retry revalidation.** Since kernel 19.40.1, Chronicle validates concurrency scopes again on every renumbered
  storage retry (Cratis/Chronicle#4713, closed 2026-10-13; Chronicle `Documentation/events/concurrency.mdx:200`). The
  same page says that **constraints are not run again on that path** (`Documentation/read-models/decision-reads.mdx:134`),
  and that writes that bypass the kernel's append path are never guarded.
- **Fresh-identifier guard.** Arc never reruns `Handle()`, and a collision is returned to the caller
  (Cratis/Arc#3129, closed 2026-10-14; Arc `Documentation/backend/csharp/chronicle/commands/fresh-identifiers.md:79,181-182`,
  which requires kernel 19.40.1 or later).
- **No allocator primitive.** Chronicle has no atomic counter or allocator. Allocation would combine decision reads
  ([0017](0017-map-declared-decision-reads-to-chronicle-decision-reads.md)), concurrency scopes and unique constraints.

## Decision (proposed)

1. **Design only.** This record defines the direction. It does not admit anything. `sequence` stays reserved:
   `PLAY0682` keeps refusing `generate sequence` in binding, describe, propose-ast and propose-source until an admission
   decision replaces this record. The language, ESM, printer and MCP surfaces do not change.
2. **Allocator.** Each counter key is its own allocator event source in the event log. A key is the counter name plus
   its scope parts, for example `InvoiceNumber/2026`. Its canonical form is an ordinal string, as for freshness in 0054
   item 8. The tenant is not part of the key, because the event-store namespace separates tenants. A period part (such
   as the year) is computed from `Now` in the generation context (0054 item 9). Its time zone is an open question.
3. **Atomic allocation.** Inside the command's unit of work:
   - the command reads the counter's last allocated value `n` through a decision read;
   - it appends `<Counter>Allocated(n+1)` to the counter source with a concurrency scope on the tail it read, or with
     `BeforeFirst` for the first number;
   - it appends the business event that carries `n+1` in the **same atomic batch**.

   A unique constraint on `(counter key, number)` is a second guard, but only alongside the scope (see item 5). A
   command that aborts appends nothing, so it allocates nothing.
4. **Contention.** A scope or constraint violation is returned to the caller as a concurrency outcome. Arc does not
   rerun `Handle` (Cratis/Arc#3129). The caller resubmits, the new invocation reads again and gets a new number. Arc
   needs an allocation step inside the command transaction (Arc.Chronicle). That step is not built yet.
5. **Retry-scope revalidation.** Uniqueness rests on the **scope** because the kernel revalidates scopes on a
   renumbered retry, but not constraints. Admission requires these:
   - kernel ≥ 19.40.1 declared as the minimum version;
   - race vectors in which two allocations for one key give exactly one success;
   - a vector in which the tail changes between storage attempts and gives a violation.

   Admission also requires one of these: the kernel runs constraints again on retry, or a proof that the scope alone
   is sufficient.
6. **Freshness evidence.** When a sequence value is also an event-source id, the 0054 item 8 freshness guard applies.
   Admission needs the same evidence as for low-entropy identifiers: the Arc guard together with the Chronicle
   minimum version.
7. **Reference-evaluator portability.** The reference evaluator computes the value from facts:
   `n = 1 + the highest number among the given <Counter>Allocated facts for that key`, or the start value when there are
   none. It does not use a seed or a fixture. A `generated <name> = <value>` pin (0054 item 12) is then an assertion
   that must equal the computed value. Both compilers and the evaluator need the same vectors.
8. **No gapless claim.** Sequence values are unique per counter key and increase in commit order. Gaps are possible:
   - writes that bypass the kernel append path;
   - imports or migrations;
   - event redaction or removal;
   - an older kernel;
   - any path that is not covered by the evidence in items 5 and 6.

   Documentation may say "unique per counter" and "increasing" once admitted. It must not say "gapless",
   "consecutive" or "no missing numbers" unless a later decision proves it.

## What admission would require

- The Chronicle evidence in items 5 and 6, at a declared minimum kernel version.
- The Arc.Chronicle allocation step and its race specs, with collisions reported as concurrency outcomes.
- Syntax for `generate sequence`: counter name, scope parts and period, start value, and format or padding (for
  example `{year}-{n:000000}`). Also an ESM `generation` member `{"kind":"sequence",…}` whose version is allocated at
  admission under [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md), and
  governance under [0004](0004-admission-and-governance-of-portable-executable-semantics.md).
- Reference-evaluator semantics (item 7) with vectors in both compilers.
- Every surface, per the end-to-end rule: runner, MCP, editor, Studio, Stage, Arc and AI skills.
- Documentation: a "sequences: why not yet" section in `generated-values/choosing.md` now, replaced by a full page at
  admission.
- `PLAY0682` is retired only by that admission decision.

## Open questions for woksin

1. Should the counter's `Allocated` event be a Screenplay-generated system event or an authored event?
2. Which time zone sets the period boundary: UTC, or a declared zone on the counter?
3. Is the unique constraint kept as a second guard even though it is not rerun on retry, or dropped once the scope
   evidence exists?
4. Should Chronicle get a native allocator primitive (an atomic increment in the kernel) instead of
   decision-read + scope? That would remove the read, but it is a Chronicle feature request.
5. Must a sequence be usable as an event-source id, or only as a property value? The answer decides whether item 6 applies.

## Options considered

- **`generate rule`.** Rejected: it has no store (0054).
- **Read-model counter or read-then-write in `Handle`.** Rejected: it gives duplicates under concurrency and
  eventual-consistency lag.
- **`random` or `pattern` with a uniqueness constraint.** Rejected: the numbers are not ordered, and a collision only
  shows up as a retry.
- **Unique constraint alone.** Rejected: it is not rerun on renumbered retries (Chronicle `decision-reads.mdx:134`).
- **Claiming gapless now.** Rejected by woksin on 2026-10-29: it is not verified.

## Consequences

- No change to code, syntax, ESM or diagnostics. `PLAY0682` and the "reserved" documentation rows stay as they are.
- Follow-ups, once woksin accepts the direction:
  - the `choosing.md` "why not yet" section (Screenplay);
  - an Arc.Chronicle allocation-step issue;
  - a Chronicle issue for constraint rerun on retry, or for a native allocator (open question 4).

## Related

Cratis/Screenplay#619, #614, #615 (0053/0054 track); 0054 items 8, 9 and 12; 0017; 0025; 0004.
Cratis/Chronicle#4712 and #4713; Cratis/Arc#3129.
