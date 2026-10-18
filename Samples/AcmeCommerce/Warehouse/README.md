# Warehouse (Acme Commerce sample)

Physical stock: what is on the shelf, what is promised to an order, what is picked, packed and dispatched. Module `Stock`, personas
`WarehouseClerk` (policy `CanWorkWarehouse`) and `OpsAdmin`. Event sources `StockItem` (SKU), `Shipment` (order) and `Location` (code).

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

| Screen | Template | Reached from |
| --- | --- | --- |
| `StockLevels` (list, `acme:stockBadge`), toolbar: Adjust stock, Count, Register SKU | `ListScreen` | Warehouse / Stock levels |
| `ItemPage` (record: numbers, locations) | `RecordPage` | row click on a stock level or alert |
| `PickingQueue` (board by shipment status) | `BoardScreen` | Warehouse / Picking |
| `ReceivingLog` (list) | `ListScreen` | Warehouse / Receiving |
| `Shipments` (list) | `ListScreen` | Warehouse / Shipments |
| `WarehouseDashboard` (KPIs, running-out alerts) | `DashboardScreen` | Warehouse / Dashboard |
| `AdjustStock`, `Count`, `SetReorder`, `RegisterSkuDialog`, `OpenLocation`, `PutAwayDialog`, `ReceiveGoods`, `Reserve`, `Dispatch` | `QuickCreateDialog` | toolbar buttons / list actions |
| `BlockSkuDialog`, `PickOrderDialog`, `PackDialog`, `CancelShipmentDialog` | `ConfirmDialog` | board and list actions |

## Honest deviations from the design (all forced by the language today)

- P5 arrives whole: `OrderConfirmedAtWarehouse` carries the order's `lines`, and the `SalesConfirmationFeed` capture fans out one `ReservationRequested` per line with `children lines identified by sku`.
  P11 is one receipt line per event because the receiving command takes one SKU at a time.
- The reorder alarm still carries the movement that crossed the reorder point as `crossingId`: it tells one crossing from the next, which is the business meaning, so it stays a command input.
- No read model is composed by a performer: `ItemPage` is composed from nine events plus a join, which keeps it executable.
- The Inventory domain blueprints bind to blueprint-shaped read models (`Items`, `InventoryFigures`) and need `Cratis.Blueprint.Inventory` in the profile
  packages, which `cratis render` cannot resolve (`CLI-RENDER-007`); the foundation templates are used.
- A command specification states the public event its slice publishes (`RegisteringASkuByHand`); the translation slices keep their own specifications for the event-driven path.
