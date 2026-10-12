---
id: 0068
title: Admit concept compliance markers into the executable model as ESM v13
status: accepted
stage: none
decided: 2026-10-12
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Documentation/screenplay/**
---

## Context

[0034](0034-sensitive-means-operational-secret.md) and [0041](0041-personal-data-secrets-and-processing-purposes.md) let a concept carry the compliance markers `pii` (alias `personal`) and `secret`. The syntax is parsed and printed (`ConceptSyntax.cs`, `ConceptComplianceParser.cs`), but the semantic binder refuses every marker with `PLAY0268` (`SemanticModelBinder.Concepts.cs:22-25`). A specification locks this in (`when_binding_compliance_qualifiers.cs`). A model with a protected concept therefore cannot reach the executable model, and consumers that render from that model, such as Stage and Studio, cannot realize `[PII]`, `[Encrypted]` and `[NotAudited]` from it ([Cratis/StudioIssues#608](https://github.com/Cratis/StudioIssues/issues/608)).

Only the markers are refused. Concept `validate` rules are already admitted: `SemanticModelBinder.Concepts.cs:28` binds them, the reader has no version gate (`SemanticModelRead.cs:104`) and the reference evaluator checks them on command inputs (`SemanticEvaluator.cs:744-748`). This record does not change them.

Facts that shape the proposal:

- `pii` and `secret` map to Chronicle attributes as in 0034: `pii` to `[PII]`, `secret` to `[Encrypted]` and `[NotAudited]`, and both together to `[PII]` only. Chronicle treats any property whose type is a `[PII]` concept as personal (`PIIMetadataProvider.cs`).
- 0041 forbids property-level and type-level markers. The marker classifies a concept's value.
- Accepted [0008](0008-one-data-subject-per-event.md) item 1 makes an event's data subject its event source unless a property is marked. [0047](0047-data-subject-mark-on-event-properties.md) added the `subject` mark as authoring syntax and planned a single phase 2 that admits lineage and `pii` together.
- Lineage cannot be realized yet. A C# provider rendering `[Subject]` trips Chronicle's CHR0026 on identity concepts, which [Cratis/Chronicle#4661](https://github.com/Cratis/Chronicle/issues/4661) tracks.
- v12 is published and frozen ([0066](0066-published-esm-versions-are-frozen.md)). The next free version is v13, checked against `origin/main` at v4.139.0 and subject to rechecking at admission under [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md).

Tracked in [Screenplay#141](https://github.com/Cratis/Screenplay/issues/141).

## Decision

Admit the compliance markers into the executable model as ESM v13, without subject lineage. Concept `validate` rules need no change.

### Model shape

`SemanticConcept` gains an optional `compliance` member, written after `numbers` and only when present:

```json
"compliance": {
  "personal": { "special": ["health"], "criminal": true },
  "secret": { "scope": "namespace" }
}
```

- `personal.special` is the closed list of GDPR Article 9 categories that `pii special <category>` accepts. `personal.criminal` is `true` for `pii criminal`. Both are optional.
- `secret.scope` is `subject`, `namespace` or `global`, optional.
- Only a model that uses a marker selects v13. A model without markers keeps its bytes and revision.
- v13 includes the v11 exact-number lowering and the v12 enum member numbers contracts.
- Free-text reasons (`pii reason`) stay report-only (`PLAY0270`) and are not stored in the model. They can travel as authoring metadata the way descriptions do.
- `pii secret` keeps both markers in the source so it round-trips, but `personal` takes precedence as in 0034. A secret scope is not stored next to `personal`; the parser already warns about that combination (`ConceptComplianceParser.cs`).

### Meaning and propagation

The marker classifies the concept's value. It applies through type references to every command, event, read-model, type and response member that uses the concept, including collection elements and composite members. No per-property bytes are written, which keeps 0041's rule that properties carry no markers.

### Data subject

In v13 the data subject of a personal value is the event's event source, as 0008 item 1 defines it. Lineage through an explicit `subject` mark is not part of v13.

A v13 model is refused with `PLAY0268` when an event holds a `personal` value, or a value whose concept has `secret scope subject`, and the event also carries an explicit `subject` mark. Until Chronicle#4661 is settled no provider can render `[Subject]` correctly, so refusing fails closed instead of encrypting or erasing under the wrong key. This stays in force until a lineage version replaces it.

### Strictness

- `PLAY0515` stays. The model validator also refuses compliance on a concept used as an event source or stream identity, matching Chronicle CHR0034 and CHR0052.
- Compliance on an enum concept is refused in v13. Loosening a refusal later is cheap; tightening is not.
- The strict reader refuses: `compliance` in a model below v13, an unknown special category, `secret.scope` on a concept that is personal without a secret, compliance on an identity or enum concept, and the personal or subject-scoped secret together with explicit `subject` case above.

### Reference evaluator

Compliance changes no outcome. The reference model has no storage, encryption or causation. A model with markers still runs its specifications and is not counted as typed-unsupported. Concept rules keep their command-validation meaning.

### Test vectors

- `full-esm-v13.json`: the v12 vector plus `pii special health`, `pii criminal`, `secret scope namespace` and `pii secret`.
- `ConceptComplianceCorpus.V13`: single-file and folder forms that pin bytes and revision.
- Specifications show that unmarked models keep identical bytes, that outcomes are identical with and without markers, and that each strict-reader refusal above fires.

### Consumers

These adapt after admission, each tracked in its own repository.

- **Stage** (must first admit v11 and v12, because v13 includes them) renders `[PII]`, `[Encrypted]` and `[NotAudited]` on the `ConceptAs` record from `SemanticConcept.Compliance`, through the existing `ConceptComplianceRendering`. `[ComplianceDetails]` stays opt-in. A STAGE-ESM refusal guards compliance on an identity concept.
- **Studio** ([Cratis/StudioIssues#608](https://github.com/Cratis/StudioIssues/issues/608)) removes its `IsPii` refusal, emits bare `pii` from its boolean flag and bumps the pinned Screenplay and Stage packages. It stores only a boolean, so it emits no reasons, special categories or secret scopes.

### Amendment to 0047

0047 planned one phase 2 for lineage and `pii`. This record splits it: **2a** is this record (compliance, default event-source subject) and **2b** is subject lineage in a later version, after Chronicle#4661. A dated note in 0047 records the split and preserves its text.

## Options considered

- **Optional `compliance` on the concept, v13 compliance only (chosen).** It follows Chronicle's runtime meaning (0001), leaves every other model's bytes unchanged and keeps lineage a separate, later addition.
- **Report-only compliance through a side input, with no new version.** Rejected. Compliance changes realization behavior (encryption, erasure, causation), which gate 1 of [0004](0004-admission-and-governance-of-portable-executable-semantics.md) places in the model. Other model consumers would drop it silently, which breaks fail-closed admission and contradicts 0041 and 0047.
- **Compliance and subject lineage together in one version.** Rejected for now. It is the closest reading of 0047, but it is blocked on Chronicle#4661 and projection lineage work, and it would hold consumers back without more protection than the default event-source subject gives.
- **Studio strips compliance, or marks properties instead of concepts.** Rejected. Stripping loses protection silently and per-property markers are forbidden by 0041.
- **Store reasons in the model.** Rejected: free text has no executable meaning, and 0035 keeps reasoning report-only.

## Default if unanswered

Models with `pii` or `secret` concepts keep failing with `PLAY0268`. Studio and Stage cannot render compliance attributes from the executable model, and Studio keeps refusing to export applications with protected concepts.

## Timeline and scope

Proposed and accepted on 2026-10-12 by Sindre Alstad Wilting. It takes effect at admission against current `main`, with the version number rechecked at merge under 0025 and 0066.

In scope: the v13 model shape, binding, canonical writer and strict reader, validator refusals, reference-evaluator neutrality, vectors, MCP readiness text and documentation.

Out of scope: subject lineage (2b), read-model subject marks, reasons in the model, new rule kinds, `[ComplianceDetails]` defaults, erasure and export behavior, and Chronicle changes.

## Verification

**Done when:** a marked model binds, writes and reads as v13 with the shape above; unmarked models keep their bytes and revision; the refusals fire in the binder, validator and reader; and `interoperability.md`, `concepts.md` and `diagnostics.md` describe v13.

**Verify by:** the `full-esm-v13.json` vector, `ConceptComplianceCorpus.V13`, binder and reader specifications for each refusal, and a byte comparison of unmarked models against v12.

## Consequences

Studio and Stage can realize concept compliance from the executable model. A consumer that does not support v13 refuses marked models instead of dropping the markers. Models that combine a protected value with an explicit `subject` mark stay unexecutable until lineage is admitted.

## Related

Builds on 0004, 0008, 0025, 0034, 0035, 0041, 0047 and 0066. Screenplay [#141](https://github.com/Cratis/Screenplay/issues/141), Studio [StudioIssues#608](https://github.com/Cratis/StudioIssues/issues/608), Chronicle [#4661](https://github.com/Cratis/Chronicle/issues/4661).
