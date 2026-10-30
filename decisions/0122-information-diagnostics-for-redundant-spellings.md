---
id: 0122
title: Report redundant key braces and implicit authorization conjunction as Information
status: accepted
stage: none
class: product
reversibility: cheap
decided: 2026-10-19
decider: woksin (delegated by the maintainer through L0, Cratis/Screenplay#953 comment 2026-10-19)
amends: 0100
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Documentation/screenplay/**
---

## Context

Decision 0100 retained the notation conventions and parked the readable alternatives. It noted that redundant spellings
which already have an equivalent form (composite-key braces, and `authorize A B` against `authorize A and B`) need no
new grammar, and that reporting them with an Information diagnostic and a repair is a separate maintainer ruling. The
spelling freeze from 0092 item 2 was lifted by 0105.

The ruling on Cratis/Screenplay#953 (2026-10-19):

> Ruling (L0, delegated by the maintainer, 2026-10-19): add these as Information-level diagnostics with repairs, never
> warnings. Composite-key braces come first. `authorize A B` → `authorize A and B` is included only if the printer
> already writes `and`; otherwise it is left out. No grammar or executable-model change…

The printer already writes `and` between authorization operands (`Source/DotNET/Screenplay/Printing/ScreenplaySyntaxText.cs:338`)
and writes composite keys without braces (`ScreenplayPrinter.Projections.cs`).

## Decision

1. **Information only.** Both spellings are reported as Information diagnostics, never Warning, by both compilers.
2. **Braces first.** A braced composite key (`key {` … `}`) is reported as `PLAY0824` (`RedundantCompositeKeyBraces`),
   one diagnostic per braced key, at the key line.
3. **Implicit conjunction is included** because the printer writes `and`. An `authorize` declaration with at least one
   implicit conjunction is reported as `PLAY0825` (`ImplicitAuthorizationConjunction`), one per declaration, at the
   `authorize` line.
4. **No grammar, syntax-tree, executable-model or ESM change.** Both spellings keep parsing to the same tree. Repairs
   are text splices that are verified by reparsing and requiring the parsed tree to be structurally equal to the
   original. Workspaces and MCP offer the repair; editors offer the matching quick fix.

## Consequences

- Old spellings keep parsing and compiling; the diagnostics are advisory.
- The Invoicing and editor samples are rewritten to the canonical spelling.
- Conformance fixtures and the embedded compiler fixture stay as legacy pins of the old spellings.
- The new codes carry no Deprecated tag in editors: they are redundant spellings, not deprecated forms.
- Release impact is minor: new diagnostics and repairs, no ESM change.
