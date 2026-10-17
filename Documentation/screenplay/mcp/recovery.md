---
title: MCP identity state and recovery
description: Durable identity assignments, interrupted apply detection, and explicit rollback without overwriting competing edits.
---

## Files owned by the server

The model's `.play` files remain canonical source. The MCP server owns a reserved
`.screenplay/` directory inside the configured model root.
Read-only opens and baseline checks accept a `.screenplay` directory others can read, such as the `0755` directory a git checkout creates, but refuse one other users can write. On Windows, write access inherited by SYSTEM and Administrators is accepted for reads. Writes and recovery still require owner-only access and create private metadata.

| File | Purpose | Retention |
| --- | --- | --- |
| `identities.json` | Versioned application identity/name, document IDs/keys/paths, and semantic/event identity catalog | Keep with the model in source control |
| `notes.json` | Versioned, committed questions, tasks and review notes anchored to model identities | Keep with the model in source control |
| `release-baseline.json` | Admitted release revisions and resolved stored event schemas | Keep with the model in source control; admit through the CLI or library, never MCP |
| `pending.json` | Exact before/after recovery data for one in-progress operation | Keep until application or rollback is verified |
| Staging and backup files | Installation and recovery intermediates | Removed only when safe; retained on uncertainty |

`identities.json` does not duplicate source text. The pending journal does contain
source snapshots needed for recovery and is created privately. Model document
operations cannot write into `.screenplay/`; it is not an arbitrary file API.

A malformed, conflicting or unsafe identity state file is an error. The server never
silently discards it and derives replacement identities from source names.

Model notes are nonsemantic state: an unreadable `notes.json` never blocks opening
or source proposals. `open-workspace` and `workspace-state` report `notesRevision`
and `notes: { status: "ok" | "absent" | "unreadable", reason, count }` (unreadable
revisions and counts are null). Note tools refuse with `UnreadableNotes`; the server
never auto-repairs the file. Resolve merge conflicts or restore known-good notes
explicitly. See [model notes](../model-notes.md).

A malformed or conflicted release baseline also permits opening, reading and
reviewing. `open-workspace` and `workspace-state` report
`releaseBaseline: {status, revision, reason, entries, violations}`. Unreadable
revision and entry count are null; a model that cannot bind reports an unknown
violation count rather than a successful check. Event-contract proposals refuse
`UnreadableReleaseBaseline`; non-contract edits proceed. Restore or review the
sidecar explicitly; MCP never admits or repairs it.

Every retained proposal pins its release-baseline evidence independently of source
and catalog revisions. `apply` refuses `StaleReleaseBaseline` if that file changes,
even when source is unchanged. Read the new baseline and propose again rather than
retrying the old plan. Baseline refusals before installation have no write outcome
to recover; interrupted source or notes installation uses the normal journal below.

## Opening and external edits

`open-workspace` loads persistent identity mappings automatically. Moves and
identity-preserving renames survive a fresh server session without requiring a
client to save a workspace export first. Case-only moves preserve the document
identity and install the requested filename casing even on case-insensitive disks.

Content edits outside MCP can be reopened when they do not change identity
assignments. Externally moved/deleted mapped files or identity-changing source
edits are rejected rather than guessed. Restore the mapped names/paths, then use
an explicit proposal for the change. A canonical `workspaceJson` import cannot
overwrite a different persisted identity authority merely by opening it.

There is one pre-binding exception: opening an unbound model may compute new
collision-free deterministic Module, Feature and Slice assignments in memory.
Every document must parse and place, every earlier assignment must still be
present in source, and all earlier IDs, origins, event revisions, document
mappings and application metadata must be unchanged. Both reconciliation checks
refuse any other drift. This exception never permits a supplied `workspaceJson`
to replace different persisted identity state, and opening never writes it.

If a partial catalog has been persisted, fix the binding error through an MCP
proposal. Fixing it outside MCP can add bound identities on open and therefore
require reconciliation. Do not discard identity state to bypass that refusal.

Pending operations block normal model reads, opening and editing. Inspect
`workspace-state` instead of trying to bypass the marker.

## Reviewing the state change

Diagnostic repairs (`PLAY0166` event declaration and `PLAY0478` explicit destination)
use the same proposal/apply envelope as other typed authoring. Discovery and
`propose-repair` do not modify source or identity state. Review both before applying;
if apply is interrupted, use the recovery procedure below rather than proposing the
repair again against partially installed files. See [Fix a diagnostic](authoring-tools.md#fix-a-diagnostic).

Every proposal includes `stateChange` with before/after state revisions. Source
and identity state are installed under the same rollback envelope. Notes proposals
pin workspace, catalog and notes revisions; `read-proposal view=notes` pages their
before/after notes. Apply stages and installs the sidecar under that same
`pending.json` envelope, preserving original notes bytes and access settings.
A notes apply can also persist container identities computed during open. The
journal retains the exact earlier identity bytes even when they omit those
containers; rollback restores both the notes and that earlier identity state.

`workspace-state` has these views:

| View | Additional arguments | Result |
| --- | --- | --- |
| `status` (default) | Optional `proposalId` | State presence/revision, identity summary, pending recovery status, optional proposed state change |
| `persisted` | `expectedStateRevision`, optional `offset`/`limit` | Exact persisted identity JSON byte pages |
| `before` / `after` | `proposalId`, `expectedStateRevision`, optional `offset`/`limit` | Exact proposed identity-state byte pages |

Use the state revision from the status or proposal response. Offsets count decoded
bytes. A changed state file rejects page continuation and apply.

Canonical `export-workspace` remains available for backups, transport and
inspection. It is no longer a prerequisite for retaining identities after a
normal MCP apply.

## Interrupted application

Before source mutation, apply writes a durable journal. Installation stages
private bytes, retains original files, installs source and identity state, and
verifies both. Only then can it discard the journal and backups.

Journals carrying notes use envelope version `2`; source/identity-only journals
continue to use version `1`. This server reads both, refuses a version-1 journal
carrying notes, and requires a complete, consistent notes section in version 2.
Library/CLI release admissions use version `3` with only a release-baseline section;
recovery restores its exact prior bytes or absence and access settings. MCP exposes
no admission write tool. This server reads all three envelope versions and rejects
inconsistent or mixed sidecar sections.
Older servers that only support version 1 refuse the newer journal instead of
completing recovery without restoring notes. Finish an interrupted notes operation
with the newer server version; do not downgrade or remove the marker.

This detects interruption; it does **not** make multiple filesystem paths become
visible simultaneously. Other processes can observe intermediate files. Run
apply with exclusive writer access to a trusted model root.

`workspace-state` reports a pending `operationId`, whether rollback can currently
be proven safe, and any conflict. The status call itself changes nothing.

## Explicit rollback

To roll back an interrupted operation:

1. Stop other writers and call `workspace-state`.
2. Inspect the reported conflict and `operationId`.
3. Call `recover-workspace` with that exact `operationId`.
4. Require `success: true` and `status: "RolledBack"` before reopening the model.

Recovery preflights every affected file. Existing bytes must match a known before
or after image; unexpected third-party content is never overwritten. It restores
and verifies original source bytes, identity state, any journaled notes sidecar,
and preserved access settings. Reopening a server after an interrupted notes write
uses the same explicit rollback procedure; it never regenerates or drops notes.
The marker is removed only after verification and safe cleanup.

On conflict or uncertainty, the result remains `RecoveryRequired` and adds
`failureKind: "RecoveryRequired"`; the marker and remaining backups stay in place.
Preserve them. Move or restore competing external edits deliberately before
requesting recovery again. Do not delete the
marker merely to unblock the server.

## Boundaries

- The server does not execute model code or follow external `file` references.
- Identity state and journals are bounded, parsed and validated, not trusted as
  instructions to access arbitrary paths.
- Recovery is rollback, not an automatic merge of competing edits.
- Root-local persistence is not remote backup. Commit the source and identity
  state together and keep an appropriate repository backup.
- A broken or tampered journal can require manual recovery; it never becomes an
  empty successful operation.
