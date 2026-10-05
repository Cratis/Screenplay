---
title: Install the MCP server
description: Install Screenplay desktop bundles or a local ChatGPT plugin, with Docker and .NET options for advanced and headless clients.
---

Use this guide to connect an MCP client to a Screenplay application. Screenplay
runs **locally**, against a model directory you own. Desktop packages include their
runtime: you do not need Docker, a .NET SDK, or a separately installed `cratis`
executable to run a downloaded bundle.

## Desktop installation

Choose the simplest supported channel, in this order:

1. **Curated directory:** when Screenplay is listed, install it from Claude Desktop
   **Settings → Extensions → Browse extensions** or ChatGPT **Plugins**. Directory
   submissions require publisher review; this release does **not** claim an
   approved listing. OpenAI public local-MCP distribution still requires OpenAI's
   explicit support. Until a listing is live, use the download below.
2. **Desktop download:** get the native package from the
   [Screenplay releases](https://github.com/Cratis/Screenplay/releases).
3. **Advanced/headless clients:** use Docker, the .NET tool, or manual stdio
   registration below. These options remain supported.

### Claude Desktop bundle

Download `screenplay-VERSION-RID.mcpb` and its matching `.sha256` file. Select
`osx-arm64` for Apple Silicon, `osx-x64` for Intel macOS, or `win-x64` for Windows
x64. Verify the checksum before opening it. Bundles currently are unsigned;
checksums from the Cratis-owned HTTPS release verify integrity, not publisher
signing. Review the package's permissions and origin.

Open the MCPB in an up-to-date Claude Desktop. If your OS does not associate the
file, use **Settings → Extensions → Advanced settings → Extension Developer →
Install Extension**. Review the trust dialog, complete installation and enable the
extension. There is no model folder to configure: Screenplay works in the folder
the host shares with it, and otherwise asks Claude to open the folder you name in
the conversation ("open the Screenplay model in ~/Projects/shop"). Check its tools
and version in **Settings → Extensions**. Enterprise device policies can restrict
installation; do not bypass them.

Install an updated bundle to update a privately downloaded extension; remove it
in Claude's Extensions UI. The CLI cannot verify this host-owned state.

### ChatGPT Desktop plugin

Download `screenplay-VERSION-RID-plugin.zip` and its `.sha256`. Use the same native
macOS/Windows RID choices, and a ChatGPT Desktop version/account that exposes local
**Plugins** and stdio MCP. An empty model starts in the plugin's persistent data
folder. To choose an existing model without authoring JSON, use the coordinated
CLI desktop installer (Cratis CLI 3.25.0 or later). First,
[install the Cratis CLI][cli-installation]; you do not need a running Chronicle
store for these desktop commands:

```bash
cratis screenplay mcp install --clients chatgpt --model-root /absolute/path/to/specifications
```

It registers a source in your **personal marketplace**, preserving unrelated
plugins. Restart ChatGPT, open **Plugins**, and install/enable Screenplay from the
local marketplace. This is source registration, not an unattended host install.
Keep confirmation enabled for tools that change files. Local plugins are not a
hosted Screenplay service and are not automatically in the public directory.

For manual local/repository marketplace development, follow the
[maintainer packaging guide](https://github.com/Cratis/Screenplay/blob/main/Source/DotNET/Screenplay.Mcp/README.md#personal-or-repository-marketplace-testing).
Never copy files into an undocumented host-internal cache.

### Install and manage with the Cratis CLI

[Install the Cratis CLI][cli-installation] using the procedure for your operating
system, then check `cratis --version`. Desktop management requires CLI 3.25.0 or
later. These commands do not require a Chronicle connection. For all options and
recovery steps, see [Screenplay desktop MCP lifecycle][cli-desktop-mcp].

```bash
cratis screenplay mcp install --clients claude,chatgpt --model-root /absolute/path/to/specifications
cratis screenplay mcp status
cratis screenplay mcp update --clients claude,chatgpt
cratis screenplay mcp uninstall --clients chatgpt
```

Omit `--clients` in an interactive terminal to choose detected supported hosts.
Noninteractive install, update and uninstall require explicit clients; status
can inspect all supported clients. Add `--dry-run` to preview
without downloading, writing or launching; release metadata may still be checked.
`--version VERSION` pins a Screenplay release independently of the CLI version.
Claude needs no model folder: the server picks the folder the host or the
conversation names. `--model-root` configures the ChatGPT source. Status distinguishes source registration/handoff from a
verified host install and reports available updates when the release check works.
Host-owned removal still happens in each host's UI.

Linux self-contained packages are available for compatible local MCP clients,
but Claude/ChatGPT Desktop support on Linux is not claimed. Windows arm64 is not
packaged. Docker and the .NET tool below are the portable/headless alternatives.

## Get the server for advanced clients

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
use its bundled `Cratis.Screenplay.Mcp` library without installing a second tool:

```bash
cratis screenplay mcp ./specifications
```

For the MCP App board (`visualize-model`), use **CLI versions that bundle
Screenplay 4.47.0 or later**, or the standalone **Cratis.Screenplay.Tool 4.47.0
or later**. CLI hosting and board support are separate requirements. The host
must also support MCP Apps. See [View a model](view.md).

In a client configuration, use `"command": "cratis"` and
`"args": ["screenplay", "mcp", "/Users/you/work/shop/specifications"]`.
Project-local AI registration and user-level desktop installation are separate
operations. Installing AI guidance alone does not install a desktop extension or
the server. `cratis screenplay generate` generates source models;
`cratis screenplay mcp` hosts the MCP server.

Host developers can consult the [embedding API](reference.md#embedding-api).

## Repair-capable server setup

The server exposes a [repair contract v1](reference.md#repair-contract-v1-and-pinned-evidence)
for saved-file `PLAY0166` and `PLAY0478` proposals. The experimental
[VS Code repair bridge](../vscode.md#preview-saved-file-c-repairs) is a separate,
explicitly configured process client; browser Monaco does not spawn a process.
Registering Copilot MCP or installing a desktop plugin does not enable editor repairs.

An editor host must explicitly approve an executable and one existing physical
application root. Supported process forms are `cratis screenplay mcp ROOT`,
`screenplay mcp ROOT`, or the `server/Cratis.Screenplay.Tool` binary from a complete,
checksum-verified unpacked native bundle (`.exe` on Windows). Launch without a shell
and pass the approved root as one argument. Do not use a project's executable,
download on startup, inspect desktop-private caches or fall back to Docker silently.
Native bundles include their runtime; the .NET global tool needs the installation
below. Linux packages require compatible native dependencies; Windows arm64 is not
packaged. Checksums do not provide publisher signatures.

A compatible host checks `repair-capabilities` after the MCP handshake and refuses
older or incompatible contracts rather than downgrading evidence protection. Both
initial actions require pinned mode in that host, complete preview and explicit
Apply. CLI 3.25.0 is the desktop-management prerequisite only; it does not prove
repair-contract support in an embedded compiler. Check the actual server's tool
and contract, and only claim compatibility for distributions containing it. The
capability-supporting server must be released before a CLI or bundle embedding it
can be called compatible; no specific CLI release is established here.

For VS Code, configure these exact launch forms as an absolute executable plus
separate argument-prefix strings in **User settings**:

| User-installed executable | `screenplay.repairs.arguments` | Effective invocation |
| --- | --- | --- |
| Absolute `cratis` path | `["screenplay", "mcp"]` | `cratis screenplay mcp /absolute/model/root` |
| Absolute `screenplay` path | `["mcp"]` | `screenplay mcp /absolute/model/root` |
| Absolute unpacked `server/Cratis.Screenplay.Tool` path (`.exe` on Windows) | `["mcp"]` | `Cratis.Screenplay.Tool mcp /absolute/model/root` |

Set `screenplay.repairs.modelRoot` to that existing physical directory and
`screenplay.repairs.enabled` to `true`. The extension appends the root, launches
without a shell and verifies the runtime contract. Keep the complete verified
bundle together; do not copy just its binary or read a desktop host's private
installation cache. This setup does not change the Copilot registration below.

## Choose where the model lives

You rarely have to. When the server starts without a root, it picks the folder the
first time it needs one:

1. A `path` the assistant passes to `open-workspace`, which also switches to another
   folder in the same session.
2. The folder your MCP client offers through its workspace roots, when the client
   offers exactly one. When it offers several, the assistant is told to choose with
   `path`. When the client changes its roots, a folder bound from them is let go.
3. The folder the server was launched from, when it already holds `.play` files or a
   `.screenplay` folder. This is what a terminal client such as Claude Code or Pi
   gives you: start it in the project and the model is the project.

```bash
screenplay mcp
```

If none of these applies, the server says so and asks for a path instead of guessing
at your home folder. In Claude and ChatGPT desktop the host manages the files, so
the model lives wherever the host puts them, and `open-workspace` with
`workspaceJson` carries a model between sessions.

Pass a root, as below, when you want one fixed folder for every session. A fixed-root
connection refuses `open-workspace.path` naming a different physical directory
with `RootChangeRefused`. A case alias is accepted only when native directory
identity proves it is the same folder, after the symbolic-link and reparse-point
guards; the originally approved root stays bound. Changing applications requires
a new authorized connection.

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

Read, inspection and proposal tools do not publish source changes; `apply` and
`recover-workspace` do. The server does not mark them destructive, because both are
journaled and recoverable, so a client that confirms every destructive call does not
ask each time. Keep approval on for them where you want a deliberate step, and
allow them where you would rather just say what you want.

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

For an existing model, you should receive compact model counts and a
`sourceRevision`, not the entire model text. Application-read tools can report
that an empty root contains no `.play` files; continue with
[Create a model](create.md) to open a workspace and propose its first documents.
No source is written until an accepted proposal is applied.

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

[cli-installation]: /cli/getting-started/#install-it
[cli-desktop-mcp]: /cli/reference/screenplay-desktop-mcp/
