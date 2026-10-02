---
title: Install the MCP server
description: Install the screenplay tool and connect Claude Code, Codex, VS Code, Claude Desktop or another MCP client to one Screenplay application.
---

Use this guide to connect an MCP client to a Screenplay application. You need the
.NET 10 SDK, an MCP client that supports local stdio servers, and a model
directory you own.

## Install or update the tool

For a first installation:

```bash
dotnet tool install --global Cratis.Screenplay.Tool
screenplay --version
screenplay --help
```

For an existing installation:

```bash
dotnet tool update --global Cratis.Screenplay.Tool
screenplay --version
```

Ensure the .NET global-tool directory is on `PATH`. The MCP server is part of the
same `screenplay` executable; no separate MCP tool or daemon is required.

Cratis CLI hosting and AI-distribution integration are coming in a coordinated
release. They will use the embeddable `Cratis.Screenplay.Mcp` library rather than
require a separate Screenplay tool installation. Until that release is available,
use the standalone setup below; installing AI guidance alone does not install this
executable. The existing `cratis screenplay generate` command generates source
models, not an MCP server.

Host developers can consult the [embedding API](reference.md#embedding-api).

## Choose one application root

Create a directory for a new model, or use an existing directory containing its
`.play` files:

```bash
mkdir specifications
screenplay mcp ./specifications
```

The server waits for MCP messages on standard input. Starting it manually does
not open a UI. Your MCP client normally launches and owns the process.

Use a physical path. Symbolic links, including ancestors of the configured root,
are rejected. On macOS and Linux, resolve the path with:

```bash
cd specifications
pwd -P
```

One root is one application, even when its model spans hundreds of files and
nested features. Keep unrelated applications in separate roots.

Every client below launches the same command: `screenplay mcp <root>`. Give it an
absolute path to the root, because the directory a client starts the server from
differs between clients and between configuration scopes.

## Connect your client

### Claude Code

From a terminal, register the server. Everything after `--` is the command Claude
Code runs:

```bash
claude mcp add --transport stdio screenplay -- screenplay mcp /Users/you/work/shop/specifications
```

The default scope is private to you and the current project, which suits a
machine-specific path. Add `--scope project` only when the path is the same for
everyone, because it writes `.mcp.json` at the project root for you to commit.

Check the registration, then start `claude` and run `/mcp` to see the server's
status and tools:

```bash
claude mcp list
```

Claude Code asks before it calls an MCP tool. To keep the two tools that change
files behind a prompt while you allow the read-only ones, put rules in
`.claude/settings.json`:

```json
{
  "permissions": {
    "allow": [
      "mcp__screenplay__describe-application",
      "mcp__screenplay__search-declarations",
      "mcp__screenplay__find-declaration",
      "mcp__screenplay__declaration-details",
      "mcp__screenplay__find-references",
      "mcp__screenplay__dependencies",
      "mcp__screenplay__diagnostics",
      "mcp__screenplay__read-document",
      "mcp__screenplay__syntax-schema"
    ],
    "ask": [
      "mcp__screenplay__apply",
      "mcp__screenplay__recover-workspace"
    ]
  }
}
```

Claude Code runs in a terminal and does not draw [MCP Apps](https://modelcontextprotocol.io/extensions/apps/overview)
views, so the server does not offer it `visualize-model`. See [View a model](view.md)
for clients that do.

### Codex

Codex reads MCP servers from `~/.codex/config.toml`, shared by the Codex CLI, the
IDE extension and the desktop app. Register the server from a terminal:

```bash
codex mcp add screenplay -- screenplay mcp /Users/you/work/shop/specifications
```

Or add the table yourself. A `.codex/config.toml` inside a trusted project
scopes the server to that project:

```toml
[mcp_servers.screenplay]
command = "screenplay"
args = ["mcp", "/Users/you/work/shop/specifications"]
startup_timeout_sec = 20
default_tools_approval_mode = "writes"
```

The server marks every tool read-only except `apply` and `recover-workspace`, so
the `writes` approval mode prompts for exactly those two. In the Codex terminal UI,
run `/mcp` to list the connected servers. `codex mcp list` shows what is
configured.

Codex is not among the hosts the MCP Apps documentation lists as drawing views. The server offers `visualize-model` only to a host that advertises support, so use a client from [View a model](view.md) when you want the board.

### VS Code

Put this in your workspace's `.vscode/mcp.json`, using a workspace whose physical
root contains `specifications`:

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

GitHub Copilot in VS Code draws MCP Apps views, so the event model board appears
inline in the chat. The [VS Code extension](../vscode.md) is a separate way to see
the same board on a `.play` file and does not need the MCP server.

### Claude Desktop

Add the server to `claude_desktop_config.json` (Settings, Developer, Edit Config)
and restart the app:

```json
{
  "mcpServers": {
    "screenplay": {
      "command": "screenplay",
      "args": ["mcp", "/Users/you/work/shop/specifications"]
    }
  }
}
```

Claude Desktop draws MCP Apps views, so it shows the event model board.

### Other clients

Configure the same executable and arguments using the client's stdio server
configuration. If the client cannot find `screenplay`, use the installed
executable's absolute path rather than changing server protocol output.

Keep approval enabled for `apply` and `recover-workspace`. Read, inspection and
proposal tools do not publish source changes; those two tools do.

## Verify the connection

Ask the client:

```text
List the tools the screenplay MCP server offers.
```

Then ask it to look at an existing model:

```text
Use the screenplay server to describe the application: how many modules,
features and slices does it have, and are there any diagnostics?
```

You should receive compact model counts and a `sourceRevision`, not the entire
model text. For an empty model directory, the answer is an empty application;
continue with [Create a model](create.md). No source is written until an accepted
proposal is applied.

## Keep the model's identity state

After the first apply, keep `.screenplay/identities.json` with the model in source
control. It contains stable identity assignments and document mappings, not a
second copy of your `.play` source. Reopening the same root loads those identities
automatically.

Do not delete a pending-operation marker or backup to make an error disappear.
Inspect `workspace-state`; use explicit recovery only after reviewing its status.
See [identity state and recovery](recovery.md).

## Check common startup failures

- **Executable not found:** verify `PATH` or configure an absolute executable path.
- **Missing runtime:** install the .NET 10 SDK and retry `screenplay --version`.
- **Root rejected:** use an existing physical directory, not a symlink or one `.play` file.
- **Server times out on first start:** raise the client's startup timeout; Codex uses `startup_timeout_sec`.
- **Identity conflict:** retain the state file; do not bootstrap over it. Restore externally changed declaration names or paths before making an explicit refactoring proposal.
- **Pending operation:** inspect recovery status before opening or editing the model.

The [MCP reference](reference.md) lists tools, validation policies and limits.
