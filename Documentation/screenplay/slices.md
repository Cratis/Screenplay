# Modules, Features and Slices

## Module

The module is the top-level namespace and maps to a bounded context. One module per file is the convention but not enforced.

```screenplay
module <Name>
  [description "<text>"]
  [depends on <Name>]*

  [<screen templates>]
  [<dialog templates>]
  [<features>]
```

Screen templates and dialog templates declared at module level are described in [Layouts and templates](templates.md).

## Features

Features are vertical slice groupings. They nest arbitrarily deep for sub-features.

```screenplay
feature <Name>
  [description "<text>"]
  [depends on <Name>]*
  [feature <Name>]*   ← sub-features
  [slice <type> <Name>]+
```

## Declared dependencies

Use `depends on <Name>` on a module or feature to state which other modules or features it intends to use. Write one target per line:

```screenplay
module Payroll
  description "Pays people for approved time"
  depends on Timesheets
  feature Handover
    depends on Timesheets.Approval
    depends on Timesheets.Reporting
    depends on Runs
  feature Runs
module Timesheets
  feature Approval
  feature Reporting
```

A bare name searches the declaring container's siblings, then each ancestor's siblings, then root modules. It does not search cousins or features in another module: qualify those targets. A dotted name matches an unambiguous trailing part of a container's full address. Equally near matches raise `PLAY0198` and name the candidates.

A sibling feature shadows a same-named root module. That root module has no longer qualifying address, so rename one of the containers to make it addressable. Self, ancestor, descendant and unresolved targets raise warning `PLAY0554`. Repeated declarations of the same resolved target (including different spellings such as `Runs` and `Payroll.Runs`) raise warning `PLAY0555` and keep the first. Unresolved repeats compare by target text.

A container without declarations is not checked. Once you declare a dependency, that container's own list must cover every counted explicit reference leaving it, including references from descendant features. A module target covers all its features; a feature target covers its descendants. Parent and child inventories are independent: neither satisfies the other. References within the checked container, or into an ancestor's own slices, are ignored.

The check counts `usesFactsFrom`, `reactsTo`, `decidesFrom`, `asks` and `shows`, not specification (`verifiedWith`) or outside-context (`outsideTheModel`) edges. It reports one warning `PLAY0552` per checked container and uncovered producer module, at the container header, naming the shared feature path when possible and listing source evidence. An unused valid declaration gets information `PLAY0553` on its line. Two containers declaring each other get information `PLAY0556` on each declaring line; this does not permit cycles or prohibit reverse coupling.

Ambiguous graph ownership never produces an undeclared warning. Any declaration covering a candidate is provisionally satisfied and is not reported as unused. The MCP [declarations view](mcp/reference.md#dependency-graph) shows this uncertainty. The graph covers explicit slice references only, not code, expressions or module-owned forms.

Declarations are optional authoring metadata, not execution or ordering rules: they add no executable model bytes and do not change revisions or identities. The printer places them after `description`, in authored order. [Folder models](folders.md) accumulate them per owner; module and feature rename repairs proven targets and refuses unresolved or ambiguous targets that could name the renamed container, or a rename that would capture another target.

## Slices

The slice is the atomic unit of behavior, aligned with Event Modeling. A slice has a type and a name, and contains the constructs that implement the behavior.

```screenplay
slice <SliceType> <Name>
  [direction inbound|outbound]    // Translate only
  [description "<text>"]
  [file <path>]
  <constructs>
```

The optional `file` line names the repository relative file this declaration is realized by, so a document can be navigated back to the code it describes. It is additive - it never stands in for any part of the declaration. See [File references](file-references.md).

## Descriptions and documentation

Use `description` for a human-readable summary. On a [specification](specifications.md), it names the rule or case that scenario witnesses. Use `documentation` for longer reasoning: assumptions, boundaries and rejected alternatives that should stay with the model.

Modules, features, slices, commands, events, read models and reactions accept one nonempty `documentation` block with a `markdown` fence. It belongs directly in the declaration body, not under a reaction trigger. A bare `documentation` line never declares a property; `documentation String` still declares a typed property where properties are allowed.

````screenplay
module Invoicing
  description "Bills customers"
  documentation
    ```markdown
    ## Boundary
    Invoicing records billing decisions. Delivery is a separate workflow.
    ```
````

Both fields are report-only authoring metadata (`PLAY0270`), not executable conditions. They do not add executable-model bytes or require a new ESM version. The printer keeps them on their owning declaration, and MCP `declaration-details` includes both fields in its summary wherever supported. In a folder model, the first module or feature documentation is kept; identical copies are accepted, while conflicting copies warn with `PLAY0559`.

Malformed, empty or repeated documentation on these new owners reports `PLAY0558`; events retain `PLAY0477`. Other kinds, including projections and screens, do not accept documentation. These diagnostics have no automatic repair: choose the text to retain, the correct fence language or the owning file explicitly.

## Descriptions

Modules, features, slices, [personas](personas.md), [commands](commands.md), [read models](readmodels.md), [reactions](reactions.md), and [specifications](specifications.md) take an optional `description` as their first body line — a human-readable summary consumers such as Prologue surface when presenting the model. At most one per declaration.

```screenplay
module Invoicing
  description "Everything related to invoicing customers"

  feature InvoiceManagement
    description "Registering and managing the lifecycle of invoices"

    slice StateChange RegisterInvoice
      description "Registers a new invoice"
```

When one line is not enough, use a fenced block — the same ``` convention as inline code blocks. The fenced text is kept verbatim:

````screenplay
module Invoicing
  description
    ```text
    Everything related to invoicing customers.
    Registration, lifecycle and payment tracking of invoices.
    ```
````

### Slice types

| Type | Description |
| --- | --- |
| `StateChange` | A command → events flow; something that changes the system |
| `StateView` | A query + projection + screen; something that reads the system |
| `Automation` | A reaction or reducer; something that runs when something happens |
| `Translate` | Translates outside occurrences to private local facts (inbound), or private facts to a public contract (outbound) |

### Translation direction (source only)

Declare `direction inbound` or `direction outbound` once directly inside a `Translate`
slice. Inbound includes existing captures of outside data and translation of another
application's public events. Outbound describes publishing a local public contract.
Direction on any other slice type, an unknown value, or a repeated directive is an error
(`PLAY0027`, invalid slice declaration). Legacy directionless Translate slices remain
accepted and are interpreted as inbound; the printer does not insert a directive into them.

`SliceSyntax.Direction` (`direction` in TypeScript) is a nullable property preserving whether the author wrote
the directive; `EffectiveDirection` supplies inbound for legacy translations and null
for other slice types. Both compilers' printers, typed JSON and the authoring workspace preserve the
explicit value, and Monaco and VS Code complete and validate it. Any explicit direction currently refuses semantic
compilation with `PLAY0268` naming #480. This allocates no ESM version and implements
no publishing, subscription, delivery or Stage behavior. Public/private usage constraints and translation
cardinality are checked by `PLAY0596`-`PLAY0609` ([diagnostics](diagnostics.md#public-event-boundaries)); `PLAY0603`
has an editor and MCP repair that declares the one consistent direction.

An outbound slice publishes its one public event with a `projection`/`reducer` whose `=>`
target is that event ([projecting to an event](projections/index.md#projecting-to-an-event)),
or with a reaction. An inbound slice may consume another application's public events with
[`source events`](captures.md#capturing-from-public-events). Both are source-only for now.

### What goes in a slice

| Construct | Typical slice type | Page |
| --- | --- | --- |
| `event` | any | [Events](events.md) |
| `command` | `StateChange` | [Commands](commands.md) |
| `constraint` | `StateChange` | [Constraints](constraints.md) |
| `query` | `StateView` | [Queries](queries.md) |
| `readmodel` | `StateView` | [Read models](readmodels.md) |
| `projection` | `StateView` | [Projections](projections/index.md) |
| `reducer` | `StateView` | [Read models](readmodels.md#reducers) |
| `screen` | `StateView` | [Screens](screens.md) |
| `reaction` | `Automation` | [Reactions](reactions.md) |
| `capture` | `Translate` | [Captures](captures.md) |

Every one of these may appear as many times as the behavior needs — several events, several commands, [several projections](projections/index.md#several-projections-in-one-slice). Only `description` is limited to one. A slice is one behavior, not one artifact of each kind.

## Example

```screenplay
module Invoicing

  feature InvoiceManagement

    slice StateChange RegisterInvoice
      command RegisterInvoice
        ...
      event InvoiceRegistered
        ...

    slice StateView InvoiceList
      query ListInvoices => InvoiceListReadModel[]
      projection InvoiceList => InvoiceListReadModel
        ...
      screen InvoiceList
        ...
```
