# Screens

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
  action RegisterInvoice
    navigate to RegisterInvoiceScreen
  action CancelInvoice
```

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

## Level 3 — Structure with inline code

Combines screen templates, structural sections, and inline React/HTML/TypeScript blocks. The surrounding Screenplay context provides the typed data contract; the inline block receives it as `Props`.

````screenplay
screen InvoiceDashboard
  template Dashboard
    header
      section title
        data InvoiceSummaryReadModel via query GetInvoiceSummary
        react
          ```
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

```
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
