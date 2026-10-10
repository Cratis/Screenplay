---
id: 0060
title: Declare read-model keys on properties and supply every key part by name in lookups
status: accepted
stage: implemented
decided: 2026-10-10
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Source/Screenplay/EventModels/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

[#459](https://github.com/Cratis/Screenplay/issues/459) needs views identified by several values, such as resource, reporting scope and period. Today queries and reads name one key. Read-model identity is inferred from query signatures. [0033](0033-composite-event-stream-ids.md) recommends named parts for read-model lookup alignment, while distinguishing Chronicle's object keys from string-encoded stream ids.

A `key a, b` declaration would overlap a legal property named `key` whose type is lowercase. A trailing modifier has no such overlap.

## Decision

1. A top-level read-model property declares a key part with the trailing `key` modifier. One or more marks replace identity inference. Without marks, existing inference is unchanged.
2. Every part is required and noncollection. `key` cannot combine with `optional`, `generated`, `identifier` or `subject`. A single key property may have a composite type. A multipart key has scalar parts. Declaration order is display order, not identity: Chronicle stores object keys.
3. The existing query `by name Type` form stays. A `by` header with at least two distinct `name Type [from source]` children declares a composite lookup. The forms are mutually exclusive.
4. A command or reaction trigger supplies named parts under a `reads View [as alias]` child `by` block, as `part = source`. Sources are required, noncollection property paths with compatible nominal types. Literals are refused. The existing single `reads View by value` form stays.
5. Resolve a lookup key from explicit marks first, otherwise from a unique single-instance query key shape. Unknown external identities stay undecided. Single-instance reads and queries supply every part exactly once. Collection queries may filter by a subset. Screen, form and navigation scalar lookups cannot supply a composite key and receive a missing-parts diagnostic. Read-model state fixtures state every part, and absence fixtures use an object with every named part.
6. PLAY0660 rejects invalid declarations and marks. PLAY0661 names missing lookup parts. PLAY0662 rejects invalid lookup shapes, parts or sources. PLAY0351 extends to every key part in state fixtures. PLAY0283 remains the scalar reads type mismatch. The parsers share PLAY0660 and shape-only PLAY0662 checks; scoped lookup checks are C# only.
7. PLAY0661 has a typed repair for command reads only, when each missing part has one same-named compatible source. It preserves authored mappings and refuses ambiguity. Queries and trigger reads are not repaired. PLAY0660 and PLAY0662 have no automatic repair. Read-model property rename remains explicitly unsupported until its whole mapping chain can be preserved.
8. A single explicit key binds into the existing identifier property. Its canonical ESM bytes match the equivalent inferred model. It is a byte-preserving extension under 0004, not a new ESM version.
9. Composite keys, by-block queries and fixtures using composite views fail executable binding with PLAY0268 naming [#599](https://github.com/Cratis/Screenplay/issues/599). The evaluator is unchanged. Composite instance identity, query arguments, fixture keys and projection alignment need their own admission contract.

## Options considered

- **`key a, b` line.** Not taken: it overlaps existing property syntax and relies on token-count or case discrimination.
- **`key` block.** Not taken: it separates each part's type from its declaration and adds name references to repair and rename.
- **Positional `by a, b`.** Not taken: reordering changes correspondence. Named mappings keep each supplied value attached to its target part.
- **One composite `type` as the key.** Retained for single-property object keys, but not required for views with several independently named key fields.
- **Composite ESM admission now.** Not taken: it changes instance-key equality, query requests, specifications and projection state contracts. #599 owns that admission separately.

## Default if unanswered

Identity remains inferred and every lookup has one scalar key. Composite-key views require an opaque external implementation. That hides their lookup contract from authoring checks.

## Timeline and scope

Holds until superseded. In scope: both parsers and ASTs, validation, explicit single-key binding, composite refusal, printer, typed repairs, MCP, reference handling, editors, board, documentation and samples in one pull request. Out of scope: composite ESM admission and execution, decision-consistent reads, event routing and stream-id encoding.

## Verification

**Done when:** named keys and complete lookups compile; invalid owners, shapes and sources receive PLAY0660 to PLAY0662; fixtures require all parts; deterministic repairs preserve comments; single explicit and inferred keys have identical canonical bytes; composite binding fails with PLAY0268 citing #599.

**Verify by:** run the read-model key parser, lookup, binder, printer and workspace/MCP repair specs, TypeScript conformance and diagnostic vectors, and the local CI gates. Review editor, board and sample coverage before merging the whole change.

## Related

[#459](https://github.com/Cratis/Screenplay/issues/459), [#599](https://github.com/Cratis/Screenplay/issues/599), decisions 0004, 0014 and 0033.
