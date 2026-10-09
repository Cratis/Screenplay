---
title: AST authoring API
description: Typed syntax serialization, original-document node handles, atomic source transactions, and identity continuity.
---

## Authoring metadata

Use nullable `description` on `SpecificationSyntax` and nullable `documentation` on `ModuleSyntax`, `FeatureSyntax`, `SliceSyntax`, `CommandSyntax`, `ReadModelSyntax` and `ReactionSyntax`. `EventSyntax` retains its existing fields. The typed AST carries the text without changing executable semantics; preserve these members during replacements. MCP `declaration-details` includes both fields in the summary for supporting kinds. See [Descriptions and documentation](slices.md#descriptions-and-documentation) for the source forms.

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

### MCP whole-source authoring

The MCP `propose-source` tool accepts whole `.play` strings and parses them into
`ApplicationSyntax` server-side. It uses `ProposeAuthoring`, not the strict
executable-only `Propose` path. Choose it instead of `propose-ast` when you have
whole source documents rather than focused typed-node edits.

Its `documents` array reuses the typed tool's `create-document`, `replace-document`,
`remove-document`, `move-document` and `rename-document-key` operations, substituting
`source` for `node` on create/replace. Creation requires `path` and `stableKey`,
with optional `encoding` (`Utf8` or `Utf8WithBom`); replacement requires `documentId`
and retains the existing encoding. Parse/import placement is resolved over the
complete post-operation set before building typed document operations. Parse errors
return `SourceParseFailed` and located `authoringDiagnostics`, without retaining a
proposal. No new library raw-text authoring contract is introduced.

Both tools require workspace/catalog revisions and explicit formatting consent,
default to `Authoring` and `Safe`, and enforce the same migrations, retirements and
identity continuity. Syntax-only source can be accepted while `Executable` refuses
with `PLAY0268`. Review dropped comments, exact before/after bytes and executable
diagnostics with `read-proposal` before explicit `apply`. For arguments and an
example see [MCP authoring tools](mcp/authoring-tools.md#propose-whole-source-documents).

## Handler intent edits

`HandlerSyntax.Implementation` is a nullable init-only member; existing positional constructors and deconstruction are unchanged. `ImplementationSyntax` holds ordered `ImplementationHintSyntax` children, each with `text` and its own source/comment anchor. It stores no payload or lifecycle state. The handler's existing `file`/`code` is the only payload; structural paths remain `handler.file`/`handler.code` even when printing nests them under the wrapper.

Use ordinary typed add/replace/remove operations with **Authoring** validation to edit hints, payloads or the wrapper. Switch file/inline sources atomically in one proposal. Parsed and typed authoring candidates reject multiple payloads and blank hints. Wrapped printing refuses conflicting sources rather than choosing one; the legacy direct printer retains its explicit omission comments for old structural trees. Removing attached metadata unwraps while retaining its payload. Removing pending metadata creates an invalid bare handler and is refused. Revision and expected-node checks remain mandatory; authoring acceptance does not confirm implementation behavior.

Old handler JSON without `implementation` reads as null. New metadata needs a capable syntax reader: strict older readers reject unknown kinds/members. Schema discovery includes both new kinds; no ESM version or canonical bytes change.

## Command named-rule intent edits

`ValidationRuleSyntax.Implementation` is nullable and init-only. Its seven positional constructor parameters, defaults and deconstruction remain unchanged; old JSON without `implementation` defaults to null. `ImplementationSyntax` stores ordered hints only. File/Code remain on the rule, including their existing structural slots and file-reference links. Walkers visit each hint and payload once.

Use typed proposals to add/replace hints, change the sole source atomically or remove attached metadata. Removing attached metadata unwraps without discarding the source. Removing a pending wrapper alone is refused: attach a predicate source or explicitly remove the rule. Existing bare legacy rules keep their authoring behavior, not executable meaning. Use expected workspace/catalog revisions and expected nodes, preview the candidate, then explicitly accept; no raw-text repair is introduced.

Transport, authoring and public binding reject concept-owned wrappers, non-named rules, malformed names, conflicting payloads and invalid hint collections. Wrapped pending command rules pass Authoring validation but fail Executable validation with `PLAY0268`. Hint-only edits leave the bound predicate contract unchanged; acceptance never proves execution or confirmation. Canonical and trivia-preserving proposals retain source/comment anchors or refuse an unsafe edit.

## Operation intent edits (syntax-only)

Systems, standalone/inline operations, typed inputs, phase attachments and hints are discoverable through `WorkspaceSyntaxIndex` without an executable model. Original document occurrences retain source and placement; unsupported operation/system kinds have no `SemanticId`, semantic address or `RequirementId`. MCP logical keys include declaration kind and full owning scope and are authoring-only, not persistent identity. Existing catalog assignments remain unchanged when editing this intent.

Use typed add/replace/remove with Authoring validation for inputs, phases, hints or a sole phase source. The phase owns `File`/`Code`; `ImplementationSyntax` owns hints only. Existing revision checks, expected-node validation, reference policy and source-preserving printing still apply. Stale handles, collisions, invalid shapes and unresolved placement refuse rather than choose a target. Executable validation reports `PLAY0268` and refuses; acceptance does not execute, realize or confirm code. Operation/system automatic rename and extraction are not claimed; use coordinated typed edits and validate references. [Manual promotion](operations.md#promote-an-inline-operation-manually) preserves production order explicitly.

## Source and stream edits (syntax-only)

Discover `EventSourceSyntax`, `EventStreamSyntax`, `EventStreamIdPartSyntax`, `CommandStreamSyntax` and `SpecificationStreamSyntax` through the same strict syntax schema and original-document handles. `ApplicationSyntax.eventSources`, `EventSourceSyntax.streams`, `CommandSyntax.stream`, `CommandSyntax.streamCandidates` and `CommandStreamSyntax.streamId` are additive members. Old JSON omissions retain null/empty defaults and existing C# positional constructors remain unchanged. Every retained ambiguity candidate, including its `propertyCandidate`, is structural JSON; source spans and reference lengths are server-owned metadata.

Use typed Add/Replace/Remove under Authoring validation. Add a source to the application's `eventSources`, a stream to its source's `streams`, or a route to the command's `stream`. A key mapping is a `PropertyMappingSyntax` with `property: "streamId"` and a typed expression in `source`; it is not an arbitrary Syntax JSON value. For a composite declaration, add `EventStreamIdPartSyntax` nodes (`name`, typed `TypeRefSyntax` in `type`) to `EventStreamSyntax.streamIdParts`. Command and specification routes use `PropertyMappingSyntax` in their `streamIdParts`, with `property` equal to the exact part name. All three collections default to empty and cannot coexist with a scalar `streamId`. Declarations retain schema order; mappings retain authored order. Part names are not references or rename targets, while their types are ordinary type references. Read each schema rather than inventing required members or numeric envelopes. Coordinated changes must pass full-input validation, print/parse fidelity and existing revision/catalog guards, with explicit preview and apply.

To select a retained property interpretation, replace the containing command: move that candidate's `propertyCandidate` into `properties` and remove it from `streamCandidates`. Canonical printing escapes the qualified property as `@stream`. If the compiler still reports an unknown qualified type, this deliberate debt requires `referencePolicy: "Draft"`; Safe refuses it rather than certifying an unavailable imported shape. To select routing, replace the containing command with that exact retained candidate in `stream`, clear its `propertyCandidate`, and remove it from `streamCandidates`. Resolve the competing qualified value-type import/declaration in the same explicit proposal; supply a required `streamId` mapping on the selected route. Use `CanonicalizeTouchedDocuments`: structural candidate selection is not supported by `PreserveTrivia`. The proof requires complete, resolved source, unchanged unique physical source/stream declarations and otherwise unchanged command members. A different route, changed source declaration, unrelated removed reference or ambiguous parent is refused. Merely deleting `propertyCandidate` cannot override blocking `PLAY0505`. These are deliberate typed authoring edits, not automatic diagnostic repairs.

Sources and streams have exact application/kind/full-owner logical keys and separate physical handles. Physical read inventories retain partial declarations and routes from errorful documents without making those documents editable. Duplicate parent sources refuse child details even if only one contains the requested stream. Unknown parsed extent reports incomplete ownership, and incomplete source refuses confident details. Unresolved placement has no authoritative key or owner. No source/stream `SemanticId` or `RequirementId` is enrolled, and rename-only pins are metadata, not identity-catalog instructions. `ProposeRename` renames sources and streams and repairs their bound command and specification routes; automatic routing diagnostic repairs remain unavailable. Invalid draft shapes return a structured `InvalidSyntaxJson` refusal when strict content or merged syntax export cannot represent them; compact AST handles remain useful for deliberate replacement.

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
- Ordinary JSON numbers decode as finite `Double` literals in Legacy source trees,
  matching the Legacy parser. Other admitted numeric CLR literal types use typed
  `literalType`/`value` envelopes to retain precision and type. Exact source trees
  require the `ExactNumber` envelope described below; no implicit conversion occurs.
- Inline JSON-shaped objects and lists have typed value nodes and individually
  addressable keys. Existing code blocks and raw expressions remain language
  constructs, not an escape hatch for structured data or untyped AST subtrees.

Being a well-typed AST does not guarantee that every hand-built combination can
be expressed in `.play`. Authoring rejects a print/parse round-trip that loses
structural content, even if the printed text itself compiles.

## Exact numeric source authoring (Phase A)

Use the top-level `numbers exact` preamble to preserve numeric literals without
Double rounding. This is source and syntax support only: Exact trees pass Authoring
validation when otherwise valid, but Executable validation refuses them with
`PLAY0268`. No supported ESM version admits Exact mode yet.

`ApplicationSyntax`, `ProjectionSyntax`, `CaptureSyntax` and `SpecificationSyntax`
carry `SourceOptions` in C# and `sourceOptions` in SyntaxJSON. Exact roots serialize
as `"sourceOptions":{"numericMode":"exact"}`; Legacy options are omitted to
preserve existing JSON bytes. Missing options default independently to Legacy,
including on nested source roots: restoration does not inherit a parent's mode.
Explicit null, unknown modes, extra option members and conflicting nested modes
are rejected. Set consistent options on every source root in a typed candidate.

In a literal-value slot, represent an exact number as
`{"literalType":"ExactNumber","value":"9007199254740993"}`. The value is a
canonical fixed-point string within the bounded Decimal domain defined by the
[grammar](grammar.md); it is not a JSON number or a JavaScript `Number`. Exponent
source such as `1e-28` is normalized before transport. The decoder rejects
noncanonical or out-of-range text, plain JSON numbers and Legacy numeric envelopes
in Exact trees. Exact tags do not opt a Legacy root into Exact mode. Business
objects with `literalType` and `value` keys remain business objects; only typed
literal slots interpret the envelope.

Typed edits and source printing preserve the owning mode. Complete Exact documents
print one preamble; fragment printing uses its owner's mode without inserting a
nested preamble. Preview mode changes as a deliberate whole-tree edit, including
numeric leaves and nested source options, rather than changing the root flag alone.
Existing revision, reference and print/parse fidelity checks still apply. Workspace
transport preserves the original preamble in [exact source bytes](workspace-transport.md#numeric-mode-restoration).

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
| `MigrateOptionalTypeSpelling` | Original type handle and expected `TypeRefSyntax`; the compiler derives the spelling change |
| `MigrateComplianceMarkerSpelling` | Original concept handle, expected `ConceptSyntax`, and original source line carrying legacy compliance spelling |

All handles and expectations address the **base snapshot**. Multiple disjoint
edits to the same document compose in memory; intermediate states do not need
to compile. The final complete application does.

`PLAY0479` repairs use `MigrateOptionalTypeSpelling` rather than a caller-supplied
text patch. With `PreserveTrivia`, each operation changes only the optionality spelling;
a document-wide repair submits all occurrences in one transaction. The candidate is
reparsed and must have the same syntax structure, without the selected diagnostics.
Do not mix spelling migrations with other edits to the same document. Explicit
`CanonicalizeTouchedDocuments` consent opts into reprinting instead. Discover a
single occurrence with `WorkspaceDiagnosticRepairs.Find`, or a whole document with
`FindDocumentOptionality` and its root handle; preview either through `ProposeRepair`.

`PLAY0565` repairs use `MigrateComplianceMarkerSpelling` to replace legacy `@pii`,
`sensitive` and `@sensitive` with bare `pii` and `secret`. With `PreserveTrivia`,
the repair changes only the marker spelling, preserving quoted reasons, comments,
spacing and line endings. The candidate is reparsed and must preserve its syntax
structure while removing the selected diagnostics. Discover a single line with
`WorkspaceDiagnosticRepairs.Find`, or every legacy line in a document with
`FindDocumentCompliance` and its root handle; preview either through `ProposeRepair`.
The same formatting consent and restriction on mixing spelling migrations apply.

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
and catalog revisions, `Authoring` validation and the repair's `RequiredFormatting`
(`CanonicalizeTouchedDocuments`). Preview with
`WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, request)`, not a direct
`ProposeAuthoring` call: it rejects `PreserveTrivia` and `PreserveExactSource` with
`FormattingConsentRequired`, and rejects any comment loss anywhere in the touched
document with `RepairWouldDropComments`. Review the candidate and `WritePlan`,
then explicitly accept the plan through the usual destination adapter.
Stale requests return typed stale conflicts with no partial candidate.

Discovery verifies `PLAY0166`, `PLAY0478`, `PLAY0469`, `PLAY0471` and `PLAY0516` repairs before
listing them: authoring acceptance and comment preservation, plus each repair's
routing, consumer or executable-model constraints. Verdicts are cached only on the
current immutable snapshot; proposals run one fresh transaction. See the
[repair conditions](mcp/authoring-tools.md#fix-a-diagnostic). `PLAY0470` remains deferred.

`PLAY0516` repairs move sibling modules, features, slices or explicit file imports,
or add verified explicit pins before a retained glob. Features and slices inserted
mid-list print before their next located sibling; other collections retain their
existing printing rule. The reference subject is selected by event name and exact
location, not by parsing the diagnostic message. Verification preserves comments,
existing catalog assignments, readiness, documents, placements and simulated ranks, removes
the selected finding, and introduces no new timeline findings or errors/warnings.
Both executable models require byte-identical ESM. When neither binds, a separate
proof requires equal merged syntax after normalizing only timeline sibling order
and approved import pins, plus the same admission diagnostic code/severity
multiset. One-sided model availability is refused. A fresh workspace may establish
missing document assignments with its existing document IDs and stable keys. All
existing assignments, origins and event contract revisions remain unchanged, and
any other catalog change refuses the repair. These assignments are persisted only
when the proposal is applied. Rediscover after each
accepted proposal; do not concatenate recipes from one snapshot.

The legacy-fence repair handles only `PLAY0397` on a `validate csharp` header.
Its discovery identifies the recipe without verifying the candidate; the proposal
may still be refused. Its parsed
subject has the unique warning location. The operation is an **identity replacement**:
it replaces the node with its own original syntax, and canonical printing performs
the migration to `validate` followed by a `` ```csharp `` fence. This reprints the
**whole touched document**, normalizing whitespace and migrating other legacy
forms in that file too. Bare description fences and standalone language lines
are not offered as individual repairs, but can change as part of this print. Cases requiring a choice, such as selecting an alias for a
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
slices, event sources and their streams. Source and stream renames repair bound command and specification routes without changing identity-catalog entries. Existing pins stay untouched; no pins are added automatically. Add `id` with a typed edit when an external stored name must be retained. Other property declarations are not automatic rename targets. Module/feature fragments change
together. Proven typed references and qualified descendant references are repaired;
read-model output aliases remain distinct from projection builder identities.
Event renames also update constraint `released by` references and screen/behavior
`on event` triggers.

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
uncertainty cannot be waived by that choice. Event renames retain an existing `id`
pin or insert the previous name by default. `EventNeverPersisted = true` omits a new
pin and removes a redundant pin equal to the current name; an earlier identity pin is kept. A pin-inserting rename refuses comment loss or duplication;
other renames retain the ordinary explicit-canonicalization contract.

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

`PreserveTrivia` patches these changes in place:

- An identifier member, such as a declaration name or an event reference, through
  its proven identifier span.
- A literal value (string, number, boolean or `null`) of a `produces` mapping or a
  specification value. Only the literal's authored text is rewritten, so a
  trailing comment on the same line survives, and the value may change type.
- A whole property mapping of a `produces` block or a specification value. When
  only its source changes, only the right-hand side is rewritten; when its target
  property changes too, the mapping text is rewritten up to the end of its source.
- An event `id` pin inserted or removed by a rename. Removing an uncommented pin
  removes the entire directive line; a trailing comment remains exactly once.

Other additions, removals or reorderings of nodes are structural and still require
`CanonicalizeTouchedDocuments`.

Canonical printing retains attached comments and normalizes blank lines. It preserves
parsed member order within a document. Added nodes do not inherit source positions
or comments from their supplied values. Moves retain their comments and internal
order. A same-document move retains its root source position only when it agrees
with the requested order among destination siblings of the same kind;
cross-document moves discard that root position. In either case,
new members follow the insertion rule in
[Printing and generating](printing.md#what-printing-does-not-keep). Before applying such a result, call
`WorkspaceDroppedComments.In(result.WritePlan)` to list any comment that could not be
placed, with its path, line, column and text. The `PLAY0288` warning for each
canonically printed document states the count and lines of comments actually lost.

Successful results provide the candidate workspace and exact `WorkspaceWritePlan`
before/after documents. The destination adapter owns file application and failure
recovery. Atomic proposal acceptance is not a promise of crash-atomic file writes.

Persist with `ScreenplayWorkspaceSerializer`; the envelope includes exact source,
revision and authoritative identity catalog. The [MCP server](mcp/reference.md) persists
identity state beside source automatically during apply, and provides paged
canonical export, reviewed application and explicit interruption recovery.
