# Forms

A form accepts one quoted or fenced-text `description` body line. It prints first before `populate` and fields, as report-only authoring metadata rather than a localizable UI label. Individual fields do not gain descriptions. Markdown `documentation` is not supported here.

A screen's `action` directive exposes a command, but says nothing about how a user enters the data that command needs. Naming every field on the screen that invokes it would tie one input surface to one place it can be invoked from - a `form` is that input surface, declared once and reused wherever its command is.

## Syntax

```screenplay
form <Name> for <Command>
  populate via query <Query> [by <param>]
  # -- or --
  populate from item

  generation auto|manual

  field <property> [from <source>|compose using <Callback>] [label "<text>"]
  ...

  columns auto
  # -- or --
  columns manual
    column <property> [label "<text>"]

  layout
    column <index> [width <width>] [min <width>] [max <width>]
    columnGap <width>
    rowGap <width>
    place <field> row <row> column <column> [rowSpan <n>] [columnSpan <n>] [width <width>]

  on submit navigate to <Screen> [by <param>]
```

- `form <Name> for <Command>` - top level, alongside `screen template` and `feature`, inside a `module`. A module can declare more than one; each name must be unique.
- `populate` - where the form's initial values come from. At most one per form, and optional - a form with no `populate` starts empty.
- `generation` - preserves Scene's `FormGenerationMode`: `auto` means generate fields from command metadata, while `manual` means the authored fields are the contract. At most one per form.
- `field` - binds one of the command's properties to the form. Zero or more.
- `columns` - the legacy display-order hint. `columns auto` lets the package infer columns, while `columns manual` lists the command properties in display order. At most one per form.
- `layout` - Scene 4.12 command-form geometry: authored columns, field placements and gaps. It is independent of `generation` and does not reinterpret `compose using` as layout metadata.
- `on submit` - what happens after a successful submit. At most one per form, and optional - a form with no `on submit` stays on the current screen.

## Example

```screenplay
form RegisterInvoiceForm for RegisterInvoice
  populate via query GetInvoiceDraft by invoiceId

  field customerName
  field dueDate label "Due date"
  field totalAmount from calculatedTotal
  field lineItems compose using BuildLineItems

  generation manual
  columns manual
    column customerName label "Customer"
    column dueDate label "Due date"
    column totalAmount

  layout
    column 1 width 1fr min 240px max 50%
    column 2 width 2fr
    columnGap 24px
    rowGap 16px
    place customerName row 1 column 1 width auto
    place dueDate row 1 column 2 width 100%
    place totalAmount row 2 column 1 columnSpan 2

  on submit navigate to InvoiceList by invoiceId
```

## A form is discovered, not referenced

A form never appears in a screen's directive tree the way a `table` or `summary` does. It is discovered by its `for <Command>` binding wherever that command is invoked - an `action RegisterInvoice` on any screen renders `RegisterInvoiceForm` as its input surface automatically, the same way a `ui profile` is discovered by a build rather than named on a screen. This keeps one command's input surface in one place, however many screens invoke it.

## Populating a form

- `populate via query <Query> [by <param>]` - seeds the form's initial values from a query result, the same shape a screen's `data` directive uses.
- `populate from item` - reuses an item already bound in scope, such as the row a table's `on row-click` navigated from. No new binding mechanism - it resolves the same way every other bare name in the document does (see [How a name resolves](screens.md#how-a-name-resolves)).

## Fields

A bare `field <property>` binds straight to the command property of the same name. Three optional refinements adjust that:

| Form | Meaning |
| --- | --- |
| `field <property> label "<text>"` | Overrides the display label. |
| `field <property> from <source>` | Binds from a differently-named source property. |
| `field <property> compose using <Callback>` | Computes the value from a callback instead of binding it directly. |

`from` and `compose using` are mutually exclusive on one field; either may still carry a `label`. A field's `label` accepts an unquoted `$strings.<key>` token in place of a literal, the same as everywhere else in the document - see [Internationalization](internationalization.md).

## Columns and layout

Use `columns auto` when the native command form runtime should derive the display order from the command and package defaults. Use `columns manual` when the business order matters or the screen needs deterministic columns across packages.

```screenplay
form RegisterInvoiceForm for RegisterInvoice
  field customerName
  field dueDate
  field totalAmount

  columns manual
    column customerName label "Customer"
    column dueDate label "Due date"
    column totalAmount
```

A manual column names a command property already present in the form. The compiler preserves the column order and labels for renderers; a renderer that cannot honor manual columns must diagnose that mismatch instead of silently reverting to an automatic layout.

`layout` carries the richer Scene 4.12 geometry contract. Widths use the same units as Scene: `auto`, fractional units such as `1fr`, pixels such as `240px`, and percentages such as `50%`. Column indexes, rows and columns are one-based for authored stability. `rowSpan` and `columnSpan` are optional positive integers.

```screenplay
form RegisterInvoiceForm for RegisterInvoice
  generation manual
  field customerName
  field dueDate
  field totalAmount

  layout
    column 1 width 1fr min 240px max 50%
    column 2 width 2fr
    columnGap 24px
    rowGap 16px
    place customerName row 1 column 1 width auto
    place dueDate row 1 column 2 width 100%
    place totalAmount row 2 column 1 columnSpan 2
```

`compose using` remains a value-composition hook only. Do not use it to smuggle width, row or placement metadata; renderers consume `layout` for geometry.

## Submitting

`on submit navigate to <Screen> [by <param>]` reuses the same navigation shape a screen's `action` and `on row-click` use. Omit it and a successful submit simply leaves the user where they were.

## How references resolve

`for <Command>`, `populate via query <Query>`, and `on submit navigate to <Screen>` all resolve the same inside-out way every other bare name in the document does (see [How a name resolves](screens.md#how-a-name-resolves)) - unresolved and ambiguous references are warnings, not errors, because a name may still resolve to something outside the document. A form sits at module level rather than inside a slice, so it disambiguates by module but not by feature or slice, since it does not sit inside either.
