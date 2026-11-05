---
title: "Cratis Screenplay localized strings"
description: "Reference localized text by key in Cratis Screenplay and supply companion .strings files; the consuming application selects the locale and resolves messages."
---

# Internationalization

Cratis Screenplay separates user-facing text from behavior through `$strings.<key>` references in a `.play` file and companion `.strings` files for each locale. Use the same key for a label or validation message across translations so changing its wording does not require changing the modeled behavior.

The compiler preserves the reference. The consuming application chooses a locale, loads the matching strings and substitutes any placeholders; a `.strings` file alone does not activate localization.

## Strings files

Place one file per locale beside the model. By convention, `MySystem.play` pairs with `MySystem.<locale>.strings`, as in this invoicing application:

```text
invoicing.play
invoicing.en.strings
invoicing.nb.strings
```

Each non-comment line assigns a double-quoted value to a dotted key. Give the same key a translated value in each locale's file:

```text
// English strings
invoices.title      = "Invoices"
invoices.registered = "Invoice {number} registered"
```

Blank lines and lines beginning with `//` are allowed. Keys start with a letter or underscore; dots separate subsequent key segments. Values cannot contain an embedded double quote. `{number}` in the example remains part of the stored text until the consumer substitutes an invoice number.

`StringsFile.Parse` reads these assignments. `Write` aligns them on the longest key to produce canonical output. `StringsFiles` discovers files beneath a root using `**/*.strings`; their references expose the base name and locale from `<base>.<locale>.strings` so a consumer can pair them with the model.

## Referencing strings with `$strings`

Use `$strings.<dotted.key>` where a value expression is accepted, including `produces` mappings and [authentication settings](authentication.md). This records a key reference rather than copying one locale's text into the model. Runtime resolution requires support in the consumer.

The following user-facing text operands also accept an unquoted `$strings.<key>` instead of a string literal:

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

The example records three keys: one validation message, one screen title and one action label. Supply all three in each locale's `.strings` file; the two assignments shown earlier illustrate the format, not a complete string table for this screen.

## Executable semantic model and rejections

The executable semantic model (ESM) retains message references on command and concept validation rules, command `require` guards, and append-time constraints. Malformed keys block semantic binding with `PLAY0357`; this checks the key's syntax, not whether a translation exists.

When a localized rule rejects an operation, the reference evaluator returns the original key in `SemanticRejected.Details` with `MessageIsStringKey = true`. The application implementing the model must resolve that key through its active locale's paired `.strings` file before displaying the message. Otherwise the user would see the key itself. Literal messages and generated default messages have `MessageIsStringKey = false`.

The reference evaluator does not load string tables, choose a locale or substitute placeholders. Those responsibilities remain with the consumer; do not assume a locale fallback or missing-key policy from the Screenplay declaration.

For specifications, write `then error "$strings.invoices.validation.reasonRequired"` to assert a localized rejection key. The `then error` grammar accepts quoted messages only; it does not accept an unquoted `$strings` token. See [Specifications](specifications.md#rejections).

## How references are stored and printed

For `message`, `label` and `title`, the syntax tree stores `$strings.<key>` in the same string property used for literal text. Consumers distinguish a reference by its `$strings.` prefix. The [printer](printing.md) emits these references unquoted, preserving them through a compile → print → recompile round trip.

A [sample data](sample-data.md) pack's `locale` controls design-data generation, not this string lookup. Configure the consumer's UI locale separately when inspecting a translated screen.
