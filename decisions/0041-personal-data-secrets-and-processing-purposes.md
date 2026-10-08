---
id: 0041
title: Mark personal data and secrets on concepts, and declare processing purposes once
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-08
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/**
  - Samples/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

Screenplay records personal-data and operational-secret markers with free-text reasons. GDPR attaches purpose, lawful basis (Art. 6), retention, recipients, transfers and erasure exceptions (Art. 17(3)) to processing, not to a data element (Art. 5(1)(b,c,e), 30(1)). Special categories (Art. 9(1)) and criminal-offence data (Art. 10) are facts about the value itself. To a GDPR reader, “sensitive” suggests special categories, not an API key.

This record amends the surface names and encryption-scope deferral of [0034](0034-sensitive-means-operational-secret.md), preserving its mapping and identity restrictions. [0008](0008-one-data-subject-per-event.md) continues to govern subject lineage. Report-only processing declarations follow the metadata approach described in [0035](0035-keep-model-reasoning-as-report-only-metadata.md).

## Decision

1. Markers are bare suffix keywords after a concept's primitive type. `pii` is canonical: personal data (GDPR Art. 4(1)); renders Chronicle `[PII]`. `personal` is an alias without a diagnostic; the printer writes `pii`. `secret` is canonical for an operational secret. `@pii`, `sensitive` and `@sensitive` remain accepted with one Information diagnostic per line and per-line/whole-document repair. Unknown markers are errors. AST wire names remain `pii` and `sensitive`; additive syntax members use binary-safe init properties.
2. Concept body lines are `pii reason "…"`, `pii special <category>`, `pii criminal`, `secret reason "…"` and `secret scope subject|namespace|global`. The `personal` alias works on body lines too. `sensitive reason` is deprecated. Reasons remain free-text notes; repairs never reinterpret or move legal text. There are no property/type-level markers.
3. Art. 9(1) categories are closed: `racialOrEthnicOrigin`, `politicalOpinions`, `religiousOrPhilosophicalBeliefs`, `tradeUnionMembership`, `genetic`, `biometric`, `health`, `sexLifeOrSexualOrientation`. At most one `special` line is allowed; `special` and `criminal` may coexist. Qualifiers without `pii`, unknown categories, `pii scope`, duplicate scopes and unknown scope values are errors. Explicit secret scope on `pii secret` warns because only `[PII]` renders. An omitted scope stays absent in syntax and printing, preserving Chronicle's Subject default.
4. Provider mapping: `pii` -> `[PII]` plus `[ComplianceDetails(qualifier prefix + reason)]`; `secret` -> `[Encrypted(scope, reason)]` plus `[NotAudited]`; both -> `[PII]` only. Composed qualifier text is deterministic and contains no processing purpose. Stage must first verify schema compatibility: Chronicle compares compliance/security details on registration. Newly emitted details must not silently break existing event generations; opt-in rendering or a Chronicle compatibility change is required if that precondition holds.
5. Processing purposes are declared once at top level and referenced on modules, features and slices. Coverage is the union down the tree, with accumulated references and collapsed duplicates in folder merges. Every purpose field is optional in parsing: description, basis and optional legal reference, condition and optional reference, authorization, interest, subjects, retention, recipients, transfer/safeguard and erasure exception. Purposes are report-only (`PLAY0270`), without ESM bytes or a new version. Compliance concept markers remain refused at binding (`PLAY0268`).
6. Purpose vocabularies are closed for Art. 6(1) basis (`consent`, `contract`, `legalObligation`, `vitalInterests`, `publicTask`, `legitimateInterests`), Art. 9(2) condition (`explicitConsent`, `employmentLaw`, `vitalInterests`, `notForProfit`, `madePublic`, `legalClaims`, `substantialPublicInterest`, `healthCare`, `publicHealth`, `research`), and Art. 17(3) erasure exception (`expression`, `legalObligation`, `publicTask`, `publicHealth`, `archiving`, `legalClaims`). Subjects are open identifiers; retention, recipients, transfers and safeguards are free text. Duplicate singleton fields and bad vocabulary values are errors; unknown references and misplaced/missing legitimate-interest statements warn.
7. Opt-in `--check purposes` warns on uncovered personal data, special data without a condition, criminal data without authorization, purposes without a basis, and unused purposes. Ordinary compilation does not run this check. Findings prompt investigation; they do not assess lawfulness.
8. An Art. 30 processing record exposes declared purpose fields plus derived concept categories, qualifiers, security measures and a DPIA prompt. It flags erasure exceptions conflicting with Chronicle's per-subject crypto-shredding. MCP `processing_record` and CLI `screenplay report processing --format json|markdown|csv` obtain controller contact details from tool input, not language syntax. Reports say: “Generated from declarations in this model. Not legal advice.”

## Options considered

- Keep `pii` (chosen) to align with Chronicle `[PII]`; accept `personal` as a readable alias rather than deprecating `pii`.
- Keep `sensitive` (rejected): invites confusion with Art. 9. `secret` describes the value, unlike `encrypted`, which names a mechanism personal data shares.
- Keep the `@` sigil (rejected): bare suffix markers align with other modifiers; `@` remains the identifier escape.
- Separate Art. 9/10 markers (rejected): these qualify personal data rather than changing its provider mapping.
- Put purpose/basis on concepts or inside `[ComplianceDetails]` (rejected): one value serves many purposes and schema details are compared on registration.
- Always-on purpose coverage (rejected): opt-in checks avoid flooding existing models and claiming compliance.
- Command-level purposes, data-category taxonomy, source derivation and structured retention (deferred): they need separate design and introduce reserved-word or enforcement costs.

## Default if unanswered

Reasons continue mixing purpose, basis and retention in prose, secrets cannot select a scope, and the model cannot yield a structured processing inventory. The existing protection mapping and binding refusal remain.

## Timeline and scope

The ruling holds until superseded. Phase 1 implements markers, aliases, deprecations/repairs, scope and qualifiers on every Screenplay language surface. Phases 2–3 follow: purpose declarations/checks, then the processing record. Stage and Chronicle adaptations are separate changes; this repository does not implement their runtime protection or retention. Later taxonomy, source derivation, command-level purposes and DSAR/export remain out of scope.

## Verification

**Done when** both compilers parse, diagnose, print and repair the canonical/legacy forms identically, unknown markers fail, qualifiers and scopes retain typed AST metadata, binding explicitly refuses compliance markers, samples use canonical warning-free forms, and every editor/documentation surface agrees. Later phases must expose report-only purposes and opt-in findings without changing executable bytes.

**Verify by** C# and TypeScript parser, printer, repair, diagnostic, admission and transport/conformance specifications; Monaco and VS Code highlighting/completion/hover/quick-fix specifications; documentation and sample compilation; repository CI gates. Stage rendering specifications for each qualifier/scope follow only after testing the schema-compatibility precondition.

## Consequences

Classification stays distinct from processing. Repairs preserve notes without guessing legal meaning. Changing encryption scope on concepts used in persisted events requires a new event generation. Purpose-level retention/erasure exceptions may conflict with per-subject crypto-shredding; the later report makes that conflict visible rather than promising enforcement.
