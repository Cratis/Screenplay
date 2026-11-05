---
title: "Screenplay workflows: model a durable business process"
description: "Model order fulfilment with a Screenplay workflow: send commands, wait for events or approvals, and specify where the process completes or fails."
---

# Workflows

A workflow in Cratis Screenplay describes a durable business process that continues through several steps, including waits for events or a person's approval. Use it when an order being Paid is only the start of the work: stock must be reserved, a parcel dispatched and delays handled.

A [state machine](statemachines.md) observes where the order is in its lifecycle. A [reaction](reactions.md) responds to an occurrence with effects or command requests. The workflow makes the longer process explicit, including where it suspends and which path it takes after a failure.

A workflow runs one at a time per subject. The subject is the read model named by `for`, and its key identifies the order or other item the run is about. The declaration is top-level and uses the semantics of Cratis Choreography's durable workflow engine. It is admitted from executable semantic model ([ESM v36](interoperability.md)).

## Syntax

Each named step is a node. Its outlets are the possible outcomes, and an edge connects an outlet to the next node: `<outlet> -> <Node>`.

This excerpt continues the [order lifecycle](statemachines.md#syntax). It assumes the `OrderProgress` read model and the referenced commands, events and approver are declared elsewhere in the application.

```screenplay
workflow FulfilOrder
  description "Reserves stock, ships the order and tells the shopper; a backorder needs a warehouse sign-off"
  for OrderProgress
  trigger when OrderLifecycle enters Paid
    -> ReserveStock
  action ReserveStock sends HoldStock attempts 3 timeout 1 minute retry after 30 seconds
    next -> InStock
    failed -> Escalate
  decide InStock when itemsInStock == true
    "in stock" -> CreateParcel
    "backorder" -> AcceptBackorder
  action CreateParcel sends CreateParcel
    next -> ParcelLeaves
  wait ParcelLeaves for OrderHandedToCarrier expires after 2 days
    resumed -> TellCustomer
    expired -> Escalate
  approval AcceptBackorder by WarehouseClerk expires after 1 day
    approved -> PlaceBackorder
    rejected -> CancelOrder
    expired -> Escalate
  action PlaceBackorder sends PlaceBackorder
    next -> ParcelLeaves
  action CancelOrder sends WithdrawOrder
  action TellCustomer sends NotifyCustomer
  action Escalate sends EscalateOrder
```

When the order enters Paid, the run sends `HoldStock`. A successful reservation leads to a decision over `itemsInStock` on the subject read model. Stock on hand leads to parcel creation; otherwise a warehouse clerk must approve the backorder. The run then waits for the carrier event, or escalates if the deadline passes.

Node words are recognized only directly inside a `workflow`. This leaves `trigger` available as a top-level declaration and `join` as a projection word elsewhere.

| Node | Line | Outlets |
| --- | --- | --- |
| trigger | `trigger [Name] on <Event>`, `when <StateMachine> enters <State>`, `every <n> <unit>`, `at <HH:mm> [on <Weekday>]` (the [reaction](reactions.md) schedule forms) or `manual`. Write its edge as `-> <Node>`. | `next` |
| action | `action <Name> sends <Command> [attempts <n>] [timeout <n> <unit>] [retry after <n> <unit>]` | `next`, `failed` |
| decide | `decide <Name> when <condition>`; the condition is over the subject read model | exactly two of your own: the first is taken when the condition holds, the second when it does not |
| wait | `wait <Name> for <Event> [expires after <n> <unit>]` waits for an event about the subject; `wait <Name> for <n> <unit>` lets time pass | `resumed`, `expired` |
| approval | `approval <Name> by <Persona or Policy> [expires after <n> <unit>]` | `approved`, `rejected`, `expired` |
| foreach | `foreach <Name> over <collection property of the subject> [parallel <n>]` | `each`, `completed` |
| join | `join <Name> all`, `join <Name> any` or `join <Name> at least <n>` | `next` |

An unnamed trigger is called `Trigger`, then `Trigger2` and so on. A workflow may have several triggers. Units are `second`, `minute`, `hour` and `day`, singular or plural.

Connect each negative outcome you want to recover from. An unconnected `failed`, `rejected` or `expired` outlet fails the run; an unconnected positive outlet ends that branch. In the example, reservation failure reaches `Escalate`, whose successful action ends its branch. Outlet names match Cratis Choreography's `WellKnownOutlets`.

An action's command receives the subject key and subject properties with matching names. The executor defines how to fill any remaining inputs. The reference evaluator records that the command was sent; it does not execute that command's business effects.

## Specifications

Write specifications inside the workflow to check its paths before relying on an executor. Establish the history, append the triggering event or start the run, then state the outcomes needed for that path.

The reference evaluator computes decisions from the subject read model and records commands sent by actions. A wait or approval takes the outcome supplied by the specification; without one, it suspends the run. This checks the process graph, not a live carrier integration or a person's response.

| Line | Meaning |
| --- | --- |
| `given <Event>` | A fact that already happened (with `for` and values). |
| `when append <Event>` | The event that sets the run off: it starts the run when it matches an `on` trigger, or moves a state machine into the state a `when ... enters` trigger waits for. |
| `when start <key>` | Starts the run by hand for the subject key, through a `manual` or scheduled trigger. |
| `outcome <Node> <outlet>` | The outlet an action (`next`, `failed`), wait (`resumed`, `expired`), approval (`approved`, `rejected`, `expired`) or foreach takes. An action defaults to `next`; a failed action sends its command once per attempt. |
| `then sends <Command>` | A command was sent. Every command sent is stated, in order. |
| `then completes` | The run ended without failing. |
| `then fails at <Node>` | The run failed at the node. |
| `then waits at <Node>` | The run is suspended at a wait or an approval. |

```text
specification TheRunWaitsForTheCarrier
  description "With no word from the carrier the run waits at ParcelLeaves"
  given CheckoutCompleted
    for "0e7d9b1a-3c5f-4a2e-8d6b-7c9e1f2a3b01"
  when append PaymentCaptured
    for "0e7d9b1a-3c5f-4a2e-8d6b-7c9e1f2a3b01"
  then sends HoldStock
  then sends CreateParcel
  then waits at ParcelLeaves
```

The example expects the stock to be available in `OrderProgress` after its given history. With no outcome supplied for `ParcelLeaves`, the run stops there after sending `HoldStock` and `CreateParcel`. Add specifications with `outcome ParcelLeaves resumed` or `outcome ParcelLeaves expired` to cover dispatch and escalation.

For collection work, a `foreach` runs its `each` branch once per item in item order, then takes `completed`. Its `parallel` setting bounds the executor, not the reference run. A `join` counts arrivals: `all` waits for every branch (the incoming edges, times the items of a `foreach` that feeds it), `any` for the first and `at least n` for n. A duration wait resumes immediately in a reference run, so it does not test elapsed wall-clock time.

## Checks

| Code | When |
| --- | --- |
| `PLAY0843` | The declaration is malformed. |
| `PLAY0844` | There is no trigger. |
| `PLAY0845` | A node can never be reached from a trigger (warning). |
| `PLAY0846` | An edge points at a node the workflow does not declare. |
| `PLAY0847` | An edge leaves by an outlet the node kind does not have. |
| `PLAY0848` | A decision does not have exactly two outlets, or one leads nowhere. |
| `PLAY0849` | A wait for an event or an approval has no expiry (warning). |
| `PLAY0850` | A join has a single incoming branch (information). |
| `PLAY0851` | A command, event, persona or policy, state machine or property is not declared. |
| `PLAY0839` | A trigger waits for a state its state machine does not declare. |
| `PLAY0841` | A condition or a foreach reads a property the subject does not have. |

See [State machines](statemachines.md) for the lifecycle that can start a workflow, and decision [0124](https://github.com/Cratis/Screenplay/blob/main/decisions/0124-state-machines-and-workflows-esm-v36.md) for the full contract.
