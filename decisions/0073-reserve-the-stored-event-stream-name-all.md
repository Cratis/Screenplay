---
id: 0073
title: Reserve the stored event stream name All
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Mcp/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

[0036](0036-admit-event-sources-streams-and-command-routes.md) admits event sources and streams and reserves one stored name: a source stored as `Default` is refused with `PLAY0273`, because an explicit route could then produce Chronicle's unrouted triple (`Default`, `All`, `"Default"`) and [0031](0031-event-source-and-stream-in-specifications.md)'s reverse mapping would read it as unrouted. Its sibling rule, in `Documentation/screenplay/event-sources.md`, says "a stream named `All` is allowed". [#700](https://github.com/Cratis/Screenplay/issues/700) asks whether that is right.

Facts:

- Chronicle stores every append that has no stream with event stream type `All`. That covers unrouted facts and source-only appends: Arc `[EventSource<T>]` without a stream and Chronicle `Append<TSource>(id, event)`. 0031 maps both to stream type `All`.
- A declared stream whose stored name is `All` therefore writes events that cannot be told apart from source-only appends. Reading them back through 0031's mapping gives the wrong route.
- Cratis Stage refuses such a model with `STAGE-ESM-030` ("The stored event stream name 'All' is reserved by Chronicle.", `SemanticCratisAdmission.cs:197`). A model that compiles and runs in the reference evaluator can therefore not be rendered.
- The canonical corpora used `All` until [#699](https://github.com/Cratis/Screenplay/issues/699) renamed it to `Archive`.
- A stream stored as `Default` is a different case. Chronicle's default stream *id* is `"Default"`, an id, not a stream type. Nothing found so far makes a stream named `Default` collide with an unrouted fact, so it stays allowed.

If nobody chooses, the language keeps accepting a model no Cratis renderer can render, and 0031's reverse mapping stays ambiguous.

## Decision

The stored event stream name `All` is reserved, like the stored source name `Default`. A stream whose stored name is exactly `All` is refused, whether the name comes from the declaration or from an `id "All"` pin. Comparison is ordinal, so `all` and `ALL` remain valid. Both compilers refuse it with one diagnostic and the same message, with parity vectors. Stage keeps `STAGE-ESM-030` as defense in depth, but it is unreachable for a model Screenplay admitted.

Amends 0036 (the stored-name rules and the `event-sources.md` sentence). Pointers are added to 0036 on acceptance, not before.

### Diagnostic code

Recommendation: reuse `PLAY0273`. It already means "stored-name collision or reserved stored name" for the `Default` source, so one code covers the family and the catalogue gains no entry. The trade-off: `PLAY0273` is also used for generated sources and generated values, so its message must name the reserved stored name for a reader to tell the cases apart. A new code gives a precise catalogue entry and a repair target, at the price of a code that is only ever raised by two rules. The final code is an open question.

### Where it is refused

Like `Default`, the refusal is part of binding (`PLAY0273` is a binder code), so Authoring validation keeps parsing and printing such a model while Executable validation refuses it. Whether Authoring should also warn is an open question.

### Migration

A pin does not help: `id "All"` keeps the stored name `All`, so the pin is refused too. The fix is to rename the stream, which changes the stored stream name. This does not strand correctly separated history, because Chronicle already cannot separate events stored under `All` from source-only appends: there is nothing of that kind to preserve. Treat existing events stored under stream type `All` as source-only, and read them through a source-only route or an unrouted fact. Where a team needs the old events under the new name, that is a Chronicle data migration, not a language feature.

### ESM

This is a binding refusal, not an ESM format change. No member, field or byte of an admitted model changes, so no ESM version is claimed, and [0066](0066-published-esm-versions-are-frozen.md) is not engaged: it freezes the bytes of models that bound, and a refused model has none. The honest cost is that a model with a stream `All` that bound under a published version (v8 and later) stops binding. That narrows admission retroactively, as 0036 did for `Default` before release. Whether that needs a release note naming the break is an open question.

### Surfaces

- C# and TypeScript binders: refuse the stored name, with shared vectors for the name, the `id "All"` pin and the lowercase non-case.
- Diagnostics catalogue and its documentation page.
- `Documentation/screenplay/event-sources.md`: replace "a stream named `All` is allowed" with the reservation and the migration.
- Canonical corpus: add the rejection vector next to the `Default` source vectors.
- MCP: `propose-rename` guidance for a stream stored as `All`, and readiness reporting the refusal.
- Monaco and the VS Code extension: the diagnostic needs no new behavior, but a rename quick fix is optional.
- Cratis/Stage: keep `STAGE-ESM-030`; raise an issue so the case is documented as unreachable for admitted models. Cratis/AI skill corpus: raise an issue to state the reservation.

## Options considered

1. **Keep allowing `All`; refuse only in renderers.** Stage already does this. Rejected: the reference evaluator and every other consumer accept a model that cannot run, and the ambiguity in 0031 stays in the language.
2. **Map `All` to another Chronicle stream type in renderers.** Rejected: it would give the model's stored name a different meaning in each renderer, breaking the rule that a stream's stored name is its Chronicle stream type, and Screenplay would have no way to say what the other type is.
3. **Also reserve a stream named `Default`.** Not taken: `Default` is Chronicle's default stream id, not a stream type, so it does not collide with the unrouted triple on its own. Revisit if evidence shows otherwise.
4. **Reserve `All` (chosen).** One ordinal rule, parallel to `Default`, checked where stored names are already checked.

## Default if unanswered

`All` stays legal. Stage keeps refusing it, models authored against the evaluator keep failing at render time, and each new renderer needs its own refusal.

## Timeline and scope

In scope: the stored stream name `All`, both compilers, diagnostics, documentation, corpus, MCP guidance. Out of scope: any ESM version, `Default` as a stream name, Chronicle data migration tooling, and source-only route syntax (still governed by 0031 and 0036).

## Verification

Done when: a model with a stream named `All`, or pinned `id "All"`, is refused with the chosen code in C# and TypeScript, a stream named `all` binds, `event-sources.md` states the reservation, and the existing v1 to current goldens are unchanged.

Verify by: the shared canonical vectors on both compilers, the golden and version-table checks, and rendering a corpus model through Stage without reaching `STAGE-ESM-030`.

## Consequences

- **Easier:**
  - Every stored stream name round-trips through Chronicle without ambiguity.
  - A model that binds also renders.
  - 0031's reverse mapping reads stream type `All` as source-only without exception.
- **Harder:**
  - Models using `All` must rename the stream, and their old events are treated as source-only.
  - Two reserved stored names to document.
- **Forecloses:**
  - a stream stored as `All`;
  - giving `All` a second meaning in any renderer.

## Open questions for the decider

1. Reuse `PLAY0273` or add a new code?
2. Refuse at binding only (like `Default`), or also in Authoring validation?
3. Is a retroactive narrowing of admission for published v8 and later acceptable with a release note, or does it need an ESM claim?
4. Should a stream named `Default` be reserved after all, if Chronicle evidence turns up?
5. Should MCP offer a typed rename repair, or guidance only?
