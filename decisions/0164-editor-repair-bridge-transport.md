---
id: 0164
title: The VS Code repair bridge talks to a user-installed native server over stdio and offers only advertised repairs
status: proposed
stage: implemented
class: convention
reversibility: costly
decided: 2026-11-15
decider: Pending - records behavior already implemented for Cratis/Screenplay#341; a maintainer accepts or amends it
applies-to:
  - Source/Screenplay/VSCodeExtension/**
  - Source/DotNET/Screenplay.Mcp/**
  - Documentation/screenplay/vscode.md
  - Documentation/screenplay/mcp/**
---

## Context

The Screenplay VS Code extension can preview and apply saved-file C# repairs. The C# compiler, not TypeScript, decides whether a repair is safe, so the extension needs a way to reach the C# repair workflow that the MCP server already exposes. This record writes down what the extension does today; it adds no behavior.

## Decision

- **Transport:** the extension starts the configured server as a child process and speaks MCP protocol 2025-06-18 over its standard input and output (newline-delimited JSON-RPC, one in-flight request, a bounded queue). It is deliberately not a general MCP host: no roots, sampling, agents or write automation.
- **Packaging:** the VSIX contains JavaScript only. It does not download a server, install .NET, invoke Docker or run an AI agent. The user installs a native server (an unpacked bundle, the `screenplay` tool or `cratis`) and configures an absolute executable, separate argument strings and one physical model root in User settings. The model root is appended as one argument and no shell is involved. Workspace and folder overrides are refused.
- **Missing executable:** a launch failure is reported as `ProcessUnavailable` and the repair actions are not offered. The extension does not search the project for a server.
- **Unsupported host:** browser and virtual workspaces cannot run the bridge. A remote extension host needs its own native installation and paths on that host; a local binary cannot serve it.
- **Compatibility is the advertised contract, not a version:** the server must list the tools the bridge uses and a v1 `repair-capabilities` document with saved-file-only, journaled apply, evidence v1, structured failures and unsupported cancellation. Without the pinned `PLAY0166` and `PLAY0478` actions the server is refused with `UnsupportedContract`. The server version is identification only.
- **Offered repairs follow the advertisement:** an editor repair is offered only when the server lists its code with the contract the editor implements. `PLAY0516`, `PLAY0661`, `PLAY0664` and `PLAY0469` are optional, so an older server degrades to the repairs it does list. `PLAY0469` must also be advertised as individually reviewed, a contract change that retires semantic addresses, and never fix-all. Anything else is not offered.
- **Apply authority stays in the extension's review:** discovery and proposals write nothing; Apply needs a complete read-only preview of every source and identity-state byte, and a separate explicit confirmation.

## Consequences

- A server release is the unit of compatibility. Adding an editor repair needs a server that advertises it, and the older editor ignores codes it does not know.
- Pinned evidence is unchanged: the server pins `PLAY0166` and `PLAY0478` only, and `PLAY0469` is proposed without a pin.
- Open and not decided here: how the bridge notices a change on disk where the macOS recursive file watcher is silent (an independent rehash at final consent, or VS Code's file-watcher service); a Monaco host-owned bridge; Windows and Linux native gates.
