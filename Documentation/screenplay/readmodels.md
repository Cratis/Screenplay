---
title: "Read models"
description: "Declare a Screenplay read model by name and shape, with keys and reducers, even when no projection builds it."
---

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

An explicit single key binds byte-identically to the equivalent inferred identifier. Several scalar key properties form a flat composite identity: each part keeps its property name and type, and equality compares every part without regard to declaration order. No separator encoding or synthetic type is introduced. Only models with at least two key parts select the composite-key executable contract.

An optional single-instance query uses a `by` block naming every part, with caller-supplied values. A collection query uses equality filters, or one ordinary `by <part> Type` line; a collection `by` block remains refused with `PLAY0268`. Context-sourced parts and non-optional single-instance returns also remain refused. Concept rules run on each supplied query part at execution; fixture state is checked for type, shape and enum membership, not concept rules.

`given readmodel` and `then readmodel` state every key part. Missing parts produce `PLAY0351`. Single-instance query results may omit parts supplied by the query arguments; collection rows must state every part. A stated part that differs from an argument makes the expected row fail to match at execution, rather than failing binding. See [Specifications](specifications.md#read-model-state).

## The arrow always points the same way

A read model never declares what composes it. Whatever builds it names it, with the same `=>` a projection uses:

```text
projection Deposits => AccountBalance     ← a projection builds it
reducer    Balance  => AccountBalance     ← or a reducer does, never both
```

So there is one arrow and one direction: to find where a read model's state comes from, you look for the thing pointing at it. A read model that declared its own sources would be a second, opposite arrow saying the same thing, and the two would eventually disagree.

**Exactly one thing may build a read model.** Two builders is a compile error — either could have produced the value in front of a reader, and nothing in the document would say which. Note this is about the read model, not the slice: a slice may still declare [several projections](projections/index.md#several-projections-in-one-slice), each building a different read model.

Query authorization can also use verified policy bodies through a host's opt-in `ISemanticBodyExecutionProvider.AuthorizeAsync`. The policy receives semantic key-argument ids, the single key as subject when available, caller roles/claims and known tenant/time. Allow continues the query; deny rejects as unauthorized. Unsupported answers, source failures, exceptions and cancellation remain Authorization unsupported. A command's failed follow-up query does not commit tentative facts or read-model changes. Query performers are still outside this seam.

## Reducers

Some views are not expressible as a projection. "Current state plus this event gives the next state" — a running balance that depends on the previous one, a state machine, a computed view that reads what it already holds. `reducer` is for exactly those:

````screenplay
reducer Balance => AccountBalance
  on AmountDeposited
    ```csharp
      return context.State is null
          ? new(Balance: context.Event.Amount, Movements: 1)
          : context.State with { Balance = context.State.Balance + context.Event.Amount };
      ```
  on AmountWithdrawn
    file Reducers/Withdrawn.cs
````

- `reducer <Name> => <ReadModel>` — reads the same way `projection <Name> => <ReadModel>` does, on purpose.
- `on <EventType>` — one rule per event the reducer folds in. Every rule needs an inline or file body to bind to the executable semantic model. A reducer with no bodies is rejected with a projection hint; mixing body-less and bodied rules is an error (`PLAY0398`).
- The reduction lives inline in a fenced block, or in a `file` — the same choice every other construct that needs exact detail offers.

C# bodies use generated PascalCase members: the example uses `Amount` and `Balance` even if the Screenplay declarations say `amount` and `balance`. Use named constructor arguments such as `Balance:` and `Movements:`; generated constructor order need not match declaration order. DSL names remain as authored; neither Screenplay nor Stage rewrites opaque body text. A provider must map C# compilation errors back to the authored body using the source map.

The `AccountBalance` shape above is kept minimal; to render it with Stage, give it an identifier. For Stage rendering, the read model needs a required Guid/Text identifier (or an identifier concept) and a matching keyed lookup query. The event source supplies the reducer key; every non-null result must retain that identity. A mismatched identifier throws and discards the batch; returning `null` deletes the instance.

Prefer a projection where one will do. A reducer is code, and code is the part of a document a reader cannot check at a glance. The executable semantic model records each observed event and an opaque implementation requirement, not the C# transition. Inline and file bodies bind the same routing contract. The key is always the event source id; state starts at null, and returning null deletes the instance. The reference evaluator cannot run opaque transitions: specifications needing reducer-built state require a target provider, rather than silently succeeding. A target must check the `pure` capability and supply the transition implementation before it can run or render the model.

A reducer may declare one leaf `from Source` or `from Source.Stream`. It observes only facts with matching stored routing names. Unrouted facts never match, and stream ids are not filtered. This selects ESM v10. Without a body provider, an opaque transition reached through matching given history or a matching when step remains Unsupported in reference execution. See [Observer filters](event-sources.md#observer-filters).

A read-model `key`, a query's `by` or a month-valued stream id does not change reducer keying. Native compilation warns with [`PLAY0690`](diagnostics.md#reducer-instance-identity) when known producer destinations disagree with a single-instance lookup, or when a reducer consumes several nominal identity domains. This compares types, not runtime ID values, and unknown producers suppress the comparison. To aggregate by an event property instead, use [projection keys](projections/keys.md); to affect related existing instances, use [joins](projections/joins.md). Joins do not create those instances.

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

## Host execution of reducer transitions

The reference evaluator can fold exclusively reducer-built read models when the host supplies resolved bodies and an `ISemanticBodyExecutionProvider`. Its additive `ReduceAsync(SemanticImplementationBody body, SemanticReducerContext context, CancellationToken cancellationToken)` method defaults to Unsupported for existing providers. Inline and file bodies use the same verified requirement identity and content hash. Screenplay supplies no C# executor; the host enforces isolation, API admission and execution limits, as for [validation bodies](commands.md#giving-a-named-rule-a-body).

`SemanticReducerContext` carries the prior read-model composite as `State` (null when absent), the event payload composite as `Event`, and `IsFirst` as `State is null`. `Key` is catalog Text: text identities are unchanged, numbers use invariant formatting, and booleans use lowercase `true` or `false`. A null or nonscalar identity is refused, never converted to an empty key. Storage and keyed comparison retain the original typed event-source identity, not its Text representation. `Tenant` remains null when the world has no tenant metadata; `Occurred` comes from the fact and is null if unknown. Nothing is read from the clock.

`SequenceNumber` is the **zero-based history position in the reference world**, including the existing prefix when extending or committing. It is not a global log position; a body depending on an absolute log position is not portable between reference and target execution.

Return `SemanticReducerBodyResult.Next(composite)` for a complete next state or `Delete()` to remove the instance. `Unsupported(diagnostic)` refuses the run. A null/default/unknown outcome, any diagnostic even alongside a state, or an invalid state is Unsupported. The state must contain only declared properties, with required values present, nested values and collection elements matching their types, and any declared key agreeing with the raw event-source key. Output is normalized to declaration order, with absent optional properties represented as null. Deletion is explicit: `Next(SemanticValue.Null)` is not deletion. Provider exceptions, timeouts and cancellation are Unsupported, with bounded, control-character-free diagnostics identifying the requirement.

History folds in order into isolated instances keyed by read model and raw event-source identity. Given history, given read-model seeds, appended events, command productions, reaction batches, replay/extension and read-only assertions share this behavior. Every reached reducer must be executable; resolving just the first cannot admit a later unresolved reducer. Unsupported work returns the original input world, not a partial fold. Mixed projection/reducer models and event-target reducers remain refused by name.

A factory supplies one live provider per execution or selected specification run, shared by validations, policies, handlers and reducers. `Decide` disposes its decision-body scope before returning; each `Commit` needing reducer work or an opaque policy in a requested query's compiled gate requests a fresh provider with the captured cancellation token and disposes it before returning, including after failure. Factories may therefore be asked for one provider per commit of the same decision. Handler bodies run once at decision, not again at commit. A purely portable commit does not request a provider; factory failure at commit is Unsupported.

## See also

- [Projections](projections/index.md) — the declarative way to build a read model, and the first thing to reach for.
- [Queries](queries.md) — how a read model is read once it exists.
- [Commands](commands.md#what-the-command-reads) — how a command declares the state it decides against.
