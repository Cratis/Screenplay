# Screens

Screens are UI declarations. They live inside `StateView` slices and support three levels of abstraction — from pure intent (Studio generates the component) to layout with inline code — plus a full external file reference.

## Layout templates

Reusable screen templates with named slots, declared at module level:

```screenplay
layout <Name>
  template
    <slot-name>+
```

```screenplay
layout MasterDetail
  template
    sidebar
    main

layout DashboardLayout
  template
    header
    left
    right
    footer
```

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

Adds named sections, tables, forms, and summary widgets, laid out in a layout template's slots:

```screenplay
screen InvoiceDetails
  layout MasterDetail
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

## Level 3 — Layout with inline code

Combines layout templates, structural sections, and inline React/HTML/TypeScript blocks. The surrounding Screenplay context provides the typed data contract; the inline block receives it as `Props`.

```screenplay
screen InvoiceDashboard
  layout DashboardLayout
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
```

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
| `csharp` | Server-side logic (validation, reactor bodies, command handlers) |
