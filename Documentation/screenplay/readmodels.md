# Read models

A [projection](projections/index.md) says how state is built from events, and until now that was the only way a read model appeared in a document — as the name on the right of an arrow. A view nothing declarative could build had nowhere to go at all: no way to name it, no way to say what it holds, no way to say what folds into it.

`readmodel` gives it somewhere.

A read model can also carry one nonempty fenced `markdown` `documentation` block to explain why this view has its chosen shape or excludes certain facts. It is authoring-only (`PLAY0270`), with no executable effect. See [Descriptions and documentation](slices.md#descriptions-and-documentation).

## Syntax

```screenplay
readmodel <Name>
  [description "<text>"]
  [file <path>]
  <property> <Type> [optional]
  ...
```

The optional `file` line names the repository relative file this declaration is realized by, so a document can be navigated back to the code it describes. It is additive - it never stands in for any part of the declaration. See [File references](file-references.md).

A read model's `file` says where the *shape* lives, which is a different question from where whatever builds it lives - a reducer rule carries its own `file`, and so does a projection.

A read model declares what it **is** — its shape, and nothing else:

```screenplay
readmodel AccountBalance
  description "What the account is worth right now"
  balance   Decimal
  movements Int
```

## Keys

Mark a top-level property with the trailing `key` modifier to identify an instance. Explicit keys replace inference. Without a mark, executable binding infers the identifier from one unambiguous keyed query or one `*Id` property.

Each key part is required and noncollection. It cannot combine with `optional`, `generated`, `identifier` or `subject`. A single key property may have a composite type. Parts of a multipart key must be scalar. Declaration order is display order, not identity.

This complete [named-key fixture](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/fixtures/named-read-model-keys.play) declares a view, a lookup and a command read:

```screenplay
concept ResourceId : Uuid
concept Period : String

module Reporting
  feature Months
    slice StateView Totals
      readmodel ResourceMonth
        resourceId ResourceId key
        period Period key
        hours Decimal
      query FindMonth => ResourceMonth optional
        by
          resourceId ResourceId
          period Period
    slice StateChange CloseMonth
      command CloseMonth
        resourceId ResourceId identifier
        period Period
        reads ResourceMonth as totals
          by
            resourceId = resourceId
            period = period
        produces event MonthClosed
          period Period = period
```

A single-instance lookup supplies every part exactly once. Collection queries may filter by a subset. Query parts declare `name Type`; read parts map `name = source`. Sources for reads are required, noncollection property paths of compatible nominal types, not literals.

`PLAY0660` rejects invalid key declarations. `PLAY0661` names missing parts. `PLAY0662` rejects invalid lookup shapes or values. The typed `PLAY0661` repair covers command reads only. Queries and trigger reads are not repaired.

An explicit single key binds byte-identically to the equivalent inferred identifier. Composite keys, by-block queries and their fixtures remain authoring-only. Binding reports `PLAY0268` citing [#599](https://github.com/Cratis/Screenplay/issues/599).

`given readmodel` and `then readmodel` state every key part. Missing parts produce `PLAY0351`. Query results may omit them when arguments already select the instance. See [Specifications](specifications.md#read-model-state).

## The arrow always points the same way

A read model never declares what composes it. Whatever builds it names it, with the same `=>` a projection uses:

```text
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
    ```csharp
      return context.State is null
          ? new(context.Event.Amount, 1)
          : context.State with { Balance = context.State.Balance + context.Event.Amount };
      ```
  on AmountWithdrawn
    file Reducers/Withdrawn.cs
````

- `reducer <Name> => <ReadModel>` — reads the same way `projection <Name> => <ReadModel>` does, on purpose.
- `on <EventType>` — one rule per event the reducer folds in. Every rule needs an inline or file body to bind to the executable semantic model. A reducer with no bodies is rejected with a projection hint; mixing body-less and bodied rules is an error (`PLAY0398`).
- The reduction lives inline in a fenced block, or in a `file` — the same choice every other construct that needs exact detail offers.

C# bodies use the identifiers generated by the provider: the example uses `Amount` and `Balance` even if the Screenplay declarations say `amount` and `balance`. DSL names remain as authored; neither Screenplay nor the provider rewrites opaque body text. A provider must map C# compilation errors back to the authored body using the source map.

Prefer a projection where one will do. A reducer is code, and code is the part of a document a reader cannot check at a glance. The executable semantic model records each observed event and an opaque implementation requirement, not the C# transition. Inline and file bodies bind the same routing contract. The key is always the event source id; state starts at null, and returning null deletes the instance. The reference evaluator cannot run opaque transitions: specifications needing reducer-built state require a target provider, rather than silently succeeding. A target must check the `pure` capability and supply the transition implementation before it can run or render the model.

A reducer may declare one leaf `from Source` or `from Source.Stream`. It observes only facts with matching stored routing names. Unrouted facts never match, and stream ids are not filtered. This selects ESM v10. An opaque transition reached through matching given history or a matching when step remains Unsupported in reference execution. See [Observer filters](event-sources.md#observer-filters).

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
