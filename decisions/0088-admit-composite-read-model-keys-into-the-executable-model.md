---
id: 0088
title: Admit composite read-model keys into the executable model as flat key parts
status: accepted
stage: none
decided: 2026-10-16
decider: Sindre Wilting (delegated to the L0 coordinator)
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Parsing/**
  - Source/DotNET/Screenplay/Printing/**
  - Source/DotNET/Screenplay/Indexing/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay/Diagnostics/**
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

[0060](0060-declare-read-model-keys-and-bind-lookups-by-name.md) item 9 left executable admission of composite keys to
#599. On main the binder refuses, all with PLAY0268: a read model with two or more `key` marks
(`SemanticModelBinder.ReadModels.cs:41`), a query `by` block (`:100`), and `given readmodel` or `then no readmodel`
over such a model (`SemanticModelBinder.Specifications.cs:323,356`). An untyped projection `key` block parses, then
fails with PLAY0273 ("composite key type '' not found", `SemanticModelBinder.ProjectionValues.cs`). The validator
requires exactly one identifier per read model (`SemanticModelValidator.Types.cs:102-110`).

KeldaKraft needs this now: `BearingSpectra` (`sensorId, axis, tier`) and `ExploredSeries` (`plantId, measurement,
field`) are marked "authoring-only (PLAY0268, #599)". Neither has a projection; both are read through equality-filter
list queries and `given readmodel`. A production model in Chronicle#4265 needs the event-projected case.

## Decision

1. **Key model.** A composite instance key is an unordered set of named, typed parts. Each part is one top-level
   read-model property. Equality is per part. The key has no separator, storage encoding or type name. Below, `K` is a
   read model with two or more `key` marks.
2. **Admitted forms.** Codes are for the refusals of that form; each is the code the cited site reports.

   | Position | Admitted | Refused |
   | --- | --- | --- |
   | Declaration | `K`, parts required, scalar, non-collection (0060 item 2) | Invalid marks: PLAY0660 (source, as today) |
   | Projection | Root `from` blocks with an untyped `key` block naming every part once (item 3) | See item 3 |
   | Query, list | `K[]` with equality `filter`s. `K[]` with one `by <part> Type` line binds as today (`argument` and `keyProperty`) | `K[]` with a `by` block: PLAY0268 (drafter's call; use filters) |
   | Query, single | Optional `K` with a `by` block naming every part, caller-supplied | Missing part: PLAY0661. Unknown, optional, collection or ill-typed part: PLAY0662 (both source, C#). Part with `from` source, or non-optional return: PLAY0268, as today |
   | `given readmodel`, `then readmodel` | Every part stated | Missing part: PLAY0351. Repeated part: PLAY0519 (parser, as today) |
   | `then no readmodel` | An object naming every part | Missing: PLAY0661. Unknown part: PLAY0662. Repeated member: PLAY0294 (parser, as today) |
   | `when query`, `then query … arguments` | Single: every by part. List: the filters. Same binder path for both | Missing, unknown or repeated argument: PLAY0468 |
   | `result` rows of a query | Single by-block query: omitted parts are taken from the arguments, as for a single key. List queries: every part stated | Missing part: PLAY0351 |

   A row whose stated part disagrees with the argument fails to match at run time; it is not an error.
3. **Projection blocks on `K`.** A parts key occurs only in the scoped shape; `IsFlat` (`Projections.cs:76-82`) never
   admits it, so flat transitions never carry one.

   | Block | Outcome |
   | --- | --- |
   | `from A, B` with a `key` block, parts written as `part = expr` | Admitted. The block key applies to each event, as a typed composite key does |
   | `from` with no key, `key <expr>`, or an inline `from A key x` | PLAY0661 (new; source, C#) |
   | Unknown, repeated or ill-typed part, non-simple expression | PLAY0662 (new; source, C#) |
   | `remove with E` carrying an untyped `key` block, `part = expr` per part | Admitted (new syntax, below). Same checks as `from`: PLAY0661 for a missing part, PLAY0662 otherwise |
   | `remove with E` with no key, `key <expr>`, or `parent` | PLAY0661 (new; source, C#) |
   | `remove via join`, `join`, `children` | PLAY0268 (new refusal in `BindScope`): join-based, and children and joins keyed by a composite parent wait for Chronicle#4768 and #551 |
   | `every`, `all`, `nested`, `clear with`, `variant` | PLAY0268 (new refusal in `BindScope`): no consumer needs them on `K`, and each can be admitted later (Options) |
   | A reducer over `K` | PLAY0268 (new refusal in `BindReducers`; main has no key-shape check there) |

   **Removal syntax.** `RemoveWithSyntax.Key` is one optional expression (`ProjectionBlocks.cs:129-141`). It gains the
   parts form beside it (the same untyped `CompositeKeySyntax` that `from` uses); the parser (`ProjectionParser.cs`,
   which today accepts only a `parent` child) and the printer take a `key` child block. The ESM carries it as a
   removal key of kind `parts`, written like `from`'s. The evaluator already resolves a root removal through
   `Key(removal.Key)` (`SemanticScopedProjection.cs:226`); it marks the document whose key equals the resolved parts,
   per part under `AreEqual`, as removed. A later `from` can recreate it.

   Part values reach state like a single identifier (`SemanticScopedProjection.cs:295-304`): when an instance is
   created, each resolved part is written to its property. Mappings and auto-map may also set a part property. After
   folding, `Complete` requires every part property to equal its key part under `AreEqual`; otherwise the projection
   fails with the existing "key disagrees with identifier" message, now naming the part. The projection-key checks
   run in the C# source validator only (`ReadModelKeyValidator`), as 0060 item 6 does for lookups. The strict
   validator rejects, in `from` and in `remove with`, a `parts` key with a duplicate part, a property that is not a key
   part, or any key property left unnamed.
4. **Refusals that stay as main reports them.**

   | Form | Code today |
   | --- | --- |
   | Command `reads … by` parts, `command reads protected` | PLAY0781 reason `ByParts`; PLAY0782 first when the command has no identifier (a by-parts read never supplies a scalar `by`); PLAY0273 when the key is not an input property |
   | Command `reads` with no preamble | PLAY0271 |
   | Trigger `reads`, reaction produces directly or has code | PLAY0268 |
   | Trigger `reads`, reaction only invokes commands | PLAY0270 (information), including a read by parts |
   | Screen, form or navigation scalar lookup of `K` | PLAY0661 (source); the binder defers screens (PLAY0269) |
   | Exact numeric source mode | PLAY0268 naming #285 |

5. **Both shapes.** An application may hold read models of both shapes: a single `key` property of composite type
   and `K` with flat parts. One read model has one shape; mixing is PLAY0660 as today.
6. **Concept rules.** Today concept rules run on query `by` arguments and filter values at execution (0072,
   `SemanticQueryRun.cs`). This record adds exactly one position: each `by` part of a single-instance query is
   validated against its concept like the single argument, with the same Validation outcome. Stays as it is for a
   single key: fixture rows and projected part values get type, shape and enum checks only; specification query
   arguments stay shape-only at binding so a Validation rejection can be specified; read-model state is not
   concept-validated (0072 excludes it).
7. **Equality.** `SemanticValueRules.AreEqual` is applied per part, in: instance identity in the world, projection
   instance lookup, query matching, `given` and `then` fixtures, and `then no readmodel`. A query matches an instance
   when every key part and every supplied filter is equal. Uuid parts compare as ordinal text, as single uuid keys do,
   with no case folding. Nothing else changes.
8. **Canonical form.** No new value variant. One ordering applies: every list here is ordered by semantic id, ordinal.
   Authored and display order is not written.
   - Read-model `properties` stay sorted by id (`SemanticModelCanonicalJson.cs:393-398`); only `identifier: true` is
     set on several of them.
   - A single-instance by-block query writes `keyParts: [{id, name, type, keyProperty}]` sorted by `keyProperty`. It
     sits where `argument` and `keyProperty` sit, after `readModel` and before `filters`, and replaces both. Each `id`
     is `SemanticAddress.ForQueryArgument(query, partName)`.
   - A projection key is `{kind: "parts", parts: [{property, value}]}`, sorted by `property`, the read-model key
     property id. It has no `type`, unlike `composite`.
   - A request, fixture or absence key is the existing composite value, already sorted by target property id.

   These are valid only at the new version. Earlier readers and validators reject them: they require exactly one
   identifier and know neither member. The golden file pins the bytes.
9. **Version.** The version is selected only when a read model has two or more key parts. Every other model keeps its
   bytes and version, including one with a single `key` mark (0060 item 8). v25 holds protected command reads (0085).
   The working number is v26 or v27, confirmed at the admission checkpoint under 0025 and 0066 against current
   `main`. The coordinator, L13, owns allocation; a branch holds no claim.
10. **Render-target neutrality.** The ESM carries named, typed parts only. Chronicle's typed key, a Marten identity or a
    SQL column set is each renderer's job. `streamIdParts` (0033) is a naming precedent only. A target that cannot look
    up one instance by parts refuses the shape. Stage refuses a single-instance query by parts until Chronicle#4265
    lands; list filters do not need it.
11. **KeldaKraft.** Checked by reading `origin/main` (5807405) against the binder; the binder was not run.
    `BearingSpectra` (a Uuid concept, two enum concepts) and `ExploredSeries` (`PlantId`, two `String` parts, an
    optional filter) use scalar parts, equality filters and `given readmodel`, so they bind without source changes. The
    "does not bind" prose in `Telemetry.play` and the slice docs is KeldaKraft's to remove. The whole model still needs
    `command reads protected` (0085) and date ordering (0087) to bind.

## Options considered

- **Projection key kind `parts` (chosen).** It carries no authored type, so nothing is invented.
- **A composite type per flat key, or a string key by convention (#459).** Rejected: invented types, and a hidden identity.
- **Split into two versions** (identity, fixtures and filters now; projections and queries by parts later). Rejected:
  it spends two versions and leaves the Chronicle case unbound.
- **Admit `every`, `all`, `nested`, `clear with`, `variant` and `remove via join` on `K` now.** Rejected: no consumer
  needs them, and they need their own checks and evaluator vectors. The cost is honest: each later admission is
  additive but takes its own ESM version, because older readers reject the new blocks on `K`.

## Default if unanswered

Composite read models stay PLAY0268, and KeldaKraft's two read models stay authoring-only.

## Verification

**Done when** a `K` model binds at the new version, every other model keeps its bytes, each form in items 2 to 4
reports its code, and earlier readers reject the new members. **Verify by:**
- Binder and strict-reader specs for every row of items 2 to 4. In particular: a reducer over `K`; `children`, `join`,
  `every`, `all`, `nested`, `clear with`, `variant` and `remove via join` into `K`; `remove with` with no key, with
  `key <expr>`, with a missing, unknown or ill-typed part; an invoking trigger reading by parts (PLAY0270); a command read by
  parts with and without an identifier (PLAY0781, PLAY0782); a `K[]` with a `by` block and with one `by` line.
- Specification specs: omitted parts, a repeated part (PLAY0519) and a repeated `then no readmodel` member (PLAY0294), result rows of single and list queries, `then query … arguments`
  with missing, extra and repeated arguments, and a `then no readmodel` with each failure.
- Parser, printer and TypeScript round-trip specs for the `remove with` `key` block. Strict-validator specs for an
  incomplete `parts` key in `from` and in `remove with`.
- Evaluator specs: removal of only the instance whose parts all match, then recreation by a later `from`; same first part with different second parts; reordered parts; a repeated fixture key; a filter on
  one part; a by-parts query; a projection with auto-map disabled, one with differently named event sources, and one
  with a mapping that contradicts a key part. Negative query specs: an invalid `by` part rejects with Validation at
  execution, and the same value as a specification argument still binds.
- Canonical vectors: a golden file and a source-backed corpus vector at the new version, in Double mode. An
  exact-mode control keeps the PLAY0268 refusal (#285) for the same source; no exact-mode vector can succeed. Legacy
  controls keep single-key and single-`key` bytes. A vector that reorders the key-marked declarations and the by-block
  parts gives identical bytes. A control shows v≤25 readers rejecting several identifiers, `keyParts` and kind `parts`.
- Samples: `Samples/Invoicing` already has `InvoicePaymentMethodTotals`; add a projection and a specification for it.
  In `when_binding_the_invoicing_sample`, remove only the #599 PLAY0268 pin (`:38-40`); keep the PLAY0781 `ByParts`
  pin (`:52-54`). Update the README line that says composite keys are authoring-only, the Monaco copy and the
  TypeScript conformance golden, per `.cratis/ai/rules/project/samples.md`.
- Surfaces: Workspaces (`WorkspaceAbsenceKeyBindings.cs` refuses composite keys with a #599 reason;
  `WorkspacePropertyLinks.Keys.cs`); Indexing (`AuthoringReadiness.cs:99`); the printer (`key {composite.Type}` prints a
  trailing space for the untyped form); MCP (`McpFixtureTypes.cs`, declaration details); the TypeScript parser round
  trip of the untyped block; `screenplay-contract.json` (retire the #599 entries); `interoperability.md`; diagnostics
  documentation for PLAY0661 and PLAY0662 on projection keys.
- KeldaKraft: run the binder over both read models once the version is admitted.
- A `Decision: 0004, 0025, 0060, 0066, 0088` trailer on the commit.

## Related issues

Screenplay #599, #459. Chronicle [#4265](https://github.com/Cratis/Chronicle/issues/4265), [#4768](https://github.com/Cratis/Chronicle/issues/4768), [#551](https://github.com/Cratis/Chronicle/issues/551). AI #580.
Follow-ups filed with the implementation under 0004: Stage (admit the version, render the
typed key), Studio (key editing), AI corpus (drop "authoring-only"). Decisions: 0004, 0025, 0033, 0060, 0066, 0071, 0072,
0085, 0087.
