---
id: 0014
title: Diagnostic repairs are typed workspace proposals
status: accepted
stage: implemented
decided: 2026-09-24
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay/Diagnostics/**
  - Source/DotNET/Screenplay.Mcp/**
  - Documentation/screenplay/ast-authoring.md
  - Documentation/screenplay/workspace-transport.md
---

## Context

[#138](https://github.com/Cratis/Screenplay/issues/138) asks for revision-checked semantic patches that humans and AI apply through one transaction contract. Two of its criteria are open. Criterion 2 lists typed `link` operations without saying what a link is. Criterion 4 asks for compiler-authored repair templates that use diagnostic codes and node ids rather than parsing messages. No repair or code-action type exists in `Source/DotNET` today.

The transaction contract exists. A workspace authoring request carries an expected workspace and catalog revision and is refused when either is stale ([`WorkspaceAuthoringTransaction.cs:23-30`](../Source/DotNET/Screenplay/Workspaces/WorkspaceAuthoringTransaction.cs)). Its operations are typed: `AddWorkspaceNode`, `ReplaceWorkspaceNode`, `RemoveWorkspaceNode` and `MoveWorkspaceNode`, each addressed by a `WorkspaceNodeHandle` and checked against the node expected in the base snapshot ([`WorkspaceAuthoring.cs:56-101`](../Source/DotNET/Screenplay/Workspaces/WorkspaceAuthoring.cs)). `ProposeAuthoring` returns a verdict and a write plan without writing anything ([`ScreenplayWorkspace.cs:147-154`](../Source/DotNET/Screenplay/Workspaces/ScreenplayWorkspace.cs)), as `Propose` does for document transactions (lines 139-145). New references are validated by the authoring reference policy, which never silently retargets an existing binding ([`WorkspaceAuthoringReferencePolicy.cs`](../Source/DotNET/Screenplay/Workspaces/WorkspaceAuthoringReferencePolicy.cs)).

## Decision

1. **A repair is a proposal.** A compiler-authored repair is a typed workspace AST operation proposal: one or more `WorkspaceAstOperation`s, keyed by the diagnostic code and the node handle the diagnostic is about.
2. **One contract.** A repair is reviewed and applied through the same revision-checked transaction contract as every other edit. It is previewed as a proposal and written only when its write plan is explicitly accepted, whether a person or an AI asked for it.
3. **No text edits.** Raw text edits are rejected as a repair form.
4. **Link.** "Link" is covered by adding nodes whose references are validated when the candidate compiles. It is not a separate operation.

## Options considered

- **Typed AST operation proposals (taken).** They reuse the one transaction contract, get stale-revision and expected-node checks for free, and meet criterion 4 because they are keyed by code and handle, not by message text.
- **Text edits (LSP-style).** Not taken: they bypass the typed contract, can go stale without detection, and would give humans and AI a second edit path, which criterion 5 rules out.
- **A dedicated `link` operation.** Not taken: a link is a reference inside a node, and adding the node already validates the reference on compile. A second operation would duplicate that check.
- **Repairs applied automatically by the compiler.** Not taken: #138 requires preview without applying, and a repair can be wrong.

## Default if unanswered

Hosts write their own quick fixes as text edits keyed by message text, which breaks when a message changes and bypasses revision checks. `link` stays an undefined criterion, so #138 cannot close.

## Timeline and scope

Settle before any repair template ships, and keep it until superseded.

In scope: the repair shape (diagnostic code, node handle, typed operations), preview and apply through the existing contract, exposure over MCP, and the meaning of `link` in #138.

Out of scope: which diagnostics get repairs first; new workspace operation kinds beyond those a repair needs; editor UI for presenting repairs; a `specification` operation, which #138 tracks separately.

## Verification

**Done when:** At least one diagnostic produces a repair that carries its diagnostic code, a node handle and typed operations. Proposing it against the current revision returns a candidate that compiles without that diagnostic. Proposing it against a stale revision returns a typed stale conflict with no partial change. No repair is expressed as a text edit. #138's `link` criterion cites this record.

**Verify by:** Workspace specs for a repair's shape, its preview, its stale-revision refusal and its apply; an MCP spec that returns the repair for a diagnostic; a reference-validation spec showing an added node with an unresolved reference is refused under the rejecting policy.

## Consequences

Repairs are safe by construction: they can go stale only detectably, and humans and AI apply them the same way. Hosts that want quick fixes consume typed proposals instead of writing their own. Diagnostics that want a repair must carry a node handle, which some diagnostics may not have today.

## Status notes

**2026-09-25 — implemented.** `PLAY0397` on `validate csharp` produces a
revision-bound typed AST repair, previewed through workspace authoring and
exposed through MCP without auto-apply. Adding a node with an unresolved
reference is refused under the safe policy; this is the `link` criterion in
[#138](https://github.com/Cratis/Screenplay/issues/138). Shipped in v4.35.0.
It is not yet `verified`: downstream release and issue acceptance remain open.

## Related issues

Screenplay: [#138](https://github.com/Cratis/Screenplay/issues/138), [#174](https://github.com/Cratis/Screenplay/issues/174), [#128](https://github.com/Cratis/Screenplay/issues/128).
