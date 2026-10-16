# Inventory

A warehouse, built from Scene's **Inventory blueprint** (`Cratis.Blueprint.Inventory`, `@cratis/scene.blueprint.inventory`). It is an application whose screens come from a
*domain blueprint* instead of being drawn by hand: every screen fills one of the blueprint's templates, the navigation is declared in the layout with
the same regions the designer shows, and the demo data is the blueprint's own sample-data pack.

```text
Inventory/
  inventory.play   concepts, one policy and persona, the application shell with its navigation, a ui profile, and one module
```

| Persona | Policy | Sees the screens of |
| --- | --- | --- |
| `WarehouseClerk` | `IsWarehouseClerk` | every screen |

Items are kept in locations, move in and out as movements and come from suppliers. Every screen fills one of the blueprint's eight screen templates or three dialogs: the dashboard, the item table and the item gallery, the item page, the location tree, the movements, the check in and out form and the quick-create dialogs.

## What to read it for

- **Screens from a blueprint.** `template blueprint Cratis.Blueprint.Inventory <Template> version "^1.0"` names a template of the blueprint package and the range of its version the author
  accepts. Where the model has to say something the template cannot know - which command a form or a button runs - the screen fills the template's slot with an `action`.
- **Navigation as regions.** The layout declares the topbar and the sidebar; the module needs no navigation block because it inherits the application's items.
- **The data the preview shows.** The `ui profile` activates the blueprint's pack with `active blueprint ...`. No pack is declared in the document: the blueprint ships it, and the
  build resolves the reference against its packages. Each read model is named after the shape the pack supplies, so the same rows reach the screen in a preview and in a running application.
- **Two ways to see the same items.** `ItemList` feeds the table (`InventoryItemList`) and the item page; the `Gallery` feature, nested under Items, is a second view of the same events for the gallery's cards (`InventoryItemGallery`). The sidebar's *Table* and *Gallery* entries target `Inventory.Items` and `Inventory.Items.Gallery`.
- **A command with its own dialog.** `AdjustStock` is the dialog `InventoryAdjustStock`, opened from an item or the locations screen; the screen names the command it runs with an `action`.
- **A form made for a phone.** `RecordMovement` is the screen `CheckInOut`, the blueprint's mobile-first form.

## Verify

```bash
screenplay --warnaserror Samples/Inventory
screenplay test Samples/Inventory
# or, from a clone of this repository
dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/Inventory
```

The blueprint itself - its screen templates, dialogs, sample-data pack and navigation - is documented in
[Scene's blueprint documentation](https://github.com/Cratis/Scene/blob/main/Documentation/blueprints/inventory-blueprint.md).
