---
title: AST authoring API
description: Typed syntax serialization, original-document node handles, atomic source transactions, and identity continuity.
---

## Validation contracts

`Cratis.Screenplay.Workspaces.ScreenplayWorkspace` exposes two distinct proposal
contracts. Neither writes files.

| Entry point | Acceptance requirement | Result |
| --- | --- | --- |
| `Propose(WorkspaceTransactionRequest)` | Parse, merge, executable semantic binding, identity continuity | `WorkspaceTransactionResult.Success` |
| `ProposeAuthoring(WorkspaceAuthoringRequest)` | Full-language source validation, structural print/parse fidelity, identity continuity; optionally executable binding | `WorkspaceAuthoringResult.Accepted` |

`WorkspaceAuthoringValidation.Authoring` admits valid source that the executable
backend profile cannot represent. `Executable` additionally requires successful
backend binding. There is no fallback from the latter to the former.

`WorkspaceAuthoringResult` separates `AuthoringDiagnostics`,
`ExecutableDiagnostics`, and `ExecutableReady`. An accepted candidate can have
`Workspace.Compilation.Success == false` under authoring validation. That does
not change the meaning of the existing strict transaction result.

Reference integrity is a separate policy. `WorkspaceAuthoringReferencePolicy.Safe`
rejects new unresolved or ambiguous model references and unintended capture of
existing references. `Draft` explicitly reports unresolved authoring debt; it does
not waive parsing, structural fidelity, identity continuity or binding protection.
An intentional edit to a valid reference target is different from an untouched
reference silently changing meaning. Acceptance never proves business correctness
or executes specifications.

## Typed syntax JSON

The public codec lives in `Cratis.Screenplay.Syntax.Serialization`.

| API | Purpose |
| --- | --- |
| `SyntaxJson.Serialize(SyntaxNode)` | Canonical typed `JsonElement` |
| `SyntaxJson.Deserialize(JsonElement)` | Admit a typed built-in syntax tree |
| `SyntaxJson.StructurallyEqual(left, right)` | Compare structural members and order without source metadata |
| `SyntaxSchema.Kinds` | Discover registered concrete syntax kinds |
| `SyntaxSchema.For(kind)` | Discover the schema for one kind |

The registry explicitly covers the built-in syntax kinds; external subclasses
are not automatically admitted. Unknown kinds, duplicate/unknown members,
incorrect child types, invalid enums and illegal nulls are rejected.

- `kind` identifies a concrete syntax type, such as `EventSyntax`.
- Structural members use camelCase. A CLR member named `Kind` uses `syntaxKind`
  to avoid colliding with the discriminator.
- Missing collections initialize empty; optional null collections normalize to
  empty arrays. Required scalar values must be supplied according to the schema.
- Source locations and description offsets are server-owned, not editable data.
- Ordinary JSON numbers decode as finite `Double` literals, matching the parser.
  Other admitted numeric CLR literal types use typed `literalType`/`value`
  envelopes to retain precision and type.
- Existing code blocks and raw expressions remain language constructs. They are
  not a general escape hatch for untyped AST subtrees and are not executed.

Being a well-typed AST does not guarantee that every hand-built combination can
be expressed in `.play`. Authoring rejects a print/parse round-trip that loses
structural content, even if the printed text itself compiles.

## Original-document handles

`WorkspaceSyntaxIndex.Create(workspace)` indexes original document occurrences,
not only the merged application. Its `Entries` contain:

| Member | Meaning |
| --- | --- |
| `Handle` | `WorkspaceNodeHandle(Revision, Document, Path)` |
| `Parent`, `Member`, `Index` | Original containing occurrence and typed slot |
| `Kind`, `Node`, `Location` | Concrete kind, typed node and source location |
| `Address`, `SemanticId`, `EventContractId` | Existing semantic correspondence where available |

`Path` is a JSON Pointer through typed camelCase members and collection indices.
The empty path identifies a document root. The handle is valid only for its
workspace revision; it is not a persistent semantic identity.

A module or feature can occupy multiple documents. Each header has its own
occurrence handle while representing one logical declaration. A logical rename
must update every fragment. Changing one fragment and leaving the rest behind
is rejected rather than silently becoming a different hierarchy.

## Authoring requests

`WorkspaceAuthoringRequest` requires `ExpectedRevision`,
`ExpectedCatalogRevision`, `Validation`, and `Formatting`.

| Node operation | Inputs |
| --- | --- |
| `AddWorkspaceNode` | Parent handle, expected parent, child member, typed node, optional insertion index |
| `ReplaceWorkspaceNode` | Target handle, expected node, replacement node |
| `RemoveWorkspaceNode` | Target handle and expected node |
| `MoveWorkspaceNode` | Target/expectation and destination parent/expectation/member/index |

All handles and expectations address the **base snapshot**. Multiple disjoint
edits to the same document compose in memory; intermediate states do not need
to compile. The final complete application does.

Overlapping subtree edits, incompatible slots, moving a node into itself and
conflicting insertion boundaries are rejected. Replace one containing subtree
when a set of child changes cannot be expressed as disjoint operations.

The `Documents` array accepts `CreateWorkspaceSyntaxDocument` and
`ReplaceWorkspaceSyntaxDocument` with complete `ApplicationSyntax`, plus document
moves, stable-key renames and removals. A typed replacement can repair a
parser-invalid document without node handles while preserving its document identity.
It does not accept raw byte replacements or semantic text patches. Those remain
available through the separate strict whole-document transaction API.

`ScreenplayWorkspace.CreateEmpty(applicationIdentity, applicationName)` starts a
new authoring workspace without relaxing ordinary nonempty workspace admission.
Its first transaction can create one or several typed documents. An empty
workspace is not executable.

## Model-aware rename

`ScreenplayWorkspace.ProposeRename(WorkspaceRenameRequest)` plans one logical
rename across source files. Supply both expected revisions, a declaration handle,
`ExpectedName`, and `NewName`. It derives the required assigned-identity migrations
rather than asking callers to rebuild them by hand.

Supported declaration domains include concepts, types, commands, events, read
models, queries, modules, features and slices. Module/feature fragments change
together. Proven typed references and qualified descendant references are repaired;
read-model output aliases remain distinct from projection builder identities.

The planner rechecks bindings after the change. Name collisions, ambiguous
references, capture, affected opaque realizations/imports, unsupported spans and
resolver disagreement reject rather than guess. It is not a global text replace
or a general property-schema refactor. Low-level typed operations remain available
for explicit coordinated edits outside the automatic planner's supported cases.

The default rename formatting is `PreserveTrivia`: verified byte patches retain
comments, BOM, line endings and every byte outside the proved member spans.
Unsupported changes require an explicit canonical formatting choice; semantic
uncertainty cannot be waived by that choice.

## Identity continuity

Explicit mappings are carried in `SemanticRenames`, `EventRenames`,
`RetiredSemanticAddresses`, and `RetiredEventAddresses`.

- Assigned identities survive explicit declaration/address migrations.
- Removing an assigned declaration requires explicit retirement.
- Rename/reparent operations must account for assigned descendants and both
  semantic and event-contract identity where applicable.
- Similar names or nearby locations never imply identity continuity.
- Existing assignments survive backend-unavailable authoring candidates.
- Newly authored declarations in an unbindable model do not acquire invented
  stable semantic identities. Their occurrence handles remain usable.

Low-level AST operations do not automatically repair references. Supply coordinated
typed edits and migrations, or use `ProposeRename` for the supported model-aware
operation. Neither path guesses identity continuity.

## Source and persistence

`WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments` explicitly permits
canonical printing and comment/trivia loss in changed documents. Untouched
source stays byte-identical; touched documents preserve their UTF-8 BOM policy.
`PreserveExactSource` rejects syntax changes requiring printing. `PreserveTrivia`
applies only byte patches whose result reparses to the complete intended AST;
unsupported changes reject without a canonicalization fallback.

Successful results provide the candidate workspace and exact `WorkspaceWritePlan`
before/after documents. The destination adapter owns file application and failure
recovery. Atomic proposal acceptance is not a promise of crash-atomic file writes.

Persist with `ScreenplayWorkspaceSerializer`; the envelope includes exact source,
revision and authoritative identity catalog. The [MCP server](mcp.md) persists
identity state beside source automatically during apply, and provides paged
canonical export, reviewed application and explicit interruption recovery.
