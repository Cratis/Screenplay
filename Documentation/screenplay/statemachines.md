---
title: "State machines"
description: "Declare in Screenplay where a thing is in its lifecycle: the read model a state machine follows, its states, the events that move it on and what entering a state does."
---

# State machines

A state machine says where something is in its lifecycle - an order is Placed, Paid, Packed, Shipped, Delivered - and what moves it on. It **follows** one read model whose key is the identity of one instance, and it never decides on commands: it follows facts that already happened. Its meaning is the meaning of the Cratis.Orleans `EventSourcedStateMachine`, so the model can be edited in Studio, executed and drawn by Stage, and pinned by specifications.

A `statemachine` is a top-level declaration, a sibling of `system`, `trigger`, `policy` and `persona`. It names events, commands, workflows and a read model declared in slices of any module. It is admitted from [ESM v36](interoperability.md); a model that declares one selects it.

## Syntax

```screenplay
statemachine <Name>
  [description "<text>"]
  follows <ReadModel>
  state <Name> [initial] [final]
    [entering send <Command>]
    [entering start <Workflow>]
    [on <Event> [when <condition>] -> <State>]
  [specification <Name> ...]
```

- Exactly one state is `initial`. Any number are `final`, and a final state has no transitions.
- `on <Event> [when <condition>] -> <State>` is a transition. The condition is the [condition grammar](commands.md) of `produces when`, over the properties of the followed read model, as the read model is after the event. A condition over an optional property is refused when the model is bound: optional properties have no portable comparison.
- `entering send <Command>` and `entering start <Workflow>` say what entering the state does. Entering may run again on redelivery, so the command or workflow must be safe to repeat.

```screenplay
statemachine OrderLifecycle
  description "Where an order is, from checkout to delivery"
  follows OrderProgress
  state Placed initial
    on PaymentCaptured -> Paid
    on OrderWithdrawn -> Cancelled
  state Paid
    entering start FulfilOrder
    on OrderPacked -> Packed
    on OrderWithdrawn -> Cancelled
    on RefundIssued -> Refunded
  state Packed
    on OrderHandedToCarrier -> Shipped
  state Shipped
    entering send NotifyCustomer
    on DeliveryConfirmed -> Delivered
    on ParcelReturned when returnReason != "" -> Refunded
  state Delivered final
  state Cancelled final
  state Refunded final
```

## What it means

- A state **allows** exactly the targets of the transitions written under it.
- When an event arrives, the machine takes the **first transition on that event, in declaration order across the whole machine**, whose condition holds and whose target the current state allows. Anything else is ignored; that is not an error. A transition is not tied to the state it is written under - the writing state only contributes its target to what is allowed - so a transition is normally written under every state that should take it.
- The current state is derived from the followed read model after a restart. It is never stored separately.

## Specifications

A state machine is pinned by specifications written inside it, with the slice steps `given <Event>` (with `for` and values) and `when append <Event>`, and these expectations:

| Line | Meaning |
| --- | --- |
| `then is <State>` | The instance is in the state after the event. |
| `then sends <Command>` | Entering the new state sent the command. Every command sent is stated, in order. |
| `then starts <Workflow>` | Entering the new state started the workflow. Every workflow started is stated, in order. |

```text
specification AReturnWithoutAReasonIsIgnored
  given CheckoutCompleted
    for "0e7d9b1a-3c5f-4a2e-8d6b-7c9e1f2a3b01"
  given PaymentCaptured
    for "0e7d9b1a-3c5f-4a2e-8d6b-7c9e1f2a3b01"
  when append ParcelReturned
    for "0e7d9b1a-3c5f-4a2e-8d6b-7c9e1f2a3b01"
    returnReason = ""
  then is Paid
```

The reference evaluator replays the facts, folds the followed read model after each one and applies the transition rule above; `screenplay test` runs these specifications with the others.

## Checks

| Code | When |
| --- | --- |
| `PLAY0834` | The declaration is malformed. |
| `PLAY0835` | There is no `initial` state, or more than one. |
| `PLAY0836` | A state can never be reached from the initial state (warning). |
| `PLAY0837` | A state that is not final has no way out (warning). |
| `PLAY0838` | A final state has transitions out. |
| `PLAY0839` | A transition targets a state the machine does not declare. |
| `PLAY0840` | An event, read model, command or workflow is not declared. |
| `PLAY0841` | A condition reads a property the followed read model does not have. |
| `PLAY0842` | Two unguarded transitions on one event leave the same state. |

See [Workflows](workflows.md) for the process a state can start, and decision [0124](https://github.com/Cratis/Screenplay/blob/main/decisions/0124-state-machines-and-workflows-esm-v36.md) for the full contract.
