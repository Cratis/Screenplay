---
id: 0108
title: Admit fixed query `where` predicates with clock operands
status: proposed
stage: none
class: contract
reversibility: costly
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

A list such as "overdue tasks", "tasks due today" or "dormant opportunities" is a fixed question about stored state and
the clock. Screenplay has no way to state it. Decision 0069 admits caller-supplied equality filters only: a filter
narrows to the same-named property and combines with AND (0069, "Meaning"). `scoped to`, `performer` and `from` stay
refused with `PLAY0268` (0069). Ordered or date-relative predicates have no home.

SoloCRM shows the cost. Its three time lists are plain `observable X[]` queries (`Crm.play:446`, `:664-665`), and the
agent filtered them by editing generated code (`Source/Crm/Tasks/Tasks/Tasks.cs:29-32`, `Pipeline.cs:34`;
`TodayRules.cs:31`, `:36`, `:41-58`). The next render overwrites those edits, and the model, the specifications and
Studio cannot see the meaning.

Facts that shape the decision:

- The command condition grammar is already admitted, evaluated and rendered (0087, 0090): comparisons, `and`, `or`,
  parentheses, enum literals and `null`. A query can reuse one expression node, one evaluator and one renderer path.
- The dormancy rule in `TodayRules.cs:40-58` means **at least seven calendar days** since the later of `ReceivedOn`
  and the local day of the last activity, with a missing stage status counted as open. The product specification asks
  for an adjustable threshold (`requirements/solocrm/docs/SPEC.md:360-362`); the code fixes it at seven
  (`TodayRules.cs:20-21`).
- Chronicle `IReadModels.Observe<T>(filter)` compiles the filter once (`IReadModels.cs:79`, `:82`; `ReadModels.cs:319-322`). A clock
  value captured in it would freeze, which decision 0109 handles.
- Decision 0010 orders by one field and pages by page number; the reference evaluator answers live queries with a
  snapshot (0069).
- A stored `isOverdue` property goes stale, because time passing writes no event. A "task became overdue" event per
  item needs a per-instance due time that 0006 deferred, and puts zone-dependent noise into the immutable log.

Evidence revisions: Screenplay `b65b3fd8`, Stage `f83af8fb`, Scene `fe5dd7d`, Arc `776ce82e4`, Chronicle `a4a1b7e26b`,
cli `accfd41`; 0105 as on branch `screenplay-1035-typed-capture-key` at `971012f4`. File and line references in this
record are to these revisions.

This record is allowed by the record that lifts the freeze (0105 on branch `screenplay-1035-typed-capture-key`). It
uses the clock operands and the application calendar of the application calendar record.

## Decision

1. **A fixed `where` on a query.** A query may carry one `where` condition. It is part of the query's contract, not a
   caller filter, and it is evaluated on the server only.

   ```screenplay
   query OverdueTasks => observable TaskItem[]
     where isCompleted == false and dueDate < today

   query DormantOpportunities => observable Opportunity[]
     argument dormantDays Integer default 7
     where isArchived == false
       and (stageStatus == null or stageStatus == Open)
       and receivedOn <= today - dormantDays days
       and (lastActivityAt == null or date of lastActivityAt <= today - dormantDays days)
   ```

2. **What it can say.** The command condition grammar over properties of the returned read model: comparisons, `and`,
   `or`, `not`, parentheses, enum literals, `null`, `today`, `now`, their arithmetic and `date of`. Types follow
   0087: `Date` with `Date` and `DateTime` with `DateTime`, never mixed.
3. **Combination.** `by` selects, then caller filters narrow, then `where` narrows. All combine by conjunction. `where` is
   allowed on a single-result query, where it is a narrower selection; an instance that fails it counts as absent.
   Filtering happens before paging and ordering (0010).
4. **Absence.** A missing value (the member is not there) is not an authored null (the member is there and null). This
   follows 0029 (line 24: "comparisons are false, except `== null`, which matches only null. Missing is not an
   authored null value") and the Stage guard specification that already implements it.
   - `x == null` matches an authored null only.
   - `x != null` matches a present, non-null value only.
   - Every other comparison (`==`, `!=`, `<`, `<=`, `>`, `>=`) with a missing or an authored-null operand is false. In
     particular `!=` is false for both; it is not the negation of `==`.
   - A missing value matches neither `x == null` nor `x != null`.
   - `not` negates the result of the comparison it wraps. `not (x == 5)` is therefore true for a missing `x`. Authors
     who mean "present and not 5" write `x != null and x != 5`.
   - Optional property-to-property comparisons (`a <= b`, either side optional) are admitted, with the same rule: false
     when either side is missing or null. This replaces the earlier refusal.
   - Validation rules keep their own rule (0087 item 5): an absent optional value passes every rule except `not empty`.
     This record does not change it.
   - Optional operands in command conditions (`require`, `produces when`) stay refused, as 0087 item 5 says. The
     command clock record does not extend them.
   - Generated C# and Chronicle predicates need explicit presence checks for each rule above. A lifted `!=` on a
     nullable value is true for a null and is wrong here.

   A row whose stage status is missing is not matched by `stageStatus == null`. SoloCRM counts a missing stage status
   as open (`TodayRules.cs:40-58`), so the read model must carry an authored null for it, or the model needs an explicit
   test for a missing member; open point 4.
5. **Dates and times.** A `Date` property compares with `today`-based `Date` values. A `DateTime` property compares
   with `now`-based `DateTime` values, or is converted with `date of` to compare with a `Date`. `today - N days` is
   calendar arithmetic and `now - N hours` is elapsed time. "At least N calendar days" is `<= today - N days`;
   "more than N" is `< today - N days`. The boundary is written by the author, not decided by the language.
6. **Typed arguments for thresholds.** A query may declare `argument <name> <Type> [default <literal>]`. An argument is
   supplied by the caller, typed, optional only with a default, and usable as the `N` of day arithmetic and as an
   operand in `where`. It is not a filter: it does not narrow by name. A policy never reads it (as 0069 says for
   filters). Specifications name arguments as they name filters.
7. **Still refused:** read-path operands, property-to-property arithmetic, `$context` other than the clock, clock
   operands in projections and concept rules, and `where` on a `performer` query (a performer is opaque, 0097).
8. **Performers stay the escape hatch.** For scoring, joins across read models and anything the condition grammar should
   not grow to cover, a performer stays available. The condition is the default for everyday predicates.
9. **Time-driven business facts** (reminder sent, invoice escalated) use clock reactions that append events. They are not
   a way to keep lists correct, and no derived flag is stored for a list.
10. **Reference evaluation and specifications.** The evaluator applies `by`, filters and `where` against the evaluation
    instant. A query specification needs `given clock` when `where` reads the clock; without it the Contract outcome for
    a missing occurrence applies. Specifications compare rows without regard to order (0069).
11. **ESM.** The next free ESM version, allocated at admission per 0025. A query gains an optional `condition`, the
    same node as command conditions, after `filters`, and an optional `arguments` array. Models without `where` keep
    their bytes and version.

## Options considered

- **Clock in caller filters (`filter dueDate before today`).** Rejected: it mixes a fixed predicate into caller-supplied
  values and breaks the 0069 model.
- **A computed property such as `isOverdue` set by a projection.** Rejected: it goes stale without events.
- **A "became overdue" event per item.** Rejected as a way to build lists. Appropriate only for a real business fact.
- **Performer code only.** Rejected as the default: it is opaque to the evaluator and specifications (0097) and puts the
  main domain rules out of `screenplay test`. Kept as the escape hatch.
- **Special-case relative-date filters only.** Rejected: they grow into a predicate language by accretion.
- **Named reusable conditions first (`condition Overdue on TaskItem`).** Parked until two queries in a real model share
  one predicate. SoloCRM has none. The expression node allows it later.
- **Client-side filtering.** Rejected: clients never evaluate clock rules (0087).

## Consequences

- **Stage:** a snapshot query computes the clock once from the receipt instant and the calendar and applies the
  condition. An observable query must not push the condition into `Observe<T>(filter)`; decision 0109 gives the rule. The
  live Host evaluates `where` in the reference evaluator.
- **Arc:** supplies the receipt instant, the calendar and, for live answers, the boundaries.
- **Scene:** none; data still arrives through `data ... via query`.
- **Chronicle, CLI:** none beyond bundled versions. **AI corpus:** teaches `where` and the escape hatch.
- SoloCRM moves `TodayRules` into `where` clauses and deletes its edits to generated files.
- The condition grammar pulls toward a general expression language. This record holds the line: no arithmetic between
  properties, no read-path operands, no calls.
- Filtering whole data sets before paging is a cost. Storage push-down is an optimisation that must not change meaning.

## Open points

1. The spelling of typed arguments (`argument`, or reuse of a `by`/`filter` style line). The semantics above hold for
   any spelling.
2. Whether a query `calendar` line (calendar record) belongs in this record's syntax example; it is the same line.
3. Whether a read model with an optional property stores an authored null or leaves the member out is a projection and
   storage question. The language keeps the two apart (item 4).
4. SoloCRM's "missing stage status counts as open" needs either an authored null in the read model or a test for a
   missing member, such as `is missing`. This record specifies none; decide with the first SoloCRM binding.

## Related

Supplements 0069 and 0010. Builds on 0006, 0025, 0066, 0087, 0090 and 0097; the application calendar and clock operands
record; Screenplay #140.
