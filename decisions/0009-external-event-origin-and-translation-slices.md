---
id: 0009
title: External events declare their origin; translating them is a Translate slice
status: accepted
stage: none
decided: 2026-09-24
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Syntax/EventSyntax.cs
  - Source/DotNET/Screenplay/Syntax/SliceSyntax.cs
  - Source/DotNET/Screenplay/Parsing/EventParser.cs
  - Source/DotNET/Screenplay/Parsing/ScreenplayParser.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModel.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.SliceMembers.cs
  - Documentation/screenplay/events.md
  - Documentation/screenplay/slices.md
  - Documentation/screenplay/reactions.md
---

## Context

[#73](https://github.com/Cratis/Screenplay/issues/73) asks how a model states that an occurrence comes from another application, and how it becomes a local fact, without observer, sequence, subscription or transport vocabulary. Its criteria require that the source occurrence and the translated local fact stay distinct. The triage left three choices open: where the origin lives, whether the source name is resolved or opaque, and whether translation is a construct of its own or a reaction that produces.

A projection can already name a `sequence` ([`projections/grammar.md:39`](../Documentation/screenplay/projections/grammar.md)); a reaction trigger cannot say where its events come from ([`ReactionSyntax.cs:44-52`](../Source/DotNET/Screenplay/Syntax/ReactionSyntax.cs)). `import <QualifiedName>` already says an event type is declared elsewhere ([`grammar.md:22`](../Documentation/screenplay/grammar.md)). The language has a `Translate` slice type, today documented for captures that convert external data into events ([`SliceSyntax.cs:31-34`](../Source/DotNET/Screenplay/Syntax/SliceSyntax.cs), [`slices.md`](../Documentation/screenplay/slices.md)). The executable semantic model (ESM) admits only `StateChange` and `StateView` slices ([`SemanticModel.cs:11-27`](../Source/DotNET/Screenplay/Semantics/SemanticModel.cs)), and reactions and captures fail binding ([`SemanticModelBinder.SliceMembers.cs:77-99`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.SliceMembers.cs)).

Under [decision 0001](0001-chronicle-runtime-semantic-authority.md), Chronicle defines the meaning. It puts origin on the event type: `[EventStore("x")]` names the event store an event type originates from ([`EventStoreAttribute.cs`](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Events/EventStoreAttribute.cs)). A reactor whose handled events all come from one store subscribes to that store's inbox without naming a sequence ([`ReactorAttribute.cs:24-29`](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/Reactors/ReactorAttribute.cs)).

## Decision

1. **Origin is on the event.** An event declaration, or the `import` that brings it in, may state its origin as an opaque store name. Every reaction or projection that consumes the event inherits the origin; none of them names a sequence or inbox. The keyword is chosen by the implementing change; this record fixes where it attaches and what it holds.
2. **The name is opaque.** Screenplay does not resolve the store name against other applications. It is carried as given, the way Chronicle carries `[EventStore("x")]`.
3. **Translation is a `Translate` slice.** Turning an external event into a local fact is declared in a slice of type `Translate`, not in an `Automation` slice. The slice kind is what keeps the source occurrence and the local fact distinct. The body reuses existing trigger and `produces` mapping forms where they fit; the implementing change states the exact form.

## Options considered

- **Origin on the event or import (taken).** It mirrors Chronicle, states the origin once where the name lives, and composes with `import`.
- **Origin on the reaction trigger.** Not taken: it recouples each reaction to where its events come from, which the trigger design deliberately avoids ([`triggers.md`](../Documentation/screenplay/triggers.md)), and it has to be repeated on every consumer.
- **Origin on a declared `trigger`.** This was the triage's first recommendation. Not taken: Chronicle attaches origin to the event type, and a trigger is not an event type.
- **First-class "other application" declarations.** Not taken: they make a document depend on how another application is deployed, which #73's non-goals exclude.
- **A resolved store name.** Not taken: it needs cross-application knowledge the compiler does not have.
- **Translation as an ordinary reaction that produces.** Not taken: in the syntax tree and the ESM it would look like any other automation, so criterion 2 (source and local fact stay distinct) could not be checked. The maintainer ruled it is a translation slice.

## Default if unanswered

Cross-application reactions read as though they observed the local log, the triage's trigger-origin option stays on the table against Chronicle's model, and Stage keeps failing closed on Translation slices because the ESM has no contract for them (Cratis/Stage#79).

## Timeline and scope

Settle before any #73 syntax or ESM work, and keep it until superseded. Admission to the ESM, including a Translation slice kind, follows [decision 0004](0004-admission-and-governance-of-portable-executable-semantics.md).

In scope: the origin clause on events and imports, the opaque store name, and Translation slices as the home of external-to-local translation.

Out of scope: the exact origin keyword and translation body syntax (left to the implementing change within the rules above); the outbox side (publishing to another application); cross-application identity and correlation typing; delivery topology, which a realization profile supplies; captures, which keep their existing CDL form.

## Verification

**Done when:** An event declaration and an `import` can each carry an origin store name, and the syntax tree and printer preserve it. A translation from an external event to a local fact is declared in a `Translate` slice, and the syntax tree keeps the source event and the produced local fact apart. `events.md` and `slices.md` describe origin and Translation slices without framework vocabulary.

**Verify by:** Parser, printer and validator specs for each case. When the Translation slice kind is admitted to the ESM, a golden vector and a reference-execution vector show the source occurrence and the local fact as distinct entries.

## Consequences

Origin is stated once and inherited, as in Chronicle, and a renderer can derive the inbox subscription without the model naming it. Translation gets its own slice kind, which gives Studio and AI a visible boundary between another application's facts and local facts. An opaque name cannot fail closed on a misspelled store; that check belongs to the realization. The outbox direction and cross-application correlation remain open.

## Related issues

Screenplay: [#73](https://github.com/Cratis/Screenplay/issues/73), [#69](https://github.com/Cratis/Screenplay/issues/69), [#128](https://github.com/Cratis/Screenplay/issues/128). Stage: [#79](https://github.com/Cratis/Stage/issues/79).
