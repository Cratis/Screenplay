---
title: "Structured values"
description: "Write objects and lists in Screenplay as indented blocks - one member or item per line, one level of indentation per level - wherever a value is assigned."
---

# Structured values

Screenplay has no JSON. When a value is an object or a list - a component property, a specification value, a seed, a
production mapping, a pinned fixture - it is written as an indented block under the line that names it, one level of
indentation for each level of the hierarchy. It reads the way the rest of a `.play` file reads, and a change to one
member is a change to one line.

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

Inside a block:

- **An object** holds one `member = value` line per member. A member that is itself an object or a list opens another
  indented block: the member name alone on its line, its contents one level deeper.
- **A list** holds one `item` line per element, in order. `item <value>` is a scalar element. A bare `item` opens a
  block for an object element - or for a nested list, when its first line is again an `item`. `item none` is an empty
  nested list and `item empty` an object element without members.
- Members are aligned however reads best; spacing around `=` does not matter.

A block whose first line is an `item` is a list; any other block is an object. A member literally named `item` is
written `@item`, and a member name that is not an identifier is quoted: `"first name" = "Ada"`.

Every header - a property, a member or an `item` - either opens a block or ends in `none` or `empty`, so a stray word on
its own is reported (`PLAY0291`) instead of being read as an empty value.

## Where structured values are written

The same form is used everywhere a value is assigned:

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

On a step header that carries one inline assignment - `given Invoice total = 1000` - the inline value must be a scalar.
A structured value goes on its own line under the header:

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

A structured value is typed syntax, not text. Members are checked against the target's declared [type](types.md),
list items against the element type, and enum members against the concept. An unknown member is `PLAY0292`, the
wrong shape for the target (an object where a list belongs) is `PLAY0293`, a missing required member is `PLAY0354`,
and a member stated twice is `PLAY0294`. Imported or undeclared shapes are not guessed.

## No JSON

A value written as JSON - `property columns = [{"id":"received"}]`, `lines = []`, `then returns {"id":1}` - is an
error (`PLAY0833`). The compiler still reads the value, so the canonical printer writes it back in block form; printing a
document is the quickest way to convert one. A malformed block is `PLAY0291`.
