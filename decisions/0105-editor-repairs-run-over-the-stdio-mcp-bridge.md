---
id: 0105
title: Editor repairs run over the stdio MCP bridge, and a review is re-checked on disk at final consent
status: accepted
stage: none
decided: 2026-10-19
decider: Sindre Wilting
class: product
reversibility: moderate
applies-to:
  - Source/Screenplay/VSCodeExtension/**
  - Source/Screenplay/Monaco/**
  - Source/DotNET/Screenplay.Mcp/**
  - Documentation/screenplay/**
---

## Context

The VS Code extension offers repairs and refactorings that only the C# compiler can prove: a fix that changes
routing, an event contract or a stored identity must be checked against the whole model, not just the open
document (https://github.com/Cratis/Screenplay/issues/341#issuecomment-5966662329). Today the extension starts a
user-configured `Cratis.Screenplay.Tool mcp <root>` process and speaks MCP JSON-RPC over stdio. The server side of
that bridge shipped in #368 (https://github.com/Cratis/Screenplay/issues/341#issuecomment-5979621112), and the
client checkpoint is recorded in https://github.com/Cratis/Screenplay/issues/341#issuecomment-5982109272.

Two questions were left open. The first is how the editor reaches the C# side, and what happens when it cannot: no
executable, an unsupported host, or a server that does not advertise an action. The second is how a stale review is
refused on macOS. A recursive Node `fs.watch` on macOS sometimes delivers no callbacks for nested file changes, so
the native root watcher cannot be the only thing that invalidates a review
(https://github.com/Cratis/Screenplay/issues/341#issuecomment-5991394441).

## Decision

1. **The stdio MCP process bridge is the only transport.** It is persistent, one process per approved model root,
   and version-checked through `repair-capabilities`. There is no language server.
2. **The VSIX stays JavaScript-only.** The user configures the executable. The extension installs no .NET runtime
   and starts no container. A packaged self-contained worker remains possible later behind the same capabilities
   handshake.
3. **Each situation has one behavior:**

   | Situation | Behavior |
   | --- | --- |
   | Repairs setting off, untrusted workspace or virtual workspace | No C# actions. Local TypeScript fixes keep working. |
   | Executable missing or not executable | A typed repair failure. Lightbulb probes stay silent; the explicit refresh command explains it. |
   | Capability or contract mismatch | No unsafe fallback. Fail only on the core v1 invariants; an action the server does not advertise is not offered. |
   | Web extension host or browser Monaco | No process can start. Only a host-owned provider can offer C# actions. |

4. **A review is re-checked on disk at final consent.** The native watcher stays as an early hint. Immediately before
   apply, the client re-reads every previewed file and compares its hash and saved version with what was reviewed;
   any difference refuses the review as stale. The server's exact disk check inside `apply` remains the last layer.
5. **Capabilities grow additively within schema v1.** Each advertised action states `canFixAll`,
   `individualReviewRequired`, `changesEventContract`, `changesRouting` and `retiresSemanticAddresses`;
   `refactorings` advertises inline-event extraction and rename, discovered through `read-workspace` with
   `view: "refactorings"`. Clients ignore fields they do not know. A change to the meaning of an existing field needs
   a new schema version.

## Options considered

- **A language server.** Rejected for now. It would duplicate the proposal store, journaling and recovery that the MCP
  server already owns, and it would reopen the unsaved-buffer question this issue defers.
- **Bundle or install the .NET tool with the extension.** Rejected for now. A silent install of a runtime or container
  is a side effect the user did not ask for; a configured executable keeps that choice with the user.
- **Move invalidation to VS Code's file-watcher service.** Rejected. It has shown duplicate invalidations, and changing
  it means re-proving the native gate on every operating system. The re-check at consent makes correctness
  independent of whichever watcher is used.
- **Pin repair evidence for PLAY0469 and extraction.** Not now. Revision and catalog checks plus the exact disk check
  cover the risk; pinned evidence guards against candidate-resolution drift in routing, which these do not have.

## Consequences

- A user without the configured executable, or in a web host, still gets every TypeScript-only fix and never sees a
  C# action that could not run.
- A missed watcher event can no longer lead to applying a stale review; it only makes the refusal arrive at consent
  instead of earlier.
- A client must feature-detect the new fields and actions. An older client that does not read them keeps its own
  per-code review rules, and an older server simply advertises fewer actions.
- Discovered refactoring offers are hints. The proposal stays authoritative and may still refuse.

## Related issues

- https://github.com/Cratis/Screenplay/issues/341
- https://github.com/Cratis/Screenplay/pull/368
