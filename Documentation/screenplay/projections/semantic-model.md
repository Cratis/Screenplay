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

Anything more - children, nested objects, joins, removals, `every` and `all`, variants, event-source, literal and composite keys, and every mapping kind - binds to a *scoped* shape. A scope holds one level's transitions, joins, child collections, nested objects, `every` mappings, removals and join removals, and a child collection or nested object carries a scope of its own:

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

## Which instances an event affects

`SemanticAffectedProjectionInstances.GetAffectedInstances(projection)` (or its application-aware overload for unset child identities) derives read-only `SemanticAffectedProjectionInstance` records for Studio. Each record identifies the event contract (or `null` for the open `all` subscription), block kind, path from the read-model root, and matching shape. Flat and scoped `from` transitions and ordinary removals affect one by key, potentially deferred until a parent exists. Root `all` addresses one by event source. A root join updates *existing* instances whose `Property` equals the event source id (never creates); a child join and child `remove via join` match across parents on the child's identity (not the join's `on` property). `Path` identifies child collections and nested objects; `Key`, `FlatKey`, `Property` and `ParentKey` retain the operands needed to draw the relationship. A root `remove via join` is reported as `Unverified` rather than as a root deletion: Chronicle wires it as a child pull with no child collection. Joins, children and remove-via-join inside `nested` are omitted because Chronicle does not subscribe to them. The projection-only overload reports an unset child identity as `Unverified` rather than guessing; the application-aware overload resolves the element property named like the ESM read-model identifier. Chronicle instead falls back to the root schema's `id`/`Id` key property. The Screenplay rule can differ only for hand-built or deserialized ESM, since the binder always supplies `identified by`. The view is derived, not authored, and never enters canonical ESM bytes. Reducers are keyed by event source only (decision 0002), not by a per-event list or an authored reducer key.

`ZeroOrOne` and `Many` on legacy *flat projection transitions* have no Chronicle routing meaning. A constructed or deserialized ESM exposes warning `PLAY0445` through `ExecutableSemanticModel.DeprecationDiagnostics`; the binder only creates `One`. These cardinalities remain in canonical ESM until the next ESM version. `SemanticQueryCardinality` is unchanged. A variant root join currently carries a key that the reference evaluator honours but Chronicle ignores in favour of the event source id; see Cratis/Chronicle#4165.

## Variants

A [variant group](variants.md) binds as one independent scoped projection per variant read model. Only the variant's `enters on` events can create an instance. Shared projection-level handlers merge before reclassification: all other `from` handlers become update-only joins correlated on the variant's identifier using the handler's event key (or event source identity). Sibling entering events remove the variant's instance by event source identity. The projection's ESM name/address is length-prefixed (`8:WorkItem:BacklogItem`) to keep each variant's semantic identity distinct; the read model is simply `BacklogItem`. This mirrors Chronicle's client SDK reclassifier. Chronicle's textual declaration-language visitor does not yet lower variants ([Cratis/Chronicle#4109](https://github.com/Cratis/Chronicle/issues/4109)).

## Joins, `every` and `all`

A join never creates an instance. At the root, when a joined event arrives, every existing instance whose `on` property equals the joined event's source is updated. Inside `children`, the child's identity is compared to the join key instead of `on`. At the root, when a `from` event sets the `on` property (or the root key), the latest joined event is read again, so a customer registered before the order still names it. Inside `children`, that backfill happens only when `on` equals the child's identity. The identifier written directly after `join` is not part of the definition - Chronicle's lowering discards it.

`every` mappings run with each `from` and `join` event of their level. `all` does the same and additionally reaches event types no block names, keyed by the event source. Only a projection's own level can subscribe to every event type: Chronicle drops that flag below it, so `all` inside `children` or `nested` binds as `every` and reports warning `PLAY0380`. The same warning reports an `automap` or `no automap` written on a joined event, which Chronicle replaces with the auto-map of the level the join sits in.

## Establishing a reference world from facts

`new SemanticEvaluator().EstablishWorld(plan, facts)` takes a capability-admitted `SemanticExecutionPlan` and an ordered `ImmutableArray<SemanticFact>`. It validates the entire fact history against the plan's event contracts before projecting it, then returns a `SemanticAccepted` whose `World.Facts` preserves occurrence order and whose `World.ReadModels` contains derived keyed instances in deterministic semantic-identity/key order. The accepted result carries the supplied facts and no query results. An empty array establishes an empty world. A default array, unknown event, malformed occurrence or payload returns `SemanticRejected` with category `Contract` and an unchanged empty world. A projection failure or an event observed by a reducer returns `SemanticUnsupported` with capability `Projection`; reducer transitions are opaque and cannot be replayed by the reference evaluator. Authored `given` events in specification runs use the same establishment operation before any explicit given read-model overrides are applied.

This is **reference semantics**, not Chronicle replay. Chronicle's lowering, validation and engine decide runtime meaning (decision 0001); use Chronicle when rebuilding runtime state. This operation does not append facts to an existing world or execute reactions.

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
| Dynamic dictionary keys such as `counts.$eventContext.eventType.id` | The semantic model has no dictionary-keyed target. |

Everything that binds is either executed by the reference evaluator or blocks its execution plan - it is never bound and then skipped. The evaluator does not execute a `remove via join` on a projection's own level (Chronicle's engine wires it as a child removal although these pages promise a delete), a join, child collection or `remove via join` inside a nested object (Chronicle's engine does not wire them), `all` beside removals or child levels, or event-context values other than the event source, which ESM v1 facts carry no occurrence context for. Specification events carry no event source in ESM v1 either, so a specification that builds an event-source-keyed projection from `given` events fails instead of guessing an identity.
