---
id: 0124
title: Host-language operators and calls outside admitted contexts warn now and become errors in the next language version
status: accepted
stage: none
class: product
reversibility: cheap
decided: 2026-10-30
decider: woksin (Q-168, delegated through L0)
amends: 0100
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Documentation/screenplay/**
---

## Context

The notation review in #350 (decision 0100) kept `&&`, `||` and calls out of Screenplay outside inline code and capture
backtick guards. Three paths still let such text through without a word:

- a mapping source that matches no known form falls back to a raw expression with no diagnostic;
- non-strict conditions use token regexes that drop or split `&`, `|` and calls instead of reporting them;
- a capture `when` property clause tokenises with a regex that drops operators.

The binder reports PLAY0268 for a raw source only where the mapping is bound into the ESM, so the other positions, and
the TypeScript compiler (which has no binder), accept the text silently.

## Decision

1. **PLAY0826 `HostLanguageExpression`, Warning.** Both compilers report it when `&&`, `||` or a host-language call
   (`Name(` or `A.B(` with no space before the parenthesis) appears in a mapping source that falls back to a raw
   expression, in a non-strict condition, or in a capture `when` property clause.
2. **Admitted contexts.** Inline code blocks and capture backtick guards are unchanged and stay silent. Quoted strings,
   backtick templates and structured values are not scanned. Strict guarded-action conditions keep PLAY0344 alone.
3. **The allowed call set is empty.** `grammar.md` names no method; arithmetic and `!` are not covered by this rule.
4. **Escalation.** Ships as Warning; becomes an Error in the release that introduces the next language version.
5. **No syntax change.** The node stays a raw expression, so the printer, workspace edits, syntax vectors and the ESM are
   unchanged.

## Options considered

- Reuse PLAY0152 (an error today): rejected, it would break input that is accepted now.
- One code per site: rejected, it is one rule with one code; the message names the fragment.
- Gate the severity on a model's bound version: rejected, adopting an unrelated feature would turn warnings into errors
  and the TypeScript compiler selects no version.
- A strictness preamble: rejected, it is new syntax that needs its own admission.

## Consequences

- No `.play` file in the repository warns today.
- Bound mappings get PLAY0826 in front of the existing PLAY0268; the warning gives the reason.
- Raising the severity is a one-line change in each diagnostic catalogue when the next language version is introduced.
- Codes are permanent; PLAY0826 is never renumbered.
