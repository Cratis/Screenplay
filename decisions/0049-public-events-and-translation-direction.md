---
id: 0049
title: Mark public events, state their origin as an opaque quoted store name, and give Translate slices a direction
status: accepted
stage: none
decided: 2026-10-09
decider: Einar Ingebrigtsen
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Syntax/EventSyntax.cs
  - Source/DotNET/Screenplay/Syntax/EventVisibility.cs
  - Source/DotNET/Screenplay/Syntax/TranslationDirection.cs
  - Source/DotNET/Screenplay/Syntax/SliceSyntax.cs
  - Source/DotNET/Screenplay/Syntax/Captures/**
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.CommandProductions.cs
  - Source/DotNET/Screenplay/Workspaces/WorkspaceDiagnosticRepairs.cs
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/events.md
  - Documentation/screenplay/imports.md
  - Documentation/screenplay/slices.md
  - Documentation/screenplay/captures.md
  - Documentation/screenplay/diagnostics.md
---

## Context

[#480](https://github.com/Cratis/Screenplay/issues/480) (translation direction) and [#481](https://github.com/Cratis/Screenplay/issues/481) (public events and origin) continue [decision 0009](0009-external-event-origin-and-translation-slices.md), which put origin on the event and made translation a `Translate` slice but left the origin keyword, the outbox side and the translation body to the implementing change. Without a public/private distinction a model cannot say which events another application may rely on, and a `Translate` slice cannot say whether it faces in or out. [#482](https://github.com/Cratis/Screenplay/issues/482) and [#483](https://github.com/Cratis/Screenplay/issues/483) cover the event-target projection/reducer and the `source events` capture; this record covers the contracts they build on.

The issue examples wrote the origin as a bare or path-like name. The implementation departs from them in one respect (item 2) and from #480's wording in another (item 4); a reader of the issues would otherwise find code that disagrees with them.

## Decision

1. **Visibility is on the event.** Events are private local facts unless declared `public event <Name>`. A public event is a contract another application may consume. Commands never produce public events; only an explicitly outbound Translate slice does.
2. **Origin is an opaque quoted string.** `event X from "Store"` and `import Qualified.X from "Store"` carry the origin as a nonblank quoted string, with ordinary string escaping. It is never resolved as a declaration, a file path or another application, and renaming never rewrites it. An origin implies public visibility. This differs from the issue examples, which showed an unquoted or path-like name: a quoted string cannot be mistaken for a file import (`import "path"`) or a declaration reference, and it matches how Chronicle carries `[EventStore("x")]` (decision 0009, item 2). A plain store name is the intended content; a `.play` path is not.
3. **Translate slices have a direction.** `direction inbound|outbound` is written once, directly inside a `Translate` slice. Inbound turns outside occurrences or another application's public events into private local events; outbound turns private local events into exactly one local public event type. Direction on another slice type, an unknown value or a repeat is `PLAY0027`.
4. **Legacy directionless Translate stays inbound.** A `Translate` slice with no `direction` and no public metadata keeps its historical inbound meaning, and the printer never inserts a directive. `PLAY0603` (direction required) fires only when a translation declares or uses public metadata. This departs from #480, which asked for an error on every directionless translation: that would break every existing capture model, and the legacy meaning is unambiguous.
5. **Usage is checked on the assembled model** with `PLAY0596`–`PLAY0609`: commands may not produce public events, foreign public events are consumed and produced only by the matching translation direction, outbound slices read private events and produce one public type, and event-target projections and `source events` belong to the matching direction.
6. **Source-only, no ESM version.** Every form here parses, prints and validates in the C# and TypeScript compilers and the editors, but binding refuses it with `PLAY0268` naming #480 or #481 (and #482 or #483 for their forms). Following [decision 0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) rule 3, no ESM version number is claimed or named; the features are refused by feature name until a claim is made.

## Options considered

- **Quoted string origin (taken)** versus the issue's unquoted name: unquoted names collide with file and declaration syntax and need their own lexical rules.
- **Origin as a resolved declaration or file path.** Not taken: needs cross-application knowledge and makes rename rewrite external names.
- **Error on every directionless Translate (#480).** Not taken: breaks existing models for no ambiguity gain. The cost is that direction stays optional for non-public translations.
- **Direction inferred from content.** Not taken: a slice that is empty or mixed has no inferable direction, and the inferred meaning would shift when an event is made public.
- **Public events produced by commands.** Not taken: it makes publication a side effect of any command and defeats the outbound boundary.

## Default if unanswered

The code ships as source-only and is refused by binding. Decision 0009 keeps saying the outbox is out of scope while the language already has an outbound direction, and the issues keep describing syntax the implementation does not use.

## Timeline and scope

Holds until superseded or until the ESM claim for these constructs is made under [decision 0004](0004-admission-and-governance-of-portable-executable-semantics.md) and [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md).

In scope: visibility, origin, direction, usage diagnostics, source-level tooling. Out of scope: executable meaning, delivery topology, cross-application identity and correlation, Stage and Chronicle realization, and an ESM version number.

## Verification

**Done when:** the syntax tree, printer and both compilers carry visibility, origin and direction; `PLAY0596`–`PLAY0609` have shared diagnostic vectors and documented repairs or explicit "no repair" reasons; binding refuses each construct with `PLAY0268` naming its issue; samples list them as preview constructs.

**Verify by:** `dotnet test`, `yarn test` (conformance vectors), and the preview-construct specification in `for_Samples`.

## Consequences

Public contracts become visible and checkable in the model, and translation direction becomes explicit where it matters. The origin string is not validated against anything, so a mistyped store name is not caught. Legacy captures remain valid, at the cost of one implicit default. Executable support, and so renderer support, waits for an ESM claim.

## Related

Supersession note: this record does not supersede 0009. It supplies the outbox side that 0009 left out of scope; 0009 carries a dated banner saying so.
