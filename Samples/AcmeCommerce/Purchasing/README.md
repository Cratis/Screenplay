# Purchasing (Acme Commerce sample)

Buying from suppliers: who the suppliers are, what they offer, what has to be replenished and the purchase orders that result. Module `Buying`
(policy `CanBuy`), personas `Buyer` and `OpsAdmin`. Event sources `Supplier`, `PurchaseOrder` (shared) and `SkuSourcing` (Purchasing's own, per SKU).

```
model/          concepts.play, suppliers.play, replenishment.play (replenishment, demand, overview), purchase-orders.play, forms.play
translation/    inbound.play (what Warehouse and Sales say) and publish.play (what Purchasing says)
```

## What it speaks to the other contexts

| # | Public event | Direction | Private event it is built from / turns into |
| --- | --- | --- | --- |
| P5 | `OrderConfirmed` (Sales) | in (`OrderConfirmedAtPurchasing`: orderId, sku, quantity) | `DemandRecorded` |
| P10 | `StockBelowReorderPoint` (Warehouse) | in (`StockBelowReorderPointAtPurchasing`) | `ReplenishmentRequested` |
| P11 | `PurchaseOrderReceived` | out (one receipt line: purchaseOrderId, supplierId, receiptId, sku, quantity, receivedAt) | `PurchaseOrderGoodsReceived` |

## The automation

**DraftReplenishmentOrder** (reaction `AutoReplenish`, runs as system role `Buyer`): `ReplenishmentRequested` -> command
`CreateReplenishmentPurchaseOrder` (reads the SKU's preferred supplier, drafts a one-line purchase order with that supplier and price).
- **Skips when an open purchase order already covers the SKU**: the constraint `OneOpenReplenishmentOrderPerSku` (`unique sku on ReplenishmentOrderPlaced`,
  released when goods are received or the order is cancelled) refuses a second order and the reaction acknowledges the refusal.
- **No preferred supplier** refuses the command's read; the reaction records `ReplenishmentBlocked` (shown in the queue).
- The new purchase order is identified by the warehouse's `crossingId`: a reaction cannot generate ids ([Screenplay#858](https://github.com/Cratis/Screenplay/issues/858)).

## Constraints

`UniqueSupplierTaxId`, `UniquePurchaseOrderNumber` (the number is given at submission), `OneSubmissionPerPurchaseOrder`, `OneOpenReplenishmentOrderPerSku`.

## Screens

| Screen | Template | Reached from |
| --- | --- | --- |
| `PurchaseOrders` list | `ListScreen` | Purchasing / Purchase orders |
| `NewPurchaseOrder` (supplier -> lines -> review, one CommandForm per step) | `WizardScreen` | toolbar "New purchase order" |
| `PurchaseOrderPage` (record with the actions Add line, Submit, Receive goods, Cancel, Archive) | `RecordPage` | row click |
| `Suppliers` (master list, offers as detail) and `SupplierPage` | `MasterDetailScreen`, `RecordPage` | Purchasing / Suppliers |
| `PreferredSuppliers`, `ReplenishmentQueue` | `ListScreen` | Purchasing / Replenishment |
| `OpenDemand` | `ListScreen` | Purchasing / Demand |
| `PurchasingDashboard` | `DashboardScreen` | Purchasing / Dashboard |
| the dialogs `NewSupplier`, `NewOffer`, `ChooseSupplier`, `AddLine`, `Submit`, `ReceiveGoods` (`SideSheet`), `Cancel`, `Archive`, `RecordDemand`, `ClearSupplier` | `QuickCreateDialog` / `SideSheet` | toolbar buttons |

## Honest deviations from the design (all forced by the language today)

- Captures cannot copy or fan out a list ([Screenplay#857](https://github.com/Cratis/Screenplay/issues/857)): P5 is consumed as one scalar line
  and P11 is published one receipt line per event (see `agents/wave2/ecommerce-contracts-p5.md`).
- The supplier is chosen by the Buyer (`ChoosePreferredSupplier`) from the offers, not computed as the cheapest: a projection cannot pick a minimum.
- A purchase order is numbered at submission (`PO-...`), not at creation: a reaction-created draft cannot generate a number.
- No read model is composed by a performer: a performer query makes every specification of the application unsupported (`PLAY0268`,
  [Screenplay#862](https://github.com/Cratis/Screenplay/issues/862)). The dashboard is projected; its counters all state every `from`
  (`add ... by 0`) because any event may be the first a row sees. What the performer version would say (not compiled):

  ````screenplay
  query CurrentPurchasingFigures => PurchasingFigures
    performer
      ```csharp
      var orders = await readModels.GetAll<PurchaseOrderRow>();
      var suppliers = await readModels.GetAll<SupplierRow>();
      return new PurchasingFigures("global", orders.Count(), suppliers.Count(),
          orders.Sum(o => o.OrderedQuantity), orders.Sum(o => o.ReceivedQuantity));
      ```
  ````
- Cross-context mappings must share one concept ([Screenplay#861](https://github.com/Cratis/Screenplay/issues/861)): an id that travels (`crossingId` from Warehouse, read as `PurchaseOrderId` here) is declared with
  the consumer's own concept in the consumer's local copy of the contract.
- A command specification cannot list a cascaded public event (`PLAY0285`): the accepted path of `ReceivePurchaseOrderGoods` is proven in `PublishGoodsReceived`.
- Domain blueprints (`Cratis.Blueprint.Orders/Inventory/Approvals`) are not used here: Purchasing has no blueprint of its own, the foundation templates are referenced.
