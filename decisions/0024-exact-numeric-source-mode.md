---
id: 0024
title: Exact numeric source mode and its ESM admission
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Printing/**
  - Source/DotNET/Screenplay/Files/**
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Source/DotNET/Screenplay.Mcp/**
  - Documentation/screenplay/**
---

## Context

[#285](https://github.com/Cratis/Screenplay/issues/285) concerns numeric precision lost during source parsing. The retained, unpublished experiment changed previously accepted models: `100000000000000020` and `100000000000000016` compare equal after Double rounding but differ mathematically; small fractions can round to integers. Replacing Double everywhere would change acceptance, execution and persisted projection definitions.

Accepted [0023](0023-command-production-model.md) requires explicit numeric mode independently of feature version. It preserves legacy behavior and requires declaration-bearing files to agree. Proposed [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) proposes reallocating ESM numbering: exact numbers leave the fixed sequence and take the next version number at a serialized release-ready admission checkpoint, once this record is accepted and Phase B is ready. That version is called the "admitting version" below. This record's numbering text depends on 0025's acceptance. Neither 0023's current allocation nor 0025 settles this proposal's spelling, exact range or transport representation. Accepted [0004](0004-admission-and-governance-of-portable-executable-semantics.md) governs semantic admission. This record is proposed on October 4, 2026; it records no new acceptance by Einar Ingebrigtsen or Sindre Alstad Wilting.

## Decision

The proposed contract selects exact numeric literal ingestion, representation and comparison with a single top-level `numbers exact` preamble, before domain, imports and declarations. Absence retains Legacy behavior, independently of cumulative language/ESM version. Exact literals use the normalized mathematical Decimal domain, immutable lossless syntax values, canonical fixed-point text and an explicit typed SyntaxJSON envelope. No arithmetic, coercion or persisted definition is silently migrated. Phase A shipped in [#381](https://github.com/Cratis/Screenplay/pull/381): opt-in exact source parsing, syntax transport, C# printing and workspace authoring are available. Exact documents remain semantically unsupported and fail binding with PLAY0268 until this record is accepted and an ESM version admits exact mode under 0004. The earlier unversioned replacement experiment remains unpublished.

### Source and physical documents

Leading blank lines, comments and an initial BOM may precede the preamble. Duplicate, unknown and late numeric directives are errors. Only `numbers exact` is a numeric-mode preamble; there is no explicit Legacy preamble (`numbers legacy` is an error) and no language-version preamble. In Exact documents, nested standalone `numbers`, `numbers exact` and `numbers legacy` lines are errors unless claimed as fields by their owning grammar; a property named `numbers` remains a property in its owning grammar. Recognized fenced bodies are not directives. Legacy nested-line handling remains unchanged.

All application, projection, specification, capture, placement and import-discovery entry points establish immutable source options before reading numbers. Options survive typed edits, restoration, extraction, complete printing and folder expansion. Fragment printing takes the owning mode and never inserts a preamble inside a declaration.

Each declaration-bearing physical document independently selects a consistent mode. Real authored module/feature declarations count; synthetic placement wrappers do not. An unmarked import-only barrel is neutral; a marked barrel asserts consistency without marking its children. Neither paths, imports, ordering, inferred versions nor hashes select a mode. Contract imports keep their existing meaning; unresolved shapes are not guessed.

### Exact literal domain

Scalar tokens follow the complete ASCII grammar `-?[0-9]+(\.[0-9]+)?([eE][+-]?[0-9]+)?`. Structured literals keep strict JSON grammar. No leading plus, hex, separators, suffixes or special values are admitted as numeric literals; owning grammars may keep such spellings as opaque operands. Conditions and policies consume exponents only as complete operands; `1e20foo` and `1e3-4` remain opaque under their existing grammars.

After removing insignificant zeros, the unsigned coefficient is at most `79228162514264337593543950335` and scale is 0–28. Int64 may be a storage optimization, never a separate identity or whole-number language limit. `9007199254740993`, integers above Int64 maximum, `1e-28` and representable trailing-zero/exponent forms are admitted. `1e-29`, `1e29`, `1e308`, maximum-plus-one and unrepresentable fractions are refused. Successful `Decimal.TryParse` is not proof of exactness. A recognized complete number never falls back to Double or successful opaque syntax after refusal.

Scanning is bounded by input length. Normalize digit counts and exponents before constructing a coefficient; never allocate a power based on an authored exponent. Canonical text is fixed-point, without exponents or insignificant fractional zeros; all zero spellings normalize to `0`. Equality, ordering and integrality use mathematical facts, not boxed numeric types or scale.

### Syntax and transport

`LiteralExpressionSyntax.Value` remains an object. New exact values carry an immutable `ExactNumber` tag and canonical text; TypeScript represents them as a discriminated canonical-string value, never `Number`. Source structured values are read recursively from original numeric tokens before floating-point conversion. Objects merely containing `literalType` and `value` remain business payloads.

A typed literal slot encodes an exact value as `{"literalType":"ExactNumber","value":"9007199254740993"}`. The decoder requires canonical, bounded text. Ordinary JSON numbers in literal-value slots retain Double interpretation and are refused in Exact source trees rather than silently cast. Structural numeric members retain their declared integer contracts. Existing programmatic Int32/Int64/Decimal/Single envelopes retain their Legacy contracts and require deliberate exact conversion for Exact authoring. Exact tags do not implicitly opt Legacy roots in.

Source-bearing Application, Projection, Capture and Specification nodes carry `sourceOptions`. Missing options default independently to Legacy, including nested source roots; they never inherit during restoration. Writers omit Legacy options, freezing old SyntaxJSON bytes, and emit `{"numericMode":"exact"}` for Exact. Explicit null, unknown modes, additional option members and conflicting nested modes are refused, as are incompatible programmatic values at restoration, printing, editing and binding boundaries. The TypeScript strict reader accepts complete Exact source roots only; it is not a general-purpose Legacy decoder. Existing public constructors and method signatures remain; new capabilities use init properties or additive overloads.

### Later semantic admission

Phase A adds no ESM numeric-mode field, ESM version or semantic numeric variant (it does add the SyntaxJSON `sourceOptions` field). All Exact roots, including declaration-only programmatic roots, fail binding with a specific unsupported diagnostic. Existing runtime behavior of released versions and unsupported constructs remain unchanged.

Phase B records mode independently of version: versions before the admitting version omit `numericMode` and their strict readers reject its presence; the admitting version and later require exactly one canonical `legacy` or `exact` root field, participating in revision computation. An Exact document selects at least the admitting version even without numeric literals. The admitting version's admission adds exact-mode vectors for every feature version admitted before it. Unchanged Legacy documents retain their existing version, bytes and revision. Later features select their own version without selecting Exact mode. The existing Decimal semantic-number value remains sufficient. Strict readers, mode-aware binding/reference behavior and canonical/source-backed vectors precede admission.

Exact literals do not promise arbitrary-precision arithmetic. Existing projection arithmetic, coercion and rounding rules remain under 0023. Exact capture guards reuse their restricted grammar with bounded complete-operand numeric parsing; their Legacy path remains unchanged. That exact parsing replaces the current `decimal.TryParse` in `SemanticCaptureExpression.cs` and is Phase B work. Unsupported computations do not become raw replacements that report success.

## Options considered

- **Explicit mode and bounded Decimal (proposed).** It fits the existing semantic domain, makes compatibility intentional and refuses numbers it cannot preserve.
- **Replace all Double parsing.** Rejected: the unpublished experiment demonstrates changed outcomes and stored text for old models.
- **Infer mode from cumulative version, comment pragma or loader setting.** Rejected: later features or external state would silently change source meaning.
- **Arbitrary BigDecimal.** Not taken: it changes arithmetic, target coercion, storage and consumer APIs beyond literal ingestion. A documented refusal is safer than an approximation.
- **Decimal.TryParse with Double or raw fallback.** Rejected: rounding, underflow and fallback hide loss after recognizing a number.
- **Plain JSON numbers as lossless transport.** Rejected: JavaScript rounds before wrapping. Exact authoring requires explicit typed slots; payload objects are not guessed to be tags.
- **Admit partial exact syntax as Legacy semantics.** Rejected: source fidelity is not runtime admission. Unsupported binding is mandatory until Phase B.

## Default if unanswered

Legacy remains the default. Existing source, programmatic numbers, SyntaxJSON, ESM bytes, revisions, arithmetic and persisted projection definitions retain their behavior. The shipped source/syntax support remains available; executable exact-number models remain blocked. The cost is delayed lossless executable models, not automatic migration or silent rounding of opted-in literals.

## Timeline and scope

The proposal applies to #285 through delivery of the admitting version, until superseded. Detailed acceptance is required before Phase B semantic admission; the shipped Phase A support does not claim acceptance or complete #285.

In scope: source preamble/options, bounded literal codecs, mathematical facts, all parser entry points, recursive structured values, typed syntax transport, mode provenance, physical-file consensus, canonical source printing, diagnostics and conformance proofs.

Phase A includes shared-compiler support and initial preamble highlighting. Phase B must deliver numeric-mode completion/hover, exponent highlighting, and numeric diagnostics (PLAY0508–0513) in the Monaco and VS Code editors across composite, standalone and placed documents.

Until a versioned recursive lossless fixture-value contract is delivered, MCP fixture queries encountering ExactNumber values, including nested values, fail explicitly (`ExactNumberFixtureTransportUnsupported`). The new contract must distinguish exact numeric leaves from business objects, preserve canonical strings recursively, and retain Legacy-client compatibility.

Out of scope for Phase A: ESM numeric-mode fields/readers/revisions/runtime admission and the lossless MCP value-tree DTO. These remain release obligations, together with explicit Stage, CLI, Studio, Generation, Arc and Chronicle admission/tracking under 0004. Consumer tracking with explicit rejection satisfies the release obligation until each consumer admits the admitting version; 0004 rejects lockstep releases. Dependency bumps do not prove admission. BigDecimal, general arithmetic, #319 flags and automatic persisted-projection migration are out of scope for this proposal.

## Open questions for the decider

- MCP lossless fixture DTO shape and compatibility strategy. Proposed default: delegated to implementation under review, versioned and additive.
- Whether lossless MCP and editor delivery gate the admitting version's release. Proposed default: they do not gate its semantic admission; both keep explicit refusal/unsupported behavior until delivered and are tracked under #285.

## Verification

**Done when:** Exact source, recursive syntax transport, printing and reparsing preserve canonical mathematical values in file and folder forms; all entry points and programmatic boundaries preserve and validate mode; complete unrepresentable numbers are refused; Exact binding is specifically unsupported before the admitting version; the admitting version records mode independently of feature version with strict readers, canonical revisions and source-backed outcomes; all Legacy behavior and bytes remain unchanged, and consumers explicitly admit or reject the version.

**Verify by:** Shared C#/TypeScript boundary and huge-exponent vectors; scalar, structured, condition/policy and culture tests; source → SyntaxJSON → decode → print → reparse comparisons of canonical strings; import/placement/barrel/folder consensus and expansion tests; incompatible-value and malformed-mode rejection tests; retained differential Legacy cases and old AST/ESM goldens; admitting-version strict-reader/version-mode/revision/golden/corpus tests; recursive MCP/editor and explicit consumer-admission evidence; native Debug/Release, supported-runtime, package API and frontend gates. Unsupported or unexecuted behavior never counts as passing.

## Consequences

Opt-in exact models can preserve large integers and small decimal literals without changing old models. Refusing values outside Decimal's exact domain is an intentional limitation. The provenance and typed transport contract add work across parsers, printers and authoring tools; shipping requires more than successful source parsing. Migration requires explicit intent and preview, never rewriting stored projections automatically.
