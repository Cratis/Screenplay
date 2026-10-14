---
id: 0077
title: Present a consumed public event to a capture over events as the event itself, and fold it into per-key state as Chronicle does
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/Execution/**
  - Source/DotNET/Screenplay/Parsing/PublicEventUsageValidator.cs
  - Source/DotNET/Screenplay/Diagnostics/DiagnosticCodes.cs
  - Source/DotNET/Screenplay.Contracts/Golden/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/captures.md
  - Documentation/screenplay/captures/grammar.md
  - Documentation/screenplay/specifications.md
  - Documentation/screenplay/diagnostics.md
  - Samples/Commerce/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

[0051](0051-admit-public-events-as-esm-v9.md) item 3 admitted `capture … source events` into ESM v9 but kept its evaluation `Unsupported` "until a record-from-event contract is accepted", because "the mapping from a consumed event's payload to capture fields is undecided". [#483](https://github.com/Cratis/Screenplay/issues/483) asks for that contract. Today `SemanticScenario.Present` refuses every `when capture` that names such a capture (`SemanticScenario.cs:168-177`). The Commerce sample `ReceiveCarrierDispatches` documents that refusal in a comment.

Two problems are not settled by the current forms:

- **The record does not identify the event.** A `when capture` record is a bag of untyped fields "named as the source names them" (`specifications.md`). With several `from` lines (the `captures.md` example reads `ShipmentDispatched` and `ShipmentDelivered`), neither the reference nor a renderer can tell which event arrived. The fields are also never checked against the event shape that the consumer declares locally ([0050](0050-public-event-execution-semantics.md) item 1).
- **A consumed event can already be presented, and the capture ignores it (fail-open).** Specification fixtures are not usage-checked (`PublicEventUsageCollector.cs:74-77`), so `given ShipmentDispatched` and `when append ShipmentDispatched` bind and drive inbound reactions. The reference never delivers the fact to a capture over events. As a result, `when append ShipmentDispatched` followed by `then no events` passes where Chronicle would append.

Chronicle defines the runtime meaning ([0001](0001-chronicle-runtime-semantic-authority.md)). Captures from events shipped in v19.37.0 (`Documentation/captures/capture-declaration-language/index.md`, "Events source"). The parts this record relies on are unchanged through v19.65.2 (`Source/Kernel/Core/Captures/**`):

- **The item and its context.** The captured item is the incoming event's content. `$context.<p>` is the incoming event's context: `eventType`, `eventSourceType`, `eventSourceId`, `eventStreamType`, `eventStreamId`, `sequenceNumber`, `occurred`, `correlationId`, `subject` (`CapturedEventContext.cs`).
- **The key.** It is resolved from the incoming event's own content or context, never from the accumulated state. An empty key fails the partition (`CaptureEventTranslator.ResolveKey`, `MissingKeyForCapturedEvent`).
- **State per key.** Each event's top-level properties are laid over the previous state (`CaptureEventTranslator.Merge`). `$.path` reads that merged state.
- **Change detection.** The merged state is compared with the previous one (`CaptureChangeDetector`). The first event for a key is `Added`. An event that changes nothing yields no change and appends nothing. `when <field>`, `and`/`or` and transitions need a `Modified` change (`WhenClauseEvaluator`).
- **Appended events.** They are private local facts recorded at handling time. The source `occurred`, sequence, type and event source go into the causation, and the correlation carries over. Each append is tagged with its source event, so redelivery appends nothing again (`CaptureEventsSubscriber.AppendOnce`).
- **What the engine refuses.** `map`, `nested`, `children`, expression `when`, and an `append` without `when` (`CaptureValidator.ValidateScopes`, `CaptureDefinitionSyntaxVisitor.ConvertAppend`).

## Decision

1. **A specification presents the consumed event itself.**
   - `given <Event>` and `when append <Event>`, where `<Event>` is a `from` event of a capture over events, are that capture's input.
   - The fixture is typed by the locally declared event. `for` states the incoming event's event source.
   - After `when append`, the appended fact is the action itself, as in 0022's status notes. `then` compares what the capture and the reactions it set off appended.
   - `given capture` or `when capture` naming a capture over events is a compile error, **PLAY0701**. If the capture has exactly one `from` event, the repair rewrites it to `given <Event>` / `when append <Event>` with the same lines. With several `from` events there is no repair, because the event is ambiguous.
   - No grammar changes.
2. **The key comes from the incoming event.** The capture's `key` names a property that every `from` event declares as a required (not `optional`) property; otherwise it is error **PLAY0700**, which mirrors Chronicle's partition failure at compile time. Key values keep 0022's rules: text or number, and numeric and text keys stay distinct. A key value that is not text or a number at run time is `Unsupported`.
3. **State is the overlay of the key's events.**
   - The capture remembers one state per key. It starts from the specification's `given` facts of its `from` events, folded in authored order. Folding a given never appends, because givens are history, as for reactions.
   - The presented event's own properties (including any it carries as null) are laid over that state. Properties it lacks keep their earlier value. Composite and list values replace the earlier value whole.
   - Running the specification never stores a new state (0022).
4. **Change and conditions follow Chronicle, evaluated on the mapped before and after states.**
   - No earlier state means `added`.
   - A merged state equal to the previous one is no change, and the event appends nothing under any rule.
   - Otherwise it is a modification:
     - `when <field>`, `and`/`or` and `from … to` hold only on a modification.
     - `when added` holds only on the first event for a key.
     - `when removed` never holds at the root.
     - An `append` without `when` holds on any change, added or modified.
     - An expression `when` is evaluated on the after state, and only when something changed.
   - `map` applies to both states as in 0022.
5. **Values.**
   - `$.<field>` reads the mapped merged state.
   - `$context.occurred` is the incoming event's occurrence. In the reference every occurrence is the scenario's `given clock`, so it is the clock, and it needs one as today.
   - Appended facts occur at the same instant, go to the event source the key names, and are typed as in 0022.
   - No other `$context` member and no `key $eventSourceId` is admitted. Both keep failing binding as they do today.
6. **Delivery and order.**
   - Each presented event is delivered once to every capture over events whose `from` lists it. Delivery happens after the reactions the fact set off have settled, in capture semantic-identity order.
   - Within a capture, appends run in authored order, and each one settles its own cascade before the next (0022).
   - A capture appends only private events, so delivery cannot cycle.
   - The 1 000-fact budget applies.
7. **Not modelled, and not guaranteed by the reference.** Redelivery and Chronicle's source-event tag deduplication, partition failure and retry, inbox sequence numbers, causation, correlation, subject carry-over, namespaces, and ordering between independent observers.
8. **What still fails closed.**
   - `children` and `nested` on a capture over events are `Unsupported` in this increment.
   - A composite or list value used as a scalar, or watched by a condition, is `Unsupported` (0022).
   - A canonical model carrying `whenCapture` or `givenCaptures` for a capture over events stays valid and keeps a typed `Unsupported` outcome, now also for `givenCaptures`.
9. **No ESM change.**
   - No new member and no new version. Fixtures use the existing `givenEvents`/`whenAppended` members, and the capture uses v9 `sourceEvents`.
   - The reference outcome changes in every version that admits captures over events (v9 and later). This follows the precedents of 0054 item 11, [0071](0071-whitespace-only-text-does-not-satisfy-not-empty.md) and [0072](0072-query-arguments-are-validated-against-concept-rules.md): bytes unchanged, outcome corrected toward the runtime.
   - The previously ignored delivery was a fail-open defect against 0051 item 3, not a contract that consumers relied on.

## Options considered

- **Reuse `when capture <Capture> <record>` with the payload as the record.** Not taken.
  - It costs nothing in the language.
  - But it cannot say which of several `from` events arrived, leaves the fields unchecked, and gives Stage no event type to put into `inbox-<origin>`.
  - It also leaves the `when append` fail-open in place.
- **Reuse `when capture` only for captures with exactly one `from` event, or infer the event from the field names.** Not taken: the contract would silently change meaning when a second `from` line is added, and field inference is ambiguous.
- **A new form `when capture <Capture> from <Event>`.** Not taken.
  - It is precise, but it changes the grammar on every surface: both compilers, printer, Monaco, TextMate and the ESM.
  - It still duplicates a form that already exists and runs: `when append` of the event.
- **Present the foreign event (chosen).** No grammar or ESM change. The fixtures are typed. It matches the runtime: Chronicle receives an event in its inbox, and Stage appends the same fixture there, with the inbox derived from the v9 `origin`. One action drives both inbound reactions and captures.
- **State: each event as a fresh record, or the last event replaces the state.** Not taken: Chronicle overlays, because "different event types carry different parts of the state".
- **First event: 0022's rule that a first record changed in every field.** Not taken here: a capture over events runs only on Chronicle, where the first event is `added` only. Specifications would pass in the reference and fail when rendered.
- **Unconditional append on every event, or `Unsupported`.** Not taken: Chronicle appends nothing for an event that changes nothing, whatever the rules. "Any change" keeps the frozen v9 corpus capture meaningful.
- **A new ESM version.** Not taken: no bytes change, and the precedents above correct outcomes across versions.

## Default if unanswered

Captures over events stay `Unsupported` when presented with a record, and the fail-open remains: given and appended events of a `from` event never reach the capture. At minimum, the reference should then return `Unsupported` when such a fact reaches a capture over events.

## Timeline and scope

Holds until superseded. In scope: the reference evaluator, PLAY0700 and PLAY0701 in both compilers and editors, docs, the Commerce sample and vectors. Out of scope: the remaining `$context` members, `key $eventSourceId`, `children`/`nested` over events, redelivery to captures, Stage realization, and record captures (`source api` and others), whose first-record rule stays as 0022 decided.

## Verification

**Done when:**

- `when append` of a `from` event appends what the capture maps.
- Given events fold so a transition fires.
- A repeated identical event passes `then no events`.
- On a first event, `when added` holds and `when <field>` does not.
- Two `from` events overlay.
- PLAY0700 and PLAY0701 have shared diagnostic vectors.
- The Commerce sample specification passes.
- Every golden and corpus byte is unchanged, and a new source-backed v9 vector pins the outcomes.

**Verify by:** runner specifications, validator specifications in C# and TypeScript, `diagnostics.json`, the contract golden, the vector specs, and a golden diff against `main`.

## Consequences

Inbound translations that use captures over events become specifiable and executable, consistent with how Chronicle runs them. Two capture families now differ on the first record, until record captures are aligned. The reference passes specifications that the Chronicle engine cannot yet realize (`map`, an `append` without `when`, expression `when`); Stage must refuse those.

## Open questions

1. Whether Chronicle's serializer writes a null optional property, so that it overlays as null, or omits it. Unverified; the reference overlays only what the fact carries.
2. Order between a capture and reactions to the same event. Chronicle guarantees none; mixed assertions may need `then events in any order`.
3. The reference has one log, so foreign facts are visible to `all`/`every` projections, whereas Chronicle keeps them in the inbox. This predates this record.
4. Aligning record captures with Chronicle's first-record rule needs its own decision ([#767](https://github.com/Cratis/Screenplay/issues/767)).
5. Whether Chronicle should accept an `append` without `when`, `map` and expressions for captures over events, or Stage must refuse them. Chronicle's "engine subset" caution still says `$context` is rejected, but the code accepts it for events sources.
6. Whether to admit `given capture` as remembered state. Chronicle stores exactly that.
7. Stage tracking issue: number to be opened.
8. Whether `for` is required when a foreign event has no local producer. To check during implementation.

## Related

Completes 0051 item 3; builds on 0009, 0022, 0049, 0050; follows 0001, 0004, 0066, and the 0071/0072 precedent.
