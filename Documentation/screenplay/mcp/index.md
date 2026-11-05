---
title: Screenplay MCP server for AI assistants
description: The Screenplay MCP server lets an AI assistant explore a local model and propose edits. Review the proposed source before approving apply in your client.
---

The Screenplay MCP server lets an AI assistant explore and edit a local
Screenplay application, a folder of [`.play` files](../folders.md). MCP, the
Model Context Protocol, gives the assistant tools to read the model and propose
changes you can review before writing them to disk.

You describe the task in your own words; the assistant calls the tools. Start by
[connecting your client to one application](install.md), then
[create a model](create.md) or [explore the one you already have](explore.md).
The guides use prompts you can paste into clients such as Claude Code, Codex,
VS Code, Claude Desktop and ChatGPT Desktop.

The server is part of the `screenplay` tool. It runs locally over standard input
and output and makes no network calls of its own. Your assistant's use of a
model provider is separate; a local MCP server does not make the conversation
local.

## What it can do

| You want to | Start with |
| --- | --- |
| Choose a client and connect one local application | [Install the MCP server](install.md) |
| Create a Screenplay model from a description | [Create a model](create.md) |
| Ask what a model contains and how its parts connect | [Explore a model](explore.md) |
| See the model as an event model board | [View a model](view.md) |
| Change, rename, move and repair parts of a model | [Edit a model](edit.md) |

## How changes stay safe

A proposal does not write model source. Use this review cycle before accepting
an edit:

1. Ask the assistant to propose the change. The server validates the candidate
   and returns a `proposalId` for the complete plan.
2. Ask to see the exact before and after source, any comments the change would
   drop, and the diagnostics. Check that the result expresses what you intended;
   language validation is not proof of the business behavior.
3. Ask the assistant to apply that proposal only when you are satisfied. Keep
   your client's approval prompt enabled for `apply` and `recover-workspace`.

The server requires an explicit `apply` call to write a proposal, but does not
ask for human confirmation itself. That approval boundary belongs to your client.
`recover-workspace` can also restore source and state after an interrupted write;
review the [recovery status](recovery.md) before authorizing it.

A proposal is bound to one revision of the model. If a file changes on disk, or
another proposal is applied first, the stale proposal is rejected rather than
merged. Ask the assistant to reopen the model and propose again.

## Where the model lives

The server root is the application boundary: one folder is one application,
however many files and nested features it holds. Confirm this folder when the
assistant opens the workspace so proposals target the model you intended.
The `.play` files remain the source. Applying changes also persists stable
identities in the reserved `.screenplay/` folder; keep that state with the model.
See [identity state and recovery](recovery.md).

## For tool authors and hosts

`apply` and `recover-workspace` report `readOnlyHint: false` and
`destructiveHint: true`: they can replace or restore workspace source and state.
Journaled recovery makes those writes recoverable, not non-destructive. Hosts
should use these hints in their confirmation flow; the server does not add a
second confirmation parameter. Non-writing tools retain non-destructive hints,
and all tools keep `openWorldHint: false`.

These pages describe the server's tools directly, for people building a client or a
host, or debugging what an assistant did:

- [Authoring with the MCP tools](authoring-tools.md) walks through the typed
  `propose-ast` workflow with complete tool arguments.
- [Identity state and recovery](recovery.md) explains `.screenplay/`, interrupted
  applies and explicit rollback.
- [MCP reference](reference.md) lists every tool, argument, limit and the embedding API.
