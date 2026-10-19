---
id: 0104
title: Show the C# completeness verdicts in VS Code through the repair bridge
status: accepted
stage: none
decided: 2026-10-19
decider: Sindre Wilting (delegated to the L0 coordinator)
class: product
reversibility: cheap
applies-to:
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/vscode.md
  - Documentation/screenplay/editor-diagnostics.md
---

## Context

The completeness checks (PLAY0530-PLAY0537, PLAY0574-PLAY0576, PLAY0602-PLAY0606, PLAY0652, PLAY0671) and PLAY0546, a binder warning, are opt-in and the
editor's own validation never computes them. Several are C#-only (PLAY0532-PLAY0537), so an author in VS Code saw
nothing unless they ran the CLI or MCP by hand (issue 527). The extension already has an opt-in, workspace-trusted
bridge to a user-installed `screenplay mcp` server for repairs.

## Decision

1. The extension offers `Screenplay: Run C# Completeness Checks`. It borrows the repair bridge: same
   `screenplay.repairs.*` configuration, trust, approved model root and server process. It never launches a server of its
   own, and a disabled or unconfigured bridge yields one message and no process.
2. It calls the MCP `diagnostics` tool with `checks: all`, pages through every result (page size 200, bounded) and pins
   `expectedSourceRevision` from the first page. A revision change aborts the run.
3. Only the completeness codes are shown. Everything else the tool returns is already reported by the editor's ordinary
   validation. The `diagnostics` tool does not run the semantic binder, so PLAY0546 comes from the same run's second
   read: `open-workspace`, then `read-workspace` with `view: executable-diagnostics`, pinned to the opened workspace
   revision and catalog, keeping only PLAY0546. The workspace and the `diagnostics` tool number revisions differently,
   so the run pins the `diagnostics` source revision again afterwards; a difference is stale and clears everything.
   If that read is refused or the model does not bind, the binder verdicts are skipped silently.
   The server keeps one active workspace per connection and `open-workspace` clears outstanding proposals, so the
   session refuses a checking `open-workspace` with `SessionBusy` while a repair review is retained, a proposal exists
   or discovery or preview is running. PLAY0546 is therefore skipped, silently, while a repair review is open; the
   completeness findings still publish. A check that reopens the workspace between a discovery and its follow-up read
   changes nothing the repair relies on: every repair call pins the revision it opened, so a differing revision is a
   clean stale refusal, never a mixed review.
   A finding about the whole application carries no path and position 0 (PLAY0537 when screens exist but no navigation
   entry point does). It is shown on the first line of the model's root file, the alphabetically first `.play` file
   found breadth-first under the approved root, and nowhere if the root holds none. Any other finding without a path
   is never placed on a guessed file. Findings are filtered by code before their locations are parsed.
4. Findings go into their own collection, `screenplay-csharp-verdicts`, with source `Screenplay C# check`, apart from
   the editor's diagnostics and from repair findings.
5. A verdict describes the saved file. Editing a document clears its verdicts; an unsaved document gets none; a stale
   revision, a configuration or root change, a disconnect and dispose clear all of them.
   A dead server is replaced by the bridge's own reconnect while connecting, and the run continues on the new
   connection; a server that dies mid-run is reported (explicit command) or logged (on save). The checks tolerate dirty
   buffers, since a document with unsaved edits simply gets no verdicts.
6. Setting `screenplay.verdicts.onSave` (machine scope, off by default) reruns the checks shortly after a save. A failure
   there is logged, never shown; the explicit command shows one concise message.

## Consequences

- Reading is not repairing: the checks add no new authority, and run through the same refusal rules as repairs.
- The editor never invents a verdict; without the bridge the CLI and MCP remain the way to get C#-only checks.
- Verdicts can lag the text between an edit and the next run. That is why edits clear them rather than shifting them.
