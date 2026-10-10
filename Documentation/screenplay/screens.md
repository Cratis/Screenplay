# Screens

A screen accepts one quoted or fenced-text `description` body line, printed first before `file` or other directives. This is report-only authoring metadata, not a localizable `title`, and does not render text in the UI. Sections and nested directives do not gain descriptions. Markdown `documentation` is not supported here.

Screens are UI declarations. They live inside `StateView` slices and support three levels of abstraction — from pure intent (Studio generates the component) to a filled template with inline code — plus a full external file reference.

## The structure a screen fills

A screen is an **instance**: it names the structure it fills and provides the content. That structure is a [screen template or dialog template](templates.md), declared at module level with named slots:

```screenplay
module Invoicing
  screen template MasterDetail
    fits slot content

    sidebar
    main
```

A screen never names the application's `layout` - the shell is selected once per build by a [ui profile](ui-profile.md), which is what keeps a screen portable across web, mobile and desktop.

A slot may also declare `contributes <ContributionPoint>`, opening it up to many contributors declared anywhere in the module/feature tree instead of the one parent that owns the slot - see [Contributions](contributions.md).

A template also says how its slots share space and vary by device size - responsive `flow` or pixel-precise `freeform` - see [Layout arrangement](layout-arrangement.md).

## Level 1 — Intent

Declares data and available actions. Studio generates the component.

```screenplay
screen <Name>
  data <ReadModel>[[] ] via query <QueryName> [by <param>]
  action <CommandName>
    [navigate to <ScreenName> [by <param>]]
    [label "<text>"]
```

```screenplay
screen InvoiceList
  data InvoiceListReadModel[] via query ListInvoices
  section registerInvoiceInput
    title "Register invoice"
    on click
      navigate to RegisterInvoiceScreen
```

A click or row-click opens a command's input screen **before** the command runs. That screen's action
issues the command and discovers its command-bound form. An action's `navigate to` is the destination
**after success**, never a route to that command's own input screen. For example, the action on
`RegisterInvoiceScreen` can return to `InvoiceList` after registration.

Screen `data ... via query ... by value` and `navigate ... by value` remain scalar lookups. They cannot supply a composite read-model key to a single-instance query. The compiler reports `PLAY0648`; no automatic screen repair is offered. Collection queries may filter by a subset. See [Read-model keys](readmodels.md#keys).

## One action, several commands

Use a label-headed **guarded action** when one user decision can run different commands as the displayed item's state changes. The label is a quoted string or a `$strings.<key>` reference; it replaces the plain action's `label` child.

```screenplay
screen PlanDetails
  data PlanReadModel via query GetPlan by planId
  section actions
    action "Read the source again"
      when item.attempt.status == "open" and item.attempt.answeredAt == null execute ReleaseStaleAttempt
        with attemptId from item.attempt.attemptId
      when item.attempt.status == "failed" execute ReleaseStaleAttempt
        with attemptId from item.attempt.attemptId
      when item.attempt == null execute RetryUnclaimedPlan
        with planId from item.planId
      otherwise hidden
      navigate to PlanList
```

### Subject and conditions

`item` is the nearest container's `data` item, or the selected element when that data is a collection. Sibling data in the action's section, filled slot or screen body takes precedence over outer data, regardless of source order. Without an item (loading or no selection), the action is always hidden, even with an execute fallback. Missing or equally near data declarations produce a warning rather than guessing a subject.

Conditions compare `item.<field>[.<field>…]` with a literal: a string, number, Boolean or `null`. They do not read route parameters, screen state, `$context` or `$env`, and cannot compare two item paths. Use `==`, `!=`, numeric `>`, `>=`, `<`, `<=`, or string `contains` and `starts with`. Combine comparisons with `and`, `or` and parentheses; `and` binds more tightly. Collection-valued fields are not supported in conditions. Unsupported punctuation, such as brackets around a literal, is rejected with `PLAY0344` rather than discarded.

A comparison over a missing or null `item.` field is false, including `!=` against a non-null literal. The exception is `== null`, which matches an explicitly null value, not a missing field. Missing and null are not interchangeable. Enum names compare case-sensitively. Ordering requires numbers and text comparisons require strings; null or incompatible values make those comparisons false. `and` and `or` combine these comparison results normally.

Neither the C# nor TypeScript compiler evaluates screen conditions: they preserve and validate this syntax. Downstream renderers own runtime evaluation and must distinguish a missing field from a present field whose value is null.

### Selection, inputs and execution

Alternatives are checked in authored order whenever query data changes. The first match selects the command. If none matches, `otherwise execute <Command>` selects a fallback; `otherwise hidden` or an omitted fallback hides the action. At least one `when` is required, and the optional `otherwise` must be last among alternatives. Overlap is intentional; only a provably shadowed alternative warns.

Each alternative and execute fallback may bind inputs with `with <property> from <binding>`. A binding may pass a terminal collection field, such as `with ids from item.ids`, to a matching collection input; it cannot traverse through a collection. Inputs resolve in this order:

1. Explicit `with` bindings.
2. Subject fields with the same name as the command input.
3. The command's declared [form](forms.md).
4. Renderer input.

The guard controls what the user is offered, not what the system accepts. The selected command still enforces its authorization, validation and constraints. If the selected command is unavailable to the caller, present the action as unavailable, as [#335](https://github.com/Cratis/Screenplay/issues/335) defines once admitted. Authorization denial never falls through to another command. A click runs the choice shown to the user; if a click-time check changes that choice, the renderer must refresh instead of executing the new one. A single optional `navigate to` runs after whichever command succeeds, never to open that command's own input screen. If input needs a separate screen or dialog, open it first with a click or row-click carrying the item's identity; place the guarded action on that input screen.

Guarded actions are preserved by the compilers and shown as one labeled prototype button on the event model board. Screens do not enter the executable semantic model. Runtime selection requires downstream renderer support; an older renderer must reject the new kind rather than silently render an empty plain action. See [interaction alternatives](interactions.md#choose-an-action-list-by-item-state) for block-form choices inside `on click`, `on double click` and `on select`; those select a whole action list at gesture time.

## Level 2 — Structure

Adds named sections, tables, and summary widgets, filling a screen template's slots. Command-bound forms are a separate, module-scoped construct - see [Forms](forms.md).

```screenplay
screen InvoiceDetails
  template MasterDetail
    sidebar
      data InvoiceDetailsReadModel via query GetInvoice by invoiceId
      section summary
        action CancelInvoice
        action ChangeInvoiceStatus
    main
      section lineItems
        table lineItems
          column lineNumber  label "#"
          column description label "Description"
          column quantity    label "Qty"
          column unitPrice   label "Unit Price"
          on row-click navigate to InvoiceLineDetail by lineNumber
```

Widgets:

| Widget | Contents |
| --- | --- |
| `table <name>` | `column <property> [label "<text>"]` rows and `on row-click navigate to <Screen> [by <param>]` |
| `summary <ReadModel>` | `field <property> label "<text>"` rows |
| `title "<text>"` | A section title |
| `toolbar <name>` | `item` actions, navigation entries and dialog entries with labels, icons, parameters and presentation hints |
| `component <Package.Component> <name>` | A package component instance with typed bindings, literal properties, presentation hints, outputs and recursive outlets |

## Package components and typed bindings

Use `component` when a package supplies the widget, but the authored screen still needs a lossless Screenplay representation for Studio, Stage and MCP tools.

```screenplay
screen BrowseInvoices
  data InvoiceList via query AllInvoices

  toolbar main
    item edit action EditInvoice
      label "Edit"
      icon edit
      presentation placement "primary"
    item details navigate to InvoiceDetails
      parameter invoiceId from component "invoices:list".selectedItem.id

  component scene.web.DataGrid invoices
    id "invoices:list"
    context from query AllInvoices.items
    property selectedItem from component "invoices:list".selectedItem null preserve
    property pageSize from literal 25
    property title = "Invoices"
    property selectable = true
    property emptyState = {"title":"No invoices","actions":["create"]}
    property noSelection = null
    icon table
    presentation density "compact"
    exposes selectedInvoice from component "invoices:list".selectedItem
    outlet detail
      summary selectedInvoice
        field invoiceId label "Invoice"
```

Bindings are typed. A legacy bare binding such as `context selectedInvoice` is still accepted and lowers to `from data selectedInvoice`.

| Source | Syntax | Meaning |
| --- | --- | --- |
| Data context | `from data <path>` | Reads from the inherited data context for the element. |
| Query result | `from query <QueryName>[.<path>]` | Reads from the latest result of a named screen query. |
| Component output | `from component <stableInstanceId>.<outputPath>` or `from component "<stableInstanceId>".<outputPath>` | Reads an exposed value from another stable component instance. Use the quoted form when the stable id contains punctuation. |
| Literal binding | `from literal <value>` | Supplies a typed literal as a binding value. |
| Literal property | `property <name> = <value>` | Assigns a typed literal property value, not a binding. |

A component may declare `id "<stable-id>"` to preserve the exact stable instance id Studio and Stage use for bindings. If it omits `id`, the authored component name remains the stable id. The compiler does not normalize quoted ids.

Literal values are typed: strings, numbers, booleans, `null`, arrays and objects all round-trip as literal syntax nodes. They are not lossy strings.

A binding may carry `mode oneWay`, `mode twoWay`, `null propagate`, `null clear`, `null preserve` and `expected <Type>`. Unsupported modifiers, malformed query bindings and malformed component bindings are reported as diagnostics and the raw authored text is preserved in the AST so an authoring tool can show and repair it. The compiler does not guess Stage-specific prefixes or reinterpret an invalid string.

These nodes are authoring syntax. The compilers preserve them, the MCP schema exposes them, and Stage decides whether a package/profile combination can execute them. An older renderer must reject unsupported components, packages or binding kinds explicitly instead of silently dropping the modeled UI.

## Navigation metadata

A navigation can carry an authored route and named parameters without turning a screen into a router implementation:

```screenplay
navigate to InvoiceDetails
  route "/invoices/{invoiceId}"
  parameter invoiceId from data selectedInvoice.invoiceId
```

Toolbar navigation items use the same parameter binding syntax. Route strings and parameter bindings are preserved through parser, printer, JSON schema and MCP edit cycles.

## Level 3 — Template with inline code

Combines screen templates, structural sections, and inline React/HTML/TypeScript blocks. The surrounding Screenplay context provides the typed data contract; the inline block receives it as `Props`.

````screenplay
screen InvoiceDashboard
  template Dashboard
    header
      section title
        data InvoiceSummaryReadModel via query GetInvoiceSummary
        ```react
          export default ({ data }: Props) => (
            <header className="dashboard-header">
              <h1>Invoice Dashboard</h1>
              <span className="badge">{data.totalCount} invoices</span>
            </header>
          );
          ```
    left
      section overdue
        data OverdueInvoicesReadModel[] via query GetOverdueInvoices
        table OverdueInvoicesReadModel
          column invoiceNumber label "Invoice #"
          column dueDate       label "Due Date"
          on row-click navigate to InvoiceDetails by invoiceId
````

## How a name resolves

A screen binds to things by name — `via query All`, `action RegisterInvoice`, `navigate to InvoiceDetails`. A bare name resolves **from the inside out**: the slice it is written in, then the enclosing feature, then the module, then the document. The innermost match wins.

That rule exists because a document generated from code cannot make every name unique. Query names come from C# method names, which are unique only per read model — one real application declares 76 queries under 37 distinct names, with `All` appearing 21 times. Two slices in one feature can each declare `All`, and each screen gets its own:

```screenplay
module Invoicing
  feature Preparation
    slice StateView Queue
      query All => QueueReadModel

      screen QueueScreen
        data QueueReadModel[] via query All

    slice StateView Deviations
      query All => DeviationReadModel

      screen DeviationScreen
        data DeviationReadModel[] via query All
```

A slice keeps its own vocabulary, and a name declared next door does not silently take over.

### Reaching across slices

A screen that aggregates read models from several slices — a routine Event Modeling shape — qualifies the name with the scope that holds it:

```screenplay
screen OverviewScreen
  data QueueReadModel[]     via query Queue.All
  data DeviationReadModel[] via query Preparation.Deviations.All
```

Any trailing part of the scope will do: `Queue.All`, `Preparation.Queue.All`, or the whole `Invoicing.Preparation.Queue.All`. Use the shortest one that is unambiguous.

### When a name matches two things equally well

If a bare name matches more than one declaration at the same depth — two sibling slices both declaring `All`, referenced from a third — the compiler **warns and names the candidates** rather than picking one:

```text
Ambiguous query 'All' - it matches 2 declarations equally well
(Invoicing.Preparation.Queue, Invoicing.Preparation.Deviations); qualify it to say which
```

Unresolved and ambiguous references are warnings rather than errors, because a name may still resolve to something outside the document. The point is that the gap is visible: before this, a screen could navigate to a screen that did not exist and nothing said so.

## File reference

Full external implementation — Stage uses the file, the Screenplay contract remains visible to Studio.

```screenplay
screen RegisterInvoiceScreen
  file Screens/RegisterInvoiceScreen.tsx
```

## Inline code languages

| Tag | Used for |
| --- | --- |
| `react` | React/TSX components |
| `typescript` | Plain TypeScript |
| `html` | Static HTML |
| `csharp` | Server-side logic (validation, reaction bodies, command handlers) |
