---
title: Imports
description: Compose an application from focused .play files - import files and glob patterns, place what they declare in a module or feature, and keep composite files at the root and per module.
---

A [folder of `.play` files](folders.md) is already one application. That solves the large file, but each file still has to say where it belongs. A slice file restates `module Ordering` and `feature Orders` above the one slice it is about. And nothing in the folder says *what* the application is made of - that is whatever happens to be on disk.

`import "<path or glob>"` fixes both. A file names the files that belong to it. Where the import is written decides where they belong, so a file that tells one part of the story can hold just that part.

## Source and stream declarations across files

[Event sources](event-sources.md) stay application-owned, including in module/feature-placed files. Streams belong to their physical source, not to the importing module or the file path. Files compiled together share declarations independently of file order; a quoted import includes files, while an unquoted contract import does not supply an unknown source or stream shape. Only exact `Source.Stream` references resolve. Duplicate physical parents make every child ambiguous, and conflicting/cyclic placement must be repaired before selecting a navigable owner.

## Operation declarations across files

[Systems](operations.md) stay application-scoped even in placed files; operations belong to their declared slice. Files compiled together share explicit operation declarations. `produces Register.NotifyAccounting` can select that operation from another slice in the same assembled model. Use enough owning scope to make the reference unique; ambiguous event/operation candidates are not guessed. Newly qualified productions must resolve to an explicit operation, not an event. An unquoted contract import does not invent an operation shape or kind. These constructs remain syntax-only, not admitted by any supported executable model (ESM) version yet (`PLAY0268`).

## Import files

At the top level of a document, `import` with a quoted path brings in whole documents:

**application.play**

```screenplay
domain Acme.Commerce

import "**/*.play"

concept OrderId : Uuid
```

That root file is the whole application: it compiles everything beneath it. Compile it with `screenplay application.play`, and only what it imports is compiled - a scratch file next to it is not part of the application unless something imports it.

The quote is what tells the two kinds of `import` apart. `import Customers.CustomerRegistered` still names a contract from another bounded context, and `import "Ordering/*.play"` names files.

## Where an import sits decides where files belong

Written inside a `module` or a `feature`, an import *places* the files it brings in: their top level is that module's or feature's body. A module file can describe the module and take in its features:

**Ordering/Ordering.play**

```screenplay
module Ordering
  description "Orders, from basket to doorstep"
  import "*/*.play"
```

A feature file does the same one level down:

**Ordering/Orders/Orders.play**

```screenplay
feature Orders
  description "Placing and cancelling orders"
  import "*.play"
```

And a slice file holds the slice - nothing above it, because the import already said where it goes:

**Ordering/Orders/PlaceOrder.play**

```screenplay
slice StateChange PlaceOrder
  command PlaceOrder
    orderId OrderId identifier
    produces OrderPlaced
      for orderId

  event OrderPlaced
    placedAt DateTime
```

Placement composes. `Orders.play` is placed in `module Ordering` by the module file, so the import inside its `feature Orders` places `PlaceOrder.play` at `Ordering.Orders`.

## Composite files at every level

A root file can import `**/*.play` while each module file imports its own folder, and each feature file its own. Every file is still compiled once, however many imports match it. When several imports place the same file, **the deepest placement wins**. `PlaceOrder.play` above is matched by the root (the application), by the module file (`module Ordering`) and by the feature file (`feature Ordering.Orders`), and it belongs to the feature.

Two placements where neither lies inside the other - one import puts a file in `module Ordering`, another in `module Billing` - are a conflict, reported where the second import is written. A file belongs in one place.

## Order on the event model board

When viewing a folder application, both boards compile every `.play` file in the folder in alphabetical file-path order, including unimported files and proposed new files. In VS Code, opening an importing document with no `application.play` above it instead compiles that document and the files it imports. A root supplies **presentation order only**; it never changes which files are compiled or drawn. An `application.play` with no file imports, including a layout written by `IPlayFileWriter.Expand`, keeps the existing path order.

VS Code recognizes `application.play` as the folder application root. When the folder has no `application.play`, the MCP App also recognizes a differently named importing document when it is the only importer not itself imported. A root supplies ranks only when it contains file imports. In VS Code, opening another importing document outside an `application.play` folder follows that document's imports as a standalone application; it does not discover a differently named folder root from a sibling file.

For ranked modules, features and slices, the board walks the root's text from top to bottom, following each import depth-first where it is written, before continuing with the next declaration. A file matched by several imports contributes ranks once, at the first encountered import that gives it its final, deepest placement. Declarations outside that walk stay visible after ranked siblings, stably in their previous path order. Placement scaffolding does not take the position of a module or feature declared elsewhere.

Explicit container declarations in the root, or in an enclosing container's own named file, take precedence over globbed files that restate those containers. For example, `T/T.play` declaring Recording then Approval determines their feature order even when slice files under Approval sort first. The outer named composite takes precedence over a more narrowly named file; equally enclosing named files use the shallower path, then first occurrence. Without such a file, the first explicit occurrence in the import walk determines container order. This does not reorder glob matches or slice declarations.

Glob matches stay alphabetical. To control their order, replace a glob with explicit imports in the sequence you want. Commerce's root lists Catalog, Ordering, then Fulfillment; its Products feature uses `*.play`, so its slices remain DiscontinueProduct, ProductList, then RegisterProduct. TimeTracking's root uses `**/*.play`, so its modules remain Engagements, Payroll, then Timesheets, while the features declared in Timesheets are Recording, Approval, then Reporting.

This is presentation only: it does not reorder the compiler's documents or change duplicate-declaration diagnostics, executable model bytes, revisions or identities. A single-file model keeps declaration order. A feature's own slices and its nested features remain separate groups on the board.

## Timeline diagnostics

[Declared dependencies](slices.md#declared-dependencies) state intended module and feature coupling; they are not an import or a timeline ordering rule.

The compiler checks event flow against the same presentation timeline: modules in order, with each feature's own slices before its sub-features. A projection, reducer `on E` rule or named reaction trigger using an event declared in a slice to its right reports `PLAY0516` once per consumer slice and event, at the first reference. The earliest declaring slice is the producer; external events and a slice's own events do not create a finding. Event names match using ordinal case-insensitive comparison in both compilers, including Greek sigma variants. A producer in the consumer's own sub-feature is reported too, with a note that reordering cannot fix it.

A command or reaction trigger's `reads R` also reports `PLAY0516` when the slice that builds `R` is drawn later. The message names the read model and its builder, once per consumer slice and read model at the first reference. Resolution matches the dependency graph's `decidesFrom`: projections (including variants) and reducers take precedence over the read-model declaration; without a builder, the earliest declaring slice is used. An unresolved read model or a slice's own read model creates no edge.

Feedback reads are excluded before backward findings and cycle grouping. Find the lowest common container of the reader and builder, then take the child of that container containing the reader. If the builder's projections or reducers that build the read model being read consume any event produced anywhere inside that child, the read creates no timeline edge. A projection that declares a matching variant also builds that read model; its shared blocks and variant transitions count. Unrelated projections and reducers in the same slice do not. For two slices in the same feature, that child is the reading slice itself; across features or modules, it includes all slices nested on the reader's side. Produced events include declared events and events named in commands' or reactions' `produces` clauses, even when imported and not declared locally. Reading a summary built from facts your own side produced is read-after-write feedback, not a story-order dependency. Event-consumption edges remain checked.

Mutually dependent sibling groups report one `PLAY0517` instead of individual `PLAY0516` findings within that group. The compiler considers event flow and non-feedback reads in both directions, groups dependencies at their lowest common container, and lists mutually dependent members in timeline order, naming each member's kind (`slice`, `feature` or `module`) so a slice and a sibling feature with the same name remain distinguishable. The message names dependencies on events or read models, since reordering cannot make every dependency flow left to right. Other backward edges still report individually. These are information diagnostics and do not fail `--warnaserror`.

A single document uses text order. For application or folder compilation, the ordering root is the sole root when there is one; otherwise it is the folder-root `application.play` if that file imports others, or the unique importing document not itself imported. With no such root, the compiler assigns no presentation ranks and skips the timeline check. When a folder has an `application.play` without imports, the compiler takes its order from the only importing document while the boards keep path order, so the check can describe an order the board does not draw. It checks the merged application once, not each physical file separately. This does not change merge order, syntax JSON, executable model bytes, revisions or identities.

For the full dependency inventory, use the read-only
[MCP dependency graph](mcp/reference.md#dependency-graph). Its cycles and story-order
suggestions include feedback reads as well; they do not apply a reorder.

### Repair a backward timeline reference

C# workspaces and MCP offer typed `PLAY0516` proposals for safe sibling declaration
or explicit file-import moves, including `reads` and reducer references. A glob repair pins an already placed file with an
explicit import immediately before the glob; the glob remains, so new files are
still discovered. If one pin would introduce a finding, the proposal can pin a
safe prefix instead. Names containing glob metacharacters have no pin repair.
A multi-pin proposal is one typed replacement of the import's parent container that keeps every existing node and comment and only inserts the pins before the glob.

The proposal first tries the producer before the consumer, then the consumer
after the producer. It must remove the selected finding without introducing
`PLAY0516` or `PLAY0517`. Own-sub-feature findings, cycle groups, unranked members,
mixed declaration/import boundaries and different parents have no repair.

Verification preserves comments, document placements, existing catalog assignments
and executable readiness. In a fresh workspace, a proposal can establish missing
document assignments using the existing document IDs and stable keys; it refuses
any other catalog change. Assignments are persisted only on apply.
Executable models require identical ESM bytes. When neither
side binds, a separate proof compares merged syntax modulo only timeline sibling
order and verified import pins, and preserves admission diagnostic counts and
severities. A one-sided change in model availability is refused. Repairs also
refuse canonical printing that disagrees with the simulated ranks, such as import
hoisting past declarations.

Use [MCP diagnostic repairs](mcp/authoring-tools.md#fix-a-diagnostic) to discover,
preview and explicitly apply one proposal, then rediscover against its new source
revision. These repairs require canonical formatting consent. There is no
TypeScript quick fix or pinned-evidence editor action for `PLAY0516`.

## Patterns

A path is relative to the folder of the file that writes the import.

| Pattern | Matches |
| --- | --- |
| `Orders.play` | That one file. It must exist. |
| `*.play` | Every `.play` file in the same folder. |
| `*/*.play` | Every `.play` file one folder down. |
| `**/*.play` | Every `.play` file at any depth, including this folder. |
| `Orders/Place?rder.play` | `?` stands for one character. |
| `../Shared/*.play` | `..` climbs out of the folder. |

Only `.play` files ever match, and a file never imports itself, so `import "*.play"` in a module file imports its siblings and not the module file. A pattern with a wildcard that matches nothing is a warning; a plain path to a file that does not exist is an error.

## What a placed file may hold

Whatever its placement, a file may declare what belongs to the application as a whole - concepts, types, policies, triggers, layouts and the rest - so a slice file can declare the concept only it uses. On top of that, its top level holds the body of where it is placed:

| Placed in | Its top level may also hold |
| --- | --- |
| the application | `module` |
| a module | `description`, `authorize`, `import`, `screen template`, `dialog template`, `form`, `contribute`, `feature`, `on`, `uses` |
| a feature | `description`, `authorize`, `import`, `feature`, `slice`, `contribute`, `on`, `uses` |

A file placed in a module may restate `module Ordering` - the module it is placed in - and its body joins the placement. Declaring any other module is an error. A `description` or `authorize` in a placed file belongs to the module or feature it is placed in, and merges with the others exactly as it does across a folder.

A file that is never imported is compiled as a whole document when you compile its folder, so a slice file nobody imports reports that `slice` belongs in a module or feature.

## Compile an application

The CLI compiles the application a file is the root of, or every file in a folder:

```shell
screenplay application.play     # the root and everything it imports
screenplay .                    # every file in the folder - imports still place what they import
```

From code, `IPlayFileCompiler.CompileApplication(path)` follows the imports from a root file, and `CompileFolder(root)` treats every file in the folder as a root. Workspaces and the executable semantic model resolve imports against their own documents, so a document set behaves the same way in a build, in an editor and over MCP. `PlayImports.Resolve` exposes the resolution itself - which documents make up the application and where each one is placed - for tools that want it.

A single-root compilation traverses imports depth-first in authored order. Folder
compilation retains alphabetical file-path order for merging; import order supplies
presentation ranks separately, as described above. Wildcard matches remain ordered
by path. `expand-layout` uses those presentation ranks when writing child imports,
so repeated expansion preserves the authored module, feature and slice order without
changing merge precedence or event ownership. Expansion uses the timeline's
ordering-root selection, including a lone importing barrel beside an import-less
`application.play`. With no ordering root, it uses path order. Folder layouts
written without imports by `IPlayFileWriter.Expand` still compile in path order.

## See also

- [Folders](folders.md) - how the documents of a folder merge into one application.
- [Grammar](grammar.md) - the `FileImport` production.
- [Diagnostics](diagnostics.md#file-imports) - `PLAY0454` to `PLAY0460`.
- The `Samples/Commerce` and `Samples/TimeTracking` folders in this repository - two applications composed from focused files.
