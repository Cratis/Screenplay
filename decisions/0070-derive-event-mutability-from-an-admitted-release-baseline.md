---
id: 0070
title: Derive event mutability from an admitted release baseline committed beside the identity catalog
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-13
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Tool/**
  - Documentation/screenplay/events.md
  - Documentation/screenplay/tool.md
  - Documentation/screenplay/mcp/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

[#686](https://github.com/Cratis/Screenplay/issues/686) asks whether event generations should be unnecessary while nothing is deployed. The question was raised on [PR #673](https://github.com/Cratis/Screenplay/pull/673#issuecomment-6120249738).

**What exists today.**
- [0044](0044-evolve-event-properties-through-mcp.md) requires exactly one of `newGeneration` or `eventNeverPersisted` for any change to an event property, otherwise `EventContractChangeRequiresDecision`. [0067](0067-report-every-selected-event-property-evolution.md) reports the choice per event in `property.generations[]` as `{ event, from, to, mode }`, with `mode` either `newGeneration` or `eventNeverPersisted` (`WorkspacePropertyReport.cs`, `WorkspacePropertyGeneration`). `propose-rename` uses `eventNeverPersisted` for a related purpose: it decides whether an event rename pins the previous name with `id "<previous name>"` (`WorkspaceRefactoring.cs`, `PlannedEventId`; `events.md`).
- `eventNeverPersisted` is an assertion by the caller. `events.md` states that an authoring catalog does not prove whether events have been stored. Before a release, the choice adds friction to every edit. After a release, choosing it wrongly edits a stored contract in place, and nothing refuses that edit.
- [0015](0015-event-generations-in-the-executable-model.md) point 6 leaves immutability open. A complete property list does not prove a historical schema unchanged, because properties refer to concepts and composite types that can change underneath it. In the executable semantic model (ESM), a property's `SemanticTypeReference` names its concept or type by `SemanticId` (`SemanticValues.cs`). It does not carry that type's shape. [#609](https://github.com/Cratis/Screenplay/issues/609) tracked refusing edits to a released generation against an admitted baseline; it is closed as superseded by #686. [#71](https://github.com/Cratis/Screenplay/issues/71) is now narrowed to the transformation between generations.
- [0053](0053-public-structural-model-comparison.md)'s structural comparison matches event generations, but it compares each property by its serialized syntax type reference (`StructuralComparison.Events.cs`, `Compare` and `Shape`). A change inside a concept, composite type or enum that an event uses is therefore invisible to it. 0053 also freezes the MCP semantic-diff JSON bytes. The word "baseline" already means the retained disk snapshot in `read-proposal view=semantic-diff` (`mcp/reference.md`).
- `.screenplay/` is server-owned and holds `identities.json`, `pending.json` and, under [0045](0045-model-notes-sidecar.md), `notes.json`. The catalog persists each event's `EventContractId` and `EventContractRevision` (`SemanticIdentityCatalog.cs`, `EventContractIdentityAssignment`). 0045 sets the sidecar conventions and says that a file which governs meaning decides its own blocking in its own record.
- [0066](0066-published-esm-versions-are-frozen.md) is the precedent for treating publication as the point where a contract freezes: from first publication, a contract cannot change underneath its number.
- Chronicle's registration check compares stored and generated JSON schemas. It tolerates titles, nullable format markers, enumerations that grow or are renamed, and added compliance metadata, and it refuses any other difference (`Source/Infrastructure/Schemas/JsonSchemaCompatibilityExtensions.cs` at Chronicle `0086a44b92`). Compliance metadata is part of the stored schema.

If nobody chooses, the per-proposal assertion stays the only protection for released events, and a concept change under a released revision remains undetected.

## Decision

An event revision's mutability comes from an **admitted release baseline**: a committed record of the event revisions that a release shipped, with their resolved schemas. A revision in the baseline is frozen. A revision outside it can be edited in place without a choice. A workspace without a baseline behaves exactly as it does today.

### Storage: `.screenplay/release-baseline.json`

The baseline lives in a server-owned sidecar committed with the model, next to `identities.json`. It is named *release* baseline so that it is not confused with semantic-diff's retained disk baseline. It follows 0045's sidecar conventions:

- The header is `schema: "cratis.screenplay.release-baseline"`, `schemaVersion: 1` and the application identity, with a published `release-baseline-v1.schema.json`.
- **Canonical JSON**: stable key order, LF, a trailing newline, entries sorted by event contract ID and then revision, and one entry per line so that admissions from separate branches merge cleanly.
- Writes are journaled under the one `pending.json` envelope.
- `releaseBaselineRevision` is `brev1:` followed by a SHA-256 over the canonical bytes, with a fixed value for an absent file.
- **It is not part of the model.** The `.play` source, the printer, both compilers, ESM bytes, the semantic revision, the workspace revision and the catalog revision never include it. Adding a baseline changes no canonical bytes.

```json
{ "event": "<EventContractId>", "revision": 2, "eventType": "ProjectRegistered",
  "schemaHash": "rsh1:…", "schema": { … },
  "lastAddress": { "kind": "Event", "parts": ["…"] },
  "admittedIn": "v1.4.0", "admittedAt": "2026-10-12T10:00:00Z" }
```

- `event` and `revision` are the catalog's `EventContractId` and `EventContractRevision`. They are the key. Entries are matched by identity only: under 0053's rules, both sides must share the application identity, and there is no fallback to address or name matching. Without persisted identities, there is no baseline.
- `eventType` is the event's effective stored type name: its `id` pin when it has one, otherwise its name. A rename that changes the stored name changes the stored contract as surely as a schema change does.
- `schema` is the revision's resolved schema in canonical form. `schemaHash` is `rsh1:` followed by a SHA-256 over that form. The stored schema lets a refusal say which property changed. On reading, a hash that does not match its schema makes the file unreadable.
- `lastAddress` is a display hint, as in 0045. `admittedIn` is a free release label. `admittedAt` is the time of admission. Neither one affects matching.

### What "frozen" covers: the resolved schema

A revision's resolved schema is the shape stored events carry, with every type reference expanded:

- every property's name, whether it is optional and whether it is a collection;
- primitives as primitives;
- each concept as its underlying primitive plus its compliance marker (ESM v13), because Chronicle stores compliance metadata in the schema;
- composite types as their members, recursively;
- enums as their members, with names and numbers ([0065](0065-explicit-enum-member-numbers.md));
- plus the event's effective `eventType`.

The resolved schema excludes names of concepts, types and enums (they appear only as Chronicle titles, which Chronicle ignores), as well as descriptions, documentation, tags and property semantic IDs. It also excludes concept `validate` rules, concept `generate` strategies, and concept and enum documentation: none of them changes the shape that stored events carry, and a change to one is not a contract change. A change confined to excluded content is not a contract change. It is computed from the bound ESM, so admission and checking require a model that binds executably.

**Frozen means equality.** Any difference from the admitted `schemaHash`, including one that Chronicle would tolerate, is a change to a baselined revision in v1. Version 1 deliberately does not reproduce Chronicle's compatibility relation (see Options).

**A change to a concept, composite type or enum that a baselined revision uses is refused. It does not force a new generation.** Historical revisions use the same type, and a historical revision cannot receive a new generation. A new generation of the current revision would leave every older baselined revision changed underneath. A new generation would also fan out to every event using that type, which breaks 0067's rule that only explicitly selected contracts change. The refusal lists every affected baselined revision. To evolve a stored contract, declare a new concept or type and use it in a new generation of the events that need it.

### Mutability per event revision

Every event-contract edit is classified by `basis`:

| `basis` | When | Edit behavior |
| --- | --- | --- |
| `noBaseline` | No `release-baseline.json` exists | Unchanged from 0044: exactly one of `newGeneration` or `eventNeverPersisted` is required, otherwise `EventContractChangeRequiresDecision`. Renames pin by default. |
| `notInBaseline` | The revision being edited is not in the baseline | With no flag, the edit happens in place in the current generation, with `mode: "inPlace"`. A rename does not pin. An explicit `eventNeverPersisted` is accepted and reported as `eventNeverPersisted`. An explicit `newGeneration` is honored. |
| `baselined` | The revision being edited is in the baseline | In-place edits and `eventNeverPersisted` refuse with `BaselinedRevisionImmutable`, which names `newGeneration` as the only option. A rename keeps its pin, and `eventNeverPersisted` on `propose-rename` refuses in the same way. Only an explicit `newGeneration` proceeds. |

For a baselined revision, `newGeneration` stays explicit and is not applied by default. Until #71 exists, a new generation is not deployable (`deployableWithoutTransformation: false`), and 0044 requires a conscious choice about stored contracts. A refusal that offers exactly one option keeps that choice visible without guessing. Historical generations stay immutable, as in 0044, whether or not they are in the baseline.

### One structural check for every path

Per-tool flags do not protect a baseline on their own. Every proposal therefore runs the same check on its candidate before it is retained, and `apply` runs it again before writing. This covers `propose-ast`, repairs, moves, renames and `propose-property`. The check refuses with:

- **`BaselinedRevisionChanged`** when the candidate's resolved schema for a baselined revision differs from the admitted one, including through a concept or type change;
- **`BaselinedRevisionMissing`** when a baselined revision, or its whole event, no longer exists. This follows 0015 point 5, under which source that declares fewer generations than persisted fails.

A proposal records the `releaseBaselineRevision` it was planned against. `apply` refuses with `StaleReleaseBaseline` when that revision has changed. Callers may also pin `expectedReleaseBaselineRevision`, which is optional so that existing calls keep working. Edits made outside MCP, in an editor or by hand, are caught by the CLI check below.

### Reports

- **`property.generations[]`** (0067): each entry gains `mutability: { "basis": "noBaseline" | "notInBaseline" | "baselined", "releaseBaselineRevision": "brev1:…" | null, "admittedIn": "<label>" | null }`, and the `mode` set gains `inPlace`. The same object appears on the C# `WorkspacePropertyGeneration`, the MCP result and the retained `read-proposal` view. `releaseBaselineRevision` is null only for `noBaseline`. `admittedIn` is set only for `baselined`.
- **`propose-rename`** of an event reports the same `mutability` object for the renamed event, along with whether a pin was added.
- **`open-workspace` and `workspace-state`** report `releaseBaseline: { "status": "absent" | "valid" | "unreadable", "revision", "reason"?, "entries", "violations" }`. Here `violations` counts the baselined revisions whose current resolved schema differs or is missing.
- A new read-only MCP tool, **`list-release-baseline`**, pages the entries with their current status (`unchanged`, `changed` with the first differing path, or `missing`). `read-proposal view=release-baseline` shows a proposal's per-event basis and violations.
- **`semantic-diff` is unchanged.** Its JSON bytes stay frozen under 0053, and baseline findings are never added to it.

### Admission: a deliberate release step

- Admission is done by **`screenplay baseline admit <root> --release <label>`**, or by the equivalent library API. A person or a CI release job runs it as part of shipping and commits the result. Admission is never implicit. Building, publishing a package, opening, applying or rendering never admits anything.
- **MCP does not admit in v1.** Admission records a release, which is not an authoring act, and MCP reports and enforces the baseline but cannot write it.
- By default, admission reads the current bound ESM. **`--from-esm <file>`** admits from the canonical ESM bytes that a release shipped instead, read by the strict reader and required to carry the same application identity. This lets a team baseline a release that already happened, even after `main` has moved on.
- **Admission is monotonic.** It adds every current revision of every event (historical revisions included) that is not already present. An entry that is already present and still matches is kept with its original `admittedIn`. If an entry is present but its current resolved schema differs, admission refuses with `BaselinedRevisionChanged`, because a changed revision cannot be re-admitted. Admission also refuses when the model does not bind, when `identities.json` is absent, or when recovery is pending.
- **No tool removes entries.** Unfreezing a revision is rarely legitimate: a release that never stored events is the usual case. It needs a person to edit or delete the file in a reviewed pull request. Deleting the file returns the workspace to `noBaseline`, the explicit-choice behavior of today, not to free in-place editing.
- **`screenplay baseline check <root>`** exits `1` when any violation exists and `2` when the file is unreadable. It uses the same check as the proposals, so CI can gate edits made outside MCP.

### Malformed baseline: open, but fail closed for what it governs

The baseline governs which edits are allowed. It does not govern what the model means: ESM bytes, compilation and specifications do not depend on it. So, following 0045's rule, it decides its own blocking as follows. **A malformed, conflicted, wrong-schema or other-application baseline does not block opening, reading, compiling or reviewing**, and the workspace reports `releaseBaseline.status: "unreadable"` with a reason. **Every proposal that would change an event contract refuses with `UnreadableReleaseBaseline`**, as do admission and `screenplay baseline check`. An unreadable file is never treated as `noBaseline` and never as `notInBaseline`, and it is never repaired automatically. Proposals that touch no event contract proceed.

### Consumers and migration

- **Identities and catalog.** The baseline keys on catalog identities and never writes them. Catalog advancement (0015 point 5) and generation-qualified property identities are unchanged. Retiring a baselined event's contract ID is reported as `BaselinedRevisionMissing`, not done silently.
- **Studio** reads the file through the published JSON Schema or the Screenplay library and shows `basis`. A coordination issue in StudioIssues is raised at implementation, as for 0045.
- **Stage and the Cratis CLI** consume `screenplay baseline check` or the library check. Stage warns about a model with violations and does not refuse to render it. The CLI `baseline check` is the gate. Both are raised as issues at implementation.
- **Migration.** Existing models have no baseline and behave as before, with byte-identical results apart from the added `mutability` field. A team that has already shipped admits once, preferably with `--from-esm` against the shipped ESM.

## Options considered

- **A committed sidecar written by an explicit admission step (chosen).** It is deterministic and works offline. The admission shows up as a reviewable diff in the pull request that ships it. It matches by the identities already in `.screenplay/`, works the same way in every repository, and is read by Studio through a published schema, without git or network access.
- **Derive the baseline from published packages or release artifacts, such as the ESM shipped with a release under 0066.** Not taken as the source of truth: reading it needs network access or a vendored copy. Most applications deploy an application, not a published ESM package. A published prerelease does not prove that events were stored. Kept as an admission input through `--from-esm`.
- **Derive the baseline from git tags.** Rejected: shallow CI clones and Studio often have no tags or no git. Which tag means "released" is a convention, not data. The answer would differ between clones, and a change to a tag never appears in a pull request.
- **A full workspace export as the baseline, compared with `ModelComparison.FromWorkspaceExport`.** Rejected: it is large and merges badly. 0053 compares serialized syntax type references, so a concept change underneath a revision would still go unseen, which is 0015 point 6's gap.
- **Hashes only, without the resolved schema.** Rejected: a refusal could not say what changed. The schemas are small.
- **Mark released generations in `.play` source.** Rejected: release state is not language. The change would land on every surface, and the ESM bytes would change on release.
- **Force a new generation when a shared concept changes.** Rejected: historical revisions cannot get one, and it would edit events nobody selected (0067).
- **Use Chronicle's compatibility relation instead of equality.** Not taken in v1: Screenplay, the reference evaluator and Stage would all need to implement the same relation. Equality is decidable and can be relaxed later without unfreezing anything.
- **Default to `newGeneration` for a baselined revision.** Rejected: it would silently produce models that cannot be deployed until #71.
- **Block opening on a malformed baseline, as identities do.** Rejected: the baseline does not change model meaning, so failing closed only for event-contract edits protects it without locking people out after a merge conflict.
- **Let MCP admit.** Rejected for v1: admitting is a release act. Agents and editors can report it but should not perform it.
- **A workspace "unreleased" flag.** Rejected by the issue: it is another assertion, coarse and global, and proves no more than `eventNeverPersisted` does.

## Default if unanswered

The explicit per-proposal choice stays. Development keeps the friction on every edit. After release, a wrong `eventNeverPersisted`, a hand edit or a concept change rewrites a stored contract without any refusal. #609 and 0015 point 6 stay open, and #71's transformations will have no record of which revisions are stored.

## Timeline and scope

Settle before #686 or #609 is implemented. The decision holds until superseded. When #71 lands, transformations treat the baselined revisions as the stored set.

In scope: the sidecar's location, shape, revision and conventions; the resolved-schema definition and equality; per-revision `basis` and the edit behavior for each; refusals on every proposal path and at `apply`; report fields; admission and checking through the CLI and the library; blocking behavior; consumers; migration.

Out of scope: the transformation construct (#71); Chronicle-side registration checks; tracking which environment runs which release; compiler diagnostics or editor display in either compiler, VS Code or Monaco; Stage's refusal behavior (it warns; see Consumers); relaxing equality toward Chronicle's compatibility relation; MCP admission; and removing entries through a tool.

| # | Size | Lands on |
| --- | --- | --- |
| 1 | M | Resolved-schema canonical form and hash from the ESM; sidecar store, JSON Schema, revision and journaling; `screenplay baseline admit` and `check`; `tool.md`, `events.md` |
| 2 | M | Per-revision `basis` in `propose-property` and `propose-rename`; the structural check on every proposal and at `apply`; refusals; `mutability` in reports; `workspace-state`, `list-release-baseline`, `read-proposal view=release-baseline`; golden contract regeneration under [0039](0039-publish-a-machine-readable-screenplay-contract.md); `mcp/edit.md`, `mcp/reference.md`, `mcp/recovery.md` |

When accepted, 0044 and 0067 gain dated pointers to this record, and their decision text is preserved. Implementation then raises a Cratis/AI corpus issue (the `eventNeverPersisted` guidance in the skills) and the StudioIssues, Stage and Cratis CLI issues named above.

## Verification

**Done when:**
- Without a baseline, every existing `propose-property` and `propose-rename` specification passes unchanged, and the results differ only by `mutability.basis: "noBaseline"`.
- With a baseline:
  - an edit to a revision outside it applies in place with no flag and reports `inPlace` and `notInBaseline`;
  - an edit to a baselined revision without `newGeneration`, or with `eventNeverPersisted`, refuses with `BaselinedRevisionImmutable`, and with `newGeneration` it reports `baselined`;
  - a `propose-ast` edit, a concept change or an enum member addition under a baselined revision refuses with `BaselinedRevisionChanged`, and the refusal lists every affected revision;
  - a change to descriptions, documentation or a concept's name alone does not refuse;
  - a removed baselined generation refuses with `BaselinedRevisionMissing`.
- An unreadable file opens the workspace, reports `unreadable`, and refuses event-contract proposals, admission and checks.
- Admission is monotonic, refuses to re-admit a changed revision, and `--from-esm` admits from shipped bytes.
- ESM bytes and the source, catalog and semantic revisions are identical across admissions.
- The semantic-diff goldens are byte-identical.

**Verify by:**
- workspace specifications for each basis, each refusal and the resolved-schema hash, covering concepts, composite types, enums with numbers, compliance markers and exclusions;
- MCP protocol, result-JSON and recovery specifications, including `StaleReleaseBaseline` at `apply`;
- CLI specifications for `admit`, `check` and their exit codes;
- an extended `RegisterProject/v2` corpus case with generation 1 baselined and generation 2 edited in place, then baselined and refused;
- the regenerated contract golden, the unchanged semantic-diff goldens, and a `Decision: 0070` trailer.

## Consequences

Before a release, event edits need no ceremony. After a release, stored contracts are protected by data rather than by an assertion, including against concept changes underneath them and against edits made outside MCP, when the CLI check runs in CI. Reviewers see in every report why an edit was allowed.

Costs:
- Teams must run an admission step when they ship. Once a baseline exists, revisions that shipped but were never admitted stay freely editable in place. Run `baseline admit` in the release job, and add a check that warns about new revisions that are not yet admitted.
- One more server-owned file has to be merged.
- Edits made on `main` between a release and the merge of its admission commit are not protected, so admission belongs in the release job on `main`.
- Enum additions that Chronicle would accept need a new generation until equality is relaxed.
- Unfreezing an entry is a manual, reviewed edit.

This forecloses a hidden or implicit "released" state and closes 0015 point 6 for revisions that have been admitted.

## Resolved questions

- `newGeneration` stays explicit for baselined revisions. It is never applied by default.
- Compliance markers are frozen: they are part of the resolved schema.
- MCP does not admit in v1.
- Stage warns about violations and does not refuse. The CLI `baseline check` is the gate.
- Frozen means equality now. Relaxing it toward Chronicle's compatibility relation is a later change.
- #609 is closed as superseded by #686.

## Related issues

Screenplay: [#686](https://github.com/Cratis/Screenplay/issues/686), [#609](https://github.com/Cratis/Screenplay/issues/609), [#71](https://github.com/Cratis/Screenplay/issues/71), [#389](https://github.com/Cratis/Screenplay/issues/389), [PR #673](https://github.com/Cratis/Screenplay/pull/673). Decisions: [0011](0011-event-generations.md), [0015](0015-event-generations-in-the-executable-model.md), [0039](0039-publish-a-machine-readable-screenplay-contract.md), [0044](0044-evolve-event-properties-through-mcp.md), [0045](0045-model-notes-sidecar.md), [0053](0053-public-structural-model-comparison.md), [0065](0065-explicit-enum-member-numbers.md), [0066](0066-published-esm-versions-are-frozen.md), [0067](0067-report-every-selected-event-property-evolution.md).
