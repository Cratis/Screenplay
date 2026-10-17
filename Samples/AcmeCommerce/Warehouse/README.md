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

- Captures cannot copy or fan out a list ([Screenplay#857](https://github.com/Cratis/Screenplay/issues/857)): P5 and P11 are consumed one scalar line per event.
  Both captures are keyed by SKU (the stock the event lands on); a repeat for the same SKU is told apart by its order (P5) or receipt (P11) with a second `append ... when <id>`.
- A command appends to one event source ([#859](https://github.com/Cratis/Screenplay/issues/859)) and a reaction-invoked command cannot generate an id
  ([#858](https://github.com/Cratis/Screenplay/issues/858)): the movement that crossed the reorder point is the P10 `crossingId`.
- No read model is composed by a performer: a performer query makes every specification of the application unsupported (`PLAY0268`). `ItemPage`
  is composed from nine events plus a join instead.
- The Inventory domain blueprints bind to blueprint-shaped read models (`Items`, `InventoryFigures`) and need `Cratis.Blueprint.Inventory` in the profile
  packages, which `cratis render` cannot resolve (`CLI-RENDER-007`); the foundation templates are used.
- A command specification cannot list a cascaded public event (`PLAY0285`); the accepted path of such a command is proven in the translation slice
  that publishes it (`RegisterSkuFromCatalog`, `PublishReservation`, `PublishDispatch`, ...).
