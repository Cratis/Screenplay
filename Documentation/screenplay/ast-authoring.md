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
- Source locations, description offsets and parsed comments are server-owned, not editable data.
- Ordinary JSON numbers decode as finite `Double` literals, matching the parser.
  Other admitted numeric CLR literal types use typed `literalType`/`value`
  envelopes to retain precision and type.
- Inline JSON-shaped objects and lists have typed value nodes and individually
  addressable keys. Existing code blocks and raw expressions remain language
  constructs, not an escape hatch for structured data or untyped AST subtrees.

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

## Diagnostic repairs

`WorkspaceDiagnosticRepairs.Find(workspace, revision, diagnostic)` (or the overload
accepting an existing `WorkspaceSyntaxIndex`) discovers compiler-authored
`WorkspaceDiagnosticRepair` values (diagnostic code, original-revision subject
`WorkspaceNodeHandle`, and one or more typed `WorkspaceAstOperation`s). An unknown,
stale or ambiguous diagnostic yields no repair. Repairs never contain text edits and
never write files. Build a `WorkspaceAuthoringRequest` with the current workspace
and catalog revisions, the repair's `Operations`, `Authoring` validation, and
explicit `CanonicalizeTouchedDocuments` formatting consent. Preview with
`ProposeAuthoring`; review the candidate and `WritePlan`, including dropped
comments, then explicitly accept the plan through the usual destination adapter.
Stale requests return typed stale conflicts with no partial candidate.

The first repair handles only `PLAY0397` on a `validate csharp` header. Its parsed
`CodeValidateSyntax` has the unique warning location, and replacing that typed
node with its own original syntax canonically prints `validate` followed by a
`\`\`\`csharp` fence. Other `PLAY0397` forms are not offered: a warning on a
bare description fence or standalone language line does not identify the same
unique subject. Cases requiring a choice, such as selecting an alias for a
repeated read, are never presented as one automatic repair.

A “link” is a reference in an added typed node, not a separate edit operation.
With the default `Safe` reference policy, adding a node with an unresolved
reference is rejected when the candidate is compiled; `Draft` must be explicitly
requested to admit new reference debt. Neither policy silently retargets an
existing binding.

## Model-aware rename

`ScreenplayWorkspace.ProposeRename(WorkspaceRenameRequest)` plans one logical
rename across source files. Supply both expected revisions, a declaration handle,
`ExpectedName`, and `NewName`. It derives the required assigned-identity migrations
rather than asking callers to rebuild them by hand.

Supported declaration domains include concepts, types, **properties of declared
composite types**, commands, events, read models, queries, modules, features and
slices. Other property declarations are not automatic rename targets. Module/feature fragments change
together. Proven typed references and qualified descendant references are repaired;
read-model output aliases remain distinct from projection builder identities.

The planner rechecks bindings after the change. Name collisions, ambiguous
references, capture, affected opaque realizations/imports, unsupported spans and
resolver disagreement reject rather than guess. It is not a global text replace
or a general property-schema refactor beyond composite-type properties. Low-level typed operations remain available
for explicit coordinated edits outside the automatic planner's supported cases.

Opaque syntax (freeform raw expressions, code blocks, imports, file references,
capture sources and template triggers, and form `compose using` callbacks)
refuses a rename only when its text contains the current or new name as a whole
identifier. The scan includes strings and keys inside *opaque* text, but valid
JSON-shaped mapping values are typed: renaming a composite-type property rewrites
only its bound object keys, not string values or keys of another type. The conflict names the document path, line and
column, the syntax pointer, and the matched name.

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
canonical printing and whitespace normalization in changed documents. Parsed comments
stay with their syntax owners, including after a typed-JSON replacement. Untouched
source stays byte-identical; touched documents preserve their UTF-8 BOM policy.
`PreserveExactSource` rejects syntax changes requiring printing. `PreserveTrivia`
applies only byte patches whose result reparses to the complete intended AST;
unsupported changes reject without a canonicalization fallback.

`PreserveTrivia` patches three kinds of change in place:

- An identifier member, such as a declaration name or an event reference, through
  its proven identifier span.
- A literal value (string, number, boolean or `null`) of a `produces` mapping or a
  specification value. Only the literal's authored text is rewritten, so a
  trailing comment on the same line survives, and the value may change type.
- A whole property mapping of a `produces` block or a specification value. When
  only its source changes, only the right-hand side is rewritten; when its target
  property changes too, the mapping text is rewritten up to the end of its source.

Adding, removing or reordering nodes is structural and still requires
`CanonicalizeTouchedDocuments`.

Canonical printing retains attached comments and normalizes blank lines. It preserves
parsed member order within a document; new members follow the insertion rule in
[Printing and generating](printing.md#what-printing-does-not-keep). Before applying such a result, call
`WorkspaceDroppedComments.In(result.WritePlan)` to list any comment that could not be
placed, with its path, line, column and text. The `PLAY0288` warning for each
canonically printed document states the count and lines of comments actually lost.

Successful results provide the candidate workspace and exact `WorkspaceWritePlan`
before/after documents. The destination adapter owns file application and failure
recovery. Atomic proposal acceptance is not a promise of crash-atomic file writes.

Persist with `ScreenplayWorkspaceSerializer`; the envelope includes exact source,
revision and authoritative identity catalog. The [MCP server](mcp.md) persists
identity state beside source automatically during apply, and provides paged
canonical export, reviewed application and explicit interruption recovery.
