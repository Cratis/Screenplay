---
id: 0081
title: Choose the active sample data pack once in the ui profile
status: accepted
stage: implemented
decided: 2026-10-15
decider: Einar Ingebrigtsen
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.Contracts/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

[Screenplay#762](https://github.com/Cratis/Screenplay/issues/762) adds the language for sample data packs, the demo rows a design surface or preview shows in place of a backend ([Scene#100](https://github.com/Cratis/Scene/issues/100), spec section 11). The first plan put the pack selector on each screen. Demo data spans several domains, so a per-screen choice scatters one decision over the whole model and lets two screens preview with different worlds.

## Decision

1. A top-level `sample data <Name>` block declares a pack: optional `title`, optional BCP 47 `locale`, and `shape <ReadModel>` blocks of `row` blocks of `<field> = <literal>` lines. A literal is a string, number, `true`, `false` or `null`.
2. The active pack is chosen **once**, in the `ui profile`, by an optional `sample data` block with `active <Pack>` (or `active blueprint <id>`), the rare `shape <Shape> maps to <ReadModel>` and the rare `bind <ReadModel> to <Pack>`. There is no per-screen, per-slice or per-component selector and none will be added.
3. Sample data is design and preview time only. It is not part of the executable semantic model: binding reports it as deferred (`PLAY0269`), it adds no ESM bytes and no ESM version, and a production build never includes it.
4. The compiler checks rows against the read model they supply (`PLAY0709`-`PLAY0711`) and the selection against the declared packs and read models (`PLAY0712`-`PLAY0715`). A `blueprint <id>` pack is resolved by the build against its packages and is not checked at compile time.
5. The TypeScript compiler skips the block whole, as it does `ui profile`; the C# compiler alone reports the diagnostics.

The resolution order the preview applies is Scene's: pack bound to the read model, the profile's active pack (or the session-only preview override), the blueprint's default pack, then deterministic generated data.

## Options considered

- **Selector per screen or slice.** Rejected: scattered, repeated and inconsistent across screens.
- **Selector on the application root.** Rejected: the `ui profile` already selects layout, theme and packages per build, so the demo world belongs with it and can differ per profile.
- **Rows as events (reuse `seed`).** Rejected: seed appends events to a store at runtime; sample data is design time rows over read-model shapes and must never run.

## Default if unanswered

A profile without `sample data` previews with the blueprint's default pack, then with generated data.

## Timeline and scope

Implemented with Screenplay#762. In scope: grammar, parser, printer, diagnostics `PLAY0702`-`PLAY0715`, binder disposition, both editors, the contract catalogue, MCP typed nodes, documentation and the Invoicing sample. Out of scope: the Scene converter (Stage#406), the Studio editors and the Scene resolution engine.

## Verification

**Done when:** a document with packs and a profile selection compiles, prints and re-parses identically, binds with only `PLAY0269`, and every invalid form reports its code. **Verify by:** the specifications under `for_ScreenplayCompiler`, `for_ScreenplayPrinter`, `for_SemanticModelBinder` and the MCP `when_authoring_sample_data`.

## Consequences

Authors state the demo world once per profile. Consumers that render or preview read `ui profile` `sample data` and the packs; consumers that execute ignore them. Diagnostic codes `PLAY0702`-`PLAY0715` are permanent.

## Status notes

**2026-10-15 - accepted.** User steering supersedes the per-screen placement of the spec: the active pack is selected once in the UI profile for the whole application.
