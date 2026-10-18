# Work orders

Work orders for a maintenance or field-service team: a work order list, a work order page with its tasks, parts and time, a board by step and a schedule of visits. Built from Scene's **Work orders blueprint** (`Cratis.Blueprint.WorkOrders`, `@cratis/scene.blueprint.work-orders`). It is an application whose screens come from a *domain blueprint* instead of being drawn by hand: every screen fills one of the blueprint's templates, the navigation is declared in the layout with the same regions the designer shows, and the demo data is the blueprint's own sample-data pack.

```text
WorkOrders/
  work-orders.play   concepts, one policy and persona, the application shell with its navigation, a ui profile, and one module
```

| Persona | Policy | Sees the screens of |
| --- | --- | --- |
| `Technician` | `IsTechnician` | every screen |

## What to read it for

- **Screens from a blueprint.** `template blueprint Cratis.Blueprint.WorkOrders <Template> version "^1.0"` names a template of the blueprint package and the range of its version the author accepts.
- **Navigation as regions.** The layout declares the topbar and the sidebar; the module needs no navigation block because it inherits the application's items.
- **The data the preview shows.** The `ui profile` activates the blueprint's pack with `active blueprint work-orders-en`. No pack is declared in the document: the blueprint ships it, and the build resolves the reference against its packages.
- **Events feeding several views.** Each entity's registration event feeds its list, and the screens that show the same events differently - `WorkOrdersList`, `WorkOrdersBoard`, `WorkOrdersSchedule` - project their own read models from it.

## Verify

```bash
screenplay --warnaserror Samples/WorkOrders
screenplay test Samples/WorkOrders
# or, from a clone of this repository
dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/WorkOrders
```

The blueprint itself - its screen templates, dialogs, sample-data pack and navigation - is documented in
[Scene's blueprint documentation](https://github.com/Cratis/Scene/blob/main/Documentation/blueprints/work-orders-blueprint.md).
