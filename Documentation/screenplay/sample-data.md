# Sample data

A design surface or a preview needs rows to show in a table, a list or a chart before a real backend feeds it. `sample data` declares those rows as a **pack**: demo records per shape, in one locale, written with the same literals as everything else in Screenplay.

Sample data is **design and preview time only**. It is not part of the [executable semantic model](interoperability.md): binding reports each pack as deferred (`PLAY0269`), and a production build, a backend or Chronicle never sees it.

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

- `sample data <Name>` - top level, alongside `seed` and `ui profile`. A document can declare several packs; each name must be unique.
- `title` - the display title a selector shows (`CRM · Nordic SMB`). Without one the pack is shown by its name. At most one.
- `locale` - the locale of the data, a BCP 47 tag such as `nb-NO` or `en-GB`. It also decides which names, cities and formats generated rows use for a shape the pack does not cover. At most one.
- `shape <ReadModel>` - the rows for one shape. A shape supplies rows for the read model of the same name, or for the read model a `ui profile` maps it to (see below). A shape appears at most once per pack.
- `row` - one record, with a `<field> = <literal>` line per field. Fields are in authored order and may be omitted; a missing field is generated.
- A literal is a quoted string, a number, `true`, `false` or `null`. Numbers keep their authored token.

The compiler checks every field against the read model: the field must be one of its properties (`PLAY0710`), and the literal must fit the type (`PLAY0711`) - a string for text, `Uuid`, `Date` and `DateTime`, one of the values for an enum concept, a whole number for `Int`, a number for `Decimal`, `true` or `false` for `Bool`, and `null` only for an `optional` property. A shape that names no read model is a warning (`PLAY0709`), because a blueprint may supply the read model.

## Choosing the active pack: once, in the ui profile

The active pack is chosen **once for the whole application**, in the `ui profile`. It is never chosen per screen, slice or component: demo data spans several domains, so one choice, inherited everywhere, keeps the preview consistent.

```screenplay
ui profile <Name>
  ...
  sample data
    active <Pack>
    active blueprint <id>
    shape <Shape> maps to <ReadModel>
    bind <ReadModel> to <Pack>
```

- `active <Pack>` - the pack the whole application previews with: a `sample data` pack declared in the document, or `active blueprint <id>` for a pack a blueprint ships (`crm-nordic-smb`). At most one. Without it the preview falls through to the blueprint's default pack and then to generated data.
- `shape <Shape> maps to <ReadModel>` - for where a pack's shape is not named like the read model it supplies rows for. A read model is mapped from at most one shape.
- `bind <ReadModel> to <Pack>` - a rare override: that read model previews with that pack whatever the active pack is. At most once per read model.

A reference to a pack the document does not declare is an error (`PLAY0714`), and so is a mapping or binding naming a read model the document does not declare (`PLAY0715`). A `blueprint <id>` pack is resolved by the build against its packages and is not checked by the compiler.

The order the preview resolves rows in is: the pack bound to the read model, then the profile's active pack, then the default pack of the blueprint the template came from, then generated deterministic data. See Scene's [sample data packs](https://github.com/Cratis/Scene/blob/main/Documentation/blueprints/sample-data.md). Studio's preview strip can override the active pack for the session only; that is never stored, and the profile stays the single source of truth.

## Example

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

- **Printer.** Packs and the profile's selection are printed in canonical form with comments kept.
- **TypeScript compiler, Monaco, VS Code.** The TypeScript compiler skips `sample data` whole, as it does `ui profile` and `theme`, so the C# compiler alone reports its diagnostics ([TypeScript compiler](typescript-compiler.md)). Monaco and the VS Code grammar highlight, document and complete the blocks.
- **MCP.** `read-ast` and `propose-ast` read and edit `SampleDataPackSyntax`, `SampleDataSelectionSyntax` and their children as typed nodes: replace a row value, change the profile's active pack, remove a pack or the selection. An active pack nothing declares is refused with `PLAY0714`.
- **Binding.** Each pack is reported as deferred (`PLAY0269`); it adds nothing to the executable model.
- **Scene and Stage.** The Scene converter turns the blocks into Scene's `SampleDataPack` and the UI profile's `sampleData`; production builds never include sample data.

The ruling behind the single choice point is [decision 0081](https://github.com/Cratis/Screenplay/blob/main/decisions/0081-choose-the-active-sample-data-pack-once-in-the-ui-profile.md).
