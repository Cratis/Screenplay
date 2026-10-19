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
| `OrdersPipeline`, `OrderLookup`, `OrderDetails`, `ClosedOrders`, `ApprovalInbox`, `SalesDashboard`, `PriceList`, `OrderState` | StateView | board by status, the desk's order list, record page with timeline and the rep's actions, cancelled/rejected orders with reasons, manager inbox with a decision dialog, figures per order (a projection joining `OrderReceived` + `OrderConfirmationIssued`) |

Screens belong to personas (the board draws a screen in the row of the persona who may use its slice), not to actors of their own:

| Persona | Screens |
| --- | --- |
| SalesRep | `OrdersPipeline` (board), `OrderList` (the customer-service desk), `OrderPage` (facts, delivery, progress; toolbar actions give discount / release hold / mark fulfilled / cancel), `Cancellations`, `PriceList` |
| SalesManager | `ApprovalInbox` and its `Decision` dialog (approve, reject, large discount), `SalesDashboard` |

Screens use the foundation blueprint templates by reference (`BoardScreen`, `ListScreen`, `RecordPage`, `DashboardScreen`, `ConfirmDialog`). Nothing models
validation, error, loading, empty or confirmation states: the components bring them. There is no returns process in Sales yet; the cancellations screen shows what
was cancelled or rejected and why. Order lines and the delivery address are composite values, which sample data cannot carry, so the order page shows the
delivery city and postal code the read model copies out of the address. The Orders/Approvals *domain* blueprints are not used: they need their own approved package sets to render.

Language notes: the private event for P5 is `OrderConfirmationIssued` (one application cannot hold two events named `OrderConfirmed`); P6 carries no
`customerId`/`at` (inbound-origin status events have no customer; `$eventContext.*` makes every specification of the application unsupported); the
reference evaluator cannot copy the composite `lines`/`shipTo` in the `ReceiveOrder` capture, so that one specification is reported `unsupported`.
