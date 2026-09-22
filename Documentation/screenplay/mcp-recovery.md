---
title: MCP identity state and recovery
description: Durable identity assignments, interrupted apply detection, and explicit rollback without overwriting competing edits.
---

## Files owned by the server

The model's `.play` files remain canonical source. The MCP server owns a reserved
`.screenplay/` directory inside the configured model root.

| File | Purpose | Retention |
| --- | --- | --- |
| `identities.json` | Versioned application identity/name, document IDs/keys/paths, and semantic/event identity catalog | Keep with the model in source control |
| `pending.json` | Exact before/after recovery data for one in-progress operation | Keep until application or rollback is verified |
| Staging and backup files | Installation and recovery intermediates | Removed only when safe; retained on uncertainty |

`identities.json` does not duplicate source text. The pending journal does contain
source snapshots needed for recovery and is created privately. Model document
operations cannot write into `.screenplay/`; it is not an arbitrary file API.

A malformed, conflicting or unsafe state file is an error. The server never
silently discards it and derives replacement identities from source names.

## Opening and external edits

`open-workspace` loads persistent identity mappings automatically. Moves and
identity-preserving renames survive a fresh server session without requiring a
client to save a workspace export first.

Content edits outside MCP can be reopened when they do not change identity
assignments. Externally moved/deleted mapped files or identity-changing source
edits are rejected rather than guessed. Restore the mapped names/paths, then use
an explicit proposal for the change. A canonical `workspaceJson` import cannot
overwrite a different persisted identity authority merely by opening it.

Pending operations block normal model reads, opening and editing. Inspect
`workspace-state` instead of trying to bypass the marker.

## Reviewing the state change

Every proposal includes `stateChange` with before/after state revisions. Source
and identity state are installed under the same rollback envelope.

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
and verifies original source bytes, identity state and preserved access settings.
The marker is removed only after verification and safe cleanup.

On conflict or uncertainty, the result remains `RecoveryRequired`; the marker
and remaining backups stay in place. Preserve them. Move or restore competing
external edits deliberately before requesting recovery again. Do not delete the
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
