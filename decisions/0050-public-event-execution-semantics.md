---
id: 0050
title: Define the executable meaning of public events, outbound translation, event targets and reaction results
status: accepted
stage: none
decided: 2026-10-09
decider: Einar Ingebrigtsen
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/Screenplay/Compiler/**
  - Documentation/screenplay/events.md
  - Documentation/screenplay/slices.md
  - Documentation/screenplay/captures.md
  - Documentation/screenplay/projections/index.md
---

## Context

[Decision 0049](0049-public-events-and-translation-direction.md) added public events, origin and translation direction as source-only constructs that binding refuses with `PLAY0268`. Admitting them needs their executable meaning settled first. This record settles the semantic design only. Following [decision 0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md), it claims no ESM version number; one is assigned at a release-ready admission checkpoint under [decision 0004](0004-admission-and-governance-of-portable-executable-semantics.md).

Chronicle v19.36.0 ships an event-sequence sink that publishes folded state as a public event, and v19.37.0 ships captures from events. This record keeps the model consistent with both.

## Decision

1. **Imported public events declare their shape locally.** A consumer writes `event X from "Store"` with its own fields. The origin stays opaque (0009, 0049). Nothing verifies the shape against the producer; that is tracked in [#593](https://github.com/Cratis/Screenplay/issues/593), which will decide published contracts and versioning.
2. **Outbound translation is a deterministic fold.** A projection or reducer in an outbound `Translate` slice folds private events into the slice's single public event. A reactor appending the public event stays permitted but has no deterministic fold, so specifications cannot predict its output and it is not part of the executable contract. A changed mapping appends new public instances on replay and never rewrites history; redelivery of the same processing step is idempotent.
3. **A projection emits at most one public instance per target key.** The instance is updated as state changes, and the published event is the state. Emitting zero or many events per input is not admitted.
4. **A reaction returns a closed set of results**: append an event, invoke a command, or nothing. Each is specifiable and renderable. An open effect is not admitted.

## Options considered

- **Import a published contract for the shape (1).** Deferred to #593, which needs a contract artifact and a versioning story first.
- **Allow a reactor to define the executable outbound contract (2).** Not taken: no deterministic fold.
- **Zero or many emissions per input (3).** Not taken: needs an emission algebra and breaks state equals publication.
- **Open reaction effect (4).** Not taken: Stage cannot render it and specifications cannot assert on it.

## Default if unanswered

The constructs stay source-only and refused by `PLAY0268`, so Stage and Studio cannot execute translations.

## Timeline and scope

Holds until superseded. In scope: the semantic design of the four items. Out of scope: an ESM version number, the canonical form, Stage realization and published contracts.

## Verification

**Done when:** a follow-up admission change meets the 0004 gate-2 evidence for these constructs and takes a number at its checkpoint.

## Consequences

The design matches what Chronicle already does, so Stage can render it without new kernel work. Shape drift between producer and consumer stays undetected until #593.

## Related

Builds on 0009 and 0049; follows 0004 and 0025.
