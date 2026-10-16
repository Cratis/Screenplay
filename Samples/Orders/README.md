# Orders

A sales team taking and fulfilling orders, built from Scene's **Orders blueprint** (`Cratis.Blueprint.Orders`, `@cratis/scene.blueprint.orders`). It is an application whose screens come from a
*domain blueprint* instead of being drawn by hand: every screen fills one of the blueprint's templates, the navigation is declared in the layout with the same regions the designer
shows, and the demo data is the blueprint's own sample-data pack.

```text
Orders/
  orders.play   concepts, one policy and persona, the application shell with its navigation, a ui profile, and one module
```

| Persona | Policy | Sees the screens of |
| --- | --- | --- |
| `Sales` | `IsSales` | every screen |

An order is placed, picked, shipped and delivered. Every screen fills one of the blueprint's four screen templates or its dialog: the orders list, the order page, the fulfilment board and the delivery calendar.

## What to read it for

- **Screens from a blueprint.** `template blueprint Cratis.Blueprint.Orders <Template> version "^1.0"` names a template of the blueprint package and the range of its version the author accepts.
- **Navigation as regions.** The layout declares the topbar and the sidebar; the module needs no navigation block because it inherits the application's items.
- **The data the preview shows.** The `ui profile` activates the blueprint's pack with `active blueprint orders-en`. No pack is declared in the document: the blueprint ships it, and the build resolves the reference against its packages.
- **One set of events, three views.** `OrderPlaced` feeds the orders list, the fulfilment board's cards and the delivery calendar's events; `OrderMoved` moves a card between columns.
- **A calendar from the same events.** `DeliveryEvents` is a read model projected from `OrderPlaced`, and the `Deliveries` feature's screen fills the `OrdersDeliveryCalendar` template.

## Verify

```bash
screenplay --warnaserror Samples/Orders
screenplay test Samples/Orders
# or, from a clone of this repository
dotnet run --project Source/DotNET/Tool -- --warnaserror Samples/Orders
```

The blueprint itself - its screen templates, dialog, sample-data pack and navigation - is documented in
[Scene's blueprint documentation](https://github.com/Cratis/Scene/blob/main/Documentation/blueprints/orders-blueprint.md).
