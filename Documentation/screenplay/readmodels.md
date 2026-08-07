# Read models

A [projection](projections/index.md) says how state is built from events, and until now that was the only way a read model appeared in a document — as the name on the right of an arrow. A view nothing declarative could build had nowhere to go at all: no way to name it, no way to say what it holds, no way to say what folds into it.

`readmodel` gives it somewhere.

## Syntax

```screenplay
readmodel <Name>
  [description "<text>"]
  <property> <Type>[?]
  ...
```

A read model declares what it **is** — its shape, and nothing else:

```screenplay
readmodel AccountBalance
  description "What the account is worth right now"
  balance   Decimal
  movements Int
```

## The arrow always points the same way

A read model never declares what composes it. Whatever builds it names it, with the same `=>` a projection uses:

```
projection Deposits => AccountBalance     ← a projection builds it
reducer    Balance  => AccountBalance     ← or a reducer does, never both
```

So there is one arrow and one direction: to find where a read model's state comes from, you look for the thing pointing at it. A read model that declared its own sources would be a second, opposite arrow saying the same thing, and the two would eventually disagree.

**Exactly one thing may build a read model.** Two builders is a compile error — either could have produced the value in front of a reader, and nothing in the document would say which. Note this is about the read model, not the slice: a slice may still declare [several projections](projections/index.md#several-projections-in-one-slice), each building a different read model.

## Reducers

Some views are not expressible as a projection. "Current state plus this event gives the next state" — a running balance that depends on the previous one, a state machine, a computed view that reads what it already holds. `reducer` is for exactly those:

````screenplay
reducer Balance => AccountBalance
  on AmountDeposited
    csharp
      ```
      return context.State is null
          ? new(context.Event.amount, 1)
          : context.State with { balance = context.State.balance + context.Event.amount };
      ```
  on AmountWithdrawn
    file Reducers/Withdrawn.cs
````

- `reducer <Name> => <ReadModel>` — reads the same way `projection <Name> => <ReadModel>` does, on purpose.
- `on <EventType>` — one rule per event the reducer folds in. A rule with no body is a complete statement that the reducer observes the event, which is the part a reader needs; the reduction itself is code by definition, since it is what a projection could not say.
- The reduction lives inline in a fenced block, or in a `file` — the same choice every other construct that needs exact detail offers.

Prefer a projection where one will do. A reducer is code, and code is the part of a document a reader cannot check at a glance.

## What a reduction is given

A rule's body compiles against `ReducerContext`, in scope as `context`, and answers with the read model as it stands after the event:

| Member | What it holds |
| --- | --- |
| `State` | the read model before this event — **`null` for the first event on an instance** |
| `Event` | the event being folded in |
| `Key` | the identity of the instance being built |
| `Tenant` | the tenant the events are being reduced for |
| `Occurred` | when the event occurred |
| `SequenceNumber` | the event's position in its sequence |
| `IsFirst` | whether `State` is null, for readability |

`State` is the only nullable member, and that is the whole shape of a reduction: every fold after the first is given what the previous one returned, and the first is given nothing to build on. A rule that ignores the null case is a rule that only works on an instance that already exists.

## See also

- [Projections](projections/index.md) — the declarative way to build a read model, and the first thing to reach for.
- [Queries](queries.md) — how a read model is read once it exists.
- [Commands](commands.md#what-the-command-reads) — how a command declares the state it decides against.
