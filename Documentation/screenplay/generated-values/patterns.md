---
title: Patterns and checksums
description: Specify finite reference formats, exact support and clock-pinned generation fixtures.
---

The shop prints an invoice reference that a person can read over the phone. Its date is useful context, not randomness; the final digit catches transcription errors. Keep this reference separate from the high-entropy order identity.

```screenplay
concept InvoiceRef : String
  generate pattern "INV-{yyyy}{MM}-#{7}" checksum luhn
  validate checksum luhn
concept TicketNumber : Int
  generate random 100000..999999
```

The [Invoicing sample](https://github.com/Cratis/Screenplay/tree/main/Samples/Invoicing) uses both as generated values in StartInvoiceDraft, maps each to a single-property unique constraint, and pins them with `generated reference` and `generated ticket`. It uses `given clock "2026-10-02T09:00:00Z"` and reference `INV-202610-00000004`. The clock supplies the date, not the random digits. Neither a short reference nor a queue ticket is suitable as a generated event-source identifier.

## Finite pattern grammar

| Form | Meaning |
| --- | --- |
| `#` | One ASCII digit, 0–9. |
| `?` | One uppercase ASCII letter, A–Z. |
| `*` | One uppercase letter or digit. |
| `[ABC]`, `[A-F0-9]` | A positive character class, including ascending ranges. Duplicate characters add no support. |
| `{n}`, `{m,n}` | Repeat the preceding token: 1 ≤ m ≤ n ≤ 64. |
| `{yyyy}`, `{yy}`, `{MM}`, `{dd}` | UTC date segment, fixed for one generation occurrence. |
| Other printable ASCII | Literal text; escape reserved characters with a backslash. |

Escape `#?*[]{}\` outside classes and `-]\` inside them; the surrounding Screenplay string needs its own backslash escaping. Empty/negated/descending classes, unknown escapes or date segments, orphan/zero/inverted repeats, and non-printable/non-ASCII text fail with `PLAY0673`. A pattern needs a random token, although a singleton class can have only one possible output. The final expanded value, **including appended check digits**, is at most 256 characters (`PLAY0681`). Prefix, case and alphabet modifiers do not apply to patterns; write literal text and classes in the pattern.

Support counts distinct output strings, not possible parses: `[AB]{1,64}[AB]{1,64}` has support Σ(2^k), k=2..128. Overlapping repeat decompositions do not add entropy. Check digits are deterministic appendages. `random` uses exact inclusive range cardinality, not floating-point endpoints. Entropy thresholds compare exact counts; displayed log2 values are estimates only for presentation. Range endpoints retain exact authored digits. Typed execution fixtures use decimal-backed integral values, but ordinary source fixture literals retain the existing Legacy numeric conversion; do not assume they can spell every Int64 endpoint without rounding. Exact numeric source mode remains separately unadmitted (#285).

## Checksums are not validation unless you ask

`checksum luhn|mod11|mod97` appends digits over every preceding ASCII digit, including digits in literals and expanded dates. It ignores nondigits for arithmetic but preserves them in the value.

- **luhn:** standard right-to-left parity, with the rightmost payload digit doubled.
- **mod11:** weights 2–7 cycle from the right. The digit is `(11 - weightedSum % 11) % 11`; output 10 cannot be represented and is excluded from support before sampling.
- **mod97:** ISO 7064 MOD 97-10, two zero-padded digits: `98 - ((payload % 97) * 100 % 97)`.

An empty digit payload counts as zero. For mod11, `000000006-[AB]` has no support: both outputs would need check digit 10. This is a binding error, not a redraw instruction.

An authored `validate checksum luhn` checks the exact trailing digit over the preceding payload. The nested `validate` / `checksum luhn` form is equivalent. Validation is String-concept-only and applies to supplied, generated and stored values; optional null retains ordinary absence semantics. Validation rejects malformed/truncated suffixes and mod11 output 10. mod97 checks the actual emitted suffix, not merely congruence.

A generated fixture **without** this authored rule checks suffix digit shape only. It need not have valid checksum arithmetic. Generation never silently adds rules that could reject old stored values.

## UTC dates and support budgets

Binding captures one UTC assessment year, or accepts an explicit binder year input. It scans every valid date from that year through year+100 inclusive, including leap days, and reports the minimum conditioned support. Supported assessment years are 1..9899. This year, support tables and proof results never enter ESM bytes; advancing a year cannot change a model revision merely by changing date literals.

Generation has hard ceilings: 2,048 automaton positions, 10,000 distinct DFA/product states, and 100,000,000 operations across support counting and the date scan (one unit per inspected edge, counting-state character transition or assessed UTC date). Exhaustion fails binding with `PLAY0691`; it is not proof Unproven. Empty support on any scanned UTC date fails with `PLAY0692`. By contrast, regex proof construction exhaustion remains Unproven (`PLAY0689`).

With `given clock`, every date segment in a fixture must match the **same UTC instant**, including repeated segments, offset rollover and leap days. Without a clock, only digit widths are checked—never today's wall clock. Time-ordered built-ins still need a whole pinned fixture; the reference evaluator generates neither identifiers nor random values.

## Repair the contract deliberately

A refuted rule can be replaced by a derived `matches` rule only when the expression is bounded and deterministic. For a checksum pattern, the same revision-checked proposal explicitly adds checksum validation if absent. Review it: both rules also change validation of inputs and stored values. An ambiguous derived expression has no repair, with a refusal reason; it does not weaken the proof rules. Date/checksum digit approximations do not justify a false counterexample or claim Proven containment.

For ordinary identities use [the strategy guide](choosing.md). For application delivery follow the [shared shop tutorial work](https://github.com/Cratis/Documentation/issues/187), not a random seed or an assumed provider capability.
