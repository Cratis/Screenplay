---
title: Create and edit a model with MCP
description: Create typed Screenplay elements, review atomic changes across files, and preserve identities between editing sessions.
---

Use this procedure with an MCP client connected to
[the Screenplay server](mcp.md). Calls below are MCP tools, not shell commands.
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
          "features": [
            {
              "kind": "FeatureSyntax",
              "name": "Orders",
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

Also supply the returned workspace values as `expectedRevision` and
`expectedCatalogRevision`, plus `formatting: "CanonicalizeTouchedDocuments"`.
The default AST validation is `Authoring`. Empty collections initialize empty;
source locations are supplied by the server.

Reference policy defaults to `Safe`: new unresolved or ambiguous model references
are rejected. For an intentionally incomplete draft, choose `referencePolicy: "Draft"` and
inspect the reported debt. Draft never waives parse, identity or
binding-safety checks.

The proposal creates an event declaration in a new document. It does not write
the file yet and does not claim that this initial model is executable. Continue
with the review and apply steps below.

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
parent/child operations. See [the authoring contract](ast-authoring.md).

## Review and apply

An accepted proposal returns a `proposalId`, before/after revisions, changed-file
count, normalization disclosure, the number of dropped comments and executable
readiness. Acceptance is not a filesystem effect.

To change a specification value or a `produces` mapping without touching anything
else, `replace` its `PropertyMappingSyntax` with `formatting: "PreserveTrivia"`.
Changing `channel = "web"` to `"store"` rewrites only `"web"`; every comment,
blank line and declaration stays where it was. `CanonicalizeTouchedDocuments`
reprints the whole document instead: comments are dropped and member order is
normalized, and the `dropped-comments` view lists exactly what is lost.

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
normalizes source formatting and checks structural equivalence; it does not copy
module forms or contributions into scaffolding. The read tools continue to see
one logical application regardless of the chosen layout.
