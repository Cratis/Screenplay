---
id: 0045
title: Record questions, tasks and notes against model elements in a committed sidecar outside the semantic model
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-08
decider: Sindre Alstad Wilting
applies-to:
  - Source/DotNET/Screenplay/Workspaces/ModelNotes*.cs
  - Source/DotNET/Screenplay.Mcp/McpRecoveryJournal*.cs
  - Source/DotNET/Screenplay.Mcp/McpState*.cs
  - Source/DotNET/Screenplay.Mcp/McpWorkspaces*.cs
  - Source/DotNET/Screenplay.Mcp/McpToolCatalog.cs
  - Source/DotNET/Screenplay.Mcp/McpToolSchemas.cs
  - Source/DotNET/Tool/**
  - Documentation/screenplay/model-notes.md
  - Documentation/screenplay/mcp/reference.md
  - Documentation/screenplay/mcp/recovery.md
  - Documentation/screenplay/tool.md
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

[#391](https://github.com/Cratis/Screenplay/issues/391) asks for durable modeling questions and review notes anchored to model elements. They must survive rename and move and must never change the executable semantic model (ESM). A member's comment adds two needs: references to external decisions (a project's own record, such as `D123`), and resolving a question by such a decision.

**What exists.**
- `.screenplay/` is server-owned and holds `identities.json` and the `pending.json` recovery journal. Model document operations cannot write into it. A malformed or conflicting state file is an error that blocks opening (`mcp/recovery.md`).
- Semantic IDs exist for the `SemanticKind` set: application through capture, properties included. Syntax-only constructs (policies, constraints, screens, event sources, streams, operations, examples) have none.
- The sentence of [0023](0023-command-production-model.md) that #391 cites concerns [#307](https://github.com/Cratis/Screenplay/issues/307)'s planned `.screenplay/implementations.json` lock, so this record rules on its own terms.
- [0035](0035-keep-model-reasoning-as-report-only-metadata.md)'s `documentation` blocks carry stateless prose. They have no open or resolved state and cannot be queried by reference. `//` comments are not durable: proposals can drop them.
- **Naming.** StudioIssues#488 uses "annotations" for host-supplied board findings. This feature is therefore called **model notes** everywhere: file, tools, documentation and code.

## Decision

### Storage

Model notes live in a server-owned sidecar, `.screenplay/notes.json`, committed with the model.
- **One file.** The header is `schema: "cratis.screenplay.notes"`, `schemaVersion: 1` and the application identity. A directory of files was rejected (see Options).
- **Canonical JSON.** Stable key order, LF, a trailing newline, notes sorted by `id`, and each note written as one object on its own line, so concurrent additions merge cleanly. `id` is `note_` followed by a random token, never derived from content or position.
- **Not part of the model.** The `.play` source, printer, both compilers, ESM bytes, semantic revision, workspace revision and catalog revision never include notes. Nothing in a compiler reads the file.

### Shape

```json
{ "id": "note_…", "kind": "question", "text": "Can a member cancel after check-in?",
  "anchor": { "semanticId": "…" }, "lastAddress": { "kind": "Slice", "parts": ["…"] },
  "author": "sindre", "createdAt": "2026-10-08T10:00:00Z",
  "refs": [ { "id": "D123", "url": "https://…" } ],
  "resolution": null }
```

- `kind` is `question`, `task` or `note`.
- **State.** `resolution: null` means open. A resolution is `{ "by": "specification" | "description" | "decision" | "external-decision" | "dismissed", "semanticId"?, "ref"?, "text", "author", "at" }`. `by: "external-decision"` carries `ref`, naming a decision outside the model, as in the issue comment ("resolved by external decision"). `refs` and `ref` are free identifiers with an optional URL; the server never fetches them.
- **`author` is unverified free text** supplied by the client. It is not authentication and is never used for authorization.
- `lastAddress` is a display hint refreshed whenever the server writes the note. It is never an anchor.
- Note text is committed to the repository. The documentation says so and tells authors not to record secrets or personal data in it.

### Anchors

- **Only semantic IDs, plus the application, in v1.** An anchor is `{ "semanticId" }` or `{ "application": true }`. It follows rename and move with no rewrite, so the rename and move planners do nothing for notes.
- **Constructs without a semantic ID anchor to their owning slice** (or, outside a slice, the nearest owning feature, module or the application). The note text names the construct. Authoring-key anchors, document anchors and anchors on unidentified syntax are refused; they may be added by a later record once those constructs gain identities.
- **Orphans.** A note is orphaned when its semantic ID no longer resolves, which is what retirement produces. This is computed on read and never stored. Orphaned notes are reported, not deleted, with an `orphanReason`; a source proposal that would orphan notes reports `orphanedNotes` (IDs) before `apply`. Re-anchoring is an explicit operation.

### Revision, writes and failure

- `notesRevision` is `nrev1:` followed by a SHA-256 over the canonical bytes, with a fixed value for an absent file. `open-workspace` and `workspace-state` report it.
- **Journaled like other state.** Writes are retained proposals applied by the existing `apply`, under the same `pending.json` journal and rollback as source and identity state. A notes proposal pins the workspace, catalog and notes revisions, because anchors must resolve when proposed.
- **A malformed or conflicting notes file never blocks opening the model**, unlike identity state: identities govern model meaning, notes do not. A file that does not parse, has a merge-conflict marker, a wrong schema, another application's identity or duplicate note IDs is reported as `notes: { "status": "unreadable", "reason": … }` in `open-workspace` and `workspace-state`. The notes tools, and any proposal that would write the file, refuse with `UnreadableNotes`. The file is never rewritten or repaired automatically.

### MCP tools

- **`list-model-notes`** is read-only. Filters: `anchor` (`semanticId`, or `address` with `kind`), `scope`, `kind`, `state` (`open` by default, `resolved`, `orphaned`, `all`) and `ref`. `view` is `items` or `findings`. It takes `offset`, `limit` and `expectedNotesRevision`, which a continuation requires. Items carry their current `address`, or null with `orphanReason`.
- **`propose-model-notes`** takes `expectedRevision`, `expectedCatalogRevision`, `expectedNotesRevision` and `operations`: `add`, `edit`, `resolve`, `reopen`, `reanchor` and `remove`. Proposals are reviewed with `read-proposal view=notes`.
- **Findings, host-only.** There is no PLAY code. `view=findings` returns one finding per open question with rule ID `model-note.open-question` and severity `info`, shaped as `severity`/`code`/`message` per slice so a host can map them to StudioIssues#488's board findings. The CLI adds `--open-questions report|fail`, honoring `--scope`; `fail` exits 1 when an open question remains in the reported set.
- New tools and parameters change the golden contract of [0039](0039-publish-a-machine-readable-screenplay-contract.md); the implementing PR regenerates it.

### Shared sidecar conventions

This is the first of a family of `.screenplay/` sidecars; #307's lock is the next. Notes set these conventions, which #307's implementation adopts unless its own record states a reason:
- a `schema` of `cratis.screenplay.<name>` and an integer `schemaVersion`, with a published JSON Schema (`notes-v1.schema.json`);
- canonical JSON as above;
- writes journaled under the one `pending.json` envelope;
- **state that does not affect model meaning never blocks open.** A file that does govern meaning, such as a lock that gates deployment, decides its own blocking in its own record.

### Studio

Studio's layout sidecar is an in-process typed record keyed by Studio GUIDs, not a file, so nothing is shared. Studio reads `.screenplay/` files through the published schema or the Screenplay library. The coordination issue is [Cratis/StudioIssues#563](https://github.com/Cratis/StudioIssues/issues/563) (Cratis/Studio has issues disabled), linked to [StudioIssues#289](https://github.com/Cratis/StudioIssues/issues/289) and [StudioIssues#488](https://github.com/Cratis/StudioIssues/issues/488).

**Out of scope:** inline note syntax, editor display (VS Code and Monaco, later), notifications, verified authorship, document anchors, authoring-key anchors, and any effect on compilation verdicts or `--warnaserror`.

## Options considered

- **One committed sidecar (chosen).** It leaves source, printer and both compilers untouched and cannot reach ESM bytes.
- **One file per note.** Avoids merge conflicts on add or remove, but needs a revision hash over a directory and journaling of many files. Rejected for v1; one object per line keeps conflicts small.
- **An inline directive in `.play`.** Rejected: a language change on every surface, and state (resolved, author) does not belong in source.
- **0035 `documentation` blocks or `//` comments.** Rejected: no state, no query by reference, and comments are not durable.
- **Authoring-key anchors for syntax-only constructs.** Rejected for v1: they would oblige the rename and move planners to re-key them. Owning-slice anchors cost nothing and can be refined later.
- **A local, uncommitted file.** Rejected: questions are shared review state.
- **A PLAY information diagnostic.** Rejected: both compilers would read the sidecar and the code table would gain a non-language rule.
- **Blocking open on a malformed file, like identities.** Rejected: a merge conflict in a note must not lock people out of the model.
- **"Annotations" as the name.** Rejected: StudioIssues#488 uses it for board findings.

## Default if unanswered

Questions stay in chat, pull request threads and `description` text, and are lost on rename.

## Timeline and scope

v1 covers storage, anchoring, orphans, the two tools, findings and the CLI flag. Editor display and Studio realization follow separately.

| # | Size | Lands on |
| --- | --- | --- |
| 1 | M | Store, canonical serializer, schema, revision, anchor resolution and orphans (C# Workspaces); durable-state journal; `recovery.md` |
| 2 | M | The two tools, `read-proposal view=notes`, `orphanedNotes` on source proposals; MCP registry; golden contract regeneration |
| 3 | S | CLI `--open-questions`, findings view; `model-notes.md` (how a resolved question becomes a specification, description or decision); `toc.yml`, `tool.md` |

Slice 2 shares the MCP registry with [0038](0038-move-logical-subtrees-with-identity-continuity.md) and [0044](0044-evolve-event-properties-through-mcp.md) and lands after them. It needs Cratis/AI and Cratis/cli issues, and the StudioIssues coordination issue above. There are no compiler, TypeScript, editor or ESM changes.

## Verification

**Done when:**
- ESM bytes and every source, catalog and semantic revision are identical across notes writes.
- Rename and move keep notes anchored by semantic ID without touching the file.
- Retirement reports orphans before `apply` and keeps them.
- Stale notes revisions refuse; an interrupted `apply` rolls the sidecar back.
- A malformed, conflicted or duplicate-ID file opens the model, reports `unreadable`, and refuses note tools and writes.
- A construct without a semantic ID anchors to its owning slice; other anchors refuse.
- `--open-questions fail` gates on open questions.

**Verify by:** workspace store specs, MCP protocol and recovery specs, and CLI specs.

## Consequences

Questions and review notes travel with the model, and tooling can list everything that cites `D123`. The repository gains one more server-owned committed file to merge. Notes on syntax-only constructs are coarser until those constructs gain identities. Free-text `author` and note text are unverified committed content.

## Related issues

Screenplay: [#391](https://github.com/Cratis/Screenplay/issues/391), [#307](https://github.com/Cratis/Screenplay/issues/307). StudioIssues: #289, #488. Decisions: 0004, 0014, 0016, 0023, 0035, 0038, 0039.
