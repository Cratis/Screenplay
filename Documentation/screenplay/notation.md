---
title: Notation conventions
description: The symbols Screenplay keeps on purpose - Type[], the three => arrows, ==, the concept colon, backtick templates, $ value sources, the @ escape and more - with where each is admitted and why.
---

This page is a reference for the notation in Screenplay that looks like programming-language syntax. Each entry says where the notation is accepted, shows one real example and gives the reason it is kept. The language is frozen (decision 0092), so none of this changes spelling; readable alternatives are recorded as parked in [decision 0100](https://github.com/Cratis/Screenplay/blob/main/decisions/0100-retain-notation-conventions-and-park-readable-alternatives.md). The audit that produced this page is [#350](https://github.com/Cratis/Screenplay/issues/350), which [decision 0023](https://github.com/Cratis/Screenplay/blob/main/decisions/0023-command-production-model.md) left open as question 2. The [grammar](grammar.md) is the normative source; this page explains it.

Examples come from `Samples/` unless they say otherwise.

## Collections: `Type[]`

A trailing `[]` makes a collection of the type. It follows the complete type name and comes before `optional`.

```text
data PricedProduct[] via query AllPricedProducts
lines InvoiceLine[] optional
```

Source: `Samples/AcmeCommerce/Catalog/model/pricing.play:97`. Grammar: `TypeRef = QualifiedName, [ "[]" ], [ "optional" | "?" ]`. See [Types](types.md).

Reason: one suffix reads the same in every position that takes a type (event, command and read-model properties, query results, screen data).

## Arrows: `=>` has three meanings

The arrow always points from the thing you declare to what it yields or becomes. It appears in three unrelated places, and the position tells you which one it is.

| Where | Meaning | Example |
| --- | --- | --- |
| `query Name => Type` | The query result type. `observable` before the type makes it a live query. | `query AllPricedProducts => observable PricedProduct[]` |
| `projection Name => ReadModel`, `reducer Name => ReadModel` | The read model this projection or reducer builds. | `projection PricedProductProjection => PricedProduct` |
| `"source" => target` inside a capture `translate` | The value a source system sends and the value Screenplay stores. | `"utkast" => draft` |

Sources: `Samples/AcmeCommerce/Catalog/model/pricing.play:74` and `:76`; the translate example is in [Captures](captures.md). Grammar: `QueryDecl`, `ProjectionDecl`, `ReducerDecl`, `TranslateEntry`.

Reason: a read model declares what it is and never what composes it. The projection or reducer names it with `=>`, so a reader follows one direction to find where state comes from. Exactly one builder may name a given read model.

See [Queries](queries.md), [Projections](projections/index.md) and [Captures](captures.md).

## Equality: `==` and `!=`

Conditions compare a path with a value using `==`, `!=`, `>`, `>=`, `<` and `<=`, and the word operators `contains` and `starts with`. Combine conditions with `and` and `or`, and group them with parentheses. `&&`, `||` and `!` are not condition operators (see [Capture guards](#capture-guards)).

```text
produces when increase == true
require OrderStateEntry.status != "shipped"
```

Sources: `Samples/AcmeCommerce/Warehouse/model/levels.play:307` and `Samples/AcmeCommerce/Sales/model/fulfilment.play:12`. Grammar: `Condition`, `CompOp`. The same operators appear in `validate` rules such as `length ==`.

Reason: the comparison symbols are the notation most readers already know, and the issue keeps `> < >= <=` and `=` as they are.

## Declared base type: `concept X : String`

A concept names a primitive value; the colon separates the name from its base type. An `example` uses the same colon to separate its name from the command or event it instantiates.

```text
concept SupplierName : String
example AcmeInvoice : RegisterInvoice
```

Sources: `Samples/AcmeCommerce/Purchasing/model/concepts.play:2` and `Samples/Invoicing/invoicing.play:965`. Grammar: `ConceptDecl`, `ExampleDecl`. See [Concepts](concepts.md).

Reason: kept as the one declaration colon. A replacement such as `based on` would have to cover both uses, or the colon would become inconsistent.

## Text templates: backticks and `${}`

A backtick string builds text from values. `${expression}` substitutes a value.

```text
displayLabel = `${invoiceNumber} (${currency})`
```

Source: `Samples/Invoicing/invoicing.play:1770`. Grammar: `Template`. See [Expressions](projections/expressions.mdx#string-templates).

Reason: backticks mark text built from values, so a quoted string stays a plain literal. The i18n placeholder form `{name}` inside quoted strings ([Internationalization](internationalization.md)) is a separate mechanism with a different job.

Backticks are used for one other thing: the guard on a capture `when`. See [Capture guards](#capture-guards).

## Value sources: `$`

A `$` prefix names a value that does not come from the event or command being mapped. The root after `$` says where it comes from.

| Source | Reads | Where | Example |
| --- | --- | --- | --- |
| `$context.<member>` | The command, query or capture context, such as `occurred` or `tenant` | Mapping sources, policy conditions, capture maps | `registeredAt = $context.occurred` |
| `$identity.<property>` | The caller. Equivalent to `$context.identity`; claim names after `claims.` are not checked | Mapping sources, `from query ... by` | `requestedBy = $identity.userName` |
| `$env.<NAME>` | An environment variable | Mapping sources, `tag` | `source = $env.SERVICE_NAME` |
| `$strings.<key>` | A localized string, resolved against the active locale at runtime | Messages, `notify`, mapping sources | `message $strings.common.done` |
| `$refusal.<part>` | `reason`, `constraint` or `message` of the refusal a reaction is handling | Refusal branches | `reason = $refusal.reason` |
| `$eventSourceId` | The event source id of the event being projected | Projection mappings | `vendorId = $eventSourceId` |
| `$eventContext.<member>` | The event context (sequence number, correlation id, occurred) | Projection mappings | `PlacedAt = $eventContext.occurred` |
| `$causedBy[.<path>]` | Who caused the event | Projection mappings | `registeredBy = $causedBy.name` |
| `$.<field>` | The item being evaluated in a capture: the record, a child or the nested record | Capture maps and guards | `status = $.status` |

Sources: `Samples/Invoicing/invoicing.play:865`, `:3062`, `:869`, `:513`, `:2865`, `:1767`, `:3404` and `Samples/Vendors/vendors.play:156`. The `$eventContext` row is from [Event context](projections/event-context.md). Grammar: `ContextPath`, `IdentityPath`, `RefusalValue`, `DollarExpr`, `SourceItemExpr`. See [Contexts](context.md).

Reason: the sigil separates ambient values from the names of your own properties, so a property called `occurred` and `$context.occurred` never collide.

### Roots without a sigil

Two roots are local to one construct and carry no `$`:

| Root | Where | Example |
| --- | --- | --- |
| `case.<parameter>` | In a specification, naming the parameter of the current case | `customerId = case.customer` |
| `item.<field>` | In a screen interaction, naming the item the user selected | `with productId from item.productId` |

Sources: `Samples/Invoicing/invoicing.play:796` and `Samples/AcmeCommerce/Catalog/model/pricing.play:100`. Grammar: `CaseValue`, `InteractionArgument`.

Reason: these are scope-local roots, not values from outside the model. A sigil would suggest an ambient source that does not exist.

## Escaping a keyword name: `@`

Prefix a name with `@` when it is spelled like a directive and would otherwise be read as one.

```text
@tag     InvoiceTag = tag
taggedAt DateTime   = $context.occurred
```

Source: `Samples/Invoicing/invoicing.play:1228`. See the note on directives and properties in the [grammar](grammar.md), and [Concepts](concepts.md) for escaped enum members.

Reason: the parser decides by line shape first. `@` is the explicit tie-breaker for the lines where shape cannot settle it, such as a property called `authorize` next to the `authorize` directive.

The older `@pii` and `@sensitive` attributes on concepts are a different, deprecated use of the sigil. They report `PLAY0565` and have a repair. Write bare `pii` and `secret`.

## The `observable?` exception

`?` after a type is a legacy spelling of `optional`. It still parses and reports Information diagnostic `PLAY0479`. One spelling is the exception and stays canonical:

```text
query Q => observable?
```

Reason: this is a one-shot query that returns an optional scalar type named `observable`. Replacing `?` with `optional` would read as a live query of a type named `optional`. The spelling produces no `PLAY0479`, and repairs and document migrations leave it unchanged. See [Observable queries](queries.md#observable-queries).

## JSON values

Mapping sources accept single-line JSON-shaped values: arrays and objects with double-quoted keys.

```text
["draft", "sent"]
{ "channel": "email", "retries": 3 }
```

Grammar: `StructuredValue`. These are examples of the shape, not lines from a sample.

Reason: JSON is the notation readers already have for structured literals, so Screenplay does not invent one. A structured value is typed syntax in mapping sources; projection expressions keep their own expression grammar.

## Comparison symbols

`>`, `>=`, `<` and `<=` order values in conditions. `=` assigns a value in a mapping.

Reason: as for `==`. Issue #350 keeps these symbols as they are.

## Regular expressions

`matches "<regex>"` in a `validate` rule uses a restricted, flagless JavaScript regular expression, the [portable match dialect](commands.md#portable-match-dialect). It succeeds if any part of the text matches, so anchor with `^` and `$` to require a whole-value match. `matches email` is a built-in conservative pattern.

```text
matches "^[A-Za-z0-9_-]{21}$"
```

Source: `Samples/Invoicing/invoicing.play:76`.

Reason: a portable subset means every target (the reference evaluator, Stage and generated code) agrees on the result. Patterns are checked when you author them.

## Comments

In a `.play` file, `//` starts a comment that runs to the end of the line. The compiler ignores it.

```text
@authorize AuthorizationCode   // a property called authorize
```

See [Overview](overview.md#comments). Standalone projection (PDL) source also accepts `#` comments.

## Capture guards

Capture backtick guards are the only place `&&`, `||` and `!` are admitted. Everywhere else, conditions use `and`, `or` and comparison symbols. The one exception is the backtick guard on a capture append:

```text
append InvoiceFlagged
  when `status == "sent" && overdue == true`
```

Source: `Samples/Invoicing/invoicing.play:3416`.

The C# binder parses the guard and admits exactly this set: field names, string, number, boolean and `null` literals, the comparisons `==`, `!=`, `<`, `<=`, `>` and `>=`, and `&&`, `||`, `!` and parentheses. Ordering compares numbers only. A guard outside that set is reported as `PLAY0268`. The TypeScript compiler checks only that the closing backtick is present. [Captures](captures.md#in-the-executable-semantic-model) gives the semantics.

Reason: the executable model admits this grammar for captures today, so removing `&&` and `||` would change ESM semantics, which the freeze forbids. A guard written with `and` and `or` is a separate, parked proposal in decision 0100; it changes meaning rather than spelling.

## Composite key braces

Braces around a composite projection key are optional. The canonical printer writes the brace-free, indented form, and the documentation uses it. See [Keys](projections/keys.md).

## What this page does not change

No grammar, keyword or ESM change comes from this page. For each convention above, decision 0100 records the readable alternative that was considered, what it would cost and why it is parked. If a spelling changes after the freeze, the old spelling keeps working, gets an Information diagnostic and a repair, and ESM bytes stay the same.
