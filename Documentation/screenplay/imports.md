---
title: "Screenplay imports: compose an application"
description: "Use quoted Screenplay imports to compose .play files and place their contents, or unquoted imports to reference external event contracts."
---

Cratis Screenplay uses quoted file imports to compose `.play` documents into one application and unquoted contract imports to reference events owned by another bounded context. Use file imports when you want a root document to choose the model's files and place their contents within modules or features.

[Folder compilation](folders.md) includes every `.play` file beneath its root, and each standalone slice document must restate its module and feature. A quoted `import "<path or glob>"` gives you another way to organize the model: name the files to include, then put the import inside the module or feature they belong to. A glob is a path pattern that matches several files.

## Import files

Start with a root document that imports the application's files. At the top level, a quoted import brings in whole documents:

**application.play**

```screenplay
domain Acme.Commerce

import "**/*.play"

concept OrderId : Uuid
```

Compile this root with `screenplay application.play` to load the files its imports reach. Here, `**/*.play` includes every `.play` file beneath the root, including scratch files. To keep work-in-progress files out of the application, put them outside the matched paths or replace the broad glob with narrower imports.

The quote is what tells the two kinds of `import` apart. `import Customers.CustomerRegistered` still names a contract from another bounded context, and `import "Ordering/*.play"` names files.

## Where an import sits decides where files belong

Put an import inside a `module` or `feature` to assign its imported files to that container. Their top-level declarations become part of the container's body rather than the application's top level. For example, a module file can describe the module and import its features:

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

The slice file needs no enclosing headers because the feature's import supplies them:

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

## Patterns

Import patterns match paths relative to the folder containing the importing file.
A whole-segment `**` matches zero or more folders; `*` and `?` stay within one path
segment. Matching is case-sensitive (only the `.play` extension check is case-insensitive).

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

A placed file can still declare application-wide elements such as concepts, types and policies. A slice file can therefore declare a concept used only by that slice. Its other top-level declarations must fit the container where it is placed:

| Placed in | Its top level may also hold |
| --- | --- |
| the application | `module` |
| a module | `description`, `authorize`, `import`, `screen template`, `dialog template`, `form`, `contribute`, `feature`, `on`, `uses` |
| a feature | `description`, `authorize`, `import`, `feature`, `slice`, `contribute`, `on`, `uses` |

A file placed in a module may restate `module Ordering` - the module it is placed in - and its body joins the placement. Declaring any other module is an error. A `description` or `authorize` in a placed file belongs to the module or feature it is placed in, and merges with the others exactly as it does across a folder.

A file that is never imported is compiled as a whole document when you compile its folder, so a slice file nobody imports reports that `slice` belongs in a module or feature.

## Compile an application

Choose `CompileApplication` when the root's imports should decide membership. Choose `CompileFolder` when every `.play` file beneath the root belongs to the model. The CLI supports both:

```shell
screenplay application.play     # the root and everything it imports
screenplay .                    # every file in the folder - imports still place what they import
```

From code, `IPlayFileCompiler.CompileApplication(path)` follows the imports from a root file, and `CompileFolder(root)` treats every file in the folder as a root. Workspaces and the executable semantic model resolve imports against their own documents. `PlayImports.Resolve` exposes which documents make up the application and where each is placed, for tools that need that information directly.

A single-root compilation traverses imports depth-first in authored order. Folder
compilation retains alphabetical file-path order for merging; import order supplies
presentation ranks separately, as described below. Wildcard matches remain ordered
by path. `expand-layout` uses those presentation ranks when writing child imports,
so repeated expansion preserves the authored module, feature and slice order without
changing merge precedence or event ownership. When collapsing imports, it also keeps
children at their parent-relative import positions among contributions, templates and
other members. Header comments stay with their physical declarations. Expansion uses the timeline's
ordering-root selection, including a lone importing root document beside an import-less
`application.play`. With no ordering root, it uses path order. Folder layouts
written without imports by `IPlayFileWriter.Expand` still compile in path order.

For layout changes that must preserve source and reject stale edits, continue with [Propose revision-safe workspace changes](folders.md#propose-revision-safe-workspace-changes). The sections below explain how imports affect board order and declarations shared across files.

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

The boards in VS Code and the MCP App mark the consumer slice's header with an information badge carrying the finding; a cycle group is marked once on the slice of its earliest backward reference.

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

When two import blocks cannot move safely as whole blocks, the repair can replace
only the affected import interval with explicit imports in the verified order.
It keeps every matched document, including files without timeline declarations,
and preserves comments and placements. The action title warns that new files
need explicit imports: a replaced glob no longer discovers them automatically.
Safe sibling moves and pins that retain a glob remain the first choices.

The proposal first tries the producer before the consumer, then the consumer
after the producer. It must remove the selected finding without introducing
`PLAY0516` or `PLAY0517`. Own-sub-feature findings, cycle groups, unranked members,
mixed declaration/import boundaries and different parents have no repair.

Verification preserves comments, document placements, existing catalog assignments
and executable readiness. In a fresh workspace, a proposal can establish missing
document assignments using the existing document IDs and stable keys; it refuses
any other catalog change. Assignments are persisted only on apply.
Executable models require identical executable semantic model (ESM) bytes. When neither
side binds, a separate proof compares merged syntax modulo only timeline sibling
order and verified import pins, and preserves admission diagnostic counts and
severities. A one-sided change in model availability is refused. Repairs also
refuse canonical printing that disagrees with the simulated ranks, such as import
hoisting past declarations.

Use [MCP diagnostic repairs](mcp/authoring-tools.md#fix-a-diagnostic) to discover,
preview and explicitly apply one proposal, then rediscover against its new source
revision. These repairs require canonical formatting consent. The MCP
pinned-evidence path remains unsupported for `PLAY0516`.

The TypeScript compiler exposes the same repair through `findQuickFixes` and
`prepareQuickFixes`. Supply a one-based diagnostic `line` and `PLAY0516`; for
cross-file repairs, also supply `path` and a complete `documents` map of paths to
source text. An edit's optional `path` identifies its destination document.
Without that map, the repair is limited to the current buffer. The compiler
checks the resulting syntax, placements, presentation ranks and diagnostics;
it does not claim executable-model or identity-catalog verification.

Monaco offers buffer-local actions by default. Hosts can pass an optional
`applicationModels` callback as the second argument of `createCodeActionProvider`;
it returns the complete path-to-`ITextModel` map. Cross-file actions pin every
analyzed model's version, not only models receiving nonempty edits. Request a new
action after any source changes.

VS Code offers local repairs for single-document applications. Multi-file repairs
use the existing saved-file repair session: connect the tool, save the application,
review the proposal and explicitly apply it. That session uses the C# proof above
without requesting unsupported pinned repair evidence. Buffer-local actions never
silently apply a saved-file proposal.

## Source and stream declarations across files

[Event sources](event-sources.md) stay application-owned, including in module/feature-placed files. Streams belong to their physical source, not to the importing module or the file path. Files compiled together share declarations independently of file order; a quoted import includes files, while an unquoted contract import does not supply an unknown source or stream shape. Only exact `Source.Stream` references resolve. Duplicate physical parents make every child ambiguous, and conflicting/cyclic placement must be repaired before selecting a navigable owner.

## Operation declarations across files

[Systems](systems.md) stay application-scoped even in placed files; operations belong to their declared slice. Files compiled together share explicit operation declarations. `produces Register.NotifyAccounting` can select that operation from another slice in the same assembled model. Use enough owning scope to make the reference unique; ambiguous event/operation candidates are not guessed. Newly qualified productions must resolve to an explicit operation, not an event. An unquoted contract import does not invent an operation shape or kind. These constructs remain syntax-only, not admitted by any supported ESM version (`PLAY0268`).

## Import a public event contract (source only)

`import Fulfillment.Dispatched from "Fulfillment"` names a public event contract
with an opaque origin. The qualified contract name remains separate from the origin.
The origin must be a nonblank quoted string; it is neither a file import nor a reference
that declaration rename resolves. Ordinary `import Fulfillment.Dispatched` keeps its
legacy behavior. A quoted first operand still imports files; adding `from` to a file
import is invalid.

The C# and TypeScript ASTs carry `Visibility` and `Origin` as additive properties. Origin-bearing
imports have public visibility. Both compilers' printers and typed JSON preserve both, and Monaco and
VS Code recognize them, but semantic compilation refuses them with `PLAY0268` naming #481. Stage and
renderer support does not exist, so source acceptance alone does not authorize execution.

## See also

- [Folders](folders.md) - how the documents of a folder merge into one application.
- [Grammar](grammar.md) - the `FileImport` production.
- [Diagnostics](diagnostics.md#file-imports) - `PLAY0454` to `PLAY0460`.
- The `Samples/Commerce` and `Samples/TimeTracking` folders in this repository - two applications composed from focused files.
