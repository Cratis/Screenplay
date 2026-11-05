---
title: "Cratis Screenplay sample data for UI design"
description: "Declare representative rows in Cratis Screenplay sample data packs and select one in a UI profile for design and previews, separate from production data."
---

# Sample data

Cratis Screenplay `sample data` supplies representative rows for a UI design before a backend supplies real records. Group the rows into a named pack, then select that pack in a UI profile so screens share the same design data. Each pack contains shapes, the named record structures that provide rows for read models.

Sample data is for design and UI previews only, not production records or backend initialization. The [executable semantic model](interoperability.md) defers each pack with `PLAY0269` and contains none of its rows. A UI consumer reads the syntax and supplies the design-time data; accepting the declaration does not mean every consumer displays it.

## Syntax

```screenplay
sample data <Name>
  [title "<text>"]
  [locale <BCP 47 tag>]
  shape <ReadModel>
    row
      <field> = <literal>
      ...
    ...
  ...
```

Declare packs at the top level, alongside `seed` and `ui profile`. Each pack name must be unique.

| Entry | Meaning and limits |
| --- | --- |
| `title` | An optional display title, such as `CRM · Nordic SMB`, once per pack. Without one, the pack is displayed by its name. |
| `locale` | An optional BCP 47 locale tag, such as `nb-NO` or `en-GB`, once per pack. It identifies the data's locale and can guide Scene's generated rows when no pack supplies a requested shape. It does not translate authored text. |
| `shape <ReadModel>` | Rows for a read model with the same name, or one mapped by the profile. Each shape can appear once per pack. |
| `row` | One record, with fields in authored order. Omitted fields remain absent in the syntax and Stage conversion; do not rely on them being generated individually. |
| `<field> = <literal>` | A quoted string, number, `true`, `false` or `null`. Numbers retain their authored token. |

The C# compiler checks authored fields against the matching read model. Unknown properties produce `PLAY0710`; values that do not fit the property type produce `PLAY0711`.

| Property type | Accepted literal |
| --- | --- |
| Text, `Uuid`, `Date`, `DateTime` | A quoted string. |
| Enum concept | A quoted value from the concept's declared values. |
| `Int` | A whole number. |
| `Decimal` | A number. |
| `Bool` | `true` or `false`. |
| An `optional` property | Its usual literal or `null`. |

Rows hold scalar literals, not collections or structured values. A shape that names no read model produces warning `PLAY0709`: the model may come from a blueprint, or the shape may need a profile mapping.

## Choosing the active pack: once, in the ui profile

Choose the active pack in the `ui profile`, once for the whole application. Screens, slices and components inherit that choice, so switching packs changes the design data together rather than leaving each screen on a different selection.

```screenplay
ui profile <Name>
  ...
  sample data
    active <Pack>
    active blueprint <id>
    shape <Shape> maps to <ReadModel>
    bind <ReadModel> to <Pack>
```

Use either `active <Pack>` for a document pack or `active blueprint <id>` for a pack supplied by a blueprint, such as `crm-nordic-smb`. These are alternatives: a selection can have at most one active pack.

When a pack's shape name differs from the read model, add `shape <Shape> maps to <ReadModel>`. Each read model can be mapped from one shape. Use `bind <ReadModel> to <Pack>` only when that read model needs a different pack from the application-wide choice; each read model can have one binding.

An undeclared document pack produces error `PLAY0714`. A mapping or binding to an undeclared read model produces `PLAY0715`. Blueprint pack IDs are resolved by the consuming build against its packages, not by the Screenplay compiler.

Scene looks for rows in this order, continuing when a pack cannot supply the requested shape:

1. The pack bound to the read model.
2. The profile's active pack, or a consumer's session-only override.
3. The default pack of the blueprint that supplied the template.
4. Deterministic generated data, when the consumer supplies a shape to generate from.

For generated rows, Scene takes the locale from the first available bound, active or blueprint-default pack, then from the consumer's context. Without an active selection, row resolution still starts with any binding before trying the blueprint default and generation. A session override does not change the authored profile. See Scene's [sample data packs](https://github.com/Cratis/Scene/blob/main/Documentation/blueprints/sample-data.md) for the resolver contract.

## Example

`NordicDemo` supplies two customer rows, and the desktop profile selects them for the application's design. The first row gives `notes` an explicit `null`; the second leaves it absent. Neither row creates a backend customer.

```screenplay
module Crm
  feature Customers
    slice StateView CustomerList
      readmodel Customer
        id Uuid
        name String
        revenue Decimal
        active Bool
        notes String optional
      query CustomerById => Customer optional
        by id Uuid

sample data NordicDemo
  title "CRM · Nordic SMB"
  locale nb-NO
  shape Customer
    row
      id = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
      name = "Fjordline AS"
      revenue = 125000.5
      active = true
      notes = null
    row
      id = "9c858901-8a57-4791-81fe-4c455b099bc9"
      name = "Nordlys Handel AS"
      revenue = 48200
      active = false

ui profile Desktop
  target platform web

  sample data
    active NordicDemo
```

## Tools

The C# compiler validates packs and profile selections. The [TypeScript compiler](typescript-compiler.md) skips sample data blocks rather than validating them; editor highlighting and completion do not establish that a pack passes the C# checks. The printer preserves packs and selections in canonical form, with comments.

For structured editing through the Model Context Protocol (MCP), `read-ast` and `propose-ast` expose typed abstract syntax tree (AST) nodes: `SampleDataPackSyntax`, `SampleDataSelectionSyntax` and their children. They let a tool replace a row value or change the selected pack without treating the file as unstructured text. An undeclared active document pack is refused with `PLAY0714`.

Stage's converter maps the blocks to Scene's `SampleDataPack` and the UI profile's `sampleData`. Running application contracts and production builds exclude sample data; packs are not a way to seed the executable backend.

The single application-wide choice is explained in [decision 0081](https://github.com/Cratis/Screenplay/blob/main/decisions/0081-choose-the-active-sample-data-pack-once-in-the-ui-profile.md). To localize labels and validation messages as well as choosing representative rows, use [internationalization](internationalization.md); a pack's locale does not select the UI's string table.
