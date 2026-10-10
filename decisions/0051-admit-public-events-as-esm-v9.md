---
id: 0051
title: Admit public events, translation direction, event-target projections and event-source captures as ESM v9
status: accepted
stage: none
decided: 2026-10-09
decider: Einar Ingebrigtsen
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/Screenplay/Compiler/**
  - Documentation/screenplay/events.md
  - Documentation/screenplay/slices.md
  - Documentation/screenplay/captures.md
  - Documentation/screenplay/projections/index.md
  - Documentation/screenplay/interoperability.md
---

## Context

[Decision 0050](0050-public-event-execution-semantics.md) settled what public events, outbound translation, event targets and event-source captures mean. [Decision 0049](0049-public-events-and-translation-direction.md) added their source forms, which binding refuses with `PLAY0268` (#480, #481, #482, #483, #484). This record is the admission checkpoint under [0004](0004-admission-and-governance-of-portable-executable-semantics.md) and [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md): it claims the next unused ESM version, 9, in the pull request that admits the constructs.

## Decision

1. **ESM v9 is selected only by use.** A model that declares a `public` event, an event origin (`from "store"`), a Translate `direction`, an event-target projection or reducer, or an events-source capture binds at language and semantic version 9.0. Every other model keeps its version and bytes.
2. **Canonical form.** An event carries `visibility: "public"` and `origin` only when set. A slice carries `direction` only when declared. A projection or reducer names `event` instead of `readModel` when it folds into the public event. A capture carries `sourceEvents`, the consumed event identities. A private event, an undirected slice and a read-model target add no member.
3. **Reference execution.** An event-target projection keeps one folded state per event source identity. Each new private fact updates it; when it is complete and differs from the last published instance for that key, one new instance of the public event is appended and observed by reactions. A repeated fact publishes nothing, and a changed mapping publishes new instances without rewriting earlier ones. Only `set` and `clear` of a root event property from a literal or event property is folded; any other mapping is a plan issue, so planning refuses it. An event-target reducer has opaque transitions and is `Unsupported` when reached. A capture over events is validated and round-trips; evaluating one is `Unsupported` with a typed reason until a record-from-event contract is accepted.
4. **Fail closed.** An outbound slice declares exactly one local public event, targets only it, folds only private events, declares no capture and produces nothing public from a command. An events-source capture sits in an inbound slice and reads only public events that carry an origin. A consumer must refuse a version it does not support.
5. **A public or foreign import has no local shape** and stays refused (`PLAY0268`); declare the foreign event locally as `event X from "store"`.

## Options considered

- **Version-less extension.** Not taken: the canonical form gains members and meaning, so a new number is required by 0004.
- **Execute events-source captures now.** Not taken: the mapping from a consumed event's payload to capture fields is undecided; refusing is honest.

## Default if unanswered

The constructs stay at the status the pull request merges them in; acceptance is a human verdict. Until accepted, ESM v9 is claimed but unreleased.

## Timeline and scope

Holds until superseded. Status 2026-10-09: implemented in the C# compiler and reference evaluator, the contract catalog, the MCP description, Monaco wording, the Invoicing and Commerce samples and `PublicEventsCorpus.V9`. The TypeScript compiler is syntax-only and already parsed every construct; it gains the `public-events-admission` conformance document.

## Verification

**Done when:** gate-2 evidence of 0004 holds on every surface and the golden `full-esm-v9.json` and a source-backed corpus vector are pinned.

## Consequences

Stage and other consumers can rely on the outbound contract; the shape of a foreign event is never verified (#593).

## Related

Builds on 0009, 0049, 0050; follows 0004, 0025.

## Status notes

**2026-10-10.** ESM v9 was released in Screenplay 4.117.0 (#598).
