---
id: 0094
title: Report enum numeric identity changes in model comparison
status: accepted
stage: none
decided: 2026-10-18
decider: Sindre Wilting (delegated to the L0 coordinator)
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Comparison/**
  - Source/DotNET/Screenplay/Completeness/EnumerationPinning.cs
  - Source/DotNET/Screenplay/Completeness/StoredTypeNames.cs
  - Source/DotNET/Screenplay.Mcp/McpSemanticDiff.cs
  - Documentation/screenplay/model-comparison.md
  - Documentation/screenplay/mcp/reference.md
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

[Screenplay#667](https://github.com/Cratis/Screenplay/issues/667) completes the baseline comparison deferred by
[0065](0065-explicit-enum-member-numbers.md). Stored enum identity is its number, explicit or the zero-based
declaration position. Reordering or inserting positional members can reinterpret stored data without changing
an event's property type. [0053](0053-public-structural-model-comparison.md) compares enum values as opaque hashes;
its MCP JSON byte freeze and [0070](0070-derive-event-mutability-from-an-admitted-release-baseline.md)'s separate
release-baseline equality check must remain intact. Without classification, every consumer must interpret hashes
and duplicate numeric identity logic.

## Decision

Compare baseline members by ordinal name within an already matched enum concept. A surviving name with a different
effective number is `NumberChanged`. A missing name whose number has no candidate holder is `Removed`. When both
sides use explicit numbers, a missing name whose number belongs to a name absent from the baseline is `Renamed`;
otherwise a missing name whose number is held by another member is `NumberReused`.

L0 accepted all three choices on 2026-10-18:

1. **Explicit number retained under a new name is `Renamed`, not `ContractBreaking`.** Every default stored and wire
   path writes enum numbers; the stored identity is unchanged. The evidence below grounds this distinction.
2. **`ContractBreaking` depends on kind, not storage usage.** It is true for `NumberChanged`, `Removed` and
   `NumberReused`, false for `Renamed`. A separate `Stored` flag reports baseline reachability from event properties,
   including nested composite types, using the same walker as `EnumerationPinning` / `PLAY0671`. It does not prove
   that events were persisted.
3. **MCP enum records stay inside `members` with appended fields.** The library adds the init-only
   `ModelDifference.Enumerations` typed list. MCP emits `enum-number-changed`, `enum-member-removed`,
   `enum-number-reused` and `enum-member-renamed` records with the existing 25 fields in their original order,
   then `afterMember`, `beforeNumber`, `afterNumber`, `beforeNumbering`, `afterNumbering` and `stored`.
   Only these records carry the appended fields. No section is added, existing records keep their bytes,
   and the matching limit statement appears only when either side declares an enum.

### Matching and compatibility limits

- Identity mode matches the concept by catalog ID through renames and moves. Address mode requires its exact
  address: a renamed concept becomes removed plus added, without member comparison.
- A positional rename cannot be distinguished from removal plus number reuse and reports `NumberReused`.
  Explicit removal then reuse under a new name cannot be distinguished from a rename and reports `Renamed`.
- A rename keeps default number-based stored data compatible but renames the member in generated C#/TypeScript
  code (a source-level API change) and in Chronicle's `x-enumNames` schema metadata. Data written with an explicit
  name-based converter, rather than the default numeric converter, would also break. `Renamed` is not
  `ContractBreaking` for the default number-based storage, not a general source or serialization compatibility verdict.
- Appending, reordering with stable explicit pins and pinning positions do not change baseline numeric identities.
  Authored member hashes still report structural changes. Enum/non-enum transitions remain ordinary `type` changes.
- Duplicate names or numbers, or unequal name/number counts, report `EnumerationNotComparable` without numeric
  records for that enum. Incomplete sources retain known changes and existing gaps; absence is not compatibility.
- The release baseline remains separate and freezes resolved event schemas by equality, including enum names
  and numbers. A nonbreaking numeric classification does not waive a release-baseline violation.

### Evidence

Verified product citations supplied with acceptance:

- **Fundamentals@v7.25.1** `Source/DotNET/Fundamentals/Json/EnumJson.cs:61,65`: `WriteEnum` writes
  `WriteNumberValue`. `Globals.cs:56` registers `EnumConverterFactory`; `EnumConverter.cs` reads numbers or names.
- **Chronicle@v19.82.1** `Source/Clients/DotNET/ChronicleClient.cs:396` adds `EnumConverterFactory` to event
  serializer options. `Source/Clients/DotNET/Schemas/JsonSchemaGenerator.cs:361-362` represents event-schema enums
  as integer values, with names only as `x-enumNames` metadata.
- **Chronicle@v19.82.1** `Source/Kernel/Storage.MongoDB/BsonValueExtensions.cs:84-86`: the read-model sink stores
  `(int)` for enums.
- **Arc@v22.118.0** `Source/DotNET/Arc/ConfigureJsonOptionsFromArcOptions.cs:24-25` removes
  `JsonStringEnumConverter` ("we want integers for enums").
  `Source/DotNET/Tools/ProxyGenerator/Templates/Enum.hbs:10-12` emits numeric TypeScript enums.
- **Stage@v4.147.0** `Source/Rendering.Cratis/Semantics/SemanticEnumMembers.cs:23` renders explicit
  `ValueNumbers` into the generated C# enum.

## Options considered

- **Classify in structural comparison (chosen):** one typed result for library and MCP consumers without
  requiring release-baseline admission.
- **Add an MCP `enumerations` section:** cleaner grouping, but changes every frozen `sections` array and golden
  under 0053 and 0070. Additive per-record fields preserve the existing contract.
- **Treat explicit renames as breaking or unclassified:** rejected for the default numeric stored contract;
  the matching and custom-serialization limits remain explicit rather than claiming universal compatibility.
- **Set `ContractBreaking` only for stored enums:** rejected because APIs and read models also carry numbers;
  the separate `Stored` flag lets consumers choose their policy without losing evidence.
- **Classify only in the release-baseline checker:** rejected because it requires admitted persisted identities,
  checks equality rather than numeric compatibility, and would alter a different boundary.
- **Leave consumers comparing `values` hashes:** rejected because each consumer would duplicate the same logic.

## Default if unanswered

Retain generic member hash changes only. Positional renumbering remains unclassified, so reviewers can miss a
stored-data change and CLI and Studio consumers must implement their own comparison logic. The accepted choices
above replace that default.

## Timeline and scope

Holds until superseded. In scope: comparison library types, both matching modes and declaration-level fallback,
shared stored-type reachability, additive MCP records, incomplete coverage, specs and documentation. This is MCP
and host API work permitted by [0092](0092-the-language-is-frozen-at-esm-v28.md), not a language admission.
No grammar, binder, ESM, printer, TypeScript, release-baseline schema or comparison CLI change is included.
Existing semantic-diff goldens and the input contract golden remain unchanged. Consumer presentation and policy
integration belong to their owning repositories.

## Verification

**Done when:** positional reorder/insertion, explicit renumbering, removal and number reuse are classified;
explicit rename is distinct and nonbreaking; stable pins and appends add no enumeration records; stored usage,
dependants, identity/address matching and incomplete comparisons are retained. The three existing semantic-diff
JSON goldens are byte-identical and a new enum golden pins appended fields and null emission. `PLAY0671` behavior
is unchanged after extracting the walker.

**Verify by:** focused library comparison/completeness specs and MCP semantic-diff, revision-diff and tool catalog
specs; the new enum JSON characterization golden; existing frozen goldens; full repository CI gates before release.

## Consequences

Reviewers can identify numeric contract risks without interpreting hashes or duplicating the matching rule.
Explicit numbers let authors reorder and rename while preserving default stored identity. Comparison still cannot
prove author intent, historical persistence, custom-converter compatibility or transitive runtime impact.
