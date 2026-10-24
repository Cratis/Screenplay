---
id: 0114
title: Let an events capture key by its event source (ESM v33)
status: accepted
stage: implemented
class: contract
reversibility: reversible
decided: 2026-10-24
decider: Sindre Alstad Wilting
amends: 0105
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
---

> **2026-10-24 — acceptance.** Accepted by the repository coordinator under Sindre Alstad Wilting's delegated authority for additive repository decisions, not a new direct answer from Sindre. Acceptance covers only the bounded opt-in contract below; it does not certify implementation or consumer readiness.

## Context

An inbound `source events` capture reads a foreign event whose source is opaque text: a fixture's `for` is not the
capture's key (`captures.md`). Stage#752 needs to know when the foreign source *is* the key, so a host can narrow the
inbox read to the key instead of scanning. The language cannot say so today. The freeze (0092) is lifted by 0105.

## Decision

1. **Syntax.** `key <property> is event source` on a `source events` capture. The suffix belongs to the `key` line.
2. **Meaning.** Every captured event is sourced by the capture's key. The reference rejects a fixture whose `for` differs
   from the key's value; a fixture without `for` takes the key. An unmarked capture is unchanged.
3. **Admission.** Only a `source events` capture with a single property key may declare it (`PLAY0809`, an error, no
   repair). Dotted paths and `$` keys stay with `PLAY0700`.
4. **Version.** A marked capture selects ESM v33 and serializes `keyIsEventSource` beside its `sourceEvents`; v32 is frozen.

## Options considered

- **`key orderId is event source`.** Chosen: it uses the documented term and adds one CDL word, `is`.
- **A bare `source` marker.** Rejected: `source` is already a CDL directive.
- **Infer it from the key's type.** Rejected: a typed key says nothing about the foreign source.

## Default if unanswered

Without acceptance, captures cannot declare this equality and hosts cannot safely narrow inbox reads by the capture key.

## Timeline and scope

This contract applies to ESM v33 marked source-events captures. ESM v32 and unmarked captures remain unchanged. Consumer admission is separate: unsupported consumers must reject v33 clearly until upgraded.

## Verification

**Done when:** marked captures preserve the key/source equality through parsing, serialization and reference execution, infer a missing fixture source, and reject a conflicting source without changing unmarked models.

**Verify by:** the focused C# and TypeScript capture specifications, v33 round-trip and frozen earlier-version vectors, followed by the assembled Release gate and consumer admission checks. Final assembled verification is outstanding.

## Consequences

- **Stage:** admit v33; for Stage#752 narrow the inbox read when `keyIsEventSource` is set.
- **Screenplay:** parser, printer, binder, serializer, runner, TypeScript compiler and editor grammars.
- **cli, Studio, Arc:** admit and round-trip v33. **AI corpus:** Cratis/AI#891.

## Related

Issue Cratis/Screenplay#1042; 0105; 0092.
