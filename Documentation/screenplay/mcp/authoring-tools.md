---
title: Authoring with the MCP tools
description: The typed propose, review and apply workflow with complete tool arguments, for client authors and for understanding what an assistant did on your behalf.
---

This page shows the tool calls behind [Create a model](create.md) and
[Edit a model](edit.md): the exact arguments an MCP client sends. If you are asking an
assistant to do the work, those guides give you prompts instead. Use this page when
you are building a client, debugging a proposal, or checking what an assistant did.

Use this procedure with an MCP client connected to
[the Screenplay server](reference.md). Calls below are MCP tools, not shell commands.
The server root is the application boundary; file layout does not create separate
applications.

## Open the application

Call `open-workspace`. Supply an `applicationName` for a new model; existing
identity state provides its saved name and identities. A root can contain one or
many `.play` files, or be empty when you are starting a new model.

Retain the returned `revision` and `catalogRevision`. They describe the exact
snapshot you are editing. Do not reuse them after applying changes.

For an existing application, call `read-workspace` with `view: "documents"` and
`expectedRevision`. Each document includes its identity, path and root handle.
Use `describe-application` for the logical hierarchy rather than reconstructing
it from directory names.

## Choose a proposal tool

| Tool | Use it for | Validation |
| --- | --- | --- |
| `propose-source` | Whole `.play` text documents, including syntax-only language constructs | `Authoring` by default; optional `Executable` |
| `propose-ast` | Focused typed node edits using `read-ast` handles, or typed whole documents | `Authoring` by default; optional `Executable` |
| `propose` | Exact byte replacements and legacy whole-document operations | Executable-only |

All three return proposals to review with `read-proposal` before explicit `apply`.
Acceptance never writes files or executes specifications.

### Propose whole source documents

Send `propose-source` with `expectedRevision`, `expectedCatalogRevision`, explicit
`formatting` consent and a `documents` array. For example, this array creates a
valid source-only model without building typed JSON:

```json
[
  {
    "operation": "create-document",
    "stableKey": "application",
    "path": "application.play",
    "source": "concept Month : Int\neventsource Account\n  stream Transactions\n    streamId Month\n"
  }
]
```

Use `formatting: "CanonicalizeTouchedDocuments"`. `validation` defaults to
`"Authoring"`; this example is accepted with `executableReady: false`, while
`"Executable"` rejects it with `PLAY0268`. `referencePolicy` defaults to `"Safe"`;
`"Draft"` reports deliberate unresolved reference debt without waiving parsing or
identity continuity.

The document operations use the same names as `propose-ast.documents`:

| Operation | Fields in addition to `operation` |
| --- | --- |
| `create-document` | `path`, `stableKey`, `source`; optional `encoding`: `Utf8` (default) or `Utf8WithBom` |
| `replace-document` | `documentId`, `source` |
| `remove-document` | `documentId` |
| `move-document` | `documentId`, `path` |
| `rename-document-key` | `documentId`, `stableKey` |

`source` is a UTF-8 text string, not base64 or a typed `node`. There is no node
`operations` array on this tool. Each document may be targeted once per batch.
Parsing resolves imports and fragment placement against the complete final document
set, including unchanged files. You can create a barrel and its imported fragments
in one proposal, in either array order. A parse failure returns `success: false`,
`failureKind: "SourceParseFailed"` and located `authoringDiagnostics`, with no
`proposalId`. Fix the text and make a new proposal.

Whole-source replacement uses the same typed authoring transaction and continuity
rules as `propose-ast`: existing IDs survive unchanged addresses; removing assigned
declarations requires explicit `retiredSemanticAddresses` and, for event contracts,
`retiredEventAddresses`. Renames require coordinated `semanticRenames` and
`eventRenames`. Replacement preserves the document's encoding policy, but printing
may normalize whitespace. Review exact before/after bytes, dropped comments and
`introducedExecutableErrors` before applying.

## Inspect inline event declarations

An event declared with `produces event` appears as an `Event` in declaration queries at its **slice-owned** address, not beneath the command. `declaration-details` exposes its typed properties and authoring documentation; the command's `produces` view retains `InlineEvent`. Reference queries include `declares` relationships from the command and slice, alongside the command's `produces` relationship. The visualization counts and draws the event like a standalone declaration. Workspace syntax entries assign the event and its properties the same stable addresses and catalog identities as their standalone equivalents in that slice. Event and containing-slice rename proposals preserve those assignments.

Descriptions, documentation, and optional rename-only `id` remain syntax metadata. They do not replace the workspace identity catalog or the portable hashed event contract id. Workspace repair and rename proposals remain separate, explicit transactions; querying or visualizing an inline event never applies a repair.

## Event source and stream authoring

After opening the workspace, use these paged `read-workspace` views with `expectedRevision`:

| View | Contents |
| --- | --- |
| `event-sources` | Application-owned source keys, identifier types, pins and physical handles |
| `event-streams` | Exact source-owned stream keys, key types, pins and ownership status |
| `event-source-details`, `event-stream-details` | Exact `authoringKey`; compact header with metadata sizes and source/AST read pointers, followed by independently paged children; rejects ambiguous or incomplete ownership |
| `command-routes` | Authored routes and all retained ambiguity candidates, never inferred effective routing |
| `event-source-diagnostics` | Paged physical parser/import and whole-assembly diagnostics, including errorful and unresolved files |

Continuation also requires `expectedCatalogRevision`. The immutable physical inventory retains partial declarations, duplicate routes and route/property candidates from all parsed files, independently of editable AST eligibility. `inventoryComplete`, `authoringDiagnosticsView` and `authoringDiagnosticsCount` disclose parser/import and whole-assembly evidence. Unknown extent reports `ownership: "incomplete"`; details refuse with `IncompleteSource`. Duplicate physical parents make every child ambiguous, including parents in errorful files. Unresolved-placement entries name refused documents, and retained nodes in those files have a null `authoringKey` rather than a fabricated owner. Keys include application, declaration kind, full owner path and name; they are not persistent semantic identities. All these entries disclose that these constructs are not admitted by any supported executable model (ESM) version yet. A command/specification/slice readiness message describes that member's constructs and command dependencies; model readiness also includes unrelated source declarations.

The detail boundary is `compact-header-v1`, not full typed syntax. No header embeds all child stream subtrees. Child items follow the actual physical parent's authored order. Pages honor item and serialized-byte budgets; continue from `nextOffset`, not `offset + limit`. Pins and descriptions above 4096 UTF-8 bytes are explicitly omitted with exact sizes and a `read-document` byte-page pointer. Use a separate `read-ast` content request for the full node when it fits; a single oversized identity cannot be narrowed by reducing the item count.

Generic source details, dependency resolution and editor navigation share physical source confidence, including unresolved placement candidates and unknown root extent. Unique unrelated event and operation references retain their existing resolution rules.

Generic `EventSource`/`EventStream` declaration queries and dependencies include source identifier/stream-id type references and separate `commandEventSource`/`commandStream` links. Stream references accept exactly `Source.Stream`, not arbitrary suffixes. Combined type/declaration collisions remain blocking and are not navigated confidently.

For edits, read the actual `read-ast` handles and `syntax-schema` kinds, then use Authoring Add/Replace/Remove with preview and explicit apply. The member paths and candidate-selection rules are in [AST authoring](../ast-authoring.md#source-and-stream-edits-syntax-only). Use `propose-rename` for source and stream renames with bound command and specification route repair. Semantic catalog enrollment and new routing quick fixes remain unavailable. Executable validation still refuses with `PLAY0268`, and `PLAY0470`/`PLAY0478` repairs require unavailable before/after executable proof.

## Operation and system intent

Systems and operations are **syntax-only**, not admitted by any supported executable model (ESM) version yet (`PLAY0268`). After `open-workspace`, call `read-workspace` with the current `expectedRevision`:

| View | Contents |
| --- | --- |
| `system-intents` | Application-scoped systems, descriptions through details, kind/full-scope authoring keys and source occurrence handles |
| `operation-intents` | Slice-owned operation keys, uses, input counts, execute/compensate state and selected source handles |
| `system-intent-details`, `operation-intent-details` | Supply the inventory's `authoringKey`; pages contain declaration intent, typed inputs, phases and ordered hints |
| `ordered-productions` | Command scope/handle and every production occurrence in authored order, including repeated events, resolved kind, candidates and mappings |

Echo `expectedRevision` for every page. Stale snapshots, wrong declaration kinds, ambiguous keys and unresolved placement refuse details rather than selecting an occurrence. Inventory includes unresolved-placement document entries so you can repair conflicting/cyclic imports. Authoring keys are name-derived logical keys, **not** admitted `SemanticId` or `RequirementId`; use revision-local handles for physical edits. Attachment states describe model selection only; no source is executed or confirmed.

Declaration search/details and dependency traversal also include systems, operations and operation specification references. `declaration-details` supports `inputs` and `phases` on Operation; `syntax` returns explicit typed content. Produces links resolve event/operation kinds together and report ambiguity without guessing. Authoring read views work without ESM binding.

Use `read-ast includeContent`, `syntax-schema` and ordinary typed `propose-ast` add/replace/remove operations with `validation: "Authoring"` to edit inputs, phases, source or hints. Each phase alone owns `file`/`code`; the wrapper owns hints. `validation: "Executable"` refuses with `PLAY0268`. Existing catalog assignments are not replaced by operation keys. Automatic operation extraction and operation/system rename are not available; [manual promotion](../operations.md#promote-an-inline-operation-manually) requires coordinated typed edits and full-source validation.

## Create the first typed document

Call `syntax-schema` for `ApplicationSyntax`, `ModuleSyntax`, `FeatureSyntax`,
`SliceSyntax` and `EventSyntax` to inspect their member contracts.

Use the following complete value for the `documents` argument of `propose-ast`:

```json
[
  {
    "operation": "create-document",
    "stableKey": "application",
    "path": "application.play",
    "node": {
      "kind": "ApplicationSyntax",
      "modules": [
        {
          "kind": "ModuleSyntax",
          "name": "Sales",
          "isPlacement": false,
          "features": [
            {
              "kind": "FeatureSyntax",
              "name": "Orders",
              "isPlacement": false,
              "slices": [
                {
                  "kind": "SliceSyntax",
                  "type": "StateChange",
                  "name": "Register",
                  "events": [
                    { "kind": "EventSyntax", "name": "OrderRegistered" }
                  ]
                }
              ]
            }
          ]
        }
      ]
    }
  }
]
```

`isPlacement` is required on every module and feature. Use `false` for one written in
this document; it is `true` only when a document was imported into the module, so
its top level is the module's body. Call `syntax-schema` to see every required
member of a kind; a proposal that omits one is rejected with the path of the
missing property.

Also supply the returned workspace values as `expectedRevision` and
`expectedCatalogRevision`, plus `formatting: "CanonicalizeTouchedDocuments"`.
The default AST validation is `Authoring`. Empty collections initialize empty;
source locations are supplied by the server.

Reference policy defaults to `Safe`: new unresolved or ambiguous model references
are rejected. For an intentionally incomplete draft, choose `referencePolicy: "Draft"` and
inspect the reported debt. Draft never waives parse, identity or
binding-safety checks.

The proposal creates one new document holding a module, a feature, a slice and an
event. It does not write the file yet. Treat the readiness it reports as a property
of this proposal, not a promise about the model you will build on it. Continue with
the review and apply steps below.

Applying the proposal writes `application.play`:

```screenplay
module Sales

  feature Orders

    slice StateChange Register

      event OrderRegistered
```

## Read and edit existing elements

Call `read-ast` with the current `expectedRevision`, `kind: "SliceSyntax"`,
`name: "Register"`, and `includeContent: true`. Select the intended occurrence
using its source location and parent/document information.

Copy its returned typed `node`, change `description`, and submit a `replace`
operation in `propose-ast`:

- `target`: the returned handle, unchanged.
- `node`: the updated typed node.
- `expected`: optionally the original typed node for an explicit structural
  expectation. Otherwise the handle's exact base revision supplies it.

If a document has parser errors, it has no editable occurrence handles. Retrieve
its document ID through `read-workspace`, construct a valid `ApplicationSyntax`,
and use a `replace-document` entry in `propose-ast.documents` with `documentId`
and `node`, or supply corrected `.play` text as `source` in `propose-source.documents`.
Both preserve its document identity without a text patch.

Use `add` with a parent handle and typed member such as `events` to add an element.
Use `remove` for a selected occurrence and `move` for a different existing parent
or document. Read the member schema before choosing a destination slot.

Node handles identify occurrences in one revision. A module or feature split
across files has several occurrences, not one magic writable location. For a
logical header rename, update all fragments in one batch; a partial rename is
rejected.

## Rename without hand-editing references

For a supported declaration, call `propose-rename` with its handle, exact
`expectedName`, `newName`, and both workspace revisions. The default formatting
is `PreserveTrivia`: verified identifier patches leave comments, line endings,
BOM and unrelated text intact. Event renames update typed consumers, including
constraint `released by` references and screen/behavior `on event` triggers.

Event sources and streams, examples and specifications are also rename targets. Source and stream renames repair bound command and specification routes, leaving existing pins and identity-catalog entries unchanged. No pins are added automatically; add `id` with a typed edit when an external stored name matters. Renaming an example updates only the steps resolved to that declaration, including qualified names and `when append`; same-named examples in other scopes remain unchanged. Renaming its underlying event, command or read model updates the example's type reference. Renaming a composite-type property updates structured keys in example bodies and step overrides. Examples remain front-end syntax and receive no ESM identity. Use `find-fixtures` to inspect effective values and origins before editing an example shared by several specifications.

The planner coordinates logical fragments, repairs proven typed references and
preserves assigned descendant/event identities. It refuses ambiguous targets,
name capture, opaque impact and unsupported spans. Do not treat a refusal as
permission to perform a global text replacement. Use explicit typed operations
and migrations only after resolving the uncertainty.

A structured value such as `lines = [{"sku":"A-1","quantity":2}]` has typed
object keys. You can rename the declared composite `type` property `sku`; the
planner rewrites only matching keys bound to that type, preserving other keys,
string values and source trivia. Other property declarations are not automatic
rename targets. Opaque expressions, code blocks and imports still block a rename
only if their text contains the old or new name as a whole identifier, including
inside a string or key. The conflict message gives the file, line and column, for example
`'Shop/Orders/PlaceOrder/PlaceOrder.play(22,11)'`, and the name it matched.

## Coordinate changes across files

Put related node edits in one `operations` array and document creations/moves in
`documents`. Every handle refers to the same base snapshot. Independent edits in
one file are combined before printing, and the final complete application is
validated once the batch is composed.

To create a destination document containing an existing element, use its typed
subtree in `create-document` and remove its original occurrence in the same
proposal. No intermediate, broken source is published.

When working below the automatic rename planner, assigned declaration changes
require explicit identity migrations:

1. Read `semantics` and, for events, `eventContracts` through `read-workspace`.
2. Include the corresponding `previousAddress`/`currentAddress` objects in
   `semanticRenames` and `eventRenames`.
3. Update affected typed references and assigned descendant addresses in the same
   batch. No tool globally replaces names in descriptions, code or literals.

Use `retiredSemanticAddresses` and `retiredEventAddresses` when removing assigned
declarations. An event has both assignments: preserve it in both `semanticRenames`
and `eventRenames`, or retire it in both retirement arrays. Include every assigned
descendant, not just the parent whose address changed.

An `InvalidIdentityMigration` refusal returns `identityMigrationIssues`. Each item
contains the exact `address` (`kind` and typed `parts`, in the input schema's shape)
and `arguments`, the arrays to correct. A stale rename names both endpoints and its
rename array; an invalid retirement names its retirement array. An unexplained
removed assignment lists the rename and retirement arrays as **alternatives**:
choose a rename to preserve identity, or a retirement only for a removed declaration.
The conflict message also names these addresses and arrays. No proposal is retained
and no source is written on refusal.

If edits
overlap, replace their common containing subtree instead of sending conflicting
parent/child operations. See [the authoring contract](../ast-authoring.md).

## Extract an inline event

Use `read-ast` to find the inline `EventSyntax` handle, then call
`propose-extract-inline-event` with `subject`, `expectedRevision`,
`expectedCatalogRevision` and `formatting: "CanonicalizeTouchedDocuments"`.
This is an explicit refactoring, not a diagnostic repair. The declaration moves
into the owning slice; an omitted inline destination becomes `for <identifier>`.
An existing destination stays unchanged. Event metadata and tags move with the
declaration, while production and mapping comments remain attached to their intent.

Extraction requires byte-identical canonical ESM, unchanged catalog assignments
and every comment preserved exactly once. It refuses unsupported or unbindable
models rather than guessing. Extract before adding `generation`; parser-invalid
inline generations cannot be edited through occurrence handles. There is no reverse
inlining operation. Review and apply the returned proposal as any other edit.

## Fix a diagnostic

Call `read-workspace` with `view: "diagnostics"` to inspect source diagnostics,
then `view: "repairs"` at the same current revision. `read-ast` reports parser diagnostics only;
compilation diagnostics belong to the paged diagnostics view. Available typed repairs include:

| Diagnostic | Proposal |
| --- | --- |
| `PLAY0166` on command `produces` | Add an event declaration in the producing slice. Types come from command property paths, retaining concepts, or from `$context.occurred` as `DateTime`. Uncertain types, conflicting producer shapes, imported/already-declared events and cross-file producers have no repair. Parser errors in any workspace document also block inference. |
| `PLAY0478` (Information) | Replace a plain production with an explicit `for <identifier>`. This deliberately selects the identifier rather than preserving allocated-identity routing. Optional or collection identifiers have no repair. Both models must be executable; a change to the language/semantic version or any other production's effective destination refuses the repair. |
| `PLAY0469` on an inline mapping | Remove the payload property and its mapping together, retiring the property address. The label says “changes the event contract”; `canFixAll` is false. This narrow repair requires an executable model without other consumers of that event (including constraint releases, projection subscriptions and `on event` triggers) or opaque syntax/attachments. It refuses changed routing or lost comments. Plain productions receive guidance only. |
| `PLAY0471` | Remove a redundant event `id`, inline or standalone, only when the executable model, catalog and comments are preserved. |
| `PLAY0397` on `validate csharp` | Replace the validation with itself so canonical printing migrates its legacy fence. Other forms of `PLAY0397` have no individual repair. |
| `PLAY0560` (Information) | Migrate one legacy compliance line or the entire document to bare `pii`/`secret`, retaining quoted notes and trivia. For one line, pass its discovered `location.line` as `line` alongside the concept subject. |
| `PLAY0479` (Information) | Write `optional` after the type. An occurrence repair changes one type; a document repair contains all spelling changes in one transaction. Both preserve syntax structure. |
| `PLAY0516` (Information) | Move a sibling declaration or explicit file import, or pin an already placed file before a retained glob. The proposal removes the selected backward edge without new timeline findings. Own-sub-feature findings, cycle groups, unranked members and mixed/different-parent boundaries have no repair. |

Discovery verifies listed `PLAY0166`, `PLAY0478`, `PLAY0469`, `PLAY0471`, `PLAY0479` and `PLAY0516` repairs.
It checks authoring acceptance and comment preservation, plus routing safety for
`PLAY0478`, consumer/routing impact and executable readiness for `PLAY0469`, and
executable-model/catalog preservation for `PLAY0471`. For example, an inferred
event that conflicts with a specification's asserted fields is not listed.
`PLAY0479` verifies all spellings together once per document and snapshot; occurrence
repairs are offered only when that document migration passes.
`PLAY0516` preserves existing catalog assignments, executable readiness, documents, placements,
comments and simulated presentation ranks. When both models bind, their ESM bytes
must match. When neither binds, merged syntax must match modulo only sibling
modules/features/slices/import order and approved explicit import pins; admission
diagnostics must retain the same code/severity multiset. One-sided model availability
and new errors/warnings are refused. Only already placed files at the same placement
may be pinned, and the glob remains. A prefix can require several pins; no other
nodes may be added. A multi-pin proposal is one typed replacement of the import's parent container that keeps every existing node and comment and only inserts the pins before the glob.
In a fresh workspace, a proposal may establish missing document assignments using
only the documents' existing IDs and stable keys. Every existing assignment, origin
and event contract revision must remain unchanged; any other catalog change refuses
the repair. Discovery and proposals do not persist these assignments; only apply does.
Rediscover after each applied move or pin instead of combining
recipes from one snapshot. `canFixAll` is true, but each proposal is still verified.
The MCP pinned-evidence path remains unsupported for `PLAY0516` (`UnsupportedRepair`);
there is no TypeScript quick fix or VS Code pinned-evidence action for it.

`PLAY0397` discovery identifies the recipe only; its proposal may still be refused.
Only acceptance and conflicts for verified repairs are cached per subject
on the current immutable workspace snapshot for discovery reuse; diagnostics are
never cached. Paging or rereading reuses the verdict; a new snapshot requires fresh
verification. `propose-repair` always verifies only the selected subject in one fresh
authoring transaction and returns its full diagnostics, even after discovery.

An unknown code, subject or recipe returns the `UnknownRepair` argument error.
A matched repair that fails verification instead returns `success: false` with typed
`conflicts`, `authoringDiagnostics` and `executableDiagnostics`, without a proposal ID.
`InvalidOperation` remains a transaction conflict, including refusal to change another
production's routing or the model version; it does not mean the repair is unknown.

Pass the selected repair's `diagnosticCode` and `subject` to `propose-repair` with
both current revisions and the returned `requiredFormatting`.
For `PLAY0479` and `PLAY0560`, this is `"PreserveTrivia"`: only selected type or compliance spellings change. `PLAY0560` line repairs require the discovered `line` when several diagnostics share a concept subject; a document-root subject migrates every legacy compliance line. Notes and legal text never move.
Choose `scope: "document"` in the discovered results to migrate the whole document
using its root `subject` handle; pass that handle, not a scope argument, to
`propose-repair`. All splices are verified together in one transaction. The server
reparses the candidate, requires unchanged syntax structure, and checks that the
selected diagnostics have disappeared. Comments, alignment, line endings and the
UTF-8 BOM are retained. `"CanonicalizeTouchedDocuments"` remains an explicit opt-in.

Other listed repairs require `"CanonicalizeTouchedDocuments"` and reprint the entire
touched file, so whitespace and other legacy fences can change. A proposal that
would drop any comment, or duplicate one, is refused. No file is written until you review the `before`/`after` bytes
with `read-proposal` and explicitly call `apply`. After external edits, reopen and
rediscover repairs rather than reusing stale handles. Applying a repair uses the
same [identity state and recovery](recovery.md) contract as other proposals.

## Review and apply

An accepted proposal returns a `proposalId`, before/after revisions, changed-file
count, normalization disclosure, the number of dropped comments and executable
readiness. Acceptance is not a filesystem effect.

To change a specification value or a `produces` mapping without touching anything
else, `replace` its `PropertyMappingSyntax` with `formatting: "PreserveTrivia"`.
Changing `channel = "web"` to `"store"` rewrites only `"web"`; every comment,
blank line and declaration stays where it was. `CanonicalizeTouchedDocuments`
reprints the whole document instead: attached comments stay with their syntax owners,
while blank lines are normalized. Existing members from the same parsed document retain their authored
order, including when an edited member is replaced through typed JSON. Newly authored
members without comparable source positions follow the canonical insertion rule;
see [Printing and generating](../printing.md#what-printing-does-not-keep). The
`dropped-comments` view lists only comments that cannot be placed. An edit that
keeps all comments reports zero dropped comments.

1. Call `read-proposal` with its ID and `view: "changes"`. Check the proposal's
   `droppedCommentCount`; when it is not zero, call `read-proposal` with
   `view: "dropped-comments"` to see each comment a changed document loses, with
   its `path`, `line`, `column` and `text`.
2. For each changed document, retrieve `before` and `after` byte pages by
   `documentId`. Base64 pages reconstruct exact source bytes, including BOM and
   Unicode. Inspect authoring and executable diagnostics separately. The proposal's
   `introducedExecutableErrors` lists only the executable-model errors this
   proposal adds, each with `code`, `message`, `path`, `line` and `column`; errors
   the model already had are not repeated. When the list is not empty,
   `executableGuidance` says to fix them in a new proposal before apply.
3. Inspect `stateChange` and, if needed, retrieve exact before/after identity state
   with `workspace-state`, the proposal ID and corresponding state revision.
4. Call `apply` with the proposal ID and the **before** workspace/catalog revisions.
5. Check `success`, `status` and `recovery`. Source and `.screenplay/identities.json`
   are installed together under the recovery envelope. Keep both in source control.

Canonical `export-workspace` is available for transport or backup. It is no longer
required for a normal fresh server session to retain identities.

An external edit or stale revision rejects apply. Interrupted operations retain
`.screenplay/pending.json`. Inspect `workspace-state` and explicitly call
`recover-workspace` with the reported operation ID when rollback is safe.
Unexpected external bytes block recovery rather than being overwritten. This is
not simultaneous crash-atomic visibility across filesystem paths.

After a successful apply, old handles and outstanding proposals are stale. Read
fresh handles against the returned revision. A later session loads the root-local
identity state automatically. A conflicting import or corrupt state is rejected,
not silently replaced.

## Change the layout

Call `recommend-layout` for advice, then `expand-layout` with `layout` set to
`single`, `module`, `feature` or `slice` (the default). Use the current revisions and
explicit formatting consent. For full-language models, set `validation: "Authoring"`.

The existing layout values also select how finely to split the model:

| Layout | Files |
| --- | --- |
| `single` | One self-contained `application.play`, with file imports inlined. |
| `module` | `application.play` imports `<Module>/<Module>.play`; each module file holds its features and slices. |
| `feature` | Module files import feature barrels; each feature file holds its slices and imports its nested features. |
| `slice` | Feature barrels additionally import `<Slice>/<Slice>.play`; each slice file contains only its slice, with no `module` or `feature` restatement. |

Split layouts use quoted imports instead of the older merge-only scope
restatements. Parameters and destination paths are unchanged: use `layout: "slice"`
for one file per slice, not a separate flag. Each parent imports its children
explicitly in declaration order rather than relying on alphabetical glob expansion.
The Commerce sample shows the same barrel idea with flat slice files. The generated
layout puts slices in `<Feature>/<Slice>/<Slice>.play` and inlines shared imports into
`application.play`. TimeTracking demonstrates inline feature barrels in module files.
Validate the whole folder or the generated `application.play`, not a slice fragment
in isolation. There is no CLI `expand-layout` command; expansion is a reviewable MCP
proposal.

Expansion selects the same ordering root as the timeline: the sole document when
there is one, otherwise an importing `application.play`, or the unique importing
document that is not itself imported. An import-less `application.play` does not
hide a lone importing barrel beside it. Before proposing a layout, expansion
checks that each scope keeps its relative module, feature and slice sibling order;
a container declared in several files ranks where its owner file places it: the
root if the root declares it, otherwise a file that declares it and is named after
the container or one of its ancestors, the outermost such name winning. Without such
a file it ranks at its first declaration in import order. Repeated sibling names
count once. A mismatch fails without
creating a proposal. With no ordering root, that guard is skipped and
the proposal's `review` text says that path order was used. Order is presentation
metadata, not executable-model bytes or identity. A child imported into a container
keeps its parent-relative position among contributions, templates and other members
when the layout is collapsed. Leading and trailing header comments stay on the
physical declarations, including the first line of placed files.

Review and apply the resulting proposal exactly like a node edit. Reorganization
normalizes source formatting and checks structural equivalence; it keeps annotations
such as `// @public` beside the declaration they describe, even when a declaration
moves to a new document. Existing file imports are replaced by the new composition;
comments attached to those old imports are retained in their owning scope. Semantic
and event identities must pass the same continuity checks as any other proposal.
The `dropped-comments` view compares comments across the
whole plan, not just documents changed in place. Expansion does not copy module
forms or contributions into scaffolding. The read tools continue to see
one logical application regardless of the chosen layout.
