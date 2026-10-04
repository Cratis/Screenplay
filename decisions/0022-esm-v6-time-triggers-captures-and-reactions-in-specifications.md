---
id: 0022
title: Admit clocks, application triggers, capture records and reactions into specifications as ESM v6
status: accepted
stage: implemented
decided: 2026-10-02
decider: Einar Ingebrigtsen
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Syntax/Specifications/**
  - Source/DotNET/Screenplay/Parsing/SpecificationParser.cs
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Documentation/screenplay/specifications.md
  - Documentation/screenplay/reactions.md
  - Documentation/screenplay/captures.md
---

## Context

Specifications start from commands, appended events and read-model state. Automation and translate slices are driven by other things: a clock (`every`, `at`), an application trigger (`trigger DirectoryChanged`), or a capture's source record. They also produce consequences through reactions, and the executable semantic model (ESM) does not run reactions. A slice of those kinds can therefore only specify its event-driven part, and an assertion after `when append` must equal the appended fact. The reaction's own effect cannot be asserted at all. Separately, a specification cannot state the occurrence time, so an event property mapped from `$context.occurred` can never be asserted exactly.

The language now parses, validates and prints `given clock`, `when clock`, `when trigger`, `given capture` and `when capture` (with diagnostics `PLAY0461`–`PLAY0467`). Binding any of them reports `PLAY0268`, as decision 0004 requires of a form the ESM does not admit. `when query` binds today, because it is the `then query` assertion spelled as an action and produces identical bytes.

## Decision

ESM v6 admits, for models that use them and no others:

1. **Occurrence time.** `given clock "<instant>"` fixes the occurrence of every command, appended event and reaction in the scenario. `$context.occurred` and `$eventContext.occurred` evaluate to it, so they can be asserted.
2. **Reactions.** Automation and translate slices bind. A reaction's `produces` and `invokes` run in the reference evaluator after each accepted fact, in authored trigger order, with the occurrence of the fact that triggered them. Invoked commands run through the full command pipeline (authorization, validation, constraints) with the reaction as causation. A `where` condition and trigger values use the same condition and value grammar as commands. `then` events after an action compare the complete set of new facts, including those reactions appended, so `when append X` / `then Y` asserts what the reaction did. Reaction bodies in code (`file`, inline blocks) are opaque attachments and return `SemanticUnsupported` when reached, as reducers do.
3. **Clocks.** `when clock "<instant>"` advances the clock from the given clock (or the Unix epoch) to the instant. Each `every` or `at` trigger whose schedule falls in that interval fires exactly once per due occurrence, in time order. The reference does not model a scheduler's at-least-once delivery.
4. **Application triggers.** `when trigger <Trigger>` fires the trigger with the stated values, typed by the trigger declaration (or a registration that states its shape). Built-in `Startup` and `Shutdown` fire with no values.
5. **Captures.** `given capture` seeds the capture's last-seen record per key, and `when capture` presents the next record. The capture's `map`, `append … when` (property, value transition, `added`/`removed`, `and`/`or`, template), `children` and `nested` rules evaluate deterministically. A `source` block stays realization metadata the reference never contacts.

Models using any of these select ESM v6 (language and semantic 6.0, `schemaVersion` 6) and keep v5's absence assertions and v4's generation history. Canonical bytes and revisions of v1–v5 models do not change.

## Options considered

- **Admit all five as ESM v6 (proposed).** Every slice kind becomes specifiable end to end, and the forms the language already parses get one meaning that Stage and generated backends can match.
- **Admit occurrence time alone as v6, reactions later.** Smaller, and it unblocks exact `$context.occurred` assertions. Not proposed alone, because the larger gap is that automation and translate slices cannot be specified at all. It is the natural first increment if this record is narrowed.
- **Keep the forms syntax-only indefinitely.** Specifications using them document intent and fail closed with `PLAY0268`. Rejected as the end state, because a specification that can never run invites authors to read it as passing.
- **Execute reactions without an ESM version, by convention in Stage.** Rejected by decision 0004: a change to what the ESM means needs a version, canonical form, golden and corpus vectors.
- **Model clock ticks as appended "tick" events.** Rejected: it invents facts that never reach the event log and makes `then` event comparisons include them.

## Default if unanswered

The forms stay parsed and fail closed. The samples in `Samples/` keep specifications for automation and translate slices that document intent but never execute, and properties mapped from `$context.occurred` stay unassertable. Stage and generated backends have no reference behavior for reactions to conform to, so each target interprets them on its own.

## Timeline and scope

In scope: the five forms above, reaction execution in the reference evaluator, the v6 canonical JSON, golden vectors and a source-backed corpus vector, and tracking issues in Stage, CLI, Studio and Generation (decision 0004, gate 2 and point 4). Out of scope: real schedulers, external capture sources, and opaque reaction bodies, which stay target-provided. It should be settled before the samples' automation and translate specifications are expected to run in CI, and before Stage renders reactions from the ESM.

## Verification

**Done when:** a model using each of the five forms binds to ESM v6, and its specifications run in the reference evaluator: a reaction's produced event and an invoked command's events appear in `then` comparisons, a clock tick fires a due `at` reaction, `when trigger` and `when capture` drive their reactions and captures, and `given clock` makes `$context.occurred` assertable. v1–v5 golden vectors are byte-identical before and after, and `Cratis.Screenplay.CanonicalCorpus` holds a v6 vector.

**Verify by:** run the `Samples/Invoicing` specifications through `SemanticSpecificationRunner`, and the canonical vector specs in `Screenplay.CanonicalVectors.Specs`. Diff `Semantics/Serialization/Golden` for v1–v5 against `main`. Confirm the tracking issues exist in each consumer.

## Consequences

Automation and translate slices become specifiable and executable. In exchange, the reference evaluator takes on a reaction loop, a scheduler model and capture evaluation, and every consumer has to admit v6 explicitly before it can read models that use it.

## Related

Decisions [0004](0004-admission-and-governance-of-portable-executable-semantics.md), [0006](0006-reaction-triggers-declare-reads.md), [0009](0009-external-event-origin-and-translation-slices.md), [0020](0020-keyed-read-model-absence-in-esm-v5.md).

## Status notes

**2026-10-02 - accepted.** Accepted by Einar Ingebrigtsen. Two points the decision text leaves open are settled
for the implementation, without changing the choice:

- `when clock` advances from the instant `given clock` states, so a specification using `when clock` states
  `given clock` too. Starting from the Unix epoch would make every daily schedule due tens of thousands of times.
- After `when append`, the appended event is the action itself, like a command's input. `then` events compare
  the facts that follow it - what reactions appended. After every other action, `then` events compare every new
  fact, the ones the action produced and the ones reactions appended. Models before v6 keep their rule: after
  `when append`, `then` events equal the appended fact.

**2026-10-02 - implemented.** ESM v6 is in the tree: the model, binder, validator, canonical JSON reader and writer,
reference evaluator, `Semantics/Serialization/Golden/full-esm-v6.json` and `ReactionsCorpus.V6` in
`Cratis.Screenplay.CanonicalCorpus`. v1-v5 golden vectors are byte-identical. The implementation settles these
points the decision text leaves open, without changing the choice:

- A reaction appending from a clock, application or built-in trigger states `for` - a value the trigger carries,
  or literal text. Only an event's reaction appends to that event's source by default.
- `where` narrows the occurrences that carry the values it names. A trigger carrying none of them, such as the
  clock, is not narrowed; one carrying only some of them does not bind.
- An invoked command runs with no caller, so a command requiring one rejects the reaction, and that rejection
  ends the scenario. A reaction's `reads` are not consulted by the reference evaluator.
- The reactions to one fact run in semantic-identity order, which canonical form preserves. Clock occurrences at
  the same instant follow the same order. An interval counts from the Unix epoch, and schedules are UTC.
- `given clock` states a time, not a caller: a mapping from `$context.causedBy` in a scenario that states one is
  Unsupported. A scenario appending more than 1 000 facts, or a clock advance making more than 10 000
  occurrences due, is Unsupported rather than run.
- A capture appends to the event source its key names, typed as the event's commands type it, or as text.
  `` when `<expression>` `` admits a small portable grammar of fields, literals, comparisons, `&&`, `||`, `!` and
  parentheses.

`Samples/Invoicing` uses imported events, PII concepts and other constructs the ESM does not admit, so it does
not bind as a whole. The verification the decision names for it runs on `ReactionsCorpus.V6` and the
specifications under `Semantics/Execution/for_SemanticSpecificationRunner/when_running_reactions` instead, which
cover each of the five forms. The tracking issues in Stage, CLI, Studio and Generation are not opened yet.
