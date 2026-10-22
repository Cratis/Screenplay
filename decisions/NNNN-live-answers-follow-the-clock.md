---
id: NNNN
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
  (`ReadModels/IReadModels.cs:82-90`; `ReadModels.cs:299-322`). Midnight alone causes no emission.
- Stage's live semantic query subscribes to world changes (`SemanticLiveQuery.cs:16-28`, `:55-79`).
- SoloCRM's wrapper filters upstream emissions only (`TodayRules.cs:61-67`), so a Today view left open overnight shows
  yesterday's list.
- The reference evaluator answers live queries with a snapshot (0069). `SemanticQueryRequest` carries a key and
  filters, not an evaluation instant (`SemanticExecutionContracts.cs:195-205`).
- Arc warns that ambient request context does not flow into stream emissions (`ObservableQueryEmissionContext.cs:23-31`),
  so a timer must carry explicit context.
- Chronicle has occurrence reminders and timers (`OccurrenceQueue.cs:18-33`, `:110-124`). An end-to-end calendar
  scheduler or query-invalidation API is not verified.

## Decision

1. **The contract.** A live answer equals the snapshot answer at the same instant. A live query whose condition reads
   the clock is re-evaluated on the server at every instant where time alone can change its answer. A change set is sent
   only when the answer changed.
2. **Evaluation epochs.** A snapshot uses one evaluation instant per request. A live query uses one instant per
   evaluation epoch. A new epoch starts on a data change, a setting change that affects the calendar, or a clock
   boundary. The receipt instant of the subscription is kept as metadata and is not reused as a moving clock.
3. **Boundaries for `today`.** The boundary is the next midnight in the effective calendar, which is not "24 hours from
   now". After a wake or resume the query evaluates at the current instant and arms the next boundary.
4. **Boundaries for `now`.** For `now +/- D` compared with a stored instant, the boundary is the earliest future instant
   at which a current item's comparison flips. It can be computed from the items being filtered, so no polling is
   needed. A live `now` condition whose next flip cannot be determined is refused for live delivery with a typed
   refusal. The first increment may refuse every live `now` condition and serve it as a snapshot.
5. **Not a trigger.** No event is written and nothing durable is scheduled. This is not a clock reaction (0006).
6. **Where the mechanism lives.** The condition is not placed inside Chronicle's compiled `Observe` filter, because that
   filter freezes. Arc provides a `TimeProvider`-driven boundary primitive. Observable queries combine data emissions
   with boundary emissions, re-evaluate with the boundary instant as "now", and rely on Arc's change-set computation to
   drop no-op emissions.
7. **Specifications.** No new syntax. Snapshot specifications at two `given clock` instants pin the semantics. A
   Stage and Arc conformance vector pins the boundary emission with a fake `TimeProvider`.
8. **Settings.** A change of the calendar either invalidates affected live queries or requires a new subscription. The
   choice is stated by the Arc decision.
9. **No ESM change.** This record adds no bytes beyond the query predicates record.

## Options considered

- **Stay stale until resubscription.** Rejected: it contradicts 0010 and is what SoloCRM has.
- **A client `interval` refresh** (Scene `IntervalInteractionTrigger`, `IntervalInteractionTrigger.cs:14`). Rejected: it
  puts the clock on the client, costs load per client, and is stale between ticks.
- **A Chronicle clock reaction that rewrites read models at midnight.** Rejected: it writes derived state, and Stage
  cannot render clock reactions (`ReactionSliceRenderer.cs:25`, `:108`).
- **Push the condition into `Observe<T>(filter)`.** Rejected: compiled once.
- **Periodic polling for everything.** An explicit fallback only; it gives no exact boundary semantics.
- **Domain tick events.** Rejected: freshness of a list is not domain history.
- **A durable scheduler per subscriber.** Rejected: invalidation is subscription-local and can be ephemeral.

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
