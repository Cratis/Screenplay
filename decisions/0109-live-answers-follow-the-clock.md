---
id: 0109
title: A live answer that reads the clock follows the clock
status: proposed
stage: none
class: contract
reversibility: costly
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Documentation/screenplay/**
---

## Context

Decision 0010 says a live query delivers the current answer, then change sets "as the answer changes". A query whose
`where` reads the clock (query predicates record) changes its answer when time passes, with no event.

Today nothing makes that happen:

- Arc subscribes to the query stream (`ObservableQueryDemultiplexer.Subscriptions.cs:314-352`) and has no timer-driven
  re-emission. The receipt instant is captured once and does not advance (`QueryContext.cs:28-34`). Emission guards
  check emissions but never create them (`observable-query-emission-guards.md`).
- Chronicle compiles the observed filter once and applies it when the collection emits
  (`IReadModels.cs:79`, `:82`; `ReadModels.cs:299-322`). Midnight alone causes no emission.
- Stage's live semantic query subscribes to world changes (`SemanticLiveQuery.cs:16-28`, `:55-79`).
- SoloCRM's wrapper filters upstream emissions only (`TodayRules.cs:61-67`), so a Today view left open overnight shows
  yesterday's list.
- The reference evaluator answers live queries with a snapshot (0069). `SemanticQueryRequest` carries a key and
  filters, not an evaluation instant (`SemanticExecutionContracts.cs:195-205`).
- Arc warns that ambient request context does not flow into stream emissions (`ObservableQueryEmissionContext.cs:23-31`),
  so a timer must carry explicit context.
- Chronicle has occurrence reminders and timers (`OccurrenceQueue.cs:18-33`, `:110-124`). An end-to-end calendar
  scheduler or query-invalidation API is not verified.

Evidence revisions: Screenplay `b65b3fd8`, Stage `f83af8fb`, Scene `fe5dd7d`, Arc `776ce82e4`, Chronicle `a4a1b7e26b`,
cli `accfd41`; 0105 as on branch `screenplay-1035-typed-capture-key` at `971012f4`. File and line references in this
record are to these revisions.

## Decision

1. **The contract.** A live answer equals the snapshot answer at the same instant. A live query whose condition reads
   the clock is re-evaluated on the server at every instant where time alone can change its answer. A change set is sent
   only when the answer changed.
2. **Evaluation epochs.** A snapshot uses one evaluation instant per request. A live query uses one instant per
   evaluation epoch. A new epoch starts on a data change or a clock boundary. A change of the calendar setting does not
   start an epoch in a running subscription; item 9 gives the rule. The receipt instant of the subscription is kept as metadata and is not reused as a moving clock.
3. **Boundaries for `today`.** A boundary is every instant at which the local calendar date in the zone changes, in
   either direction. It is not "24 hours from now", and it is not only the start of the next date: in a daylight-saving
   overlap the local date can go backwards and then forward again, and `today` is re-evaluated at each change. For
   example, in `America/St_Johns` on 1987-10-25 the local date is October 25 at 02:30Z, October 24 again at 02:31Z and
   October 25 again at 03:30Z, so there are three boundaries.
   - In a daylight-saving gap the start of a date can be a skipped local time; the boundary is the first valid instant
     of that date.
   - If a whole local date is skipped, the boundary is the start of the next existing date.
   - On an overlap (a local time that occurs twice), each instant at which the local date changes is a boundary; when
     the date does not change, the overlap adds none.

   After a wake or resume the query evaluates at the current instant and arms the next boundary.
4. **Boundaries for `now`.** For `now +/- D` compared with a stored instant `s`, the boundary is the earliest future
   instant at which a candidate item's comparison flips. Rewrite the comparison so that `now` stands alone on one side
   and let `T` be the other side: `now - D < s` becomes `now < s + D`, so `T = s + D`; `now + D < s` becomes
   `now < s - D`. Let `tick` be the tick precision of 0087 (one tick, 100 ns). As `now` advances past `T`, the
   comparison changes as follows:

   | Comparison | True before `T` | Transition instants |
   | --- | --- | --- |
   | `now < T` | yes | one: `T` (false from `T`) |
   | `now <= T` | yes | one: `T + 1 tick` (false from there) |
   | `now > T` | no | one: `T + 1 tick` (true from there) |
   | `now >= T` | no | one: `T` (true from `T`) |
   | `now == T` | no | two: `T` (true), then `T + 1 tick` (false) |
   | `now != T` | yes | two: `T` (false), then `T + 1 tick` (true) |

   Comparisons with `now` on the right mirror these. The boundary set is computed from the complete candidate set (item
   6), so no polling is needed. A live `now` condition whose next transition cannot be determined is refused for live
   delivery with a typed refusal. The first increment may refuse every live `now` condition and serve it as a
   snapshot.
5. **Not a trigger.** No event is written and nothing durable is scheduled. This is not a clock reaction (0006).
6. **Candidate set.** Boundaries are scheduled over the complete candidate set before `where`: every item that the
   query's source, `by` and caller filters admit, not only the items the query currently returns. An item that is
   filtered out now can enter the answer at a boundary.
7. **Where the mechanism lives.** The condition is not placed inside Chronicle's compiled `Observe` filter, because that
   filter freezes. Arc provides a `TimeProvider`-driven boundary primitive. Observable queries combine data emissions
   with boundary emissions, re-evaluate with the boundary instant as "now", and rely on Arc's change-set computation to
   drop no-op emissions.
8. **Specifications.** No new syntax. Snapshot specifications at two `given clock` instants pin the semantics. A
   Stage and Arc conformance vector pins the boundary emission with a fake `TimeProvider`. Conformance vectors cover at
   least: a skipped midnight (a zone whose date starts in a gap, and one whose whole date is skipped), a DST overlap,
   an overlap where the local date goes backwards (`America/St_Johns` 1987-10-25: 02:30Z, 02:31Z and 03:30Z),
   strict against inclusive comparisons (`<` against `<=`, `>` against `>=`), and equality with its second transition.
9. **Settings.** A live subscription keeps the zone it resolved when it started, for its whole lifetime (Arc decision
   0012). A changed application zone takes effect after a restart, because options are bound at start. A changed
   tenant zone applies to new subscriptions and to reconnects. A running subscription is not re-evaluated when the
   calendar setting changes, so until it reconnects its answer equals the snapshot answer at the same instant in the
   zone it resolved, not in the changed one. This qualifies item 1.
10. **No ESM change.** This record adds no bytes beyond the query predicates record.

## Options considered

- **Stay stale across clock boundaries until resubscription.** Rejected: it contradicts 0010 and is what SoloCRM has.
- **A client `interval` refresh** (Scene `IntervalInteractionTrigger`, `IntervalInteractionTrigger.cs:14`). Rejected: it
  puts the clock on the client, costs load per client, and is stale between ticks.
- **A Chronicle clock reaction that rewrites read models at midnight.** Rejected: it writes derived state, and Stage
  cannot render clock reactions (`ReactionSliceRenderer.cs:25`, `:108`).
- **Push the condition into `Observe<T>(filter)`.** Rejected: compiled once.
- **Periodic polling for everything.** An explicit fallback only; it gives no exact boundary semantics.
- **Domain tick events.** Rejected: freshness of a list is not domain history.
- **A durable scheduler per subscriber.** Rejected: invalidation is subscription-local and can be ephemeral.
- **A new epoch on a calendar-setting change.** Rejected with Arc decision 0012: it needs an Arc change signal and
  invalidation fan-out to every affected subscription, for a setting that rarely changes. Reconnecting picks up the
  new zone.

## Consequences

- **Arc:** the boundary primitive, small to medium, testable with a fake `TimeProvider`. Timers hold explicit tenant and
  identity context and must be disposed with the subscription.
- **Stage:** the renderer and the live Host share one re-evaluation path over their world model.
- **Scene, Chronicle, CLI:** none; the CLI refuses unsupported live plans.
- Every subscriber re-evaluates at midnight at the same moment. The cost per subscription is bounded; batching is
  deferred until profiles show a need (unmeasured).
- Reconnects, clock corrections, daylight-saving transitions and selection changes after a refresh need conformance
  vectors.
- Live `now` stays unavailable until next-flip evaluation exists. Live Stage must not claim admission before then.

## Open points

1. Whether a deliberately bounded staleness (a declared refresh cadence) is wanted for expressions whose next flip
   cannot be computed.
2. Whether the Arc decision holds the boundary primitive alone or also the zone abstraction.

## Related

Supplements 0010 (live semantics). Builds on 0006 and 0069; the query predicates record and the application calendar record.
