---
title: Screenplay reference patterns and checksums
description: Define readable Screenplay references and numeric tickets, pin UTC dates in fixtures, and choose checksum validation without adding randomness.
---

Screenplay patterns define readable references with random characters, UTC date segments, and optional check digits. The shop uses one for an invoice reference that someone can read over the phone. Its date gives context and its checksum can detect typing mistakes, but neither adds randomness. Keep the reference separate from the high-entropy order identity.

```screenplay
concept InvoiceRef : String
  generate pattern "INV-{yyyy}{MM}-#{7}" checksum luhn
  validate checksum luhn
concept TicketNumber : Int
  generate random 100000..999999
```

The [Invoicing sample](https://github.com/Cratis/Screenplay/tree/main/Samples/Invoicing) generates both values in `StartInvoiceDraft` and protects each mapped event property with a single-property unique constraint. These short values can collide; the constraint prevents a duplicate append, not a duplicate draw. Neither is suitable as a generated event-source identifier.

The sample pins `generated reference` to `INV-202610-00000004` and `generated ticket` to `100001`, with `given clock "2026-10-02T09:00:00Z"`. The clock fixes the reference's date segments. The fixture supplies the random digits and the checksum; the evaluator does not draw them.

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

To assess collisions, count distinct output strings, not the ways a pattern can produce them. For example, `[AB]{1,64}[AB]{1,64}` has support Σ(2^k), k=2..128. Splitting the same output between the two repeats in different ways adds no entropy. Check digits are deterministic too, so they do not add random choices.

For `random`, support is the exact inclusive range width. Entropy thresholds compare exact counts; displayed log2 values are estimates for presentation only. Range endpoints retain their authored digits. Source fixtures have a separate limit: ordinary numeric literals still use the Legacy conversion and cannot be assumed to represent every Int64 endpoint without rounding. Typed execution fixtures use decimal-backed integral values. Exact numeric source mode remains separately unadmitted (#285).

## Checksums are not validation unless you ask

`checksum luhn|mod11|mod97` appends check digits calculated from every preceding ASCII digit, including digits in literals and expanded dates. Nondigits stay in the value but do not enter the arithmetic.

| Algorithm | Calculation |
| --- | --- |
| `luhn` | Standard right-to-left parity, with the rightmost payload digit doubled. |
| `mod11` | Weights 2–7 cycle from the right: `(11 - weightedSum % 11) % 11`. Output 10 cannot fit in one digit and is excluded from support before sampling. |
| `mod97` | ISO 7064 MOD 97-10, two zero-padded digits: `98 - ((payload % 97) * 100 % 97)`. |

An empty digit payload counts as zero. For mod11, `000000006-[AB]` has no support: both outputs would need check digit 10. This is a binding error, not a redraw instruction.

Generating check digits and validating them are separate decisions. In the example, `validate checksum luhn` checks the exact trailing digit against the preceding payload. The nested `validate` / `checksum luhn` form is equivalent. Without that authored rule, a generated fixture only needs the right suffix digit shape; its checksum arithmetic need not be valid. Generation never adds validation that could reject old stored values.

Checksum validation is available only on String concepts and applies to supplied, generated and stored values; optional null retains ordinary absence semantics. It rejects malformed or truncated suffixes and mod11 output 10. mod97 checks the actual emitted suffix, not merely congruence. A checksum does not establish the reference's format or length, prevent collisions, or authorize its holder. Declare format and length rules separately when needed.

## UTC dates and support budgets

A date-bearing reference must have enough possible values even on its most restrictive date. Binding captures one UTC assessment year, or accepts an explicit binder year input. It scans every valid date from that year through year+100 inclusive, including leap days, and reports the smallest number of distinct outputs after excluding any impossible checksum outcomes. Supported assessment years are 1..9899. The assessment year, support tables and proof results never enter Executable Semantic Model (ESM) bytes, so advancing a year cannot change a model revision merely by changing date literals.

Fixed-width patterns reuse support counts across equivalent dates: one count without a checksum, or at most 10/11/97 checksum-contribution classes with fixed ASCII-digit positions. Every valid date is still visited, preserving the first empty-support date. Variable repeats and mixed digit/nondigit checksum positions retain the full scan; no budget limit is raised.

To bound the compiler's work, generation limits its pattern-matching state machines to 2,048 automaton positions and 10,000 distinct states, including combined states. It also allows at most 100,000,000 operations across support counting and the date scan (one unit per inspected edge, counting-state character transition or assessed UTC date). Exhaustion fails binding with `PLAY0691`; it is not proof Unproven. Empty support on any scanned UTC date fails with `PLAY0692`. By contrast, regex proof construction exhaustion remains Unproven (`PLAY0689`).

For a repeatable date-bearing scenario, use `given clock`. Every date segment in the fixture must match the same UTC instant, including repeated segments, offset rollover and leap days. Without a clock, the evaluator checks only digit widths and never consults today's wall clock. Time-ordered built-ins still need a whole pinned fixture; the reference evaluator generates neither identifiers nor random values.

## Repair the contract deliberately

A refuted rule can be replaced by a derived `matches` rule only when the expression is bounded and deterministic. For a checksum pattern, the same revision-checked proposal explicitly adds checksum validation if absent. Review it: both rules also change validation of inputs and stored values. An ambiguous derived expression has no repair, with a refusal reason; it does not weaken the proof rules. Date/checksum digit approximations do not justify a false counterexample or claim Proven containment.

Use [Choose a generation strategy](choosing.md) to compare these readable values with ordinary identities, or [Generated values](../generated-values.md) to add the chosen concept to a command and pin its specifications. The [shared shop tutorial work](https://github.com/Cratis/Documentation/issues/187) tracks the wider application journey.
