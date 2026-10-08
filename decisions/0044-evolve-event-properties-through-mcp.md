---
id: 0044
title: Add, rename and remove a property along its mapping chain through a planned proposal, with an explicit event-evolution choice
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-08
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/Workspaces/WorkspaceStructuredReferences.cs
  - Source/DotNET/Screenplay/Workspaces/WorkspaceReferenceMembers.cs
  - Source/DotNET/Screenplay/Workspaces/WorkspaceEventRefactorings.cs
  - Source/DotNET/Screenplay/Workspaces/WorkspaceRefactoring.cs
  - Source/DotNET/Screenplay.Mcp/McpToolCatalog.cs
  - Source/DotNET/Screenplay.Mcp/McpToolSchemas.cs
  - Source/DotNET/Screenplay.Mcp/McpTools.cs
  - Documentation/screenplay/mcp/edit.md
  - Documentation/screenplay/mcp/reference.md
  - Documentation/screenplay/events.md
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

[#389](https://github.com/Cratis/Screenplay/issues/389) asks to add, rename or remove a property everywhere it flows. Today `propose-rename` renames only composite `type` properties. Reference coverage leaves out expression identifiers and property paths, which is where mappings live (`mcp/reference.md`). AutoMap is on by default in `from` blocks, so a same-named event and read-model property is an implicit mapping.

**Event properties are a stored contract.**
- [0011](0011-event-generations.md) and [0015](0015-event-generations-in-the-executable-model.md) give each later generation its own property identities, with no link between same-named properties (0015 point 2). A model with generation 2 or later and no transformation is not deployable to Chronicle (point 4).
- **Chronicle refuses an in-place schema change at registration.** `JsonSchemaCompatibilityExtensions.IsCompatibleWith` tolerates only a nullability marker, enum additions or renames, and `title`. Any other difference, a new optional property included, must go through a new generation (`EventTypeRegistrar.cs:61-69, 368-402`). So an in-place add is not "non-breaking"; it fails against any store that holds that generation.
- `events.md` says an authoring catalog does not prove whether events have been stored, so `propose-rename` already pins by default and takes `eventNeverPersisted`.
- [0014](0014-diagnostic-repairs-are-typed-workspace-proposals.md) requires every edit to be a typed, revision-checked proposal.

Moving subtrees is [0038](0038-move-logical-subtrees-with-identity-continuity.md)'s subject. This record covers properties only.

## Decision

### `propose-property`

One planner tool computes typed AST operations and identity changes and returns a retained proposal through the existing authoring transaction (0014). It is reviewed with `read-proposal`, including `view=semantic-diff`, and written only by `apply`. It never writes on propose and accepts no caller-supplied identity arrays.

**Common rules.**
- Targets are revision-bound `read-ast` handles with an expected name, as `propose-rename` uses. A property is one declaration, so unlike 0038's moves it needs no logical-address form.
- `expectedRevision` and `expectedCatalogRevision` are required. `formatting` (default `PreserveTrivia`), `validation` (default `Authoring`) and `includeContent` are optional. Reference policy is Safe.
- It refuses rather than guesses: ambiguity, capture, opaque text naming an affected name and unsupported spans refuse with candidates and locations. A planner defect surfaces as the catalog's `identityMigrationIssues` refusal, never as a retirement.
- Failures are MCP failure or conflict kinds, not PLAY codes.
- Adding the tool and the `find-references`/`dependencies` parameters below changes the golden contract of [0039](0039-publish-a-machine-readable-screenplay-contract.md). The implementing PR regenerates it.

### Prerequisite: a property-path reference index

`find-references` and `dependencies` accept `kind: "Property"` addresses and report these roles:
- `mapping-source` and `mapping-target`;
- `automap`, a derived link;
- `condition` (`require`, `when`, policy paths);
- `screen-binding`;
- `fixture` (specification and example assignments);
- `assertion` (`then` values);
- `opaque` (code naming the property, which blocks automation).

An unresolved link marks the index incomplete, as today.

### The chain

A property's mapping chain is every property connected to it through resolved links: command input, the `produces` mapping or capture append, the event property, the projection mapping or AutoMap, the read-model property, then query results and screen bindings. Fixture and assertion occurrences belong to it. A same-named property off the chain is never edited and is reported in `notChanged` with the reason `offChain`.

```json
{ "action": "rename", "target": {…}, "expectedName": "customerName", "newName": "name",
  "alsoRename": [ { "target": {…}, "expectedName": "customerName" } ],
  "eventNeverPersisted": true }
{ "action": "remove", "target": {…}, "expectedName": "priority", "alsoRemove": [ … ], "newGeneration": true }
{ "action": "add", "owner": {…}, "expectedOwnerName": "ProjectRegistered",
  "name": "priority", "type": "Priority", "optional": false,
  "hops": [ { "at": { "…": "command RegisterProject" }, "name": "priority" },
            { "at": { "…": "produces ProjectRegistered" }, "value": "priority" },
            { "at": { "…": "read model Project" }, "name": "priority" } ],
  "fixtureValue": "\"Normal\"",
  "newGeneration": true }
```

- **Rename.** Renames the target and rewrites every reference, including typed examples ([0032](0032-expand-typed-specification-examples-in-the-front-end.md)). Each `alsoRename` entry must be on the chain, or the proposal refuses with `HopNotOnChain`. If the rename would break an AutoMap link and the counterpart is not in `alsoRename`, the planner inserts an explicit mapping and reports it, so meaning is preserved. Properties outside events keep their semantic IDs.
- **Remove.** Refuses with `PropertyReadersExist` while a `mapping-source`, an `automap` into an existing property, a `condition`, a `screen-binding` or an `opaque` reader remains, listing each with its location. Writers (mapping targets) and fixture values are removed with the property. Removed `assertion` values are listed with `weakensAssertion: true`. An upstream input left unused is reported in `chainCandidates`, never removed implicitly. Retiring the identity is explicit.
- **Add.** Each hop adds a property (`name`, at a declaration, with the same type) or a mapping (`value`, at a `produces`, `from` or `append` block). AutoMap satisfies a same-named projection hop. A producer or consumer left without a decision refuses with `SourcesRequired`, listing candidates; the planner never infers business mappings. A required property needs a value at every existing fixture occurrence, because 0032 allows no implicit defaults. `fixtureValue` is applied to each occurrence, and to the example itself when the step's value comes from an example. Without it the proposal refuses with `FixtureValueRequired`, listing the occurrences.
- **Result.** `property` carries `changed` (`address`, `kind`, `role`, `change`), `notChanged`, `needsDecision` (with candidates), `chainCandidates`, and `generation` (`event`, `from`, `to`, or null).

### Event properties: one explicit choice, no in-place add by default

**Adding even an optional property to a persisted event generation is never done in place in v1.** Any action on an event property, including adding an optional one, requires exactly one of `newGeneration` or `eventNeverPersisted`. Otherwise the proposal refuses with `EventContractChangeRequiresDecision`, naming the event and both options.

- **`eventNeverPersisted`** means **the event's current generation was never stored**. The edit happens in place, and only in the current generation N of that event: an event with one generation edits generation 1, one with several edits the highest. Older generations stay untouched, and a target inside a historical generation refuses with `HistoricalGenerationImmutable`. A rename keeps the property ID. The caller asserts this fact; the catalog cannot prove it.
- **`newGeneration`** writes the current shape as full generation N and the changed shape as generation N+1 ([0011](0011-event-generations.md)), advancing the catalog per 0015. Properties of the new generation get new identities (0015 point 2): a rename does not keep the ID. Command and read-model property IDs are kept.
  - The result states `deployableWithoutTransformation: false` (0015 point 4) until the transformation construct of [#71](https://github.com/Cratis/Screenplay/issues/71) exists.
  - The result states `esmVersionChange: { "from": …, "to": 4 }` when the proposal makes the model's first event with several generations and thereby selects ESM v4 (0015 point 7); otherwise `null`.
- **Inline events** refuse with `InlineEventRequiresExtraction` and point to `propose-extract-inline-event`.

Non-event properties need neither flag.

**Acceptance example.** In the v2 corpus (0015 point 9) `ProjectRegistered` is already at generation 2, so adding a required `priority` to it means either generation 3 or `eventNeverPersisted`. With `eventNeverPersisted` the edit lands in generation 2 in place; with `newGeneration` generation 3 is added, the model stays on v4, and the result says `deployableWithoutTransformation: false` and `esmVersionChange: null`.

**Out of scope:** property type changes and nested composite member paths; concept and composite type properties, which `propose-rename` already covers; moving declarations (0038); the transformation construct (#71); changes to the TypeScript compiler, editors, samples or the ESM.

## Options considered

- **A planner tool over the one transaction (chosen).** It reuses revision checks, review and journaled apply, and callers stop enumerating migrations.
- **More `propose-ast` operations.** Rejected: that path is the explicit low-level one and cannot discover references.
- **Letting an optional add skip the choice.** Rejected: Chronicle refuses it at registration. `semantic-diff` already marks `property-added` as `ContractBreaking` (`McpSemanticDiff.cs`). Relaxing it belongs to #71, where an optional add is the natural first implied transformation. It would still need a generation.
- **`eventNeverPersisted` on any generation.** Rejected: editing a historical generation rewrites a stored contract.
- **Matching properties by name.** Rejected: it renames unrelated fields.
- **Inferring add sources, or cascading remove.** Rejected: the issue forbids guessing mappings, and silent reader removal changes behavior.
- **Keeping a property identity across generations on rename.** Rejected: 0015 point 2 claims no link until transformations exist.

## Default if unanswered

Property changes stay hand-composed `propose-ast` batches, and renames silently break AutoMap links the author misses.

## Timeline and scope

From acceptance until superseded. The index and tool land in the slices below. `newGeneration` models are honest but not deployable until #71, so `eventNeverPersisted` will be the common path for unreleased models; its risk is that it is chosen wrongly, which the result's `generation` field and the `events.md` text make visible.

| # | Size | Lands on |
| --- | --- | --- |
| 1 | M | Property-path index (`WorkspaceStructuredReferences`, `WorkspaceReferenceMembers`), `find-references`/`dependencies` roles, `reference.md` coverage |
| 2 | M | `propose-property` rename and remove, with generation authoring shared with `WorkspaceEventRefactorings`; MCP registry; golden contract regeneration; `edit.md`, `events.md` |
| 3 | M | Add with hops and fixture values; the `RegisterProject` corpus acceptance spec |

Slice 1 can run beside 0038's work; slices 2 and 3 share the MCP registry and land one after another, after 0038's tool. They need a Cratis/AI issue (`mcp-tools.md`, `mcp-loop.md`) and a Cratis/cli tool-count check, per [#495](https://github.com/Cratis/Screenplay/issues/495).

## Verification

**Done when:**
- A property rename updates mappings, examples and specifications, and requires a generation choice on events.
- A broken AutoMap link becomes an explicit mapping.
- Remove refuses while readers exist and lists them.
- Adding a required property to `RegisterProject` binds and passes its specifications, in both modes of the acceptance example.
- Adding an optional property without a choice refuses.
- A historical-generation target and an inline event refuse.
- The results carry `deployableWithoutTransformation` and `esmVersionChange` as stated.
- Off-chain same-named properties are never edited.
- The golden contract is regenerated and reviewed.

**Verify by:** workspace specs per action and refusal, MCP protocol specs, `semantic-diff` output on the resulting proposals, and the canonical corpus acceptance spec.

## Consequences

Property changes become one reviewable proposal that cannot lose an identity or a mapping silently. Authors of persisted events must choose consciously, and `newGeneration` output is not deployable until #71. Each tool addition touches the shared MCP registry and the golden contract.

## Related issues

Screenplay: [#389](https://github.com/Cratis/Screenplay/issues/389), [#71](https://github.com/Cratis/Screenplay/issues/71), [#495](https://github.com/Cratis/Screenplay/issues/495). Decisions: 0011, 0014, 0015, 0032, 0038, 0039.
