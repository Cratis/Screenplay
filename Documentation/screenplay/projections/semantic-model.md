# Projections in the semantic model

A projection is written once, in the Projection Declaration Language, and read by two consumers. Cratis Chronicle compiles it with the Screenplay compiler and runs it. The Screenplay semantic binder turns it into the executable semantic model (ESM) that Stage, rendered applications and the reference evaluator share. This page explains how the two stay in agreement and where the semantic model deliberately stops.

## Chronicle is the semantic reference

The grammar belongs to Screenplay, and Chronicle uses the Screenplay parser as it is. What a projection *means* - which instance an event affects, what a join may create, what a removal removes - is decided by Chronicle's lowering (`ProjectionDefinitionSyntaxVisitor`), its validation (`ProjectionValidator`) and its engine. The semantic binder mirrors those rather than defining meaning of its own. Where these pages and Chronicle disagree, Chronicle is right and the page is wrong.

In practice that means:

- **Every block is legal at every level.** Chronicle lowers the projection body, a `children` body and a `nested` body with one recursive switch and no per-level restrictions, so `join`, `every`, `all`, `remove with`, `remove via join` and `clear with` bind inside `children` and `nested` as well as at the top.
- **The default key is the event source.** A `from` block without a key, and one with `key $eventSourceId`, are the same declaration. The key is resolved from the event's inline key, then the block's key, then the event source - never from a `key` written directly on the projection.
- **`from A, B` is one transition per event**, and all of them share the block's mappings.
- **`clear x` and `x = null` are one operation**, and **`count` is `increment`.**
- **Arithmetic is typed by the read model.** `add` and `subtract` convert both operands to the target property's type before the operation - a whole-number target rounds half to even - and a target with no value yet starts at zero. There are no arithmetic operators; `add … by` and `subtract … by` are the whole arithmetic surface.
- **A source with no value sets the target to null.**
- **Nested paths create what is missing.** `quantity.amount = amount` creates `quantity` if it is absent; `basis = quantity.basis` reads through a composite event property.

## The two shapes of a projection

A projection the original vertical can express - one or more `from` blocks keyed by an event property, setting read-model properties directly - keeps its flat shape, so everything that already reads it is unaffected:

```screenplay
projection ProjectSummaryProjection => ProjectSummary
  from ProjectRegistered key projectId
    name = name
```

Anything more - children, nested objects, joins, removals, `every` and `all`, event-source, literal and composite keys, and every mapping kind - binds to a *scoped* shape. A scope holds one level's transitions, joins, child collections, nested objects, `every` mappings, removals and join removals, and a child collection or nested object carries a scope of its own:

```screenplay
projection OrderDetails => OrderView
  every
    lastSeen = $eventContext.occurred
    exclude children
  from OrderPlaced key orderId
    label = label
  join customer on customerId
    with CustomerRegistered
      customerName = name
  children lines identified by lineNumber
    from LineAdded key lineNumber
      parent orderId
      add subtotal by amount
      count quantity
    remove with LineRemoved key lineNumber
      parent orderId
  nested shipping
    from OrderShipped
      carrier = carrier
    clear with ShippingCleared
  remove with OrderCancelled key orderId
```

What a block means depends on the level it sits in. `remove with` deletes the instance at the top, removes one child inside `children`, and clears a nested object back to null inside `nested`. Keys inside `children` identify the child, and `parent` identifies the parent - the event source when it is left out. Inside `nested`, keys keep addressing the enclosing instance, because a nested object lives in the same document.

## Joins, `every` and `all`

A join never creates an instance. When a joined event arrives, every existing instance whose `on` property equals the joined event's source is updated. When a `from` event sets that property, the latest joined event is read again, so a customer registered before the order still names it. The identifier written directly after `join` is not part of the definition - Chronicle's lowering discards it.

`every` mappings run with each `from` and `join` event of their level. `all` does the same and additionally reaches event types no block names, keyed by the event source. Only a projection's own level can subscribe to every event type: Chronicle drops that flag below it, so `all` inside `children` or `nested` binds as `every` and reports warning `PLAY0380`. The same warning reports an `automap` or `no automap` written on a joined event, which Chronicle replaces with the auto-map of the level the join sits in.

## What stays out of the semantic model

Each of these is reported with a precise message rather than bound and ignored:

| Construct | Why |
| --- | --- |
| `$causedBy` | Chronicle compiles it but cannot run it. Write `$eventContext.causedBy.subject`, `.name` or `.userName` instead. |
| `$eventContext.<path>` for a path Chronicle's event context does not have | Chronicle would throw when it builds the projection. The binder admits a small set of scalar paths until a shared event-context catalog replaces it. |
| String templates and raw expressions in mappings and keys | The semantic model has no portable string-template expression. |
| A number or Boolean literal key | Chronicle reads any key literal other than text as a property path. Write `key literal "…"`. |
| `parent` outside `children` | Chronicle never reads it there. |
| A second `every` or `all` on one level | Chronicle keeps only the last at the top and merges them below; the semantic model admits one. |
| `sequence` | Which event sequence a projection observes is a realization concern, not portable behavior. |
| `variant` | Chronicle's lowering has no variants yet. |
| Dynamic dictionary keys such as `counts.$eventContext.eventType.id` | The semantic model has no dictionary-keyed target. |

Everything that binds is either executed by the reference evaluator or blocks its execution plan - it is never bound and then skipped. The evaluator does not execute a `remove via join` on a projection's own level (Chronicle's engine wires it as a child removal although these pages promise a delete), a join, child collection or `remove via join` inside a nested object (Chronicle's engine does not wire them), `all` beside removals or child levels, or event-context values other than the event source, which ESM v1 facts carry no occurrence context for. Specification events carry no event source in ESM v1 either, so a specification that builds an event-source-keyed projection from `given` events fails instead of guessing an identity.
