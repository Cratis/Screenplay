---
id: 0124
title: Admit state machines and workflows (ESM v36)
status: accepted
stage: implemented
class: contract
reversibility: costly
decided: 2026-11-01
decider: Einar Ingebrigtsen (requested in Cratis/Screenplay#1118 and #1119)
amends: 0025
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

A domain says where a thing is in its lifecycle (an order is Placed, Paid, Packed, Shipped, Delivered) and runs processes longer than one reaction (send several commands, choose a path, wait for a fact or a person, give up after a deadline, fan out and join). Both lived in prose and hand-written code. Cratis.Orleans runs the first as `EventSourcedStateMachine` and the second as a durable workflow engine ([Cratis/Orleans#167](https://github.com/Cratis/Orleans/issues/167)). Studio edits, Stage executes and renders; specifications pin both ([#1118](https://github.com/Cratis/Screenplay/issues/1118), [#1119](https://github.com/Cratis/Screenplay/issues/1119)).

## Decision

### 1. `statemachine`

A top-level declaration, sibling of `system`, `trigger`, `policy` and `persona`. It may sit in a placed file.

```screenplay
statemachine OrderLifecycle
  follows OrderProgress
  state Placed initial
    on PaymentCaptured -> Paid
  state Paid
    entering start FulfilOrder
    on ShipmentReturned when returnReason != "" -> Refunded
  state Refunded final
  specification PaidOrdersAreFulfilled
    given OrderPlaced
      for "..."
    when append PaymentCaptured
      for "..."
    then is Paid
    then starts FulfilOrder
```

- `follows <ReadModel>`: the read model whose key identifies one instance. The machine never decides on commands.
- `state <Name> [initial] [final]`: exactly one `initial`, any number of `final`.
- `on <Event> [when <condition>] -> <State>`: the condition is the existing condition grammar (`produces when`), over the followed read model's properties. The issue's `when returnReason is set` is not part of that grammar; use a comparison such as `returnReason != ""`. Conditions over optional properties are refused by the existing condition binder (no portable comparison).
- `entering send <Command>` and `entering start <Workflow>`: what entering a state does. It may run again on redelivery, so it must be safe to repeat.

Semantics match Cratis.Orleans `EventSourcedStateMachine`, deliberately including its one surprise:

- A state allows exactly the targets of the transitions written under it.
- On an event, the machine takes the **first transition on that event, in declaration order across the whole machine**, whose guard holds over the followed read model as it is after the event and whose target the current state allows. A transition is not tied to the state it is written under; the writing state only contributes its target to what is allowed. Anything else is ignored; that is not an error.
- The current state is derived from the followed read model after a restart, never stored.

### 2. `workflow`

```screenplay
workflow FulfilOrder
  for OrderProgress
  trigger when OrderLifecycle enters Paid
    -> ReserveStock
  action ReserveStock sends ReserveStock attempts 3 timeout 1 minute retry after 30 seconds
    next -> InStock
    failed -> Escalate
  decide InStock when itemsInStock == true
    "in stock" -> CreateShipment
    "backorder" -> AcceptBackorder
  wait ParcelLeaves for ShipmentDispatched expires after 2 days
    resumed -> TellCustomer
    expired -> Escalate
  approval AcceptBackorder by WarehouseLead expires after 1 day
    approved -> PlaceBackorder
    rejected -> CancelOrder
    expired -> Escalate
```

A workflow runs one at a time per subject: the key of the read model named by `for`. Nodes have unique names inside the workflow; edges sit under the node as `<outlet> -> <Node>`. The node words are contextual: they are read only on the lines directly inside a `workflow`, so `trigger` (a top-level construct) and `join` (a projection word) keep their meaning elsewhere, and the Monaco `constructKeywords` list carries only `statemachine` and `workflow`.

| Node | Line | Outlets |
| --- | --- | --- |
| trigger | `trigger [Name] on <Event>`, `when <StateMachine> enters <State>`, `every <n> <unit>`, `at <HH:mm> [...]` (the reaction schedule grammar), or `manual`. An unnamed trigger is named `Trigger`, `Trigger2`, ... | `next` (written `-> Node`) |
| action | `action <Name> sends <Command> [attempts <n>] [timeout <n> <unit>] [retry after <n> <unit>]` | `next`, `failed` |
| decide | `decide <Name> when <condition>` | exactly two author-named outlets; the first is taken when the condition holds, the second when it does not |
| wait | `wait <Name> for <Event> [expires after <n> <unit>]` or `wait <Name> for <n> <unit>` | `resumed`, `expired` |
| approval | `approval <Name> by <Persona or Policy> [expires after <n> <unit>]` | `approved`, `rejected`, `expired` |
| foreach | `foreach <Name> over <collection property of the subject> [parallel <n>]` | `each`, `completed` |
| join | `join <Name> all`, `any` or `at least <n>` | `next` |

Outlet names equal Cratis.Orleans `WellKnownOutlets`. An unconnected negative outlet (`failed`, `rejected`, `expired`) fails the run; an unconnected positive outlet ends that branch. A wait for an event waits for an event about the subject (same event source as the subject key). An action's command receives the subject key and the subject's properties of the same name; filling the rest is the executor's contract, and the reference evaluator records only that the command was sent.

### 3. Specifications

The issue pins state machines with the slice-level Given/When/Then. State machines and workflows are not owned by a slice, and 0115 makes a specification run its own slice only, so a `specification` is written **inside** the `statemachine` or `workflow` it pins. It reuses the slice step forms:

- `given <Event>` (with `for` and values), `when append <Event>`, `when start <key>` (workflow, manual or scheduled trigger).
- `outcome <Node> <outlet>`: the outlet an action, wait or approval takes. An action defaults to `next`; a wait or approval with no stated outcome suspends the run.
- `then is <State>`, `then sends <Command>`, `then starts <Workflow>`, `then completes`, `then fails at <Node>`, `then waits at <Node>`. The sent commands and started workflows are compared as ordered lists: a specification states every one.

### 4. ESM v36

Versions are numbered at admission (0025). v35 is reserved by 0119 for handler result contract v2 and is not yet published, so this change takes the next unused number, **v36**. v36 builds on v34; when 0119 admits v35 the chain becomes v34, v35, v36. The version is selected only when the model has a state machine or a workflow. New members of `application`, after `triggers`:

- `stateMachines`: `{ name, readModel, states: [{ name, initial, final, entering: [{ kind: send, command } | { kind: start, workflow }] }], transitions: [{ from, event, guard?, to }] (declaration order is semantic), specifications? }`.
- `workflows`: `{ name, subject, nodes, edges: [{ from, outlet, to }], specifications? }`. A node is `{ name, kind }` with `kind` one of `trigger`, `action`, `decision`, `wait`, `approval`, `forEach`, `join` plus the members of its kind: `trigger` (`event` | `stateEntered` with `stateMachine`, `state` | `schedule` with `every` or `at`, `onDayOfWeek`, `onDayOfMonth` | `manual`); `command`, `maxAttempts`, `attemptTimeout`, `retryDelay` (seconds); `condition`, `whenTrue`, `whenFalse`; `event` or `duration`, `expiresAfter`; `approverKind` (`persona` | `policy`), `approver`; `items`, `maxParallel`; `joinMode` (`all` | `any` | `atLeast`), `joinCount`. These map one to one onto the Cratis.Orleans workflow definition (node kinds Trigger, Action, Decision, Wait, Approval, ForEach, Join; node policy MaxAttempts, AttemptTimeout, RetryDelay).
- `specifications`: `{ name, given, whenAppended?, whenStarted?, outcomes, then }`.

State machines and workflows are keyed by name, like policies; they get no identity-catalog entries. References to events, commands, read models and properties are semantic ids.

### 5. Evaluator

`SemanticLifecycleRunner` derives a state machine from the facts (replaying them, folding the followed read model after each) and steps a workflow run deterministically: decisions are computed from the subject read model, an action sends its command (`attempts` times when its outcome is `failed`), a wait or approval takes its stated outcome or suspends the run, a foreach runs its `each` branch once per item in order (`parallel` is advisory in the reference run), and a join counts arrivals against its mode. A `wait` for time resumes at once.

### 6. Diagnostics

`PLAY0833` to `PLAY0850` (see the diagnostics page): invalid declaration, initial state, unreachable state, no way out, final state with transitions, unknown state, unknown reference, unknown condition property, ambiguous transitions; invalid workflow, no trigger, unreachable node, unknown node, unknown outlet, decision outlet problems, missing expiry, one-branch join, unknown workflow reference.

## Consequences

Stage reads `stateMachines` and `workflows` from v36 models and refuses a model whose version it does not admit. Cratis/AI#972 carries the corpus work. Orleans#167 owns the engine; its definition may add node kinds only by a new ESM version.
