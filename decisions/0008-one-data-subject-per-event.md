---
id: 0008
title: Personal data in an event belongs to one subject
status: accepted
stage: none
decided: 2026-09-24
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Syntax/EventSyntax.cs
  - Source/DotNET/Screenplay/Parsing/EventParser.cs
  - Source/DotNET/Screenplay/Parsing/PropertyLineParser.cs
  - Source/DotNET/Screenplay/Parsing/ScreenplayValidator.cs
  - Source/DotNET/Screenplay/Semantics/**
  - Documentation/screenplay/events.md
  - Documentation/screenplay/concepts.md
---

## Context

[#141](https://github.com/Cratis/Screenplay/issues/141) asks for a way to say which identity a personal value is about, separate from PII classification, and illustrates it per property: `email EmailAddress about customerId`. Screenplay already classifies personal data on the concept (`@pii`, [`concepts.md`](../Documentation/screenplay/concepts.md), [`ConceptSyntax.cs:23-25`](../Source/DotNET/Screenplay/Syntax/ConceptSyntax.cs)), but nothing says whose data it is. The triage on #141 left three choices open: where the relationship attaches, the default when it is absent, and how strong the promise is.

Under [decision 0001](0001-chronicle-runtime-semantic-authority.md), Chronicle defines the meaning. It has one subject per event:

- `[Subject]` marks one property or record parameter of an event as its subject ([`SubjectAttribute.cs`](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/SubjectAttribute.cs)). The resolver reads the first marked member ([`SubjectResolver.cs:17-25,51`](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/SubjectResolver.cs)).
- An append carries one optional subject; when it is omitted, the event source id is the subject ([`IEventSequence.cs:113`](https://github.com/Cratis/Chronicle/blob/main/Source/Clients/DotNET/EventSequences/IEventSequence.cs)). The subject selects the encryption key for PII.

## Decision

Personal data in an event belongs to exactly one subject, as in Chronicle.

1. **Default.** An event's subject is its event source.
2. **Explicit subject.** A property of an event may be marked as the event's subject. The keyword is chosen by the implementing change; this record fixes what it attaches to and what it means.
3. **Mixed subjects fail.** An event that carries personal data (`@pii` values) of more than one subject fails compilation with a diagnostic, and the fix is to split the event. Marking more than one property as the subject is the first case the compiler detects. Any further detection it adds is stated in the implementing change and must not guess.
4. **Promise.** The relationship is lineage metadata only. The ESM records the subject for each event and preserves it into projections. It makes no erasure, export or redaction guarantee; any such capability needs its own record.

## Options considered

- **One subject per event, marked on a property (taken).** It is what Chronicle can realize: one subject selects one encryption key per event.
- **Per-property `about` (the issue's illustration).** Not taken: Chronicle cannot hold two subjects in one event, so a model could state a relationship no realization preserves. It would also change the shared property-line grammar for commands, events and types at once ([`PropertyLineParser.cs`](../Source/DotNET/Screenplay/Parsing/PropertyLineParser.cs)).
- **Subject on the concept.** Not taken: a concept such as `EmailAddress` is used for many subjects, so the concept cannot know whose value it holds.
- **No default, or a warning when absent.** Not taken: Chronicle defaults to the event source, and a warning on every event with personal data would be noise for the common case.
- **An ESM capability that blocks targets without erasure.** Not taken now: it needs a capability model the ESM does not have, and it would imply a guarantee beyond lineage.

## Default if unanswered

Models keep classifying personal data without saying whose it is. A realization defaults to the event source silently, and events that mix two people's data pass compilation and are encrypted under one key.

## Timeline and scope

Settle before any #141 syntax, and keep it until superseded. Admission to the ESM follows [decision 0004](0004-admission-and-governance-of-portable-executable-semantics.md).

In scope: the event-level subject mark, the event-source default, the mixed-subject compile error, subject lineage in the ESM and into projections, and documentation.

Out of scope: erasure, export and redaction guarantees; read-model subject marking for manual release; subjects on commands or types; changes to Chronicle.

## Verification

**Done when:** An event without a mark has its event source as subject in the ESM. An event with one marked property has that property as subject. Marking two properties fails compilation. A projection built from the event carries the originating subject in its lineage. `events.md` states the rule and the default, and says the relationship promises no erasure.

**Verify by:** Parser, validator and binder specs for each case, a golden vector for the subject in canonical form, and a lineage spec through one projection.

## Consequences

The model states whose data an event holds in a form Chronicle can realize. Events that mix subjects must be split, which is sometimes more events than a modeler would write by instinct. The issue's per-property `about` is closed off unless Chronicle gains per-property subjects. Anyone reading the relationship as an erasure guarantee is wrong by design; that promise needs its own record.

## Related issues

Screenplay: [#141](https://github.com/Cratis/Screenplay/issues/141), [#128](https://github.com/Cratis/Screenplay/issues/128). Decisions: [0001](0001-chronicle-runtime-semantic-authority.md), [0004](0004-admission-and-governance-of-portable-executable-semantics.md).
