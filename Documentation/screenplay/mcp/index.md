---
title: MCP server
description: Let an AI assistant read, draw and edit a Screenplay application through the Model Context Protocol, with every change reviewed before it is written.
---

The Screenplay MCP server connects an AI assistant to a Screenplay application: a
folder of [`.play` files](../folders.md). The assistant can answer questions about
the model, draw it as an event model board, propose edits and, once you approve
one, write it. It is part of the `screenplay` tool, runs locally over standard
input and output, and makes no network calls of its own.

You talk to the assistant in your own words. The assistant calls the server's
tools; you never type a tool call. The guides here are written as prompts you can
paste into Claude Code, Codex, VS Code or Claude Desktop.

## What it can do

| You want to | Start with |
| --- | --- |
| Connect Claude Code, Codex, VS Code or Claude Desktop | [Install the MCP server](install.md) |
| Create a Screenplay model from a description | [Create a model](create.md) |
| Ask what a model contains and how its parts connect | [Explore a model](explore.md) |
| See the model as an event model board | [View a model](view.md) |
| Change, rename, move and repair parts of a model | [Edit a model](edit.md) |

## How changes stay safe

Reading is free. Writing is deliberate and happens in three steps:

1. **Propose.** The assistant describes an edit. The server checks it against the
   exact snapshot it was made from and returns a `proposalId`. Nothing is written.
2. **Review.** You, or the assistant on your behalf, read the exact before and after
   bytes, the comments a change would drop and whether the result still compiles.
3. **Apply.** Only `apply` and `recover-workspace` change files. Keep your client's
   approval prompt on for both.

A proposal is bound to one revision of the model. If a file changes on disk, or
another proposal is applied first, the stale proposal is rejected rather than
merged. Ask the assistant to reopen the model and propose again.

## Where the model lives

The server root is the application boundary: one folder is one application, however
many files and nested features it holds. The `.play` files stay canonical source;
the server adds a reserved `.screenplay/` folder next to them with stable
identities. See [identity state and recovery](recovery.md).

## For tool authors and hosts

These pages describe the server's tools directly, for people building a client or a
host, or debugging what an assistant did:

- [Authoring with the MCP tools](authoring-tools.md) walks through the typed
  `propose-ast` workflow with complete tool arguments.
- [Identity state and recovery](recovery.md) explains `.screenplay/`, interrupted
  applies and explicit rollback.
- [MCP reference](reference.md) lists every tool, argument, limit and the embedding API.
