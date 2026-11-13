# Shipping

A small model that asks systems outside itself for something. It shows every place an ability can be
invoked and how the answer is handled.

```text
Shipping/
  shipping.play   two systems, a state machine, a workflow and one automation slice
```

| Construct | Where | What it shows |
| --- | --- | --- |
| `system Carrier`, `system Chat` | top level | Typed abilities, an effect class, a returned value and a declared outcome. |
| `invokes Carrier.BookPickup` with `on returned`, `on NoCapacity`, `on failed` | the `PickupBooking` reaction | One branch per answer, reading `$result` and `$failure`. |
| `given Carrier.BookPickup returns`, `answers`, `fails` | the reaction's specifications | Scripting what the system answers. |
| `action BookPickup invokes Carrier.BookPickup` | the `Pickup` workflow | An edge per outcome, and a later node reading `BookPickup.trackingNumber`. |
| `entering invokes Chat.Post` | the `ParcelFlow` state machine | A state's entry asking a system for an ability. |

Read [Systems and abilities](../../Documentation/screenplay/systems.md) for the rules.
