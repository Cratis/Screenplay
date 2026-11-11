---
title: "Screenplay state machines: follow a business lifecycle"
description: "Use a Screenplay state machine to follow an order's lifecycle from recorded events, define transitions and specify commands or workflows on entry."
---

# State machines

In Cratis Screenplay, a state machine follows recorded events to show where one business item is in its lifecycle. Use it to describe an order moving from Placed to Paid and eventually Delivered, rather than to decide whether a command is allowed.

The machine follows a read model: a view built from events, whose key identifies one instance. After `PaymentCaptured`, the order can enter Paid and start fulfilment. The state machine describes the lifecycle consequence of that fact; the [workflow](workflows.md) describes the work that follows, including waits and recovery paths. Command validation and [requirements](commands.md) still decide whether a requested change may happen.

Its semantics match Cratis Choreography's `EventSourcedStateMachine`. A `statemachine` is a top-level declaration, beside `system`, `trigger`, `policy` and `persona`, and can reference declarations in slices of any module. It is admitted from executable semantic model ([ESM v36](interoperability.md)); declaring one selects that version.

## Syntax

```screenplay
statemachine <Name>
  [description "<text>"]
  follows <ReadModel>
  state <Name> [initial] [final]
    [entering invokes <Command>]
    [entering starts <Workflow>]
    [on <Event> [when <condition>] -> <State>]
  [specification <Name> ...]
```

- Exactly one state is `initial`. Any number are `final`, and a final state has no transitions.
- `on <Event> [when <condition>] -> <State>` is a transition. The condition is the [condition grammar](commands.md) of `produces when`, over the properties of the followed read model, as the read model is after the event. A condition over an optional property is refused when the model is bound: optional properties have no portable comparison.
- `entering invokes <Command>` and `entering starts <Workflow>` say what entering the state does. Entering may run again on redelivery, so the command or workflow must be safe to repeat.

The following lifecycle is an excerpt: `OrderProgress`, its events, `NotifyCustomer` and `FulfilOrder` must be declared elsewhere in the application. Read it alongside the [fulfilment workflow](workflows.md#syntax), which uses the same order subject.

```screenplay
statemachine OrderLifecycle
  description "Where an order is, from checkout to delivery"
  follows OrderProgress
  state Placed initial
    on PaymentCaptured -> Paid
    on OrderWithdrawn -> Cancelled
  state Paid
    entering starts FulfilOrder
    on OrderPacked -> Packed
    on OrderWithdrawn -> Cancelled
    on RefundIssued -> Refunded
  state Packed
    on OrderHandedToCarrier -> Shipped
  state Shipped
    entering invokes NotifyCustomer
    on DeliveryConfirmed -> Delivered
    on ParcelReturned when returnReason != "" -> Refunded
  state Delivered final
  state Cancelled final
  state Refunded final
```

## What it means

In the example, `PaymentCaptured` allows an order in Placed to move to Paid. Entering Paid starts `FulfilOrder`; entering Shipped invokes `NotifyCustomer`. These entry effects may run again on redelivery, so their receivers must be safe to repeat.

Transition lookup has a detail worth checking when you add states. A state allows exactly the targets written under it, but the machine searches transitions for an event across the whole declaration, in declaration order. It takes the first transition whose condition holds and whose target the current state allows. The condition sees the followed read model after the event.

A transition is therefore not restricted to the state it is written under. Write it under each state that should allow its target, and check the order of any competing transitions. If none qualifies, the event is ignored rather than rejected. After a restart, the current state is derived from the followed read model; it is never stored separately.

## Specifications

Write a specification inside the state machine to check a lifecycle outcome. Use `given <Event>` (with `for` and values) for history, then `when append <Event>` for the new fact. The expectations describe both the resulting state and any entry effects:

| Line | Meaning |
| --- | --- |
| `then is <State>` | The instance is in the state after the event. |
| `then invokes <Command>` | Entering the new state invoked the command. Every command invoked is stated, in order. |
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

Here, `ParcelReturned` has an empty reason, so it does not move the paid order to Refunded. The reference evaluator replays the facts, updates the followed read model after each one and applies the transition rule above. `screenplay test` runs these specifications with the others.

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

## Migrating from `send` and `start`

`invokes` is the one verb for asking a command to run. The old forms still compile and raise the deprecation warning `PLAY0397` with a repair; printing a document writes the new forms.

| Before | After |
| --- | --- |
| `entering send <Command>` | `entering invokes <Command>` |
| `entering start <Workflow>` | `entering starts <Workflow>` |
| `then sends <Command>` | `then invokes <Command>` |
