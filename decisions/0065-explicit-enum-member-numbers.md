---
id: 0065
title: Give enum members optional explicit numbers, all or none, and carry them in a later ESM version
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Printing/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/EventModels/**
  - Samples/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/concepts.md
  - Documentation/screenplay/types.md
  - Documentation/screenplay/grammar.md
  - Documentation/screenplay/interoperability.md
  - Documentation/screenplay/diagnostics.md
---

## Context

An enum concept is an indented list of member names (`[ "@" ], LowerIdent` in `ConceptDecl`, grammar.md). `SemanticConcept.Values` holds the names in declaration order, and the executable model treats an enum as a text concept whose values the reference evaluator compares by name. No member carries a number.

Every application that stores the enum does store a number, though. Fundamentals `EnumJson.WriteEnum` writes the member's numeric value. Chronicle's client schema generator emits `enum` from `Enum.GetValuesAsUnderlyingType(type).Cast<int>()` with `x-enumNames`, and Arc keeps the integer through to its TypeScript proxies. The stored number is therefore the member's identity, and that is what makes renaming a member safe. Stage renders bare members (`ConceptRenderer.RenderEnum`), so C# numbers them by position from 0. Reordering an unnumbered enum, or inserting a member before existing ones, renumbers stored data when the application is rendered again. Chronicle's schema compatibility check rejects shrinking the enum value set, but it accepts a reorder or an insertion, because the old numbers remain a subset of the new ones. Appending or removing at the tail does not renumber the surviving members. A model extracted from code with sparse values (`Draft = 1, Issued = 4`) cannot keep them. [Screenplay#643](https://github.com/Cratis/Screenplay/issues/643) and [Stage#296](https://github.com/Cratis/Stage/issues/296) track this. Stage cannot fix it alone: the model must own member identity.

Today a member line such as `draft = 1` is refused with `PLAY0009` (invalid enumeration value), so no valid model changes meaning.

[0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) treats a published version, including a published prerelease package, as released: its number is never reused and its contract is not extended by claim. ESM v10 already ships in published packages (Screenplay v4.125.0 to v4.131.0). Whether v10 may still be extended is a pending ruling and is not assumed here. Under 0025 item 3, a feature without a claim names itself, not a number.

## Decision

An enum member line may end in `= <number>`, for example:

```screenplay
concept InvoiceStatus : Enum
  draft = 1
  issued = 4
  @validate = 7
```

The number is an optional `-` followed by decimal digits, with no fraction, exponent, `+` sign or leading zero. `-0` is the number 0 and prints canonically as `0`. Its value must lie in the int32 range, because Chronicle casts enum values to `int`. Numbers are unique within the enum; gaps and negative numbers are allowed. Numbering is all or none per enum: once one member carries a number, every member must. Mixing them is a compile error, and there is no C#-style continuation from the previous member. An enum without numbers keeps its positional meaning, the zero-based declaration index, so its source, syntax tree, canonical bytes and revision are unchanged. The `@` escape, descriptions and `validate` blocks keep working.

**Diagnostics.** Four new errors, each with its own code: mixed numbering, duplicate number, number outside int32, and malformed number literal. Codes are allocated above the highest existing code at implementation time (currently `PLAY0666` in `DiagnosticCodes.cs`), in C#, TypeScript and Monaco alike, and are permanent.

Members are still named everywhere else. Specifications, fixtures, validation operands and conditions name members as they do today, a number never names a member, and the evaluator compares by name, so reference outcomes do not change. The printer writes the canonical form `name = N`, so numbers equal to their positions are kept and round-trip, while `-0` prints as `0`. Exact source mode is out of scope. The TypeScript compiler has no printer; only the C# printer round-trips.

**Syntax tree and AST.** Both compilers carry the numbers on the concept syntax node: in C# as an init property, `ConceptSyntax.ValueNumbers`, not as a positional parameter. The AST distinguishes absent numbers from populated ones. When populated, the count equals the number of values. Numbers on a concept that is not an enum are rejected. The legacy syntax JSON omits the member when absent, so existing documents are byte-identical.

**Repair.** Positional enums are valid and carry no diagnostic, and workspace repair discovery (`WorkspaceDiagnosticRepairs`) is diagnostic-backed, so "Number every member by its current position" is not a repair. It is an opt-in typed refactoring proposal, offered only for a wholly unnumbered enum, that writes `= 0..n-1` and goes through the usual propose and apply contract. It never renumbers a numbered or mixed enum.

**ESM.** `SemanticConcept` gains an optional `numbers` array of int32, emitted as the concept's last canonical member and parallel to `values`. It is emitted only when the numbers differ from `0..n-1`. A model that pins numbers equal to the positions therefore keeps its previous version and bytes. Any other explicit numbering selects the version that admits enum member numbers and counts for activation-by-use. That version is not named here. Per 0025 item 3, the feature takes the next ESM version claimed at its admission checkpoint under 0025, and until then no supported ESM version admits it: binding refuses a numbered enum with the typed not-admitted outcome, and the documentation says so without a number. The strict reader refuses `numbers` below the admitting version, on a concept without enum values, with a length different from `values`, with duplicates, with values outside int32, and when the numbers equal `0..n-1` (non-canonical). Programmatic ESM validation enforces the same uniqueness, int32 range and cardinality rules as the strict reader. This decision admits the contract in Screenplay only; Stage and other consumers must admit it explicitly.

## Options considered

- **Names on the wire.** Rejected because Chronicle's event schema is `integer` and Arc keeps integers end to end.
- **Numbers derived from a hash of the name.** Rejected because renaming would change identity, which removes the very safety that numbers give.
- **Number pins kept by Stage.** Rejected because every renderer and extractor would keep its own table, and the model would no longer own its contract.
- **Explicit numbers required on every member of every enum.** Rejected because every existing model would break, and nothing is gained over positional defaults for enums that were never stored.
- **C#-style continuation (an unnumbered member takes the previous member's number plus one).** Rejected because inserting an unnumbered member renumbers everything after it, which is the hazard this decision removes. An extractor writes every number explicitly instead.
- **Aliases (duplicate numbers).** Rejected because a number would map to several names, which breaks `x-enumNames` and name-based specifications.
- **A `members: [{ name, number }]` list in the ESM.** Rejected because it either duplicates `values` or replaces it, changing bytes and readers for every enum. A parallel `numbers` array leaves `values` where every consumer reads it.
- **Extending published v10.** Not chosen here. A ruling is pending, and 0025 item 2 treats published versions as released.
- **A diagnostic-backed repair for positional enums.** Rejected because a positional enum is valid and has no diagnostic to attach it to.
- **Detecting the reorder or removal of a published member in this change.** Deferred because it needs a baseline. It belongs to the structural model comparison of [0053](0053-public-structural-model-comparison.md) as a separate issue, not to single-model compilation.

## Default if unanswered

Enums stay positional. Any reorder, or insertion before existing members, silently renumbers stored data when the application is rendered again, extracted sparse values remain lost, and Stage#296 cannot be completed.

## Timeline and scope

This decision requires human acceptance before merge. The ESM admission needs a release-ready checkpoint under 0025: the version is the next unused number at that time, claimed in the pull request that adds it to `Versions.cs` and the unreleased part of the version table, with a dated status note on this record. Until then the authoring surfaces ship and the executable model refuses the construct. It holds until superseded.

In scope, every surface in this repository (see the language-change surface rule):

- Both parsers: C# and TypeScript compiler parsing, and transport parity.
- Syntax tree, diagnostics (C#, TypeScript, Monaco) and `diagnostics.md`.
- The C# printer and its round-trip.
- The AST/MCP JSON schema and golden, the MCP authoring tools, repair and refactoring discovery with their documentation, and the typed refactoring above.
- The binder, the ESM member, the strict reader, programmatic validation and the typed not-admitted outcome.
- The VS Code TextMate grammar and Monaco: tokenization, its bundled sample (`samples/invoicing.play` holds enums), completion, hover and validation.
- Samples that use enums (Invoicing, Library, Commerce, TimeTracking), updated where a numbered enum teaches something.
- Canonical corpus, transport and conformance vectors, and feature-named golden and corpus vectors that take the version number at claim.
- Documentation: `grammar.md` `ConceptDecl`, `concepts.md` and `types.md` pages for enums (concept, when to number and when not, pitfalls), `interoperability.md` (the `numbers` member, not admitted yet), and the diagnostics reference.

Unaffected, checked in the repository: the reference evaluator (names only), the event model board and dependency graph (enum values do not appear), and the CLI tool in `Source/DotNET/Tool`, which has no enum-specific surface. Generation and Prologue are not in this repository; they are handled with the cross-repository issues below.

Out of scope:

- [Flags] semantics. Under [#319](https://github.com/Cratis/Screenplay/issues/319), a flags marker would read these numbers as bit masks, may require explicit numbers on a flags enum and refuse negative numbers, and may allow combination-only members such as `all = 7`. Combination values and set equality remain #319's.
- Baseline diffing of published enums.
- Exact source mode.
- The consumers' own admission: Stage rendering `Draft = 1` and mapping stored numbers, the Arc and Screenplay.CritterStack generators emitting numbers, and Studio export and import.

After acceptance, create issues in Stage, the Arc generator, CritterStack, Studio, `Cratis/AI` (the skill corpus) and the model-comparison breaking-change detection here (reordering an unnumbered enum, renumbering a member), and link them from the pull request.

## Verification

**Done when:** `draft = 1` / `issued = 4` compiles in both compilers, prints identically and round-trips through the AST/MCP JSON, and C# printer round-trips. Mixed numbering, duplicate numbers, values outside int32 and malformed literals each report their own diagnostic in both compilers, and `-0` prints as `0`. The numbering refactoring proposes `= 0..n-1` for a wholly unnumbered enum and nothing for a numbered or mixed one. Numbers on a non-enum are rejected. A sparse enum binds `numbers: [1, 4]` once admitted and is refused with the typed not-admitted outcome before. An enum pinned to its positions and an unnumbered enum keep their previous version, bytes and revision. Specifications naming members of a numbered enum have unchanged outcomes. The strict reader and programmatic validation refuse `numbers` below the admitting version, on non-enums, with a length mismatch, with duplicates, outside int32 and when it equals the positions. Released v1 to v10 goldens, corpus bytes, revisions and outcomes are unchanged, and the legacy syntax JSON is unchanged for documents without numbers.

**Verify by:** parser and printer specifications in both compilers, the refactoring specification, `for_SemanticModelBinder` concept binding specifications, `for_ExecutableSemanticModel` strict-reader and validation refusals, transport and conformance vectors, a source-backed corpus vector and a feature-named golden, the existing golden and corpus compatibility specifications, and the CI-equivalent local gates.

## Consequences

The model, not the renderer, owns stored member identity. An author can reorder members freely once they are numbered, and an extracted model keeps the numbers of the system it replaces. Positional enums keep their reorder and insert hazard until they are numbered, so the documentation carries a pitfall: never renumber, and never reuse the number of a published member. Consumers that render or extract enums must adopt `numbers` before they admit the version that carries it, and #319 gains its member-to-bit mapping without another syntax.

## Open questions

- Should a stored but unnumbered enum get a diagnostic or lint, given that positional enums are valid?
- A pinned enum whose numbers equal the positions and a positional enum are indistinguishable in the ESM. Is that acceptable for consumers that want to know whether the author pinned identity?
- Is int32 the right range, or should the model allow int64 for future consumers that store wider values?
- May the published ESM v10 still be extended with this feature, or must it take the next version? The ruling is pending; this record is written to hold under either answer.

## Related

Builds on 0004, 0025, 0043 and 0053. Related work: Screenplay#643, Screenplay#319 and Stage#296.
