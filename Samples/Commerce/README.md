# Commerce

A webshop described in Screenplay. Merchandisers maintain a catalog. Customers search it and place orders. A payment provider settles the orders, and the warehouse ships them to the customer's door.

The sample shows how to compose an application from small, focused files with [file imports](../../Documentation/screenplay/imports.md). Each file tells one part of the story. There is a composite file at every level: the application, each module and each feature. A slice file holds only its slice.

## How placement works

`import "<path or glob>"` brings in `.play` files. The path is relative to the importing file. The position of the import decides where the imported files belong:

- **At the top level of `application.play`**, an import brings in whole documents. That is how the root reads as a table of contents.
- **Inside `module Catalog`**, an import places the imported file in that module. The file's top level *is* the module body, so `Products.play` starts with `feature Products` and needs no `module Catalog` header.
- **Inside `feature Products`**, an import places the imported file in that feature. The file's top level is the feature body, so `RegisterProduct.play` starts with `slice StateChange RegisterProduct` and nothing above it.

Placement composes. `Catalog.play` places `Products.play` in the module, and `Products.play` places `RegisterProduct.play` in `Catalog.Products`. A nested feature works the same way: `Products.play` imports `Repricing/Repricing.play` inside `feature Products`.

A placed file may also declare things that belong to the whole application. A concept or trigger that only one slice uses lives in that slice's file. For example, `Weight` is declared in `RegisterProduct.play` and `CarrierReturnedParcel` in `HandleReturnedParcels.play`.

Every file is reached from `application.play`, and none is imported in two places. Compiling the root and compiling the folder therefore cover the same 36 files.

## Files

```text
Commerce/
  application.play             the table of contents: domain, then one import per part
  seed.play                    the demo catalog a fresh environment starts from
  commerce.en.strings          English text for every $strings key
  Shared/
    concepts.play              the values several modules share: ids, Money, Sku, statuses
    types.play                 the composite shapes: Address, OrderLine
    access.play                policies, the four personas and the identity providers
  Shell/
    look.play                  theme, the Storefront layout with its Navigation slot, the Web ui profile
    behaviors.play             ConfirmDestructive, attached wherever a command cannot be undone
  Catalog/
    Catalog.play               module: description, templates, product forms, imports its features
    Products/
      Products.play            feature gated to merchandisers, its menu entry, imports its slices
      RegisterProduct.play     StateChange: a product enters the catalog (declares Weight)
      DiscontinueProduct.play  StateChange: a product is retired, once
      ProductList.play         StateView: the merchandiser's live catalog
      Repricing/
        Repricing.play         nested feature: repricing products
        ChangeProductPrice.play  StateChange: a new price
        PriceHistory.play      StateView: launch price against current price
    Storefront/
      Storefront.play          feature: what customers see, and the Shop menu entry
      ProductSearch.play       StateView served by the search index, not built from events
  Ordering/
    Ordering.play              module gated to signed-in users: templates, order forms
    Orders/
      Orders.play              feature: the My orders menu entry
      PlaceOrder.play          StateChange: a customer checks out
      CancelOrder.play         StateChange: the customer or support cancels
      MyOrders.play            StateView: a customer follows their orders out the door
    Payments/
      Payments.play            feature: settlements from the payment provider
      ImportPaymentSettlements.play  Translate: a settled payment becomes OrderPaid
    Support/
      Support.play             feature gated to support agents
      OrderDetails.play        StateView: everything support needs about an order
  Fulfillment/
    Fulfillment.play           module: warehouse templates, the dispatch form
    Shipping/
      Shipping.play            feature gated to warehouse clerks
      StartFulfillment.play    Automation: a paid order is queued for shipping
      ReleasePickingWave.play  Automation on the clock: the morning wave at 06:00
      ShipmentQueue.play       StateView: what to pack and send today
      DispatchShipment.play    StateChange: a parcel is handed to a carrier
    Tracking/
      Tracking.play            feature: parcels on their way to the door
      TrackCarrierDeliveries.play   Translate: a carrier scan becomes ShipmentDelivered
      NotifyCustomerOfDispatch.play Automation: the customer hears their parcel shipped
      HandleReturnedParcels.play    Automation on a trigger: a returned parcel is recorded
```

The largest file has 122 lines.

## What else it shows

- **Cross-file resolution.** `OrderPaid` is declared in `Payments`, projected in `Orders` and `Support`, and reacted to in `Shipping`. The storefront search navigates to the checkout screen in another module.
- **Contributions.** The layout's `navigation contributes Navigation` slot collects one `contribute to Navigation` entry from each feature file.
- **Layered authorization.** Module, feature and command or query gates are combined with AND. Every persona lists the policies it holds, so each screen goes to the right persona:

  | Persona | Screens |
  | --- | --- |
  | Customer | ProductSearch, PlaceOrderScreen, MyOrders, CancelOrderScreen |
  | Merchandiser | RegisterProductScreen, DiscontinueProductScreen, ProductList, ChangeProductPriceScreen, PriceHistory |
  | WarehouseClerk | ShipmentQueue, DispatchShipmentScreen |
  | SupportAgent | OrderDetails, OrderDetail, CancelOrderScreen |

- **Every way to state a scenario.**
  - State changes use `given caller`, `when <Command>` and `then <Event>`, plus `then error` and `then denied` specifications.
  - Projected views use `when append` with `then readmodel` and `then query`.
  - The search index view uses `when query` with `then result`, `then no result` and `then denied`.
  - The translations use `given capture` and `when capture`. One automation is driven by `when clock`, another by `when trigger`.
  - `given clock` fixes the time a cancellation is stamped with.

## Verify it

Compile the root file, which compiles the application it imports:

```bash
screenplay Samples/Commerce/application.play --warnaserror
```

Or compile the folder, which treats every file as a root while imports still place what they import:

```bash
screenplay Samples/Commerce --warnaserror
```

Both print `36 file(s) compiled - 0 error(s), 0 warning(s)`. Without the installed tool, run it from source:

```bash
dotnet run --project Source/DotNET/Tool -- Samples/Commerce/application.play --warnaserror
```

The repository's sample specifications also cover this folder:

```bash
dotnet test Source/DotNET/Screenplay/Screenplay.csproj --filter "FullyQualifiedName~for_Samples"
```
