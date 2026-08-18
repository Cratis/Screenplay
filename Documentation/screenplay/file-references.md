# File references

`file <path>` is the one keyword Screenplay points at code with. It appears in two places that look the same and mean slightly different things, and the construct it sits on is what tells them apart.

## Two relationships, one word

**On a construct that has an implementation**, `file` stands in for the inline body — *the implementation lives there, do not expect it here*. It is an alternative to a code block, and it has always worked this way:

```screenplay
command RegisterInvoice
  invoiceId InvoiceId identifier
  handler
    file Invoicing/RegisterInvoice/RegisterInvoiceHandler.cs
```

The constructs that carry it in this sense are a command's [`handler`](commands.md) and its [validation rule predicates](commands.md), a query's [`performer`](queries.md), a [reducer](readmodels.md) rule, a [reaction](reactions.md) trigger, a [`constraint`](constraints.md) and a [`screen`](screens.md).

**On a pure declaration** there is no body to delegate — a concept *is* its primitive, an event *is* its properties — so the same word can only say one other thing: *this is the file that realizes the declaration*.

```screenplay
concept InvoiceId : Uuid
  file Invoicing/InvoiceId.cs
```

The declarations that carry it are `concept`, `type`, `event`, `readmodel`, `projection`, `slice`, `specification` and the top-level `trigger`.

One keyword covers both because the construct already decides which is meant. A second keyword would carry no information a reader or a tool does not already have from the node it is reading, and would be one more word to learn.

## A worked example

```screenplay
slice StateChange RegisterInvoice
  description "Registers an invoice against a customer"
  file Invoicing/RegisterInvoice/RegisterInvoice.cs

  command RegisterInvoice
    invoiceId InvoiceId identifier

    produces InvoiceRegistered
      invoiceId = invoiceId

  event InvoiceRegistered
    file Invoicing/RegisterInvoice/RegisterInvoice.cs
    invoiceId InvoiceId

  readmodel Invoice
    file Invoicing/RegisterInvoice/Invoice.cs
    invoiceId InvoiceId

  projection Invoices => Invoice
    file Invoicing/RegisterInvoice/Invoices.cs
    from InvoiceRegistered
      invoiceId = invoiceId

  specification RegisteringAnInvoice
    file Invoicing/RegisterInvoice/when_registering_an_invoice.cs
    when RegisterInvoice
      invoiceId = "0f5f5f7f-0f6f-4f47-9f39-5c1f2f0a1a9f"
    then InvoiceRegistered
```

Several declarations naming the same file is normal and correct — a Cratis slice keeps its backend artifacts in one file by convention.

The `file` line is written directly under the header, after a `description` when there is one. That is where the printer puts it too, so a generated document and a hand-written one read the same.

## The rules a path follows

**It is repository relative, never absolute.** The same path then means the same thing wherever the document is read. An absolute path is reported as [`PLAY0264`](diagnostics.md) — a warning, so a document carrying one still compiles.

**It is never resolved.** The compiler does not look for the file, at any severity. A `.play` document is read in a designer, in a build, and on a machine where the source tree is not present, so a path that has gone stale must not be what makes an otherwise valid document invalid. Whatever *can* resolve paths — an editor, a generator, a renderer — decides for itself what an unresolvable one means.

**It never replaces the declaration.** On a declaration `file` is additive: a `projection` still declares its blocks, an `event` still declares its properties, a `type` still declares at least one property. The language's guarantee that [a document is expressible with zero `file` references](grammar.md#declarative-first--file-is-never-required) is unchanged — this adds a place to record where code ended up, not a way to leave the document unsaid.

## Telling it from a property named `file`

`event`, `readmodel` and `type` bodies also read property lines, and `file` is a legal property name. The two are told apart by shape, which is the rule [`description`](types.md) already follows in the same bodies: a type reference is a bare identifier, so a value carrying a separator or an extension is a path and nothing else.

```screenplay
type Upload
  file Attachment
  size Int
```

That declares a property named `file` of type `Attachment` — the property wins the tie, so a document written before the directive existed keeps meaning what it meant. A single-segment path with no extension (`file Makefile`) is read as a property for the same reason; write it as the repository relative path it is.

A `trigger` body has always reserved the word, so there the directive wins and a trigger value named after it is written `@file`:

```screenplay
trigger LedgerFileArrived
  description "A ledger export landed in the drop folder"
  file Integrations/LedgerFileTrigger.cs
  @file
  name
```

## Reading them from the syntax tree

Every one of these is a `FileReferenceSyntax` on the node that declares it, reached through [`ScreenplaySyntaxWalker.VisitFileReference`](visitors.md). Override that one method and you see every file a document names, whichever construct named it.

## See also

- [Grammar](grammar.md) — the EBNF, and the declarative-first guarantee.
- [Diagnostics](diagnostics.md) — `PLAY0264`, and why an unresolvable path is not one.
- [Visitors and traversal](visitors.md) — walking a document to collect what it points at.
