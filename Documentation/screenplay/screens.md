---
title: "Screenplay screens: compose a user interface"
description: "Screenplay screens connect query data to user actions. Add sections, fill template slots, or reference screen code to describe the UI a slice needs."
---

# Screens

A Screenplay screen connects the data a user needs to see with the actions they can take. Declare it inside a slice, then choose how much UI structure to describe: data and actions alone, sections that fill a template, or a template with inline code. You can also reference an external screen file.

Start with a query and the commands the user needs. Add a template when you need to place that content in named regions; use [forms](forms.md) for command input rather than redeclaring command properties on the screen.

## The structure a screen fills

A template gives screens a shared structure. It declares named slots, such as `sidebar` and `main`, and each screen supplies the content for those slots. Declare the [screen template or dialog template](templates.md) on the module:

```screenplay
module Invoicing
  screen template MasterDetail
    fits slot content

    sidebar
    main
```

The screen names a template, not the application's `layout`. A [UI profile](ui-profile.md) selects the application shell once per build, separately from the content each screen supplies.

A template may also declare [outlets](outlets.md) - item lists such as a toolbar's buttons or a list's filters - which the screen changes with `outlet <name> inherits|extends|overrides` and any module or feature adds to with [`contribute to`](contributions.md).

A template also says how its slots share space and vary by device size - responsive `flow` or pixel-precise `freeform` - see [Layout arrangement](layout-arrangement.md).

## Level 1 — Intent

Declare the query result the screen displays and the commands available to the user. This describes the screen's intent without choosing widgets or placement; rendering depends on the target.

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

Screen `data ... via query ... by value` and `navigate ... by value` remain scalar lookups. They cannot supply a composite read-model key to a single-instance query. The compiler reports `PLAY0661`; no automatic screen repair is offered. Collection queries may filter by a subset. See [Read-model keys](readmodels.md#keys).

## Action subjects

Choose whether an action applies to the selected data item (`for item`) or to the screen as a whole (`for screen`). Plain actions, guarded actions and action toolbar items accept that choice after the command or label, before an inline `label`. Plain actions also accept ordered `with <input> from <binding>` children.

Write the subject explicitly in new documents so the reader does not have to infer whether selection matters. [Action subjects](action-subjects.md) explains how the compiler supplies omitted subjects and binds command inputs.

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

For a guarded action (omitted subject or `for item`), `item` is the nearest container's `data` item, or the selected element when that data is a collection. Sibling data in the action's section, filled slot or screen body takes precedence over outer data, regardless of source order. Without an item (loading or no selection), the action is always hidden, even with an execute fallback. Missing or equally near data declarations produce a warning rather than guessing a subject.

Conditions compare `item.<field>[.<field>…]` with a literal: a string, number, Boolean or `null`. They do not read route parameters, screen state, `$context` or `$env`, and cannot compare two item paths. Use `==`, `!=`, numeric `>`, `>=`, `<`, `<=`, or string `contains` and `starts with`. Combine comparisons with `and`, `or` and parentheses; `and` binds more tightly. Collection-valued fields are not supported in conditions. Unsupported punctuation, such as brackets around a literal, is rejected with `PLAY0344` rather than discarded.

A comparison over a missing or null `item.` field is false, including `!=` against a non-null literal. The exception is `== null`, which matches an explicitly null value, not a missing field. Missing and null are not interchangeable. Enum names compare case-sensitively. Ordering requires numbers and text comparisons require strings; null or incompatible values make those comparisons false. `and` and `or` combine these comparison results normally.

Neither the C# nor TypeScript compiler evaluates screen conditions: they preserve and validate this syntax. Downstream renderers own runtime evaluation and must distinguish a missing field from a present field whose value is null.

### Selection, inputs and execution

Alternatives are checked in authored order whenever query data changes. The first match selects the command. If none matches, `otherwise execute <Command>` selects a fallback; `otherwise hidden` or an omitted fallback hides the action. At least one `when` is required, and the optional `otherwise` must be last among alternatives. Overlap is intentional; only a provably shadowed alternative warns.

Each alternative and execute fallback may bind inputs with `with <property> from <binding>`. A binding may pass a terminal collection field, such as `with ids from item.ids`, to a matching collection input; it cannot traverse through a collection. Inputs resolve in this order:

1. Explicit `with` bindings.
2. Compatible subject fields with the same case-sensitive name as the command input.
3. A unique declared nominal identifier/key match for required, non-generated identifier inputs.
4. The command's declared [form](forms.md).
5. Renderer input.

The guard controls what the user is offered, not what the system accepts. The selected command still enforces its authorization, validation and constraints. If the selected command is unavailable to the caller, present the action as unavailable, as [#335](https://github.com/Cratis/Screenplay/issues/335) defines once admitted. Authorization denial never falls through to another command. A click runs the choice shown to the user; if a click-time check changes that choice, the renderer must refresh instead of executing the new one. A single optional `navigate to` runs after whichever command succeeds, never to open that command's own input screen. If input needs a separate screen or dialog, open it first with a click or row-click carrying the item's identity; place the guarded action on that input screen.

Guarded actions are preserved by the compilers and shown as one labeled prototype button on the event model board. Screens do not enter the executable semantic model. Runtime selection requires downstream renderer support; an older renderer must reject the new kind rather than silently render an empty plain action. See [interaction alternatives](interactions.md#choose-an-action-list-by-item-state) for block-form choices inside `on click`, `on double click` and `on select`; those select a whole action list at gesture time.

## Level 2 — Structure

Fill template slots when the screen needs a particular reading or working order. In `InvoiceDetails` below, the sidebar holds the invoice data and actions, while `main` holds the line-item table. Command-bound forms remain separate, module-scoped declarations; see [Forms](forms.md).

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
| `toolbar <name>` | `item` actions, navigation entries and dialog entries with labels, icons, parameters and presentation hints, and `button` lines that invoke a State Change slice - see [Toolbar buttons](#toolbar-buttons) |
| `outlet <name> inherits\|extends\|overrides` | Changes an [outlet](outlets.md) the screen's template declares |
| `component <Package.Component> <name>` | A package component instance with typed bindings, literal properties, presentation hints, outputs and recursive slots (`slot <name>`) |

## Toolbar buttons

A `button` invokes a State Change slice. The slice names the command and the screen that opens over the page, so the button needs no command of its own - this is Scene's `ToolbarButton`. A button linked to a slice is a *hook*; see [Placement](#placement) for where it opens the slice and how it runs one without a form.

```screenplay
module Shop
  feature Orders
    slice StateChange PlaceOrder
      command PlaceOrder
        orderId Uuid identifier
        produces OrderPlaced
          orderId = orderId
      event OrderPlaced
        orderId Uuid
      screen PlaceOrder
        action PlaceOrder
    slice StateView OrderList
      readmodel OrderRow
        orderId Uuid
      screen List
        toolbar actions
          button place title "Place order" slice Shop.Orders.PlaceOrder emphasis primary icon plus
          button later title $strings.orders.later slice Orders.PlaceOrder emphasis quiet
          button draft title "Draft"
```

`button <id> title "<text>" [slice <Slice>] [emphasis primary|secondary|quiet] [icon <name>] [for item|for screen] [opens dialog|drawer|in slot <name>]`:

- `id` is unique within the toolbar, shared with the `item` names (`PLAY0765`). The title is a quoted string or a `$strings.<key>` token.
- `slice` is a dotted path `Module.Feature.Slice` or any suffix that is unambiguous (`PLAY0764` otherwise). It must name a slice the document declares (`PLAY0761`) and a State Change slice (`PLAY0762`): a button invokes State Change slices only.
- A button without a `slice` is legal while designing but invokes nothing, so it warns (`PLAY0763`).
- `emphasis` is `primary` (the main action, a filled button), `secondary` or `quiet`. Without it the toolbar default applies.
- A malformed line - no or an unquoted title, an unknown or repeated part - is `PLAY0760`.

Buttons sit beside `item` entries; the printer writes the items first, then the buttons, each in authored order. The C# compiler checks buttons (`PLAY0761`-`PLAY0765`); the TypeScript compiler reads them into the same syntax tree and reports a malformed line with `PLAY0760`. Renaming or moving a slice that a button names is refused with an explicit message - rename or move it together with the button's `slice` in coordinated typed edits. Like every screen construct, buttons are authoring syntax: the executable model does not carry them.

## Feature and module toolbars

A feature or a module adds buttons to the toolbar of every screen beneath it once, instead of repeating them on each screen. Since Screenplay 6 that is an [outlet](outlets.md): the template declares `outlet toolbar of actions`, and the feature extends it.

```screenplay
module Shop
  screen template Page
    category Page
    header
    main
    outlet toolbar of actions in header

  feature Orders
    outlet toolbar extends
      button cancel title "Cancel order" slice CancelOrder icon trash
        confirm "Cancel this order?"
      button place title "Place order" slice PlaceOrder emphasis primary icon plus
        confirm $strings.orders.confirmPlace

    slice StateChange PlaceOrder
      command PlaceOrder
        orderId Uuid identifier
        produces OrderPlaced
          for orderId
      event OrderPlaced
        orderId Uuid
      screen PlaceOrder
        action PlaceOrder

    slice StateChange CancelOrder
      command CancelOrder
        orderId Uuid identifier
        produces OrderCancelled
          for orderId
      event OrderCancelled
        orderId Uuid
      screen CancelOrder
        action CancelOrder
```

- A module, feature and screen write `outlet <name> inherits|extends|overrides`, and may `hide` an inherited button or place one `before` or `after` another - see [Outlets](outlets.md#change-an-outlet-below-the-template).
- `confirm "<text>"` (or a `$strings.<key>` token) under a `button` asks before the command runs.
- A button's `slice` is checked as on a screen (`PLAY0761`-`PLAY0764`), and an id repeated within the outlet or across the levels it inherits from is `PLAY0905`.
- Screenplay 5 wrote `toolbar <name>` on a feature or module. That form is an error since Screenplay 6 (`PLAY0896`); write `outlet <name> extends`, or run `screenplay migrate --from 5 --to 6`.

Like every screen construct these are authoring syntax: the executable model does not carry them, and the printer writes them back where they were written.

## Placement

The template offers places, the slice says which it fits, and the hook that opens it picks one.

- A template declares slots with roles (`page`, `pane`, `panel`; see [Templates](templates.md)), and the page always offers a dialog and a drawer over it.
- A slice's screen lists where it fits, in order of preference: `fits dialog, drawer, pane`. Without `fits` it fits anywhere.
- A hook - a `button` linked to a State Change slice - opens the slice's screen in its first fit the page has free, unless it names the place: `opens dialog`, `opens drawer` or `opens in slot <name>`.

```screenplay
module Catalog
  screen template ListPage
    category Lists
    main
    detail pane routed by sku optional
    outlet toolbar of actions
    outlet rowActions of actions for item

  feature Products
    outlet toolbar extends
      button add title "Add product" slice DraftProduct emphasis primary opens dialog
      button price title "Set price" slice SetProductPrice for item opens in slot detail
    outlet rowActions extends
      button withdraw title "Withdraw" slice WithdrawProduct
        runs
        enabled when item.status == "onSale"
        confirm "Withdraw this product?"

    slice StateChange DraftProduct
      command DraftProduct
        sku String identifier
        name String
        produces ProductDrafted
          for sku
      event ProductDrafted
        sku String
      screen DraftProduct
        fits dialog, drawer, pane
        action DraftProduct for screen

    slice StateChange SetProductPrice
      command SetProductPrice
        sku String identifier
        price Decimal
        produces PriceSet
          for sku
      event PriceSet
        sku String
      screen SetProductPrice
        fits pane, drawer
        action SetProductPrice for screen

    // No screen: the row's 'Withdraw' hook runs it, because its one input comes from the row.
    slice StateChange WithdrawProduct
      command WithdrawProduct
        sku String identifier
        produces ProductWithdrawn
          for sku
      event ProductWithdrawn
        sku String

    slice StateView ProductCatalog
      readmodel ProductRow
        sku String
        status String
      query AllProducts => ProductRow[]
      screen ProductCatalog
        template ListPage
          main
            table products
              column sku
              column status
          detail
            show ProductDetails by sku from products.selection

    slice StateView ProductDetails
      readmodel ProductDetail
        sku String
      query ProductBySku => ProductDetail
        by sku String
      screen ProductDetails
        data ProductDetail via query ProductBySku by sku
```

On a hook:

- `for item` makes the hook wait for a selected row, and the row supplies the command's identifier; `for screen` acts on the screen's own record. Without it the outlet decides (`outlet rowActions of actions for item`).
- A `runs` line under the button issues the command directly, without a form. Every input must resolve without one - the identifier from the item or screen the hook acts on, generated values by the command - or it is `PLAY0907`. A State Change slice issued only by a `runs` hook needs no screen.
- A slice with no screen fits nothing and can only run: a hook that names no place runs it, and one whose slice cannot run - an input needs a form - or that names a place to open it is `PLAY0908`.
- `opens in slot <name>` needs a slot the template declares (`PLAY0909`), that the screen the hook is on does not already fill (`PLAY0910`), and whose role the slice's screen lists in its `fits` (`PLAY0911`).
- `enabled when <condition>` uses the condition grammar of a [guarded action](#subject-and-conditions); `item.<field>` reads the selected row, so a hook that acts `for screen` cannot read `item` (`PLAY0915`). The same line goes under a toolbar `item`.
- `confirm "<text>"` asks before the command runs.

A malformed `fits` line - an unknown place, one listed twice, a second `fits` line, or a template's `fits slot <name>` on a screen - is `PLAY0906`.

### A slot that shows the selection

`show <Screen> by <param> from <element>.selection`, in a template slot, shows another screen for the row a table or component on this screen has selected. With a routed slot (`detail pane routed by sku`), the selection is in the address, so a shared link opens the same row. The line must stand in a template slot, name a screen the document declares and a table or component on this screen, and key the shown screen by a parameter its data is keyed by (`PLAY0914`).

### Text bound to nothing

A component with no `context`, no `property`, no `exposes`, no slot content and no behavior can only show sample text. On a screen that shows data - a State View slice's screen, or any screen with a `data` line, a table or a summary - it warns (`PLAY0912`). Bind it, or give it static text with `property <path> = "..."`. A form's screen with no data is not checked, because a component there is an input or decoration, and neither is a component a slot shows the selection of (`show <Screen> by <param> from <component>.selection`).

## Package components and typed bindings

Use `component` when a package supplies the widget. Declare its data sources, properties and outputs in Screenplay so the screen's wiring stays visible alongside the rest of the model.

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
    property emptyState
      title = "No invoices"
      actions
        item "create"
    property columns
      item
        field = "number"
        label = "Invoice"
      item
        field = "total"
        label = "Total"
    property rowActions none
    property noSelection = null
    icon table
    presentation density "compact"
    exposes selectedInvoice from component "invoices:list".selectedItem
    slot detail
      summary selectedInvoice
        field invoiceId label "Invoice"
```

Bindings are typed. A legacy bare binding such as `context selectedInvoice` is still accepted and lowers to `from data selectedInvoice`.

| Source | Syntax | Meaning |
| --- | --- | --- |
| Data context | `from data <path>` | Reads from the inherited data context for the element. |
| Query result | `from query <QueryName>[.<path>]` | Reads from the latest result of a named screen query. |
| Component output | `from component <stableInstanceId>.<outputPath>` or `from component "<stableInstanceId>".<outputPath>` | Reads an exposed value from another stable component instance. Use the quoted form when the stable id contains punctuation. |
| Literal binding | `from literal <value>`, or `from literal` with an indented block | Supplies a typed literal as a binding value. |
| Literal property | `property <name> = <value>` | Assigns a typed scalar property value, not a binding. |
| Structured property | `property <name>` with an indented block, or `property <name> none` | Assigns an object or a list, written as a [structured value](structured-values.md). |

A component may declare `id "<stable-id>"` to give bindings an exact stable instance id to reference. If it omits `id`, the authored component name remains the stable id. The compiler does not normalize quoted ids.

Literal values are typed: strings, numbers, booleans, `null`, lists and objects all round-trip as literal syntax nodes. They are not lossy strings.

Screenplay has no JSON. An object or a list is written as an indented block under the property, one level of indentation per level of the hierarchy - the same [structured value](structured-values.md) form specifications, seeds and productions use:

- an object holds one `member = value` line per member; a member that is itself an object or a list opens another indented block;
- a list holds one `item` line per element - `item <value>` for a scalar, a bare `item` with a block under it for an object;
- `property <name> none` is an empty list, and `property <name> empty` an object without members.

Writing `property columns = [{"field":"number"}]` is an error (`PLAY0833`).

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

Use an inline code block for content that needs custom rendering within a template slot. The example keeps the summary query beside its React component and uses a structural table in another slot. The component expects its renderer to supply `data` through `Props`; the fence itself does not define that type.

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

This lets slices reuse useful names such as `All` without making every query name unique across the application. Two slices in one feature can each declare `All`, and each screen resolves the query in its own slice:

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

Unresolved and ambiguous references are warnings rather than errors because a name may still resolve outside the document. Check those warnings before relying on a query, command or navigation target; qualify the name when the compiler lists competing declarations.

## File reference

Use `file` to reference an external screen implementation instead of describing its contents inline. Screenplay records the path; the rendering target determines how to use that file.

```screenplay
screen RegisterInvoiceScreen
  file Screens/RegisterInvoiceScreen.tsx
```

## Sketches, mockups and images

Early in modeling a screen is often a quick sketch, a screenshot of the system being replaced or a hand-written HTML page rather than a composed screen. Keep them with the screen so they travel with the model and reach whoever builds it:

```screenplay
screen RegisterInvoiceScreen
  sketch Screens/register-invoice.sketch.json
  mockup Screens/register-invoice/index.html
  mockup Screens/register-invoice/confirm.html
  image Screens/register-invoice-today.png
```

| Line | Holds |
| --- | --- |
| `sketch <path>` | A drawing of the screen kept as vector drawing data, such as a board sketch export. |
| `mockup <path>` | One HTML/CSS page of a coded mockup. Repeat the line for each page, in the order a reader should see them. |
| `image <path>` | A screenshot or a design export: PNG, JPG, GIF, SVG or WebP. |

A screen can carry any number of each, beside or instead of its structure. Paths are relative to the model root, like the `file` directive, and the files live in the repository next to the `.play` files. They are authoring metadata: the printer keeps them in the order written, directly after `description`, `file` and `fits`, the AST and MCP editing tools carry them, and they never change what the screen does or reach the executable semantic model.

The compiler refuses a line without a path (`PLAY0950`) and warns when an `image` is not an image file or a `mockup` is not an HTML page (`PLAY0951`). Opening a model root also checks that every file is there and inside the root, and warns about each one that is not (`PLAY0952`).

## Screen metadata

A screen accepts one quoted or fenced-text `description` body line. It describes the screen in reports and does not render UI text; use `title` for a localizable UI heading. The printer places `description` before `file` or other directives. Sections and nested directives do not accept descriptions, and screens do not support Markdown `documentation`.

## Inline code languages

| Tag | Used for |
| --- | --- |
| `react` | React/TSX components |
| `typescript` | Plain TypeScript |
| `html` | Static HTML |
| `csharp` | Server-side logic (validation, reaction bodies, command handlers) |
