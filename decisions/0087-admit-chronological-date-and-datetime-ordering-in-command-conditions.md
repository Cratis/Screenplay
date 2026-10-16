---
id: 0087
title: Admit chronological Date and DateTime ordering in command conditions and record the receipt clock rule
status: accepted
stage: none
decided: 2026-10-16
decider: Sindre Wilting (delegated to the L0 coordinator)
class: contract
reversibility: costly
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

Screenplay parses comparisons between `Date` and `DateTime` values in `require`, in `produces when` and in command and
property validation rules (`to >= from`, `dueDate > "2026-01-01"`). The binder refuses them with PLAY0268, because no ESM
version says how they are ordered. One consumer needs them now:
KeldaKraft's `KeepPlantWindow` and `KeepSensorWindow` write `require to >= from` on two required `DateTime` command
properties. Those two slices also read `RetentionPolicy.keepDays` through `reads`, so they bind only once the model also
declares `command reads protected` (0085); that is a KeldaKraft change, separate from this record. `today` appears in
the Invoicing and Library samples and in fixtures, but in no consumer model.

Ordering dates as text is not enough. A `DateTime` in the "O" format can carry an offset, so two strings that name the
same instant compare as different, and two that are ordered one way as text can be ordered the other way in time.

## Decision

1. **Where, and with which operands.** A new ESM version admits chronological ordering with `==`, `!=`, `>`, `>=`, `<`
   and `<=` in these positions only:

   | Position | Left operand | Right operand |
   | --- | --- | --- |
   | `require` and `produces when` conditions | a command property | a command property or a literal |
   | Command and property validation rules | the rule's subject property | a literal |

   - Both sides have the same primitive: `Date` with `Date`, `DateTime` with `DateTime`. Mixing them stays refused.
   - Validation rules keep literal-only right operands; property-to-property validation rules are not added.
   - Still refused with PLAY0268, as today: `$context` operands (including `$context.occurred`), `today`, protected read-path operands (0085), date rules in
     concept rules, and date ordering in reaction `where`.
   - Concept rules stay refused because, under 0072, they also run on query keys and filters and in client validators;
     admitting them needs its own decision on both.
2. **Meaning.** `Date` orders as a calendar date with no time zone. `DateTime` orders as an instant: both values are
   normalized to UTC and compared at tick precision (seven fractional digits). In these positions `==` and `!=` compare
   instants, not text. Equality everywhere else (specification `then` matching, read-model keys, constraints,
   freshness) is unchanged and stays ordinal.
3. **Canonical form.** No new value variant and no new member:
   - A date literal operand is a text value in canonical spelling: `yyyy-MM-dd` for `Date`, and for `DateTime` UTC with
     seven fractional digits and `Z`. The binder converts an authored `DateTime` literal with an offset or `Z` to that
     spelling. A `DateTime` literal without an offset, and any `Date` or `DateTime` literal that does not parse, is
     refused at bind time with PLAY0273.
     A comparison that is refused as a whole (item 1) reports only PLAY0268; its literals are not checked.
   - The validator types such a text constant from the other operand (conditions) or from the rule subject (validation
     rules), and only at the new version or later. Readers and validators of earlier versions keep rejecting it: a
     text constant against a date property fails their scalar type check, and an ordering rule with a text operand
     fails their number-operand check.
   - The golden file and corpus vector pin the exact bytes.
4. **Values without an offset.** One rule, applied the same way by the reference evaluator and every target: the input
   value of a `DateTime` command property that takes part in an admitted comparison must carry an offset or `Z`. A value
   without one is rejected at input validation with a Validation outcome, before any rule or condition runs, whether
   or not the comparison would be evaluated. Targets apply it before a deserializer can attach a local offset.
5. **Absent values.** Validation rules keep their semantics: an absent value passes every rule except `not empty`.
   Optional operands in conditions stay refused, as for every other primitive.
6. **Selection and number.** The version is selected only when a model uses an admitted date ordering; every other
   model keeps its bytes and version. The working number is v26, agreed with the version-table owner; under 0025 and
   0066 the number is confirmed at the admission checkpoint against current main.
7. **The clock rule for `today` and now (#824).** Recorded now so the follow-up is mechanical; neither is admitted here.
   - The server reads the clock once, when it receives the operation. That reading is the receipt instant. `today` is
     its UTC calendar date and now is the instant itself. Nothing reads the clock again during evaluation.
   - For a command invoked by a reaction, the receipt instant is the instant of the occurrence that triggered the
     reaction: the triggering event's occurred time, the scheduled time of a clock trigger, or the received time of an
     application trigger. Redelivery reuses it, so the answer does not change. The reference evaluator does not do this
     on redelivery yet (it forwards the scenario clock); that change and its conformance tests belong to #824.
   - Clients never evaluate `today` or now.
   - They are allowed only in command positions: validation rules, `require` and `produces when`. They are refused in
     reaction `where`, which has no operation, and in concept rules.
   - Specifications supply the instant with `given clock`. A command that reaches `today` without one fails with the
     existing Contract outcome for a missing occurrence.
   - This record does not change the generation clock (0054 item 9, 0083).
8. **Renderers.** Targets admit the version explicitly. They compare `Date` as a calendar date and `DateTime` as an
   instant at tick precision. A target that cannot keep tick precision (JavaScript `Date` has milliseconds) refuses
   the comparison rather than truncating it, and client-side validators omit date ordering rules. Targets apply
   item 4 at input binding.

## Options considered

- **Compare the canonical text ordinally.** Wrong for values with offsets, and it hides the meaning from targets.
- **Admit `today` and now with the ordering.** No consumer uses them; it adds evaluator and renderer work and a client
  validation question, for samples and fixtures only.
- **Admit date rules on concepts too.** They reach query arguments and client validators; no consumer needs them.
- **A new date value variant in the ESM.** More bytes and reader work for the same information the operand types give.
- **Read `DateTime` values without an offset as UTC.** It hides input errors and disagrees with targets that read them
  as local time.
- **Truncate to milliseconds to match JavaScript.** Two instants within a millisecond would compare equal in one target
  and not in another.

## Default if unanswered

KeldaKraft's two retention slices stay authoring-only, and its whole model cannot bind.

## Verification

- Binder specs: every operator on `Date` and on `DateTime` in each admitted position; mixed primitives, `$context`
  operands, `today`, read-path operands, concept rules and reaction `where` stay refused with PLAY0268; a `DateTime`
  literal without an offset and an unparsable date literal are refused with PLAY0273.
- Evaluator specs: ordering across offsets, equality of one instant written with different offsets, tick precision,
  absent values in validation rules, and an input value without an offset rejected with a Validation outcome before
  any rule runs, including when the comparison is not evaluated; specification `then` equality unchanged.
- Canonical vectors: a golden file and a corpus vector at the new version, plus controls showing that models without
  date ordering keep their bytes and version, and that earlier readers reject the new shape.
- Samples: Invoicing gains a date ordering; `today` in Invoicing and Library stays refused and pinned.
- KeldaKraft's `RejectingAnInverted…` specifications bind and pass once the model also declares `command reads
  protected`.

## Related issues

#803, #824; Cratis/Stage and Cratis/Arc rendering issues; decisions 0004, 0025, 0054 (generation clock), 0066, 0072.
