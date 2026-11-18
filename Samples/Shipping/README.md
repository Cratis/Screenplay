# Shipping
A parcel's journey from the warehouse to the receiver, and the two outside systems it depends on: a carrier that books pickups and a chat service the dispatchers watch. It shows a **state machine** that follows a parcel, a **workflow** that deals with the one thing that goes wrong - the carrier cannot take the parcel - and the **reactions** that talk to the outside systems.
```text
Shipping/
  shipping.play   two systems, a state machine, a workflow, three automations, a parcel board and the dispatchers' commands
```
## The story
1. An order is packed. `PickupBooking` asks the carrier to collect the parcel and records the answer: a tracking number, no capacity, or a failure.
2. A parcel the carrier cannot take is not finished, it is *waiting*. The `ParcelFlow` state machine moves it to `Unbookable`, and entering that state starts the `Pickup` workflow.
3. `Pickup` waits four hours, asks for the pickup again (the `RequestPickup` command) and gives the carrier an hour to book it. If the carrier still has not, the workflow escalates (`EscalatePickup`), and `TellDispatchers` posts it to chat.
4. Once the pickup is booked the dispatchers record what happens next - collected, delivered or returned - and the parcel ends in `Delivered` or `Returned`.
## Where the outside systems are called
The workflow and the state machine never call a system. They send commands, and reactions answer them:
| Construct | Where | What it shows |
| --- | --- | --- |
| `system Carrier`, `system Chat` | top level | Typed abilities, an effect class, a returned value and a declared outcome. |
| `invokes Carrier.BookPickup` with `on returned`, `on NoCapacity`, `on failed` | the `PickupBooking` and `RetryPickupBooking` reactions | One branch per answer, reading `$result` and `$failure`, each recording the fact that follows. |
| `given Carrier.BookPickup returns`, `answers`, `fails` | those reactions' specifications | Scripting what the system answers, so nothing calls a real carrier. |
| `invokes Chat.Post` | the `TellDispatchers` reaction | An ability with no answer worth branching on. |
A workflow action or a state's `entering` that invokes an ability parses and validates but does not bind yet (#1252), so this sample keeps the call in a reaction. [Systems and abilities](../../Documentation/screenplay/systems.md) has the rules and the fixture for the other call sites.
## The state machine and the workflow
| Construct | Where | What it shows |
| --- | --- | --- |
| `statemachine ParcelFlow` `follows ParcelView` | top level | Seven states from `Packed` to `Delivered` or `Returned`; it observes the read model and never decides. |
| `entering starts Pickup` | `Unbookable` | Entering a state sets a workflow off. |
| `on ParcelDelivered -> Delivered` from `InTransit` only | the specifications | A delivery reported before the carrier collected the parcel changes nothing. |
| `workflow Pickup` with `trigger when ParcelFlow enters Unbookable` and a `manual` trigger | top level | Started by the state machine, or by a dispatcher. |
| `wait ... for 4 hours` and `wait ... for PickupBooked expires after 1 hour` | the workflow | A pause that costs nothing, and a wait for a fact that gives up. |
| `outcome <Node> resumed`, `outcome <Node> expired` | the workflow's specifications | Choosing the way each wait ends. |
Read [State machines](../../Documentation/screenplay/statemachines.md) and [Workflows](../../Documentation/screenplay/workflows.md) for the rules.
