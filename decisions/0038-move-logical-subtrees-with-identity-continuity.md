---
id: 0038
title: Move logical slices and features across parents with identity and behavior continuity
status: accepted
stage: none
class: product
reversibility: costly
decided: 2026-10-08
decider: Sindre Alstad Wilting (delegated to the implementing agent's recommendation)
applies-to:
  - Source/DotNET/Screenplay/Workspaces/**
  - Source/DotNET/Screenplay.Mcp/**
  - Documentation/screenplay/mcp/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

A semantic address includes its parents. Moving a subtree therefore changes the address of every assigned descendant. Manual `propose-ast` moves require explicit semantic and event-contract migrations, discovered by enumeration. [#386](https://github.com/Cratis/Screenplay/issues/386) asks for the same safety as `propose-rename`, including logical parents split across documents. The exact-address refusal improvement (cab4536d) is already on main; this change builds on it rather than duplicating it.

## Decision

`propose-move` names `target` and `newParent` by kind and logical address. Optional occurrence handles must resolve to those same logical declarations. The first release moves slices to features and features to modules or features. Modules and declarations moving between slices are not supported; the latter changes slice-owned event contracts and production ownership and is deferred.

The planner reuses `WorkspaceRefactoring`. It rewrites the address prefix and derives `semanticRenames` for every assigned declaration whose address changes, and `eventRenames` for every affected event contract. Event names and explicit identity pins stay unchanged. Qualified typed references, including `depends on` container references and interaction `navigate to`, `execute` and `refresh` operands, are repaired. Named-behavior parameter operands remain parameters; declaration-valued arguments are bound and repaired at their `uses` site. A parameter used across multiple target domains is refused with its behavior and argument named. Collision, capture, no-op, move into a descendant and disallowed destination kind are structured refusals naming the affected addresses.

The candidate executable semantic model must equal the original modulo address migrations. Changes to inherited `authorize` or screen `on`/`uses` interaction bindings are always refused in this release, with differences reported. Interaction continuity compares resolved targets, not only attachment text, including the targets supplied through named-behavior arguments. No identity may be lost: the proposal reports `identityMigrations` (semantic and event, previous to current), `retired: []`, `referenceRepairs` and `fragmentsMoved`. Revision, formatting and validation arguments follow `propose-rename`; publication uses the existing `apply` tool.

Every physical fragment of the logical subtree moves in one proposal. Inline and restated-header fragments stay in their documents, with only ancestor headers changed. Only ancestors inside the document's import-placement scope are header-less wrappers; restated ancestors outside that scope retain authored headers. A placing import is moved only when it owns the moved fragment directly, not merely an ancestor. Import placement is supported only for literal paths and a single destination: globs matching files outside the subtree and split destinations without a single own-named file are refused. Files are never relocated; `expand-layout` can realign them later. An emptied wrapper fragment with no metadata is removed; the old logical parent is never deleted.

## Options considered

- **Logical addresses and the rename planner (chosen).** A single address names every fragment and shares identity and reference safety machinery.
- **Handles alone.** Rejected: a handle identifies one physical occurrence, not an entire split feature.
- **Relocate files or insert everything into a destination fragment.** Rejected: placement and layout are separate concerns; split destinations are ambiguous.
- **Allow inherited behavior changes with a warning or opt-in.** Rejected for this release: restructuring must preserve behavior.
- **Move declarations between slices now.** Deferred: event-contract and production ownership require a separate contract.

## Default if unanswered

Moves remain coordinated `propose-ast` batches with manually enumerated migrations and reference repairs.

## Timeline and scope

Holds until superseded. In scope: slice and feature moves, split fragments, literal-path import placement, MCP reports, reference repair and continuity checks. Out of scope: module moves, declarations between slices, file relocation and inherited-behavior opt-ins.

## Verification

**Done when** a slice containing events, a command, a read model and specifications keeps every semantic and event-contract identity, and its executable model changes only in addresses; split features move atomically; collisions, capture, descendants, authorization changes and unsafe globs refuse; applying and reopening retains the migrated identities.

**Verify by** workspace and MCP specifications covering migrations, reference repairs, split parents, imports, executable equivalence and structured refusals.

## Consequences

Restructuring no longer requires hand-written identity migrations. Behavior changes remain explicit edits. File-layout alignment is a separate proposal.
