---
id: 0137
title: Admit event subject lineage into the executable model (ESM v39, opt-in until Stage supports it)
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Conformance/**
  - Source/DotNET/Screenplay.Contracts/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
---

## Context

[Decision 0008](0008-one-data-subject-per-event.md) fixes that personal data in an event belongs to one subject, as in Chronicle. [Decision 0047](0047-data-subject-mark-on-event-properties.md) delivered phase 1 of [#141](https://github.com/Cratis/Screenplay/issues/141): an explicit `subject` mark on one event property. Phase 1 is authoring metadata. Binding reports `PLAY0270` and the mark adds no ESM bytes. [Decision 0068](0068-admit-concept-compliance-markers-as-esm-v13.md) therefore refuses a `pii` or `secret scope subject` value in an event that carries an explicit mark, because nothing executable says whose data it is.

Phase 2b makes the mark executable. Two things that were missing are now in place or in flight:

- Chronicle 19.116.0 no longer flags `[Subject]` by its type (Chronicle#4661), so a rendered `[Subject]` is valid there.
- Cratis/Stage#28 is the change that admits the new version and renders `[Subject]`. It has not shipped.

A new ESM version breaks consumers that do not know it. Stage and Studio refuse versions above the ones they admit. If the compiler emitted the new version for models that already carry a phase-1 mark, source that renders today would stop rendering. That ordering risk, not the shape of the field, is the hard part of this change.

## Decision

Screenplay records an event's subject in the executable model as ESM v39. It emits v39 only when a model opts in, and it keeps every existing model's bytes unless it does.

1. **Semantics.** One subject per event ([0008](0008-one-data-subject-per-event.md)). Each event contract, and each generation (prior revision) of it, carries an optional `subject` that names the property marked as the subject. An absent `subject` means the event source is the subject. The reference is a single field per revision, so a second subject cannot be written.
2. **Lifting the v13 refusal.** Under v39, an event with a marked subject may carry `pii` and `secret scope subject` values. The binder, validator and reader refuse them under any earlier version, as 0068 does today. A canonical model below v39 never carries a `subject`.
3. **Release intent: MINOR, behind an ordering gate.** Source that renders today must keep rendering.
   - Until a released Stage contains Cratis/Stage#28 support, v39 bytes are emitted only for a model that explicitly opts in. A modeler who opts in before that release accepts that no released Stage renders v39 yet.
   - Existing phase-1 models keep their current bytes and version by default. The `subject` mark stays authoring metadata with `PLAY0270`, as in 0047.
   - Selecting v39 automatically for marked models is not part of this record. Changing it is a bytes change for phase-1 models and needs its own record, written after a released Stage admits v39.
4. **Proposed opt-in.** An application-root declaration, `event subjects recorded`, following the pattern of `command reads protected` and `reaction batches atomic`. The name is proposed and can change at implementation.
   - It maps to a nullable init-only enum on `ApplicationSyntax`, absent by default and omitted from syntax JSON when absent, as `CommandReadMode` and `ReactionProductionMode` are.
   - It is declared once, at the application root. A repeat or a non-root declaration is a diagnostic.
   - A model that declares it binds at v39 whether or not it marks any subject. An unmarked event then has no `subject` field and means the event source.
   - Without the declaration, a mark together with protected values is still refused as in 0068.
5. **Prerequisites.** Cratis/Stage#28 and Chronicle at or above 19.116.0. Screenplay emits no C#. The renderer contract is that Stage renders `[Subject]` from the contract's `subject` for each generation, and from `PropertySyntax.IsSubject` on the source path.
6. **ESM version number.** v39 is provisional. The implementation holds it as one constant, so it can be renumbered at admission. It goes in after v37, and v38 is earmarked for #1138.
7. **Promise.** Lineage metadata only, as in 0008 item 4. It makes no erasure, export or redaction guarantee. Projection lineage is derived from the event reference, and no projection bytes are added. Read-model subjects are #559.

## Options considered

- **One optional event-level `subject` per revision (taken).** One field makes "one subject per event" structural. It matches Chronicle's one subject per append and the existing `workflows/subject` scoped identity.
- **A property-level `isSubject` boolean.** Not taken: it allows two marks and needs a further rule to forbid them.
- **Multiple subjects per event.** Not taken: Chronicle selects one encryption key per append, so the model could state what no realization preserves (0008).
- **Per-property `about` (the issue's illustration).** Not taken, for the reasons in 0008: it needs per-property subjects in Chronicle, and it changes the shared property-line grammar.
- **A major version break.** Not taken: nothing here removes or reinterprets existing source or bytes, so MINOR is enough once emission is gated.
- **Select v39 automatically for any marked model.** Not taken now. It changes bytes for phase-1 models and makes Stage and Studio refuse them until Stage#28 ships. It may follow as a separate record after that release.
- **Gate by "only events that also hold protected values".** Not taken: an event's version would depend on concept markers declared elsewhere, and a mark without protected values would be silently dropped.
- **Gate on Stage by a runtime check instead of an opt-in.** Not taken: a compiler cannot see which Stage consumes its output.

## Default if unanswered

Marks stay report-only (`PLAY0270`). Protected values in an event with an explicit mark stay refused, and a model cannot say whose personal data an event holds in a form other consumers read. Phase 1 remains in place and nothing breaks.

## Timeline and scope

Accept before any v39 implementation. Phase B (implementation) is built only after v37 is admitted, so v39 is numbered above it. The opt-in stays the selector until a later record changes it.

In scope: the `subject` field on event contracts and generations, the v39 admission and its refusals, lifting the v13 refusal under v39, the opt-in, the renderer contract for Stage, the ESM version constant, goldens, corpus and documentation.

Out of scope: rendering C# (Stage#28), erasure and export guarantees, read-model subject marking (#559), subjects on commands or types, Chronicle changes, and automatic v39 selection.

## Verification

**Done when:**

- A model without the opt-in binds to the same version and bytes as before, including models with phase-1 marks. Every earlier golden is byte-identical.
- A model with the opt-in binds at v39. A marked event has `subject` equal to the marked property's id on each generation. An unmarked event has no `subject`.
- Protected values together with a mark bind at v39 and are refused below it.
- The writer and reader refuse `subject` below v39. The validator refuses a `subject` that is unknown, optional, a collection, a composite, a bare `Int`, a `pii` or secret concept, or on another revision.
- A projection from a marked event resolves its originating subject through the event reference.
- v39 is held in one constant. Documentation states the rule, the opt-in, the ordering gate and the lack of an erasure promise.

**Verify by:** binder, validator and reader specs for each case; a `full-esm-v39.json` golden with its canonical vector and a corpus entry; the frozen-golden specs for earlier versions; and one projection-lineage spec. Stage#28 states the same `subject` contract in its own admission.

## Consequences

Models can state whose data an event holds in a form that Stage and other consumers can realize, and the personal-data refusal can be lifted for marked events. Nothing that renders today changes, because emission is opt-in until Stage supports v39. The cost is that a modeler must declare the opt-in, and a model that opts in before a Stage release admits v39 cannot yet be rendered by a released Stage. Marked models do not get lineage by default until a later record changes that. A provisional version number may be renumbered at admission, so goldens and corpus paths are produced from the one constant. Per-property subjects and multiple subjects per event stay closed off unless Chronicle gains them.

## Related issues

Screenplay: [#141](https://github.com/Cratis/Screenplay/issues/141), [#559](https://github.com/Cratis/Screenplay/issues/559), [#1138](https://github.com/Cratis/Screenplay/issues/1138). Stage: [Cratis/Stage#28](https://github.com/Cratis/Stage/issues/28). Chronicle: [Chronicle#4661](https://github.com/Cratis/Chronicle/issues/4661). Decisions: [0008](0008-one-data-subject-per-event.md), [0047](0047-data-subject-mark-on-event-properties.md), [0068](0068-admit-concept-compliance-markers-as-esm-v13.md).
