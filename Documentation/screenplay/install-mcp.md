---
title: Install the Screenplay MCP server
description: Install the Screenplay .NET tool and connect a local MCP client to a model directory.
---

Use this guide to connect an MCP client to a Screenplay application. You need the
.NET 10 SDK, an MCP client supporting local stdio servers, and a model directory
you own.

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

Host developers can consult the [embedding API](mcp.md#embedding-api).

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
are rejected. On macOS/Linux, resolve the path with:

```bash
cd specifications
pwd -P
```

One root is one application, even when its model spans hundreds of files and
nested features. Keep unrelated applications in separate roots.

## Connect VS Code

Put this configuration in your workspace's `.vscode/mcp.json`, using a workspace
whose physical root contains `specifications`:

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

For other clients, configure the same executable and arguments using their stdio
server configuration. If the client cannot find `screenplay`, use the installed
executable's absolute path rather than changing server protocol output.

Keep approval enabled for `apply` and `recover-workspace`. Read, inspection and
proposal tools do not publish source changes; those two tools do.

## Verify the connection

Ask the client to list the server's tools, then call `describe-application` on an
existing model. You should receive compact model counts and a `sourceRevision`,
not the entire model text. Use its `children` view to navigate modules, features
and slices.

For an empty model directory, call `open-workspace` and then
[create the first typed document](mcp-authoring.md). No source is written until
an accepted proposal is applied.

## Keep the model's identity state

After the first apply, keep `.screenplay/identities.json` with the model in source
control. It contains stable identity assignments and document mappings, not a
second copy of your `.play` source. Reopening the same root loads those identities
automatically.

Do not delete a pending-operation marker or backup to make an error disappear.
Inspect `workspace-state`; use explicit recovery only after reviewing its status.
See [identity state and recovery](mcp-recovery.md).

## Check common startup failures

- **Executable not found:** verify `PATH` or configure an absolute executable path.
- **Missing runtime:** install the .NET 10 SDK and retry `screenplay --version`.
- **Root rejected:** use an existing physical directory, not a symlink or one `.play` file.
- **Identity conflict:** retain the state file; do not bootstrap over it. Restore externally changed declaration names/paths before making an explicit refactoring proposal.
- **Pending operation:** inspect recovery status before opening or editing the model.

The [MCP reference](mcp.md) lists tools, validation policies and limits.
