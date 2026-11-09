---
title: UI contract
description: Export every screen of a Screenplay model as a stack-neutral JSON contract of its data, actions, navigation and layout, so any UI stack can build from it.
---

## Build a UI on any stack from the model

[Screens](screens.md) are written with Scene vocabulary. A builder for React
without Scene, Vue, Angular or a native app needs to know what each screen
shows, which commands it issues, where it leads and how it is laid out, without
interpreting Scene. The UI contract says exactly that:

```bash
screenplay export path/to/screenplays --format ui --output screens.json
```

Over MCP, `read-workspace` with `view: "ui-contract"` pages the same bytes.

The contract needs only the sources. When the model also has an executable
model, every screen, query and command carries its semantic identity and its
HTTP route from the [API description](api-description.md), and `modelRevision`
names the executable model they come from. Without one, those members are
absent and the CLI says so on standard error.

## What a screen holds

```json
{
  "name": "BookCatalog",
  "slice": "Lending.Catalog.BookCatalog",
  "title": "Catalog",
  "data": [
    {
      "query": "AllBooks", "readModel": "CatalogEntry", "many": true,
      "fields": [{ "property": "title", "label": "Title" }],
      "route": "/api/lending/catalog/book-catalog/all-books", "live": true
    }
  ],
  "actions": [
    {
      "command": "BorrowBook", "label": "BorrowBook", "subject": "item",
      "inputs": [
        { "name": "bookId", "type": "BookId", "from": "item.bookId" },
        { "name": "memberId", "type": "MemberId" }
      ],
      "route": "/api/lending/catalog/borrow-book"
    }
  ],
  "navigation": [{ "screen": "BookDetails", "trigger": "row", "by": "bookId" }],
  "layout": { "fits": [], "regions": [{ "kind": "list", "shows": "CatalogEntry", "regions": [], "actions": [] }] }
}
```

| Member | Meaning |
| --- | --- |
| `data` | One entry per query the screen shows: the read model, whether it is a list, the value it is narrowed `by`, the fields shown with their labels (empty when the screen shows them all) and whether it is `live` |
| `actions` | One entry per command the screen issues: what it applies to (`screen` or `item`), its inputs with their types and the screen value that fills each one (`from`), and, for an action that picks one of several commands, the condition in `when` |
| `navigation` | Where the screen leads and what leads there: a `link`, a table `row`, an `action`, a toolbar `button` or a behavior trigger such as `click`, with the value passed `by` and how the target `opens` |
| `layout` | Where the screen `fits` (`page`, `dialog`, `drawer`, `pane`, `panel`), the screen shape (`template`) it follows, and its regions: `template`, `slot`, `section`, `list`, `details`, `embed`, `component` and `toolbar`, with the actions placed in each |

Nothing in these members needs Scene to interpret: a `component` region names
the component instance and nothing else.

## The Scene layer

`scene` is present only when a screen uses Scene vocabulary, and a builder for
another stack may ignore it. It lists the Scene templates with their blueprint
and slots, the components with their properties, bindings and presentation
values, the toolbars and the outlets a screen offers to lower levels. A
component property keeps its binding (`query AllBooks`) or its value; lists
and objects stay JSON.

## Versions and round trips

`contract` names the version, `screenplay-ui/1`. The same model always gives
the same bytes, and reading them back with `UiContractJson.Read` and writing
again gives the same bytes. A reader refuses another version.

## Use it from .NET

```csharp
using Cratis.Screenplay.Ui;

var contract = UiContractBuilder.For(workspace);
foreach (var screen in contract.Screens)
{
    Console.WriteLine($"{screen.Slice}.{screen.Name}: {screen.Data.Count} data, {screen.Actions.Count} actions");
}

File.WriteAllBytes("screens.json", UiContractJson.Write(contract));
```

`UiContractBuilder.For(syntax, model, application)` reads a merged
`ApplicationSyntax` with an optional executable model, and
`UiContractBuilder.Select(contract, scope)` keeps the screens of one module,
feature or slice.

## Screens in the executable model

The neutral members of every screen can also live in the executable model
itself, as ESM v42. Admission is on request, so a default compilation keeps its
version, bytes and revision:

```bash
screenplay export path/to/screenplays --screens --output model.esm.json
```

Over MCP, `read-workspace` with `view: "executable-model"` and `screens: true`
pages the same bytes. From .NET:

```csharp
using Cratis.Screenplay.Ui;

var admitted = ExecutableScreens.Admit(workspace);
if (!admitted.Success)
{
    foreach (var refusal in admitted.Refusals) Console.Error.WriteLine(refusal);
    return;
}

var model = admitted.Compilation!.Model;
var contract = UiContractBuilder.For(model);
```

Each slice then holds its `screens`: the data a screen shows keyed to query
identities, its actions keyed to command identities with every input as a
command property identity, and its navigation keyed to screen identities, with
the layout regions as written. `UiContractBuilder.For(model)` reads the
`screenplay-ui/1` contract from that model alone, without the sources and
without the Scene layer; its neutral members equal the ones read from the
sources.

A reference the executable model does not hold refuses admission with a reason
that names the screen and the reference; nothing is dropped. A model without
an executable model has nothing to admit screens to. See the ESM
[interoperability](interoperability.md#claimed-unreleased-version) notes for the
exact members.

Behavior wiring beyond navigation and command execution (refreshing a query,
setting state, notifications, dialogs opened from a template) is not in
`screenplay-ui/1`; read it from the screen's source.
