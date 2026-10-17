# Sales (module `Orders`)

What a customer bought: **Intake, Approval, Fulfilment, Reporting**. An order arrives as the public `OrderPlaced`, is translated into
the private `OrderReceived`, approved (a policy-named automation approves small orders), confirmed and shipped. What Sales says about itself
leaves as `OrderConfirmed` (P5) and `OrderStatusChanged` (P6).

| Slice | Kind | What it shows |
| --- | --- | --- |
| `ReceiveOrder`, `UpdatePriceList`, `RecordReservation`, `HoldOrderOnShortage`, `RecordShipment` | Translate (inbound) | P4/P2/P7/P8/P12 -> `OrderReceived`, `PriceListUpdated`, `OrderReservationCompleted`, `OrderPutOnHold`, `OrderShipmentRecorded` |
| `PublishOrderConfirmed`, `PublishOrderStatus` | Translate (outbound) | `OrderConfirmationIssued` -> public P5 `OrderConfirmed`; seven private status events -> ONE public P6 `OrderStatusChanged` |
| `AutoApproveSmallOrders` (reaction `SmallOrderAutoApproval`), `ConfirmApprovedOrder` | Automation | `quotedTotal < 500` approves itself; an approved order is confirmed. Constraint `OneOpenOrderPerBasket` |
| `ApproveOrder`, `RejectOrder`, `ApplyDiscount`, `ApplyLargeDiscount`, `ConfirmOrder`, `CancelOrder`, `ReleaseHold`, `MarkOrderFulfilled` | StateChange | decisions run against `OrderState` (protected `reads`); managers vs reps (`IsSalesManager`); a rep's discount stops at 15%, `OneConfirmationPerOrder` |
| `OrdersPipeline`, `OrderDetails`, `ApprovalInbox`, `SalesDashboard`, `PriceList`, `OrderState` | StateView | board by status, record page with timeline, manager inbox with a decision dialog, per-market figures (a projection joining `OrderReceived` + `OrderConfirmationIssued`) |

Screens use the foundation blueprint templates by reference (`BoardScreen`, `ListScreen`, `RecordPage`, `DashboardScreen`, `ConfirmDialog`); `OrderPage`
carries the custom component `acme:orderTimeline` in its `activity` slot. The Orders/Approvals *domain* blueprints are not used: they need their own approved package sets to render.

Language notes: the private event for P5 is `OrderConfirmationIssued` (one application cannot hold two events named `OrderConfirmed`); P6 carries no
`customerId`/`at` (inbound-origin status events have no customer; `$eventContext.*` makes every specification of the application unsupported); the
reference evaluator cannot copy the composite `lines`/`shipTo` in the `ReceiveOrder` capture, so that one specification is reported `unsupported`.
