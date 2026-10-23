---
id: 0107
title: Admit `today` and `now` in command conditions
status: accepted
stage: none
decided: 2026-10-23
decider: L0 (delegated by woksin)
class: contract
reversibility: costly
amends: 0087
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Monaco/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

Decision 0087 admitted date ordering between command properties and literals, and recorded the clock rule so the
follow-up would be mechanical (item 7). The follow-up is Screenplay #824, "Admit today / now operands in command
conditions (needs a consumer)". It was closed on 2026-10-22 with no comment and no commit. The binder still refuses
`today` (`SemanticModelBinder.DateComparisons.cs:45-68`), so the closure is not delivery. Its label was
`language-freeze`; the freeze is lifted by the record that lifts it (0105 on branch `screenplay-1035-typed-capture-key`).

SoloCRM is the consumer #824 waited for. Its overdue and due-today lists (`SPEC.md:289`) need a clock, and a rule such as
"a due date is not in the past" belongs on the command that sets it. Two clock rules would be a trap: a task shown as
due today in the list and refused as "in the past" by its own command.

Stage already carries the receipt instant for commands (`CommandReceiptTimeRendering.cs`,
`Documentation/guides/build-renderer-target.md:378`). Arc exposes it as `CommandContext.ReceivedAt`
(`CommandContext.cs:63`).

Evidence revisions: Screenplay `b65b3fd8`, Stage `f83af8fb`, Scene `fe5dd7d`, Arc `776ce82e4`, Chronicle `a4a1b7e26b`,
cli `accfd41`; 0105 as on branch `screenplay-1035-typed-capture-key` at `971012f4`. File and line references in this
record are to these revisions.

## Decision

1. **Where.** `today` and `now`, with the arithmetic and calendar rules of the application calendar record, are
   admitted in the positions 0087 item 1 lists for date ordering: `require`, `produces when`, and command and
   property validation rules.
   - In `require` and `produces when`, either side may be a clock operand: `require dueDate >= today`,
     `produces when now > deadline`.
   - In validation rules the right operand may be a clock operand where it may be a literal today:
     `dueDate >= today`.
   - Types follow 0087: `Date` with `Date`, `DateTime` with `DateTime`. A `DateTime` property meets a day through
     `date of`.
2. **Receipt instant and redelivery.** One rule, as 0087 item 7:
   - A command received from a client uses its receipt instant.
   - A command invoked by a reaction uses the instant of the occurrence that triggered the reaction: the event's
     occurred time, the scheduled time of a clock trigger, or the received time of an application trigger.
   - Redelivery reuses that instant, so for a fixed zone the answer does not change. The zone in effect is resolved
     again on redelivery (application calendar record, item 3), so a changed zone can change the answer. This is
     accepted because zones change rarely; open point 3.
   - The reference evaluator forwards the scenario clock on redelivery today; it moves to the triggering instant with
     this record, with conformance tests.
3. **Absence.** Optional operands stay refused in command conditions (`require`, `produces when`), as 0087 item 5
   says; this record does not extend them. The query predicates record's absence rule (missing and authored null match
   no ordinary comparison, `== null` matches authored null only) applies to queries, not here. In a validation rule an
   absent value passes every rule except `not empty`, so `dueDate >= today` does not fail an absent `dueDate`.
4. **Clients.** Clients never evaluate a clock operand. Client-side validators omit any rule that contains one, as
   0087 item 8 does for date ordering. The server stays authoritative.
5. **Still refused with `PLAY0268`:** clock operands in reaction `where` (it has no operation), in concept rules
   (which also run on query keys, filters and in client validators) and in read paths.
6. **Specifications.** A specification of a command that reaches a clock operand supplies `given clock`. Without it,
   the existing Contract outcome for a missing occurrence applies (0087 item 7).
7. **Version.** The next free ESM version, allocated at admission per 0025, shared with the application calendar
   record when both are admitted together.
8. **Issue.** #824 is reopened for this work, or superseded by it explicitly, so the tracker matches the decision.

## Options considered

- **Queries only.** Rejected: it splits the clock rule.
- **Read the clock at each condition.** Rejected: a command could disagree with itself across midnight, and redelivery
  would change the answer.
- **Client validation of clock rules.** Rejected: the client has no authoritative clock (0087 item 7).
- **Admit in reaction `where` and concept rules too.** Rejected for now: reaction `where` has no operation, and concept
  rules run in more contexts. Each needs its own record.

## Consequences

- **Stage:** renders the receipt day in the application calendar; JavaScript cannot keep tick precision, so `now` is
  rendered server-side only and client validators omit it.
- **Arc:** supplies the receipt instant and the zone abstraction.
- **Samples:** Invoicing and Library move deliberately from "refused" to "admitted".
- **Scene, Chronicle:** none. **CLI:** bundles the versions. **AI corpus:** stops saying `today` cannot be modelled.
- A backdated business value is not an execution time. Rules that compare against `today` apply to the receipt
  instant, not to the event's occurred time, and authors choose accordingly.

## Open points

1. Whether reaction `where` may later use the occurrence instant consistently.
2. Whether a command needs `calendar` overrides beyond the application calendar; the syntax is the one in the
   calendar record.
3. Whether a redelivered reaction should keep the zone of its first evaluation instead of resolving it again.

## Related

Decisions 0025, 0066 and 0087; the application calendar and clock operands record; Screenplay #824.
