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
and `node`. This preserves its identity without a text patch.

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
BOM and unrelated text intact.

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

Use explicit retirement addresses when removing assigned declarations. If edits
overlap, replace their common containing subtree instead of sending conflicting
parent/child operations. See [the authoring contract](../ast-authoring.md).

## Fix a diagnostic

Call `read-workspace` with `view: "diagnostics"` to inspect source diagnostics,
then `view: "repairs"` at the same current revision. `read-ast` reports parser diagnostics only;
compilation diagnostics belong to the paged diagnostics view. Available typed repairs include:

| Diagnostic | Proposal |
| --- | --- |
| `PLAY0166` on command `produces` | Add an event declaration in the producing slice. Types come from command property paths, retaining concepts, or from `$context.occurred` as `DateTime`. Uncertain types, conflicting producer shapes, imported/already-declared events and cross-file producers have no repair. Parser errors in any workspace document also block inference. |
| `PLAY0478` (Information) | Replace a plain production with an explicit `for <identifier>`. This deliberately selects the identifier rather than preserving allocated-identity routing. Optional or collection identifiers have no repair. Both models must be executable; a change to the language/semantic version or any other production's effective destination refuses the repair. |
| `PLAY0397` on `validate csharp` | Replace the validation with itself so canonical printing migrates its legacy fence. Other legacy forms have no individual repair. |

Listed `PLAY0166` and `PLAY0478` repairs are verified, not unchecked suggestions.
Discovery checks authoring acceptance and comment preservation, plus routing safety
for `PLAY0478`. For example, an inferred event that conflicts with a specification's
asserted fields is not listed. Only acceptance and conflicts are cached per subject
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
both current revisions and `formatting: "CanonicalizeTouchedDocuments"` (also
returned as `requiredFormatting`). Repairs reprint the entire touched file, so
whitespace and other legacy fences can change; a proposal that would drop any
comment is refused. No file is written until you review the `before`/`after` bytes
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
   Unicode. Inspect authoring and executable diagnostics separately.
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
`single`, `module`, `feature` or `slice`. Use the current revisions and explicit
formatting consent. For full-language models, set `validation: "Authoring"`.

Review and apply the resulting proposal exactly like a node edit. Reorganization
normalizes source formatting and checks structural equivalence; it keeps annotations
such as `// @public` beside the declaration they describe, even when a declaration
moves to a new document. The `dropped-comments` view compares comments across the
whole plan, not just documents changed in place. Expansion does not copy module
forms or contributions into scaffolding. The read tools continue to see
one logical application regardless of the chosen layout.
