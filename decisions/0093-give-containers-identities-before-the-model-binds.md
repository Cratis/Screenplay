---
id: 0093
title: Give applications, modules, features and slices identities before the model binds
status: accepted
stage: none
decided: 2026-10-17
decider: Sindre Wilting (delegated to the L0 coordinator)
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
when semantic compilation succeeds (`ScreenplayWorkspace.cs:443`), and proposals on an unbound model invent none
(`WorkspaceAuthoringIdentity.cs:42-50`). A slice written since the model last bound has no identity, so its note lands
on the nearest identified owner, often the application, as a reported substitution (#784, #795). Studio
(StudioIssues#563) and agents need slice notes while modelling (#885). Container addresses are names: indexing derives
them without binding (`WorkspaceSyntaxAddresses.cs:42-50`) as the binder does (`SemanticCompilation.cs:174-198`), and
a new ID is a deterministic address hash, origin `LegacyBootstrap` (`SemanticIdentityCatalog.cs:459`, enforced at 688).

## Decision

1. **Scope.** Module, Feature and Slice declarations get identities from the authoring hierarchy when the model does
   not bind. The Application already anchors as `{ "application": true }`.
2. **Freeze ([0092](0092-the-language-is-frozen-at-esm-v28.md)).** Only catalog materialization changes. The grammar,
   binder, ESM, diagnostics and `notes.json` stay as they are.
3. **Derivation.** An unbound container gets exactly the assignment binding would make: its source-indexed address,
   `SemanticId.Create(address)`, origin `LegacyBootstrap`. It is made only where the catalog has no assignment at that
   address. An existing assignment is never re-derived.
4. **Continuity (MUST).** Binding preserves every existing ID and origin (`PlanSemantics`,
   `SemanticIdentityCatalog.cs:439-460`), so an element keeps the identity it showed while unbound. A rename or move
   before the first binding follows the migration as for bound models: A assigned unbound and renamed to B keeps A's ID
   with origin `Persisted`, not hash(B). Bound catalogs are byte-equal only when both paths share the same earlier
   assignments and migrations and differ only by deterministic pre-binding materialization.
5. **Persistence.** Assignments are computed in memory when an unbound workspace is created or proposed, and they move
   its catalog revision. `.screenplay/identities.json` (root-local identity state) receives them only on `apply`,
   through the state plan that source and notes proposals already carry (`McpWorkspaces.Notes.cs:46`), under the same
   journal and rollback. Opening never writes.
6. **Reopen exception (exact predicate).** Opening persisted state from disk on a model that does not bind is admitted
   only when every earlier assignment, origin, event revision, document mapping and application metadata value is
   unchanged, every earlier assignment is still present in source, and the only change is collision-free deterministic
   container additions from source in which every document parses and places. Both reconciliation checks
   (`PersistedWorkspaceOpener.cs:86-100`) refuse any other drift. `IdentityImportConflict` (`McpWorkspaces.cs:98`) is
   not waived for a supplied `workspaceJson`.
7. **Completeness gate.** Indexing skips documents that do not parse or place (`WorkspaceSyntaxIndex.cs:161-164`), so
   their containers cannot be enumerated. In that state no new assignment is made anywhere, including for valid
   containers in other documents, because the full address set (and so ambiguity and collisions) is unknown. Existing
   assignments stay.
8. **Ambiguity before assignment.** An address claimed by more than one logical declaration (two Slices; split fragments
   and placement wrappers of one Module or Feature count as one) gets no new identity. So does an address whose
   deterministic ID another assignment already holds (`SemanticIdentityCatalog.cs:695`). Authoring proposals that
   create duplicate slices stay refused (`WorkspaceSyntaxAddresses.cs:113-116`).
9. **Ambiguity after assignment.** A duplicate created outside MCP for an identified container is not checked today:
   catalog-first targets resolve it (`McpNotesTargets.cs:47-69`). From now on, existing assignments and notes are kept
   and nothing is retired or orphaned. The address is reported as ambiguous whether it is assigned or not. New note
   targeting is refused through every form (`semanticId` anchor, dotted, typed and handle), including descendants whose
   owner is ambiguous. Existing notes on it stay resolved and carry `ambiguous: true`; they are not orphans.
10. **Rename and move are unchanged.** `propose-rename` and `propose-move` derive migrations from assigned identities
    (`WorkspaceRefactoring.cs:395-398`, `WorkspaceRefactoring.Move.cs:313`) and already accept unbound models
    (`WorkspaceRefactoring.Move.cs:116-119`). Pre-binding containers migrate like bound ones, and notes follow without
    rewrites (0045). Manual `propose-ast` still needs explicit `semanticRenames` or retirements.
11. **Removal before binding** is an explicit retirement (`retiredSemanticAddresses`), with `orphanedNotes` reported
    before `apply`. The notes are kept as orphans, never deleted (0045).
12. **Re-adding an address**, subject to items 7 and 8, recreates its deterministic `LegacyBootstrap` assignment. The
    catalog keeps no tombstones, and bound models behave the same way. Orphaned notes resolve again only when the
    recreated assignment has their exact ID. Notes on identities that were not derived from that address (carried by
    a rename, or assigned externally) need an explicit `reanchor`.
13. **Only containers.** Commands, events, read models, projections, queries, specifications, reactions, captures and
    properties keep requiring binding: event contracts take IDs and revisions from bound generations
    (`ScreenplayWorkspace.cs:490-495`), and property and generation-qualified addresses need resolved owners, so syntax
    could assign identities binding contradicts. Their notes now go to their own slice (reported substitution, 0045).
14. **Reporting.** `open-workspace` and `workspace-state` report each container without an identity (`ambiguous`,
    `idCollision`), each ambiguous assigned address, and each document that does not parse (`unparsed`) or place
    (`unplaced`), stating that no new identities are assigned until those documents do. Notes tools refuse with the
    same reasons.
15. **Recovery guidance.** If a partial catalog has been persisted and the binding error is then fixed outside MCP, the
    first binding adds identities on open, so reopening can require reconciliation, as it does for bound models today.
    Make the fix through an MCP proposal.

## Options considered

Rejected: note-only IDs or authoring-key anchors (0045: the planners would have to re-key them); refusing notes on
unbound models (useless when notes are needed); assigning every declaration from syntax (item 13); fresh IDs or
tombstones on re-add (item 12); writing state on open (open never writes identity state today).

## Default if unanswered

Container notes on unbound models keep landing on the nearest identified owner, as a reported substitution.

## Timeline and scope

Holds until superseded, within the v28 freeze. In scope: materialization, reopen reconciliation, notes resolution, MCP
reporting and documentation. Out of scope: in-slice declarations, the `notes.json` schema, release baselines and
structural comparison (they still need a bound model), and the TypeScript compiler.

## Verification

**Done when** these specs ship:
- Unbound split Module, nested Feature and Slice: a note on each anchors to that element without substitution, through
  dotted, typed and handle targets. Fixing the binding error through MCP keeps IDs, origins and notes.
- Assign A, rename to B, bind: B has A's ID, origin `Persisted`; the same for move; notes follow across apply/reopen.
- Removal before binding orphans, reports before `apply` and keeps notes; re-adding resolves only exact-ID notes.
- Duplicate slices, an ID collision, or an unparsed or unplaced document: no new identity, reason reported, refused.
- Duplicating an identified Slice keeps its assignment and notes, reports it ambiguous, and refuses new targeting of
  it and its descendants in all four forms.
- Reopen: older state on an unbound model admits only the item 6 delta, and any other drift refuses. A supplied
  `workspaceJson` still refuses. Repeated opens give the same catalog and workspace revisions.
- Recovery: an interrupted or unknown-outcome notes `apply` that also persists new identities rolls back both the
  sidecar and the identity state. Competing source and notes proposals: the second one refuses on stale revisions.
- Goldens: models that always bound are byte-identical (identity state, catalog and workspace revisions). Histories
  that were unbound are checked against item 4's equality condition, not against the always-bound goldens.

**Verify by** `Workspaces/for_ScreenplayWorkspace`, `Workspaces/for_ModelNotesAnchors`, `Screenplay.Mcp/for_ModelNotes`
and the MCP recovery specs.

## Consequences

Notes land where the author meant them while modelling. Unbound catalog revisions move as containers appear, and a
pre-binding identity in a document that later stops parsing blocks reopen, as a bound identity does today. New fields
regenerate the 0039 golden contract. Cross-repo: Studio's per-slice notes view works on unbound models and shows
ambiguity (StudioIssues#563). The Cratis/AI `cratis-screenplay-model-authoring` skill must say that container notes no
longer need binding, in-slice notes go to their slice, and binding fixes go through MCP; the implementing PR opens that
issue.

## Related issues

#885, #784, #795, StudioIssues#563; decisions 0038, 0039, 0045, 0092.
