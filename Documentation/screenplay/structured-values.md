---
title: "Cratis Screenplay structured values for objects and lists"
description: "Write objects and lists in Cratis Screenplay with indented member and item blocks, including nested records, empty values, specifications and seeds."
---

# Structured values

Cratis Screenplay writes objects and lists as indented blocks, not JSON. Use member assignments for an object and `item` lines for a list, with another level of indentation for each nested value. The same form supplies domain records in specifications and seeds, as well as screen component properties.

This screen fragment combines a list of column objects, an `emptyState` object with nested actions, and an empty `rowActions` list:

```screenplay
component Cratis.Blueprint.Default.kanbanBoard board
  property label = "Orders by status"
  property columns
    item
      id    = "received"
      label = "Received"
    item
      id    = "approved"
      label = "Approved"
  property emptyState
    title = "No orders"
    actions
      item "create"
      item "import"
  property rowActions none
```

## The form

| You write | It is |
| --- | --- |
| `name = <value>` | A scalar: a string, a number, `true`, `false` or `null`. |
| `name` with lines indented under it | An object, when the first line under it is `member = ...` or a member that opens its own block. |
| `name` with `item` lines indented under it | A list, one `item` line per element. |
| `name none` | An empty list. |
| `name empty` | An object without members. |

An object holds one `member = value` line per member. When a member is itself an object or a list, put its name on a line of its own and indent its contents underneath. In the fragment above, `emptyState` is an object; its `actions` member opens a list.

A list holds one `item` line per element, in order. Use `item <value>` for a scalar element, such as `item "create"`. A bare `item` opens a block for an object element, as the columns show, or for a nested list if its first child is another `item`. Use `item none` for an empty nested list and `item empty` for an object element without members.

Align member assignments however reads best; spacing around `=` does not change their meaning.

A block whose first line is an `item` is a list; any other block is an object. A member literally named `item` is
written `@item`, and a member name that is not an identifier is quoted: `"first name" = "Ada"`.

A header without an inline value must open an indented block or end in `none` or `empty`. A bare header with no children is malformed (`PLAY0291`), not an empty value. An empty list, an object without members and `null` are different values; choose the one the target type accepts.

## Where structured values are written

Use the block form at these assignment sites. The header changes with the surrounding construct, but the member and `item` lines follow the same rules:

| Where | Header line |
| --- | --- |
| [Screen component](screens.md#package-components-and-typed-bindings) property | `property columns` |
| Literal UI binding | `property filter from literal`, `parameter range from literal mode oneWay` |
| Composition `set` and contributed item value | `set "shell:header".actions`, `actions` under an `item` |
| [Specification](specifications.md) `given`, `when`, `then`, `case` and `example` values | `lines` |
| Absence key | `then no readmodel OrderView for` |
| Structured command response | `then returns value` |
| Generated and pinned fixtures | `generated receipt`, `given generated PlaceOrder.receipt` |
| [Seed](seeding.md) event | `lines` |
| `produces`, inline `produces event`, reaction `produces` and `capture append` mappings | `lines`, `lines InvoiceLine[]` |
| Operation inputs | `contact Contact` |
| Projection literal (exact numeric mode) | `amount` |

For an order specification, first declare the `PlaceOrder` command and `OrderPlaced` event with a `lines` collection whose item type has `sku` and `quantity` members. `PlaceOrder` also needs a numeric `total` property. The fragment below supplies one line to the command and expects that line in the event.

An inline assignment on a step header, such as `given Invoice total = 1000`, must be scalar. Put a structured value on its own line under the step:

```screenplay
specification Placing
  when PlaceOrder
    lines
      item
        sku      = "A-1"
        quantity = 2
    total = 5000
  then OrderPlaced
    lines
      item
        sku      = "A-1"
        quantity = 2
```

## Types are checked

The declaration tells the compiler what the value can contain. For `lines InvoiceLine[]`, it checks that `lines` is a list and checks each item's members against the declared [type](types.md). Enum values are checked against their concept. The compiler does not infer members of undeclared shapes or shapes only named by external imports.

An unknown member produces `PLAY0292`; an incompatible shape, such as an object where a list belongs, produces `PLAY0293`. Repeating a member produces `PLAY0294`. When the model is bound for execution, a missing required member produces `PLAY0354`, so writing `empty` does not bypass a record's required fields.

To use these values as an application's initial facts rather than a specification's inputs, group them under event sources in [Event seeding](seeding.md).

## No JSON

JSON spellings such as `property columns = [{"id":"received"}]`, `lines = []` and `then returns {"id":1}` produce error `PLAY0833`. The parser retains the value so the canonical printer can rewrite it as blocks. Printing an existing document converts those values; for new values, use the block form directly. A malformed block produces `PLAY0291`.
