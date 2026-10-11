---
id: 0065
title: Give enum members optional explicit numbers, all or none, and carry them in the claimed unreleased ESM v10
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Syntax/**
  - Source/DotNET/Screenplay/Printing/**
  - Source/DotNET/Screenplay/Semantics/**
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

Every application that stores the enum does store a number, though. Fundamentals `EnumJson.WriteEnum` writes the member's numeric value. Chronicle's client schema generator emits `enum` from `Enum.GetValuesAsUnderlyingType(type).Cast<int>()` with `x-enumNames`, and Arc keeps the integer through to its TypeScript proxies. The stored number is therefore the member's identity, and that is what makes renaming a member safe. Stage renders bare members (`ConceptRenderer.RenderEnum`), so C# numbers them by position from 0. Reordering, inserting or removing a member renumbers stored data when the application is rendered again. Chronicle's schema check accepts this silently, because the old numbers remain a subset of the new ones. A model extracted from code with sparse values (`Draft = 1, Issued = 4`) cannot keep them. [Screenplay#643](https://github.com/Cratis/Screenplay/issues/643) and [Stage#296](https://github.com/Cratis/Stage/issues/296) track this. Stage cannot fix it alone: the model must own member identity.

Today a member line such as `draft = 1` is refused with an invalid-enumeration-value error, so no valid model changes meaning. ESM v9 was released in Screenplay 4.117.0. [0043](0043-reaction-identity-runs-as.md) claims the single unreleased ESM v10, which [0063](0063-admit-production-routes-and-observer-filters-as-esm-v10.md) and proposed [0064](0064-exact-number-literals-join-esm-v10.md) join. [0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md) permits only one unreleased entry.

## Decision

An enum member line may end in `= <number>`, for example:

```screenplay
concept InvoiceStatus : Enum
  draft = 1
  issued = 4
  @validate = 7
```

The number is an optional `-` followed by decimal digits, with no fraction, exponent, `+` sign or leading zero. Its value must lie in the int32 range, because Chronicle casts enum values to `int`. Numbers are unique within the enum; gaps and negative numbers are allowed. Numbering is all or none per enum: once one member carries a number, every member must. Mixing them is a compile error, and there is no C#-style continuation from the previous member. An enum without numbers keeps its positional meaning, the zero-based declaration index, so its source, syntax tree, canonical bytes and revision are unchanged. The `@` escape, descriptions and `validate` blocks keep working.

Members are still named everywhere else. Specifications, fixtures, validation operands and conditions name members as they do today, a number never names a member, and the evaluator compares by name, so reference outcomes do not change. The printer writes `name = N` exactly as authored, including numbers that happen to equal their position, so explicit pinning round-trips. Both compilers carry the numbers on the concept syntax node: in C# as an init property, `ConceptSyntax.ValueNumbers`, not as a positional parameter, together with an optional member in the AST/MCP JSON. A line repair, "Number every member by its current position", pins a positional enum before an author reorders it. Monaco and the VS Code grammar tokenize the number with the existing numeric scope.

`SemanticConcept` gains an optional `numbers` array of int32, emitted as the concept's last canonical member and parallel to `values`. It is emitted only when the numbers differ from `0..n-1`. A model that pins numbers equal to the positions therefore keeps its previous version and bytes, while any other explicit numbering selects language and semantic version 10.0 and schemaVersion 10 and counts for v10 activation-by-use. The strict reader refuses `numbers` below v10, on a concept without enum values, with a length different from `values`, with duplicates, with values outside int32, and when the numbers equal `0..n-1` (non-canonical). This decision admits the contract in Screenplay only; Stage and other consumers must admit it explicitly.

## Options considered

- **Names on the wire.** Rejected because Chronicle's event schema is `integer` and Arc keeps integers end to end.
- **Numbers derived from a hash of the name.** Rejected because renaming would change identity, which removes the very safety that numbers give.
- **Number pins kept by Stage.** Rejected because every renderer and extractor would keep its own table, and the model would no longer own its contract.
- **Explicit numbers required on every member of every enum.** Rejected because every existing model would break, and nothing is gained over positional defaults for enums that were never stored.
- **C#-style continuation (an unnumbered member takes the previous member's number plus one).** Rejected because inserting an unnumbered member renumbers everything after it, which is the hazard this decision removes. An extractor writes every number explicitly instead.
- **Aliases (duplicate numbers).** Rejected because a number would map to several names, which breaks `x-enumNames` and name-based specifications.
- **A `members: [{ name, number }]` list in the ESM.** Rejected because it either duplicates `values` or replaces it, changing bytes and readers for every enum. A parallel `numbers` array leaves `values` where every consumer reads it.
- **Detecting the reorder or removal of a published member in this change.** Deferred because it needs a baseline. It belongs to the structural model comparison of [0053](0053-public-structural-model-comparison.md) as a separate issue, not to single-model compilation.

## Default if unanswered

Enums stay positional. Any reorder, insert or removal silently renumbers stored data when the application is rendered again, extracted sparse values remain lost, and Stage#296 cannot be completed. v10 stays claimed for its other features.

## Timeline and scope

This decision requires human acceptance, and a recheck that v10 is still the only claimed, unreleased version, before merge. It must land before the v10 release checkpoint; afterwards it would need its own version. It holds until superseded.

In scope: syntax, diagnostics, the printer, the syntax tree and AST/MCP JSON, the C# and TypeScript compilers, the repair, editor highlighting, the ESM member, strict-reader rules, v10 selection, the canonical corpus, golden evidence and documentation.

Out of scope:

- [Flags] semantics. Under [#319](https://github.com/Cratis/Screenplay/issues/319), a flags marker would read these numbers as bit masks, may require explicit numbers on a flags enum and refuse negative numbers, and may allow combination-only members such as `all = 7`. Combination values and set equality remain #319's.
- Baseline diffing of published enums.
- Exact source mode.
- The consumers' own admission: Stage rendering `Draft = 1` and mapping stored numbers, the Arc and Screenplay.CritterStack generators emitting numbers, and Studio export and import.

## Verification

**Done when:** `draft = 1` / `issued = 4` compiles in both compilers, prints identically and round-trips through the AST/MCP JSON. Mixed numbering, duplicate numbers and values outside int32 each report their own diagnostic in both compilers. The positional-pin repair produces `= 0..n-1`. A sparse enum binds `numbers: [1, 4]` and selects v10. An enum pinned to its positions and an unnumbered enum keep their previous version, bytes and revision. Specifications naming members of a numbered enum have unchanged outcomes. The strict reader refuses `numbers` below v10, on non-enums, with a length mismatch, with duplicates, outside int32 and when it equals the positions. Released v1 to v9 goldens, corpus bytes, revisions and outcomes are unchanged.

**Verify by:** parser and printer specifications in both compilers, the repair specification, `for_SemanticModelBinder` concept binding specifications, `for_ExecutableSemanticModel` strict-reader refusals, a source-backed corpus vector and the extended `full-esm-v10.json`, the existing golden and corpus compatibility specifications, and the CI-equivalent local gates.

## Consequences

The model, not the renderer, owns stored member identity. An author can reorder members freely once they are numbered, and an extracted model keeps the numbers of the system it replaces. Positional enums keep their hazard until they are pinned, so the documentation carries a pitfall: never renumber, and never reuse the number of a published member. Consumers that render or extract enums must adopt `numbers` before they claim v10, and #319 gains its member-to-bit mapping without another syntax.

## Related

Builds on 0004, 0025, 0043, 0053, 0063 and proposed 0064. Related work: Screenplay#643, Screenplay#319 and Stage#296.
