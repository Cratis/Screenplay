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

Both boards compile every `.play` file in the folder in alphabetical file-path order, including unimported files and proposed new files. A root supplies **presentation order only**; it never changes which files are compiled or drawn. An `application.play` with no file imports, including a layout written by `IPlayFileWriter.Expand`, keeps the existing path order.

VS Code recognizes `application.play` as the folder application root. The MCP App also recognizes a differently named importing document when it is the only importer not itself imported. A root supplies ranks only when it contains file imports. In VS Code, opening another importing document outside an `application.play` folder follows that document's imports as a standalone application; it does not discover a differently named folder root from a sibling file.

For ranked modules, features and slices, the board walks the root's text from top to bottom, following each import depth-first where it is written, before continuing with the next declaration. A file matched by several imports contributes ranks once, at the first encountered import that gives it its final, deepest placement. Declarations outside that walk stay visible after ranked siblings, stably in their previous path order. Placement scaffolding does not take the position of a module or feature declared elsewhere.

Explicit container declarations in the root, or in an enclosing container's own named file, take precedence over globbed files that restate those containers. For example, `T/T.play` declaring Recording then Approval determines their feature order even when slice files under Approval sort first. The outer named composite takes precedence over a more narrowly named file; equally enclosing named files use the shallower path, then first occurrence. Without such a file, the first explicit occurrence in the import walk determines container order. This does not reorder glob matches or slice declarations.

Glob matches stay alphabetical. To control their order, replace a glob with explicit imports in the sequence you want. Commerce's root lists Catalog, Ordering, then Fulfillment; its Products feature uses `*.play`, so its slices remain DiscontinueProduct, ProductList, then RegisterProduct. TimeTracking's root uses `**/*.play`, so its modules remain Engagements, Payroll, then Timesheets, while the features declared in Timesheets are Recording, Approval, then Reporting.

This is presentation only: it does not reorder the compiler's documents or change duplicate-declaration diagnostics, executable model bytes, revisions or identities. A single-file model keeps declaration order. A feature's own slices and its nested features remain separate groups on the board.

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

## See also

- [Folders](folders.md) - how the documents of a folder merge into one application.
- [Grammar](grammar.md) - the `FileImport` production.
- [Diagnostics](diagnostics.md#file-imports) - `PLAY0454` to `PLAY0460`.
- The `Samples/Commerce` and `Samples/TimeTracking` folders in this repository - two applications composed from focused files.
