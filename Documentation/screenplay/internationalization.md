# Internationalization

User-facing text — validation messages, screen titles, labels — should not be welded into the declaration of behavior. Screenplay separates the two: the `.play` file references strings by key, and the text per locale lives in companion `.strings` files next to it.

## Strings files

By convention `MySystem.play` pairs with `MySystem.<locale>.strings` — one file per locale:

```text
invoicing.play
invoicing.en.strings
invoicing.nb.strings
```

The format is line based, with dotted keys and `//` comments:

```text
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

- the `message` operand of validation rules and command `require` guards,
- the `label` operand of screen actions, table columns and summary fields,
- the `title` operand of screens and sections,
- the `label` operand of a [contribution](contributions.md)'s `contribute to` block,
- the `label` operand of a [form](forms.md) field.

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

## Executable semantic model and rejections

The executable semantic model (ESM) retains message references symbolically on command and concept validation rules, command `require` guards, and append-time constraints. A value beginning with `$strings.` is a key, **not text to display**. Its dotted key must follow the `.strings` assignment-key grammar; malformed keys block semantic binding (`PLAY0357`). The reference evaluator returns the original key in `SemanticRejected.Details` with `MessageIsStringKey = true`. A realization resolves that key using its active locale's paired `.strings` file before presenting the rejection. Literal messages and generated default messages have `MessageIsStringKey = false`. The reference evaluator does not load string tables or choose a locale.

For specifications, write `then error "$strings.invoices.validation.reasonRequired"` to assert a localized rejection key. The `then error` grammar accepts quoted messages only; it does not accept an unquoted `$strings` token. See [Specifications](specifications.md#rejections).

## How references are stored and printed

For the `message`, `label` and `title` operands the reference is stored in the same string property as a literal would be — as the literal text `$strings.<key>`. A consumer recognizes a localized value by the `$strings.` prefix. When printing, the [printer](printing.md) emits values starting with `$strings.` unquoted, so a compile → print → recompile round trip preserves the reference exactly.
