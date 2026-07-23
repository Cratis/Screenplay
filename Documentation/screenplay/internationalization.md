# Internationalization

User-facing text — validation messages, screen titles, labels — should not be welded into the declaration of behavior. Screenplay separates the two: the `.play` file references strings by key, and the text per locale lives in companion `.strings` files next to it.

## Strings files

By convention `MySystem.play` pairs with `MySystem.<locale>.strings` — one file per locale:

```
invoicing.play
invoicing.en.strings
invoicing.nb.strings
```

The format is line based, with dotted keys and `//` comments:

```
// English strings
invoices.title      = "Invoices"
invoices.registered = "Invoice {number} registered"
```

- One `<key.path> = "<value>"` assignment per line; blank lines and `//` comments are allowed.
- Values are standard double-quoted strings. `{placeholder}` tokens are kept verbatim — the consumer substitutes them at runtime.

`StringsFile.Parse` reads the format and `Write` produces the canonical form with assignments aligned on the longest key. `StringsFiles` discovers every `.strings` file beneath a root using the `**/*.strings` glob and exposes the base name and locale parsed from the `<base>.<locale>.strings` file name, so consumers can pair each file with its `.play` file.

## Referencing strings with `$strings`

Wherever a value expression is accepted — `produces` mappings, [authentication settings](authentication.md) — a `$strings.<dotted.key>` expression references a string by key. The compiler keeps the reference symbolic; the key resolves at runtime against the `.strings` file of the active locale.

In addition, the operands that carry user-facing text accept an unquoted `$strings.<key>` token as an alternative to a string literal:

- the `message` operand of validation rules,
- the `label` operand of screen actions, table columns and summary fields,
- the `title` operand of screens and sections.

```screenplay
command CancelInvoice
  reason String

  validate
    reason not empty  message $strings.invoices.validation.reasonRequired

screen InvoiceList
  title $strings.invoices.title
  action RegisterInvoice
    label $strings.invoices.actions.newInvoice
```

## How references are stored and printed

For the `message`, `label` and `title` operands the reference is stored in the same string property as a literal would be — as the literal text `$strings.<key>`. A consumer recognizes a localized value by the `$strings.` prefix. When printing, the [printer](printing.md) emits values starting with `$strings.` unquoted, so a compile → print → recompile round trip preserves the reference exactly.
