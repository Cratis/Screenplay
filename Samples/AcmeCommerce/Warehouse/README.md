# Warehouse (Acme Commerce sample)

Physical stock: what is on the shelf, what is promised to an order, what is picked, packed and dispatched. Module `Stock`, personas
`WarehouseClerk` (policy `CanWorkWarehouse`). Event sources `StockItem` (SKU), `Shipment` (order) and `Location` (code).

```
model/          state changes, state views, automations, screens, forms, specifications
translation/    inbound.play (what Catalog, Sales and Purchasing say) and outbound.play (what the warehouse says)
```

## What it speaks to the other contexts

| # | Public event | Direction | Private event it is built from / turns into |
| --- | --- | --- | --- |
| P1 | `ProductPublished` (Catalog) | in (`ProductPublishedAtWarehouse`) | `SkuRegistered` |
| P3 | `ProductRetired` (Catalog) | in (`ProductRetiredAtWarehouse`) | `SkuBlockedForReceiving` |
| P5 | `OrderConfirmed` (Sales) | in (`OrderConfirmedAtWarehouse`: orderId, sku, quantity) | `ReservationRequested` |
| P11 | `PurchaseOrderReceived` (Purchasing) | in (`PurchaseOrderReceivedAtWarehouse`: one receipt line) | `StockReceived` |
| P7 | `StockReserved` | out | `ReservationMade` |
| P8 | `StockReservationFailed` | out | `ReservationFailed` |
| P9 | `StockLevelChanged` | out | `StockLevelSnapshotTaken` |
| P10 | `StockBelowReorderPoint` | out | `ReorderPointCrossed` |
| P12 | `ShipmentDispatched` | out | `ShipmentHandedToCarrier` |

## The automations

- **ReserveStock**: `ReservationRequested` -> command `ReserveStockUnits` (reads the stock level, refuses a blocked SKU or too little stock).
  Accepted: `ReservationMade`. Refused: the reaction produces `ReservationFailed` (all or nothing: `shortBy` is the whole requested quantity).
- **WatchReorderPoint**: after every drop of available stock the command `RaiseReorderAlarm` checks `available < reorderPoint` and that no
  alarm is open - one `ReorderPointCrossed` per crossing; the alarm re-arms when stock comes back.
- **SnapshotStockOnMovement**: every stock movement leads to a `StockLevelSnapshotTaken` (the thing that is published as P9).
- **PlanShipmentWhenReserved** and **TakeStockWhenDispatched** connect reservation -> shipment -> stock leaves the shelf.

## Constraints

`UniqueLocationCode` (`unique code on LocationRegistered`) and `OneDispatchPerShipment` (`unique event ShipmentHandedToCarrier`).

## Screens

Every screen belongs to the `WarehouseClerk` (the module is gated by `CanWorkWarehouse`); there is no actor row per screen. Only real screens are modelled:
the components bring the error, validation, loading, empty and confirmation states, and a command is an `action` (with its command-bound form, see `forms.play`)
on the screen the clerk already works in.

| Screen | Template | Reached from |
| --- | --- | --- |
| `StockLevels` (list), actions: Adjust stock, Count, Reorder point, Block SKU; button Register SKU | `ListScreen` | Warehouse / Stock levels |
| `ItemPage` (record: numbers, locations), actions: Put away, Open location | `RecordPage` | row click on a stock level or alert |
| `StockAdjustments` (list of corrections with their reason), actions: Adjust stock, Count | `ListScreen` | Warehouse / Adjustments |
| `Reservations` (what is promised to which order), action: Reserve stock | `ListScreen` | Warehouse / Reservations |
| `PickingQueue` (board by shipment status), actions: Pick order, Pack, Cancel shipment | `BoardScreen` | Warehouse / Picking |
| `ReceivingLog` (list), action: Receive goods | `ListScreen` | Warehouse / Receiving |
| `Shipments` (list), action: Dispatch | `ListScreen` | Warehouse / Shipments |
| `WarehouseDashboard` (KPIs, running-out alerts) | `DashboardScreen` | Warehouse / Dashboard |
| `RegisterSkuDialog` (a creation flow started from the list) | `QuickCreateDialog` | button on Stock levels and in the topbar |

## Honest deviations from the design (all forced by the language today)

- P5 arrives whole: `OrderConfirmedAtWarehouse` carries the order's `lines`, and the `SalesConfirmationFeed` capture fans out one `ReservationRequested` per line with `children lines identified by sku`.
  P11 is one receipt line per event because the receiving command takes one SKU at a time.
- The reorder alarm still carries the movement that crossed the reorder point as `crossingId`: it tells one crossing from the next, which is the business meaning, so it stays a command input.
- No read model is composed by a performer: `ItemPage` is composed from nine events plus a join, which keeps it executable.
- The Inventory domain blueprints bind to blueprint-shaped read models (`Items`, `InventoryFigures`) and need `Cratis.Blueprint.Inventory` in the profile
  packages, which `cratis render` cannot resolve (`CLI-RENDER-007`); the foundation templates are used.
- A command specification states the public event its slice publishes (`RegisteringASkuByHand`); the translation slices keep their own specifications for the event-driven path.
