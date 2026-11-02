---
applyTo: "**/*.play, **/*.play.txt, Source/**/*, Documentation/**/*, Samples/**/*"
---

## Screenplay has no JSON

A `.play` document never contains JSON. An object or a list is written as an indented
[structured value](../../../../Documentation/screenplay/structured-values.md) under the line that names it, one level
of indentation per level of the hierarchy:

```screenplay
property columns
  item
    id    = "received"
    label = "Received"
property emptyState
  title = "No orders"
  actions
    item "create"
property rowActions none
```

- An object holds one `member = value` line per member; a member that is an object or a list opens another indented block.
- A list holds one `item` line per element: `item <scalar>`, or a bare `item` opening a block for an object or a nested list.
- Any header - property, member or `item` - ending in `none` is an empty list, and ending in `empty` an object without members.
- A member named `item` is written `@item`; a member name that is not an identifier is quoted.

This applies everywhere a value is assigned: component properties, literal UI bindings, composition `set` and item
values, specification and `case`/`example` values, absence keys (`then no readmodel X for`), structured returns
(`then returns value`), generated fixtures, seeds, production, reaction, capture and operation mappings, and exact-mode
projection literals.

When you add a construct that carries structured data, give it the same block form - reuse `StructuredBlockParser`
(C#) and `StructuredBlockParser.ts` (TypeScript) and the writer's `Assignment`/`Structured` for printing. Never add a
grammar position that accepts `{...}` or `[...]`, never print JSON from the printer, and never write JSON in a sample,
documentation example, test fixture or AI skill. Inline JSON is error `PLAY0833`; the editors offer a quick fix and the
printer writes the block form, so printing a document converts it.

JSON stays where it is a transport, not Screenplay: syntax JSON, the ESM wire format, MCP payloads and contract files.
