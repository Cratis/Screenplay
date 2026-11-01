---
title: "Workflows"
description: "Declare in Screenplay a durable process: a graph of triggers, actions, decisions, waits, approvals, fan-outs and joins that runs one at a time per subject."
---

# Workflows

An Automation slice expresses one reaction: when X happens, do Y. Real processes are longer. They send several commands, choose a path, wait for a fact or a person, give up after a deadline, fan out over items and join again. A `workflow` says that as a graph, with the meaning of the Cratis.Orleans durable workflow engine, so Studio can edit it, Stage can execute and draw it, and specifications can pin a run.

A workflow runs **one at a time per subject**: the key of the read model named by `for`, for example one run per order. It is a top-level declaration, admitted from [ESM v36](interoperability.md).

## Syntax

A workflow is a list of named **nodes**, each with named **outlets**; an **edge** connects an outlet to a node. Edges are written under their node as `<outlet> -> <Node>`.

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

The node words are contextual. They are read only on the lines directly inside a `workflow`, so `trigger` (also a top-level construct) and `join` (also a projection word) keep their meaning everywhere else.

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

An unconnected negative outlet (`failed`, `rejected`, `expired`) **fails the run**. An unconnected positive outlet **ends that branch**. The outlet names are exactly those of Cratis.Orleans `WellKnownOutlets`. An action's command receives the subject key and the subject properties of the same name; how the executor fills the rest is its contract.

## Specifications

A run is pinned by specifications written inside the workflow. They state the facts that happened, one event or a start, what the waits, approvals and actions turn out to be, and what follows. The reference evaluator steps the graph deterministically: decisions are computed from the subject read model, an action sends its command, and a wait or approval takes the outcome the specification states or, with none stated, suspends the run.

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

A `foreach` runs its `each` branch once per item in item order (`parallel` bounds the executor, not the reference run) and then takes `completed`. A `join` counts arrivals: `all` waits for every branch (the incoming edges, times the items of a `foreach` that feeds it), `any` for the first and `at least n` for n. A wait for a duration resumes at once in a reference run.

## Checks

| Code | When |
| --- | --- |
| `PLAY0842` | The declaration is malformed. |
| `PLAY0843` | There is no trigger. |
| `PLAY0844` | A node can never be reached from a trigger (warning). |
| `PLAY0845` | An edge points at a node the workflow does not declare. |
| `PLAY0846` | An edge leaves by an outlet the node kind does not have. |
| `PLAY0847` | A decision does not have exactly two outlets, or one leads nowhere. |
| `PLAY0848` | A wait for an event or an approval has no expiry (warning). |
| `PLAY0849` | A join has a single incoming branch (information). |
| `PLAY0850` | A command, event, persona or policy, state machine or property is not declared. |
| `PLAY0838` | A trigger waits for a state its state machine does not declare. |
| `PLAY0840` | A condition or a foreach reads a property the subject does not have. |

See [State machines](statemachines.md) for the lifecycle that can start a workflow, and decision [0124](https://github.com/Cratis/Screenplay/blob/main/decisions/0124-state-machines-and-workflows-esm-v36.md) for the full contract.
