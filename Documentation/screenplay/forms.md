# Forms

A screen's `action` directive exposes a command, but says nothing about how a user enters the data that command needs. Naming every field on the screen that invokes it would tie one input surface to one place it can be invoked from - a `form` is that input surface, declared once and reused wherever its command is.

## Syntax

```screenplay
form <Name> for <Command>
  populate via query <Query> [by <param>]
  # -- or --
  populate from item

  field <property> [from <source>|compose using <Callback>] [label "<text>"]
  ...

  on submit navigate to <Screen> [by <param>]
```

- `form <Name> for <Command>` - top level, alongside `layout` and `feature`, inside a `module`. A module can declare more than one; each name must be unique.
- `populate` - where the form's initial values come from. At most one per form, and optional - a form with no `populate` starts empty.
- `field` - binds one of the command's properties to the form. Zero or more.
- `on submit` - what happens after a successful submit. At most one per form, and optional - a form with no `on submit` stays on the current screen.

## Example

```screenplay
form RegisterInvoiceForm for RegisterInvoice
  populate via query GetInvoiceDraft by invoiceId

  field customerName
  field dueDate label "Due date"
  field totalAmount from calculatedTotal
  field lineItems compose using BuildLineItems

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

`from` and `compose using` are mutually exclusive on one field; either may still carry a `label`.

## Submitting

`on submit navigate to <Screen> [by <param>]` reuses the same navigation shape a screen's `action` and `on row-click` use. Omit it and a successful submit simply leaves the user where they were.

## How references resolve

`for <Command>`, `populate via query <Query>`, and `on submit navigate to <Screen>` all resolve the same inside-out way every other bare name in the document does (see [How a name resolves](screens.md#how-a-name-resolves)) - unresolved and ambiguous references are warnings, not errors, because a name may still resolve to something outside the document. A form sits at module level rather than inside a slice, so it disambiguates by module but not by feature or slice, since it does not sit inside either.
