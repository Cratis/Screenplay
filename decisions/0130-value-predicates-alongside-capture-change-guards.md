---
id: 0130
title: Capture `when` guards gain value predicates alongside the existing change guards; backticks are deprecated only after a verified repair
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-29
decider: woksin (ruling on #955)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

[#955](https://github.com/Cratis/Screenplay/issues/955) (part of [#350](https://github.com/Cratis/Screenplay/issues/350)) proposes that capture `when` guards use the shared condition grammar (`and`/`or`, `not`, comparisons), so that `Samples/Invoicing/invoicing.play` can read `when status == "sent" and overdue == true`, and that backtick guards then be deprecated. Backtick guards are today the only place where `&&`, `||` and `!` are admitted (see `Documentation/screenplay/notation.md`). The issue was parked under the language freeze (decision 0092), which [0105](0105-lift-the-screenplay-language-freeze.md) lifts. It stays separate from the spelling-only proposals because it changes meaning.

## Decision

This record writes down the owner's ruling. It adds no design beyond it. The ruling, quoted verbatim from [woksin on #955, 2026-10-29](https://github.com/Cratis/Screenplay/issues/955#issuecomment-6503442292):

> Decision: Value predicates are added compatibly alongside the existing capture change guards, with defined precedence and operand scope.
>
> Reason: Existing guards keep working; backticks are deprecated only once a verified equivalent repair exists.
>
> Next: Specify precedence and operand scope in a decision record.

So:

1. Capture `when` guards gain value predicates. They are added alongside the existing change guards, compatibly.
2. Existing guards keep working with their current meaning.
3. Backtick guards are deprecated only once a verified equivalent repair exists.
4. Precedence and operand scope are defined before the predicates ship.

## Open: precedence and operand scope

The ruling asks for precedence and operand scope to be specified in a decision record. This record does not specify them, because the ruling does not state them. They are specified in an amendment to this record, or in a successor record, before value predicates are admitted.

## Timeline and scope

1. This record.
2. The precedence and operand-scope specification (amendment or successor record).
3. Both parsers and binders, documentation and the Invoicing sample.
4. The backtick deprecation, gated on item 3 of the decision.

## Verification

Done when value predicates compile in both compilers with the specified precedence and operand scope, every existing guard compiles unchanged, and any backtick deprecation diagnostic offers a verified equivalent repair.

## Consequences

Guards can be written with words, while models that use change guards or backticks keep compiling. Deprecation of backticks waits for a repair that is shown to be equivalent.
