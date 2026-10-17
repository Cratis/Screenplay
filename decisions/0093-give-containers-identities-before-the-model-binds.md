---
id: 0093
title: Give applications, modules, features and slices identities before the model binds
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Workspaces/ScreenplayWorkspace.cs
  - Source/DotNET/Screenplay/Workspaces/WorkspaceAuthoringIdentity.cs
  - Source/DotNET/Screenplay/Workspaces/WorkspaceSyntaxAddresses.cs
  - Source/DotNET/Screenplay/Workspaces/PersistedWorkspaceOpener.cs
  - Source/DotNET/Screenplay/Workspaces/ModelNotes*.cs
  - Source/DotNET/Screenplay.Mcp/McpNotes*.cs
  - Source/DotNET/Screenplay.Mcp/McpWorkspaces*.cs
  - Documentation/screenplay/model-notes.md
  - Documentation/screenplay/mcp/**
---

## Context

Model notes anchor only to catalog identities ([0045](0045-model-notes-sidecar.md)). The catalog gains assignments only
when semantic compilation succeeds (`ScreenplayWorkspace.cs:443`), and authoring proposals on an unbound model invent
no new identities (`WorkspaceAuthoringIdentity.cs:42-50`). A slice written since the model last bound therefore has no
identity, and a note on it lands on the nearest identified owner, often the application, with the substitution reported
(#784, #795). Studio shows notes per slice (StudioIssues#563), and agents ask about slices while modelling, when the
model is least likely to bind (#885). Container addresses are names: source indexing derives them without binding
(`WorkspaceSyntaxAddresses.cs:42-50`), the same way the binder does (`SemanticCompilation.cs:174-198`). A new
assignment's ID is a deterministic hash of its address with origin `LegacyBootstrap` (`SemanticIdentityCatalog.cs:459`,
enforced at 688).

## Decision

1. **Scope.** Module, Feature and Slice declarations get identities from the authoring hierarchy when the model does
   not bind. The Application already anchors as `{ "application": true }`.
2. **Freeze ([0092](0092-the-language-is-frozen-at-esm-v28.md)).** Only catalog materialization changes: no grammar,
   binder, ESM, diagnostic or `notes.json` change.
3. **Derivation.** An unbound container gets exactly the assignment binding would make: its source-indexed address,
   `SemanticId.Create(address)`, origin `LegacyBootstrap`. It is made only where the catalog has no assignment at that
   address. An existing assignment, including a rename-carried `Persisted` one, is never re-derived.
4. **Continuity (MUST).** Binding keeps every assignment whose address is unchanged
   (`SemanticIdentityCatalog.cs:439-460`). An element keeps the identity it showed while unbound, and the bound
   catalog is byte-identical to the one the same source gets with no pre-binding assignments.
5. **When and what is persisted.** Assignments are computed in memory whenever an unbound workspace is created or
   proposed, and they move its catalog revision. `.screenplay/identities.json` (root-local identity state) receives
   them only on `apply`, through the state plan that source and notes proposals already carry
   (`McpWorkspaces.Notes.cs:46`). Opening never writes. Reopening persisted state admits exactly one difference: added
   pre-binding container assignments on a model that does not bind. Every other drift still refuses with
   `IdentityReconciliationRequired` (`PersistedWorkspaceOpener.cs:86-100`) or `IdentityImportConflict`
   (`McpWorkspaces.cs:98`).
6. **Completeness gate.** New assignments are made only when every document parses and places, because indexing skips
   documents that do not (`WorkspaceSyntaxIndex.cs:161-164`) and the address set would be partial. Existing assignments
   stay. This record serves models that parse but do not bind.
7. **Ambiguity: no identity, never a guess.** A container address gets no new identity, and is reported, when more than
   one logical declaration claims it, or when its deterministic ID is already held by another assignment (for example
   one renamed away from it; the catalog rejects duplicate IDs at `SemanticIdentityCatalog.cs:695`). Split fragments
   and placement wrappers of one Module or Feature are one declaration; two Slices with one address are ambiguous. A
   note request naming an ambiguous container is refused with the reason, not substituted. Authoring proposals that
   create duplicate slices stay refused (`WorkspaceSyntaxAddresses.cs:113-116`).
8. **Rename and move are unchanged.** `propose-rename` and `propose-move` derive migrations from assigned identities
   (`WorkspaceRefactoring.cs:395-398`, `WorkspaceRefactoring.Move.cs:313`) and already accept unbound models
   (`WorkspaceRefactoring.Move.cs:116-119`). Pre-binding containers migrate like bound ones and notes follow without
   rewrites (0045). Manual `propose-ast` still needs explicit `semanticRenames` or retirements.
9. **Removal before binding** is a retirement under the existing rules: explicit `retiredSemanticAddresses`, with
   `orphanedNotes` reported before `apply`. The notes are kept as orphans, never deleted (0045).
10. **Re-adding the same address gives the same identity again**, because the derivation is deterministic, so orphaned
    notes anchored to that ID resolve again. Bound models already behave this way, since the catalog keeps no
    tombstones. A fresh ID would need tombstone state that changes bound catalog bytes. Exception: an identity that
    arrived by rename carries an ID from an older address, so its notes stay orphaned until an explicit `reanchor`.
11. **Only containers.** Commands, events, read models, projections, queries, specifications, reactions, captures and
    properties keep requiring binding. Event contracts take a contract ID and revision from bound generations
    (`ScreenplayWorkspace.cs:490-495`), every event address needs a semantic assignment, and property and
    generation-qualified addresses need resolved owners. Assigning them from syntax risks an identity that binding
    contradicts, which breaks item 4. Their notes now anchor to their own slice (reported substitution, 0045).
12. **Reporting.** `open-workspace` and `workspace-state` list each container without an identity and the reason:
    `ambiguous`, `idCollision`, `unparsed` or `unplaced`. The notes tools return the same reason when they refuse.

## Options considered

- **Note-only IDs or authoring-key anchors.** Rejected by 0045: the planners would have to re-key them.
- **Refuse notes on unbound models.** Honest, but useless at the moment notes are needed.
- **Assign every declaration from syntax** (item 11), **fresh IDs on re-add** (item 10), **write state on open**
  (open never writes identity state today). Rejected.

## Default if unanswered

Container notes on unbound models keep landing on the nearest identified owner, often the application, with the
substitution reported.

## Timeline and scope

Holds until superseded, within the v28 freeze. In scope: materialization, reopen reconciliation, notes resolution, MCP
reporting and documentation. Out of scope: in-slice declarations, the `notes.json` schema, release baselines and
structural comparison (they still need a bound model), and the TypeScript compiler.

## Verification

**Done when** these specs ship:
- In an unbound model with a split Module, a nested Feature and a Slice, a note on each anchors to that element with no
  substitution, through dotted, typed and handle targets.
- Fixing the binding error keeps the same IDs and the notes stay attached. The catalog bytes equal those of the same
  source bound with no prior state.
- Renaming or moving an unbound Slice or Feature keeps its notes through derived migrations, across apply and reopen.
- Removing a container before binding orphans its notes, reports them before `apply` and keeps them. Re-adding the
  address resolves them again.
- Duplicate slice addresses, an ID collision, and a document that does not parse each assign no identity, report it
  and refuse the note request.
- Reopening older persisted state on an unbound model adds only container assignments; any other drift refuses.
- A model that binds is byte-identical: identity state, catalog revision and workspace revision match current goldens.

**Verify by** `Workspaces/for_ScreenplayWorkspace`, `Workspaces/for_ModelNotesAnchors`, `Screenplay.Mcp/for_ModelNotes`
and the MCP recovery specs.

## Consequences

Notes land where the author meant them while modelling. On unbound models the catalog revision moves as containers
appear, and a pre-binding identity in a document that later stops parsing blocks reopen, as a bound identity does today.
New report fields regenerate the 0039 golden contract. Cross-repo: Studio's per-slice notes view works on unbound models
(StudioIssues#563). The Cratis/AI `cratis-screenplay-model-authoring` skill must say that container notes no longer need
binding while in-slice notes go to their slice; the implementing PR opens that issue.

## Related issues

#885, #784, #795, StudioIssues#563; decisions 0038, 0039, 0045, 0092.
