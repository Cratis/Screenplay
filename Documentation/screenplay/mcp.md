---
title: MCP server
description: Read Screenplay syntax and review revision-bound workspace changes through the local stdio MCP server.
---

## Command and configuration

```bash
screenplay mcp ./specifications
```

The installed `Cratis.Screenplay.Tool` command starts one local MCP session rooted
at an existing directory. Use a physical path: symbolic links and reparse points,
including ancestors of the root, are rejected. On macOS or Linux,
`screenplay mcp "$(cd ./specifications && pwd -P)"` resolves the directory first.

For VS Code, this complete `.vscode/mcp.json` configuration uses `.play` files in
the workspace's `specifications` directory and a `screenplay` command on `PATH`:

```json
{
  "servers": {
    "screenplay": {
      "type": "stdio",
      "command": "screenplay",
      "args": ["mcp", "${workspaceFolder}/specifications"]
    }
  }
}
```

The root is fixed at launch. Tool arguments cannot select another root. Keep the
client's approval prompt enabled for `apply`: it changes local source files.

## Protocol

The server implements MCP `2025-06-18` over UTF-8 standard input/output. Each
JSON-RPC message occupies one line. Standard output contains protocol responses
only; startup and transport errors go to standard error. Closing standard input
ends the session.

Supported methods are `initialize`, `ping`, `tools/list`, and `tools/call`.
Send `notifications/initialized` after initialization. Other notifications receive
no response. Requests run sequentially; cancellation, progress, resources,
prompts, HTTP transport, pagination, and server-initiated requests are not
implemented. An unsupported requested protocol version negotiates `2025-06-18`.

A complete read-only input transcript is:

```jsonl
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"example","version":"1.0"}}}
{"jsonrpc":"2.0","method":"notifications/initialized"}
{"jsonrpc":"2.0","id":2,"method":"tools/list"}
{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"describe-application","arguments":{}}}
{"jsonrpc":"2.0","id":4,"method":"ping"}
```

Tool results contain both `structuredContent` and equivalent JSON text in
`content`. Unknown methods return `-32601`; invalid arguments and unknown tools
return `-32602`; malformed JSON returns `-32700`. Execution failures return a
tool result with `isError: true`, never a successful empty result.

## Read tools

Reads reload the admitted disk files and use the full syntax compiler. They do
not require executable semantic-model binding and remain available for forms,
screens, reactions, specifications, and other syntax outside that subset.
Invalid source returns diagnostics and any available partial syntax; inspect
`success` rather than treating a partial tree as a valid application.

| Tool | Arguments | Result |
| --- | --- | --- |
| `describe-application` | None | Compact module/feature/slice hierarchy, descriptions, commands and produced events, specification assertion counts, declaration summaries, unresolved or ambiguous indexed references, diagnostics |
| `find-declaration` | `name`; optional `kind` | Exact short-name or dotted-address matches, with original paths, one-based source locations, and concrete syntax |
| `find-references` | `address`, `kind` | Resolved references to exactly one declaration; ambiguous candidates reported separately |
| `merged-document` | None | Merged concrete syntax tree, canonical `.play` text, diagnostics |
| `diagnostics` | None | Full-syntax parser, folder-merge, and reference diagnostics, plus file count |

Names and kinds are case-sensitive. For example, a command declaration can have
address `Projects.Registration.RegisterProject.RegisterProject` and kind
`Command`. Other declaration kinds include `Module`, `Feature`, `Slice`,
`Event`, `Query`, `ReadModel`, `Projection`, `Specification`, and `Form`.

Reference lookup walks AST reference nodes, not source-name matches. Bare names
resolve from the nearest shared module/feature/slice scope outward. Qualified
names match the declared scope suffix. Multiple matches at the nearest scope
remain ambiguous. Supported references cover commands, produced/subscribed
events, queries, read models, types, policies, screens, templates, forms,
specification steps, and named reaction triggers. It does not index identifiers
inside code, property paths, imports, profile settings, or external host
registrations. These limits accompany the result; this is not a whole-program
code reference index.

## Workspace tools

Every argument is a string. `tools/list` returns the argument schemas.

| Tool | Required arguments | Optional arguments |
| --- | --- | --- |
| `open-workspace` | None | `applicationName`, `workspaceJson` |
| `propose` | `expectedRevision`, `expectedCatalogRevision`, `operation` | Operation-specific fields below |
| `expand-layout` | `expectedRevision`, `expectedCatalogRevision` | None |
| `apply` | `proposalId`, `expectedRevision`, `expectedCatalogRevision` | None |

`open-workspace` admits exact UTF-8 documents, including a UTF-8 BOM when present,
and returns `revision`, `catalogRevision`, `workspaceJson`, `semanticSuccess`,
diagnostics, and a document count. Without a name it uses the root directory
name. `workspaceJson` is the canonical `ScreenplayWorkspaceSerializer` envelope:
it preserves exact bytes, document identities, and the authoritative semantic
identity catalog. When reopening it, its complete document set and bytes must
match disk. Reopening discards the session's outstanding proposals.

`propose` accepts one typed workspace operation per call:

| `operation` | Additional required fields |
| --- | --- |
| `update-slice-description` | `semanticId`, `expectedDescription`, `description` |
| `move-document` | `documentId`, `path` |
| `add-document` | `stableKey`, `path`, `bytesBase64` |
| `replace-document` | `documentId`, `bytesBase64` |
| `remove-document` | `documentId` |

Document and semantic IDs come from `workspaceJson`. Paths are portable relative
`.play` paths. `stableKey` is a non-path document key. `bytesBase64` carries a
complete UTF-8 document, not a text patch. Description updates address an
existing single-line quoted slice description by semantic identity and require
its exact current value.

The core transaction validates revisions, parsing, merging, semantic binding,
and identity continuity before producing a candidate. A failed candidate returns
typed conflicts and diagnostics with no proposal ID. Semantic/event identity
rename and retirement migrations are not exposed by this adapter; an operation
requiring them fails rather than silently replacing identities. Removing the
last document is also rejected.

`expand-layout` calls `PlayFileWriter.Expand` and proposes whole-document
replacement/addition/removal operations. It verifies the printed syntax survives
a round trip, allowing only module, feature, and slice group ordering to change.
Expansion normalizes formatting and does not preserve source comments; review
its complete before/after documents. It still requires successful semantic
binding, unlike the read tools.

A successful proposal returns an opaque `proposalId`, exact before/after text
and base64 bytes for every changed document, and serialized before/after
workspaces. It does not write files. `apply` accepts only a proposal created by
the same connection, never a client-supplied write plan. Successful apply advances
the active workspace and invalidates all outstanding proposals.

## Persistence and recovery

Save the proposal's `before.workspaceJson` and `after.workspaceJson` before
applying, then retain the applied workspace envelope. Reopen the matching envelope
to preserve identities across server restarts. Reopening from disk alone performs
a fresh bootstrap and is not an identity-preserving substitute after moves.
Proposal IDs are session-local; after restart, reopen and propose again.

Before applying, the server checks both revisions, the complete `.play` file
set, exact original bytes, portable path collisions, and destination ownership.
It stages replacement bytes, moves originals to backups, and installs without
overwriting occupied paths. On a caught failure, it removes only unchanged
installed output and restores backups without overwriting competing files.

| Status | Meaning |
| --- | --- |
| `Applied N document changes` | All candidate documents verified on disk; active workspace advanced |
| `AppliedWithRetainedBackups` | Documents applied and verified; cleanup failed and `recovery` names retained backups |
| `RolledBack` | Apply failed; original changed documents restored; inspect `recovery` for any retained staging files |
| `RecoveryRequired` | Apply and rollback failed; `recovery` identifies destinations and retained backup paths |

This is a local, trusted-root adapter, not a hostile-filesystem sandbox or a
crash-atomic multi-file transaction. Do not run concurrent writers or rename
root directories during apply. Intermediate changes can be visible to other
processes. Process termination or power loss can leave sibling
`.screenplay-mcp-*.stage` and `.screenplay-mcp-*.backup` files. Preserve them for
manual recovery using the saved before/after envelope. Newly created empty
directories can remain after rollback. No tool follows external `file` references
or executes application code.

## Resource limits

Limits reject the operation; they never silently trim results.

| Resource | Limit |
| --- | --- |
| Request line | 4,194,304 UTF-16 characters; an oversized line terminates the transport |
| JSON request depth | 64 |
| Source files | 1–128 `.play` files |
| Source bytes | 256 KiB per file, 1 MiB combined |
| Source lines | 20,000 per file; 8,192 characters per line; 128 leading whitespace characters |
| Directory traversal | 4,096 entries, 16 levels below the root |
| Reopened workspace envelope | 2 MiB UTF-8 and small enough to fit a reopening request |
| Outstanding proposals | 16; reopen to discard them |

Discovery excludes `.git`, `.ai-work`, `bin`, `obj`, and `node_modules`
directories. These names are also forbidden in write destinations. Other
symlinks and reparse points encountered during discovery are rejected.

Protocol contracts follow the first-party
[MCP stdio transport](https://modelcontextprotocol.io/specification/2025-06-18/basic/transports),
[lifecycle](https://modelcontextprotocol.io/specification/2025-06-18/basic/lifecycle), and
[tools](https://modelcontextprotocol.io/specification/2025-06-18/server/tools) specifications.
