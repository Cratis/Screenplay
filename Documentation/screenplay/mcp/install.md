---
title: Install the MCP server
description: Install the screenplay tool and connect Claude Code, Codex, VS Code, Claude Desktop or another MCP client to one Screenplay application.
---

Use this guide to connect an MCP client to a Screenplay application. You need
Docker (or the .NET 10 SDK), an MCP client that supports local stdio servers, and a
model directory you own.

## Get the server

The server ships in the `cratis/screenplay` Docker image, and in the .NET tool
`Cratis.Screenplay.Tool`. Both contain the same `screenplay` program, including
the MCP server and the event model board. Pick one:

- **Docker** needs only Docker. Nothing else is installed on your machine.
- **.NET tool** needs the .NET 10 SDK and runs the server as a local process. See
  [Use the .NET tool instead](#use-the-net-tool-instead).

Pull the image once before you connect a client, so the first launch is not spent
downloading it:

```bash
docker pull cratis/screenplay
```

`latest` follows the newest stable release. To stay on a version, use its tag, for
example `cratis/screenplay:1.2.3`, in every configuration below.

Cratis CLI hosting shipped in **CLI 3.11.0**. If you have the CLI installed,
use its bundled server without installing a second tool:

```bash
cratis screenplay mcp ./specifications
```

For the MCP App board (`visualize-model`), use **CLI versions that bundle
Screenplay 4.47.0 or later**, or the standalone **Cratis.Screenplay.Tool 4.47.0
or later**. CLI hosting and board support are separate requirements. The host
must also support MCP Apps. See [View a model](view.md).

In a client configuration, use `"command": "cratis"` and
`"args": ["screenplay", "mcp", "/Users/you/work/shop/specifications"]`.
Installing AI guidance alone does not install the server.
`cratis screenplay generate` generates source models; `cratis screenplay mcp`
hosts the MCP server.

Host developers can consult the [embedding API](reference.md#embedding-api).

## Choose one application root

Create a directory for a new model, or use an existing directory containing its
`.play` files:

```bash
mkdir specifications
```

The container sees only the directory you mount. Every configuration below mounts
it at `/model` and starts the server on that path:

```bash
docker run -i --rm -v /Users/you/work/shop/specifications:/model cratis/screenplay mcp /model
```

The server waits for MCP messages on standard input, so the container runs with
`-i` and without `-t`. Starting it by hand does not open a UI. Your MCP client
normally launches and owns the process; when the client stops, the container is
removed.

Use an absolute, physical path on the left of the `-v` mapping. A relative path
fails, because the directory a client starts the command from differs between
clients and between configuration scopes. The server also rejects a root that is a
symbolic link, or that has one in an ancestor. Inside the container `/model` is a
real directory, so the mapping is safe; keep the host path itself free of links. On
macOS and Linux, resolve it with:

```bash
cd specifications
pwd -P
```

On Linux, the container runs as an unprivileged user that may not own the mounted
directory, so `apply` can fail with a permission error. Add
`--user "$(id -u):$(id -g)"` before the image name to write files as yourself.

One root is one application, even when its model spans hundreds of files and
nested features. Keep unrelated applications in separate roots, each with its own
mount and its own server entry.

## Connect your client

Every client launches the same command: `docker run -i --rm -v <root>:/model
cratis/screenplay mcp /model`. Replace the path with your own. If a client cannot
find `docker`, give the absolute path of the executable, which on macOS Docker
Desktop is usually `/usr/local/bin/docker`.

### Claude Code

From a terminal, register the server. Everything after `--` is the command Claude
Code runs:

```bash
claude mcp add --transport stdio screenplay -- \
  docker run -i --rm -v /Users/you/work/shop/specifications:/model cratis/screenplay mcp /model
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
codex mcp add screenplay -- \
  docker run -i --rm -v /Users/you/work/shop/specifications:/model cratis/screenplay mcp /model
```

Or add the table yourself. A `.codex/config.toml` inside a trusted project
scopes the server to that project:

```toml
[mcp_servers.screenplay]
command = "docker"
args = [
  "run", "-i", "--rm",
  "-v", "/Users/you/work/shop/specifications:/model",
  "cratis/screenplay", "mcp", "/model",
]
startup_timeout_sec = 30
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
      "command": "docker",
      "args": [
        "run", "-i", "--rm",
        "-v", "${workspaceFolder}/specifications:/model",
        "cratis/screenplay", "mcp", "/model"
      ]
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
      "command": "docker",
      "args": [
        "run", "-i", "--rm",
        "-v", "/Users/you/work/shop/specifications:/model",
        "cratis/screenplay", "mcp", "/model"
      ]
    }
  }
}
```

Claude Desktop draws MCP Apps views, so it shows the event model board.

### Other clients

Configure `docker` with the same arguments in the client's stdio server
configuration. Do not add flags that write to standard output; the server's
output must carry only protocol messages.

Keep approval enabled for `apply` and `recover-workspace`. Read, inspection and
proposal tools do not publish source changes; those two tools do.

## Use the .NET tool instead

If you would rather run the server as a local process, install the tool. It needs
the .NET 10 SDK:

```bash
dotnet tool install --global Cratis.Screenplay.Tool
screenplay --version
```

To update it later, run `dotnet tool update --global Cratis.Screenplay.Tool`.
Ensure the .NET global-tool directory is on `PATH`.

Then in any configuration above, replace the Docker command with `screenplay` and
the arguments with `mcp` followed by the absolute path of the root, with no
mount. For example, in Claude Code:

```bash
claude mcp add --transport stdio screenplay -- screenplay mcp /Users/you/work/shop/specifications
```

In a JSON or TOML configuration that becomes `"command": "screenplay"` and
`"args": ["mcp", "/Users/you/work/shop/specifications"]`.

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

- **`docker` not found:** verify `PATH` or configure the absolute path of the executable. Check that Docker is running with `docker info`.
- **Server times out on first start:** the image was still downloading. Run `docker pull cratis/screenplay`, or raise the client's startup timeout; Codex uses `startup_timeout_sec`.
- **Permission denied on apply (Linux):** add `--user "$(id -u):$(id -g)"` before the image name.
- **Root rejected:** the path passed to `mcp` must be an existing physical directory, not a symlink or one `.play` file. With Docker that is the container path `/model`, so check the host path on the left of `-v` instead.
- **Empty model although files exist:** the host path on the left of `-v` does not exist, or is misspelled. Docker creates a missing host directory for a `-v` mount instead of failing, so the server opens an empty folder. Check the path exists before you start the client.
- **Missing runtime (.NET tool):** install the .NET 10 SDK and retry `screenplay --version`.
- **Identity conflict:** retain the state file; do not bootstrap over it. Restore externally changed declaration names or paths before making an explicit refactoring proposal.
- **Pending operation:** inspect recovery status before opening or editing the model.

The [MCP reference](reference.md) lists tools, validation policies and limits.
