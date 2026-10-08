---
id: 0047
title: Mark an event's data subject with a trailing subject modifier on a property (phase 1)
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-08
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/Syntax/PropertySyntax.cs
  - Source/DotNET/Screenplay/Parsing/PropertyLineParser.cs
  - Source/DotNET/Screenplay/Parsing/EventParser.cs
  - Source/DotNET/Screenplay/Parsing/ProducesParser.cs
  - Source/DotNET/Screenplay/Parsing/InlineEventValidator.cs
  - Source/DotNET/Screenplay/Parsing/ScreenplayValidator.cs
  - Source/DotNET/Screenplay/Parsing/EventSubjectValidator.cs
  - Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Structure.cs
  - Source/DotNET/Screenplay/Printing/**
  - Source/DotNET/Screenplay.Mcp/McpDeclarationDetails.cs
  - Source/Screenplay/Compiler/Parsing/PropertyLineParser.ts
  - Source/Screenplay/Compiler/Parsing/EventSubjectValidator.ts
  - Source/Screenplay/Monaco/screenplay-language/**
  - Source/Screenplay/VSCodeExtension/syntaxes/**
  - Documentation/screenplay/events.md
  - Documentation/screenplay/concepts.md
  - Documentation/screenplay/commands.md
  - Documentation/screenplay/diagnostics.md
  - Samples/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

Accepted [0008](0008-one-data-subject-per-event.md) settles [#141](https://github.com/Cratis/Screenplay/issues/141)'s meaning, following Chronicle:
- An event has exactly one subject, defaulting to its event source.
- A property may be marked as the subject. 0008 leaves the keyword to the implementing change.
- Marking two properties fails compilation.
- The promise is lineage only.

Chronicle's `[Subject]` resolver reads the first marked member and converts it to a string. A null or empty value falls back silently to the event source (`SubjectResolver.cs:26-73`). Chronicle's `Subject` is a plain string, and the kernel stores it per appended event.

**Interaction with 0041.** Accepted [0041](0041-personal-data-secrets-and-processing-purposes.md) says compliance markers (`pii`, `secret`) are bare suffix keywords on concepts, with "no property/type-level markers", and that 0008 continues to govern subject lineage. There is no conflict: `pii` and `secret` **classify a value** (what it is), while `subject` is a **role marker** (which of an event's values identifies the data subject). 0041's rule keeps classification off properties and types; this record adds a role to one property of an event, and 0008, not 0041, governs it.

**What cannot happen yet.** 0008's verification needs the ESM to record the subject and carry it into projections. The binder refuses compliance markers with `PLAY0268` (`SemanticModelBinder.Concepts.cs`; 0034, 0041), and admitting them needs an admission record under [0004](0004-admission-and-governance-of-portable-executable-semantics.md) and a version claim under [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md), which #407, 0030's admission and #383 also contend for.

## Decision

Phase 1 adds the mark, its checks and its tooling, and leaves the ESM unchanged. **Phase 2** (ESM lineage and admission together with `pii`) is a separate admission record. It must not change the bytes of models that already bind with marks unless it selects a newly claimed version.

### Syntax

A trailing bare `subject` modifier on an event property, **last in the modifier order** after any other modifier, on standalone and inline events:

```screenplay
concept CustomerId   : Uuid
concept EmailAddress : String pii

event CustomerEmailChanged
  customerId CustomerId subject
  email EmailAddress

command ChangeEmail
  accountId AccountId identifier
  customerId CustomerId
  email EmailAddress
  produces event CustomerEmailChanged
    customerId CustomerId subject = customerId
    email EmailAddress = email
```

- On inline events the modifier sits before the mapping `=`.
- A property named `subject` or of type `subject` still parses (`subject String` in `Samples/Invoicing`): the modifier is purely positional.
- The shared modifier grammar is `optional generated identifier`. `InvalidGeneratedModifiersRegex` is extended with `subject` in last position in both compilers, so malformed orders such as `x T optional subject` get a precise error rather than a generic one.
- An event without a mark has its event source as subject (0008). The resulting value is readable as `$eventContext.subject`, which already means "the event source identifier unless one was given".

### Valid targets

The mark is valid only on a property of a standalone or inline event. Each generation is marked independently and phase 1 adds no cross-generation rule.

**Allowed:** `Uuid`, `String`, concepts over either, and Int-backed concepts. **Bare `Int` is refused.** This is exactly the stream-id set (PLAY0506, `EventSourceValidator.cs:138-141`), so the two identity rules cannot diverge. Phase 2 can reuse the canonical formatting of [0023](0023-command-production-model.md) and [0033](0033-composite-event-stream-ids.md): text unchanged, UUID lowercase hyphenated, integer in invariant decimal.

**Refused**, each with a message that names the rule:
- `optional` values, because a null silently moves the key to the event source;
- collections and composite types;
- enums;
- `Decimal`, `Bool`, `Date` and `DateTime`, bare or as concepts;
- concepts marked `pii` or `secret`, including the legacy `@pii`, `sensitive`, `@sensitive` forms and the `personal` alias. The subject is the key id and is stored in plaintext as `EventContext.Subject`; Chronicle refuses `[PII]` on event source ids for the same reason (CHR0034), and Screenplay already refuses protected identifiers (PLAY0515). The message suggests a surrogate identity.

An imported type whose shape is unavailable stays unresolved, as stream ids do: an unknown-type warning, not an error. A mark on an event with no personal data is allowed without a diagnostic.

### Other owners

- **Two marks on one event are an error**, reported on each extra mark and naming the first. There is no automatic repair; choosing the subject is a modeling judgment. Subtler mixing stays undetected and is documented ("must not guess", 0008).
- **Commands, types and responses: refused permanently**, with a message citing 0008. Neither Arc nor Chronicle has a counterpart.
- **Read models: "not yet supported".** Chronicle now resolves `[Subject]` on read models (`ReadModelSubjectResolver.cs`) and reserves the property names `_subject`, `__subject` and `__subjects` (CHR0035). The message says so, and [#559](https://github.com/Cratis/Screenplay/issues/559) covers the read-model mark and the reserved names, since Screenplay identifiers allow them.

### Syntax tree, binding and MCP

- `PropertySyntax` gains `IsSubject`, following `IsGenerated`; the writer omits it when false. A flag on the property, not an event-level member naming one, travels through rename, move and `propose-extract-inline-event` with no repair code.
- **Binding.** At binding the mark reports `PLAY0270` (information) as report-only metadata and adds **no ESM bytes**: bytes and revision are identical to the unmarked model. Models using `pii` or `secret` concepts still fail with `PLAY0268`, as today.
- **MCP.** `declaration-details` for events adds `subject: { "source": "eventSource" }` or `{ "source": "property", "property": "<name>" }`, and the `properties` view adds `isSubject`. `syntax-schema` shows the member. The golden contract of [0039](0039-publish-a-machine-readable-screenplay-contract.md) changes with it and is regenerated.

### Editors

`subject` already sits in the global keyword alternation of the TextMate grammar, so highlighting is unchanged; the property-modifier pattern gains a `subject` capture. **Hover and completion are context-sensitive** because the word has three meanings:

| Position | Meaning |
| --- | --- |
| Event property line | The data subject of the event (this record) |
| Policy `claim … matches subject` | The identifier of the thing acted on, not the caller |
| `secret scope subject` (0041) | Encryption scoped per data subject |

Monaco hover looks the word up with no context today (`hover-content.ts:200`). It is changed to branch by position. The existing `subject` entry in `keyword-docs.ts` is also wrong, because it says `matches subject` is the caller; the caller is only `$context.causedBy.subject`. That entry is corrected in the same pull request.

### Providers and downstream

- **C# providers rendering `[Subject]` are NOT promised.** Stage renders a concept used as a command `identifier` or projection key as `EventSourceId<T>`, and Chronicle's analyzer CHR0026 then warns on `[Subject]` for it and tells the author to remove it. The canonical #141 case, `[Subject] CustomerId` on an Order event, trips it. Following the analyzer silently moves the PII to the wrong key. Screenplay events never carry their own source id, so here the warning always points at a different source's identity. [Cratis/Chronicle#4661](https://github.com/Cratis/Chronicle/issues/4661) asks to narrow or correct CHR0026; the Stage side follows once that direction is settled. Provider output waits for both.
- Only the .NET client resolves `[Subject]` on append. Other clients pass the subject explicitly, so they need the subject in the ESM, which is phase 2.
- **Out of scope:** reactions and translations forwarding `context.Subject` ([0009](0009-external-event-origin-and-translation-slices.md)); erasure, export and redaction; mixed-subject detection beyond two marks; Chronicle changes.

## Options considered

- **Trailing `subject` modifier (chosen).** It matches Chronicle's `[Subject]`, `Subject` and `EventContext.Subject` and Screenplay's own `$eventContext.subject`, and is purely positional.
- **`@subject`.** Rejected: `@` is the identifier escape, so `@subject` already reads as "a name called subject".
- **An event-level `subject customerId` directive.** Rejected: it would reserve `subject` as a first word in event bodies, breaking the shipped `subject String`, and duplicates a property name that rename and extraction must repair.
- **Per-property `about X`.** Rejected by 0008: Chronicle holds one subject per event.
- **`personal subject` or `datasubject`.** Rejected: no grammar gain, and it drifts from Chronicle naming.
- **Bare `Int`, or concepts only.** Bare `Int` is refused to match stream ids and phase 2's formatter. Concepts-only would refuse models that stream ids accept; stricter than Chronicle for no runtime gain.
- **Anything Chronicle stringifies.** Rejected: decimals and dates give culture-dependent key ids and phase 2 has no portable formatter.
- **An information diagnostic when the marked value is mapped from the event's own `for`.** Not in v1; it can be added without changing anything else.
- **Refusing the mark at binding (PLAY0268).** Rejected: without `pii` the mark has no executable consequence, so refusing blocks models for no safety gain.
- **Doing phase 2 now.** Rejected: it needs the contended version claim.

## Default if unanswered

Models cannot say whose data an event holds, and providers default to the event source silently.

## Timeline and scope

From acceptance, governing #141 until a phase 2 admission record supersedes its ESM section. Implementation lands after #557 phase 1, which changes the same parser and validator files.

**Done when**, in one pull request across both compilers, the MCP server, editors, documentation and one sample:
- both compilers parse, print and round-trip marks on standalone and inline events;
- every diagnostic fires on both compilers: two marks, invalid target, protected target, wrong owner and the order error;
- binding reports `PLAY0270` and produces bytes identical to the unmarked model;
- MCP shows `subject` and `isSubject`; extraction and rename preserve the mark;
- hover branches by context and the wrong keyword text is fixed;
- `events.md` states the default, the rule, the lineage-only promise, and that an empty `String` subject cannot be caught at compile time (suggest a `not empty` rule on the concept) and that equal ids across entity kinds share one subject (favor `Uuid`).

**Verify by:** parser, printer, syntax JSON and conformance vectors; validator specs per rule in both compilers; a binder byte-comparison spec; MCP `declaration-details` specs; `propose-extract-inline-event` and `propose-rename` specs.

Downstream issues: Chronicle (CHR0026), Stage (`[Subject]` rendering), Cratis/AI skills (`cratis-screenplay-command-surface`, `cratis-screenplay-slice-design`, `cratis-screenplay-model-review`), Screenplay.Generation and Prologue, and the read-model follow-up above.

## Consequences

Authors can state whose data an event holds in a form Chronicle realizes, and loosening the allowed types later is cheap while tightening would break models, so the set starts strict. Until phase 2 the statement lives in syntax and MCP, not in the executable model, and provider output waits on the CHR0026 fix. Two marks fail early.

## Related

Screenplay [#141](https://github.com/Cratis/Screenplay/issues/141), [#128](https://github.com/Cratis/Screenplay/issues/128). Decisions 0001, 0004, 0008, 0009, 0023, 0025, 0033, 0034, 0039, 0041.
