# Captures

Captures convert external data into events — change data capture as a first-class construct. The body of a `capture` block is written in the **Change Data Capture Language (CDL)** — an embedded sub-grammar the Screenplay parser delegates to (see [Sub-language Pluggability](sub-languages.md)). Captures live inside `Translate` slices.

## Syntax

```screenplay
capture <Name>
  <CDL grammar>
```

## Example

```screenplay
capture LegacyInvoiceCapture
  source api
    api LegacyInvoicingApi
    route /invoices
    poll 5m
  key id
  map
    status = status translate
      "utkast"     => draft
      "sendt"      => sent
      "betalt"     => paid
      "kansellert" => cancelled
  append InvoiceStatusChanged
    when status
      invoiceId = $.id
      status    = $.status
      changedAt = $context.occurred
  children lineItems identified by lineNumber
    append InvoiceLineItemAdded
      when added
        invoiceId  = $.id
        lineNumber = $.lineNumber
        quantity   = $.quantity
        unitPrice  = $.unitPrice
    append InvoiceLineItemRemoved
      when removed
        invoiceId  = $.id
        lineNumber = $.lineNumber
```

## The CDL vocabulary at a glance

| Construct | Meaning |
| --- | --- |
| `source` | Where the captured data comes from (`api` with `api`, `route`, and `poll` interval). |
| `key <property>` | Which source property identifies an instance. |
| `map` | Maps and translates source values before events are appended. |
| `translate` | Maps source values to Screenplay values (`"utkast" => draft`). |
| `append <EventType>` | Appends an event when the source changes. |
| `when <property>` | Appends when the named property changes. |
| `when added` / `when removed` | Appends when an item appears in / disappears from the source. |
| `children <collection> identified by <key>` | Captures changes in a child collection. |
| `$.<property>` | A value from the current source item. |
| `$context.occurred` | The capture timestamp. |

## Full CDL grammar

The authoritative CDL grammar is published with the Chronicle documentation alongside the PDL grammar.
