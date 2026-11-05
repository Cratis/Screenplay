---
title: "Screenplay event context: map recorded event metadata"
description: "Use $eventContext in Screenplay projections to read recorded event metadata. Check which paths Chronicle resolves and which specifications can evaluate."
---

<a id="event-context"></a>

Event context is the metadata recorded alongside an event, separate from its business payload. In a Screenplay projection, use `$eventContext` to carry values such as occurrence time or correlation ID into a read model so a reader can place a change in context.

Choose a path by the question the view needs to answer: when the event happened, where it sits in its sequence or which identity caused it. Chronicle resolves the paths below, but Screenplay's reference evaluator supports only event source identity and certain occurrence-time assignments. If you are writing an executable specification, read [In the Semantic Model](#in-the-semantic-model) before selecting a path.

## Syntax

Access event context members with the `$eventContext.` prefix followed by a dotted path:

```pdl
$eventContext.{path}
```

A path is written camelCase, as in the table below. Chronicle, which runs the projection, also resolves a segment whose first letter is uppercased (`$eventContext.EventType.Id`); no other spelling resolves.

Compilation checks each path against an event-context catalog shared with the editor. An unlisted member or sub-path produces a warning (`PLAY0295`, `PLAY0296`). Chronicle may resolve additional paths through reflection, but those paths are not portable. Reading below a collection or using an empty path is an error (`PLAY0297`, `PLAY0298`). [Diagnostics](../diagnostics.md#event-context-paths) explains these checks; they are separate from binding and executing a specification.

## Available Properties

The table is generated from the catalog the compiler and the editor use, which mirrors Chronicle's `EventContext` record.

<!-- event-context-catalog:start - generated from EventContextCatalog, do not edit by hand -->
| Path | Type | Description |
|---|---|---|
| `eventType` | `EventType` | The type of the event - its identifier, generation and whether it is a tombstone. |
| `eventType.id` | `EventTypeId` | The identifier of the event type. |
| `eventType.generation` | `EventTypeGeneration` | The generation of the event type. |
| `eventType.tombstone` | `bool` | Whether the event is a tombstone event. |
| `eventSourceType` | `EventSourceType` | The type of the event source the event was appended to. |
| `eventSourceId` | `EventSourceId` | The identifier of the event source the event was appended to - the value `$eventSourceId` resolves. |
| `eventStreamType` | `EventStreamType` | The type of the event stream the event belongs to. |
| `eventStreamId` | `EventStreamId` | The identifier of the event stream the event belongs to. |
| `sequenceNumber` | `EventSequenceNumber` | The position of the event in its event sequence. |
| `occurred` | `DateTimeOffset` | When the event occurred - the time it was appended unless one was given. |
| `occurred.Week` | `int` | The ISO 8601 week of the year the value falls in. |
| `eventStore` | `EventStoreName` | The name of the event store the event belongs to. |
| `namespace` | `EventStoreNamespaceName` | The namespace within the event store the event belongs to. |
| `correlationId` | `CorrelationId` | The correlation identifier the event was appended with. |
| `causation` | `IEnumerable<Causation>` | The chain of causes that led to the event - a collection, not addressable below. |
| `causedBy` | `Identity` | The identity that caused the event. |
| `causedBy.subject` | `string` | The subject identifier of the identity. |
| `causedBy.name` | `string` | The display name of the identity. |
| `causedBy.userName` | `string` | The user name of the identity. |
| `causedBy.onBehalfOf` | `Identity` | The identity this identity acted on behalf of, if any - an identity with the same members. |
| `tags` | `IEnumerable<Tag>` | The tags the event was appended with - a collection, not addressable below. |
| `hash` | `EventHash` | The hash of the event content. |
| `observationState` | `EventObservationState` | Whether the observer sees the event for the first time or again on replay - differs between live processing and replay. |
| `subject` | `Subject` | The compliance subject the event is about - the event source identifier unless one was given. |
| `subjectIsEventSourceId` | `bool` | Whether the subject is the event source identifier. |
<!-- event-context-catalog:end -->

- `causation` and `tags` are collections. There is no syntax to address an element, so `$eventContext.causation.occurred` is an error.
- A member typed by a concept - such as `eventSourceId`, `correlationId`, `sequenceNumber` or `eventType.id` - also exposes its underlying `.value`, such as `$eventContext.correlationId.value`. The table leaves those rows out.
- `Week` is a derived function, not a member of the value. It is case-sensitive: `$eventContext.occurred.Week` (or `Week()`) resolves, `$eventContext.occurred.week` does not.
- `observationState` differs between live processing and a replay of the same event, so it is unstable as read model data.
- `causedBy.onBehalfOf` is itself an identity with the same members, to any depth.

### occurred

The timestamp when the event occurred:

```pdl
from UserRegistered
  Name = name
  CreatedAt = $eventContext.occurred
```

Chronicle can also derive the ISO 8601 week from that timestamp. This function is not admitted by Screenplay's executable semantic model:

```pdl
from TimeRecorded
  Week = $eventContext.occurred.Week
```

### sequenceNumber

The position of the event in its event sequence:

```pdl
from EventProcessed
  LastSequenceNumber = $eventContext.sequenceNumber
```

### correlationId

The correlation ID recorded with the event can connect this view to other work in the same operation:

```pdl
from OrderPlaced
  OrderId = orderId
  CorrelationId = $eventContext.correlationId
```

### eventType

The type of the event, with its `id`, `generation` and `tombstone`:

```pdl
from OrderPlaced
  LastEventType = $eventContext.eventType.id
```

### causedBy

The identity that caused the event, with its `subject`, `name`, `userName` and `onBehalfOf`:

```pdl
from OrderPlaced
  PlacedBy = $eventContext.causedBy.subject
  PlacedByName = $eventContext.causedBy.name
```

The short form `$causedBy.subject` also parses and Chronicle has a runtime resolver for it. Screenplay's executable semantic model still rejects that short form, so use `$eventContext.causedBy.<property>` when binding a projection. The earlier runtime limitation is recorded in [Cratis/Chronicle#4119](https://github.com/Cratis/Chronicle/issues/4119).

### eventSourceId

The event source ID:

```pdl
from UserAssignedToGroup
  GroupId = $eventContext.eventSourceId
```

## `$eventSourceId` and `$eventContext.eventSourceId`

`$eventSourceId` and `$eventContext.eventSourceId` resolve to the same value: the identity of the source the event was appended to. They are distinct expressions in the syntax tree and in Chronicle, but you do not need that distinction to map the value. Prefer the shorter `$eventSourceId` in keys:

```pdl
from UserCreated key $eventSourceId
  Name = name
```

## In Dynamic Dictionary Keys

A mapping target can end in a dynamic key - a static property path followed by `.$eventContext.<path>`. The value at that path becomes the dictionary key the mapping writes under, so one property holds one entry per distinct value:

```pdl
projection EventStatistics => EventStatisticsReadModel
  all
    count eventCountByType.$eventContext.eventType.id
    increment eventsPerSource.$eventContext.eventSourceId
```

The path is checked against the same catalog. Only `$eventContext` is resolved in a key; any other `$` source, such as `countByUser.$causedBy.subject`, is kept as the literal key text, which is a warning (`PLAY0299`). Write `countByUser.$eventContext.causedBy.subject` instead.

## Not the Same as `$context`

`$eventContext` and `$context` are separate namespaces. `$eventContext` reads the event being projected and is available in projections. [`$context`](../context.md) reads the command or query being handled and is available in `produces` and `capture` mappings. Neither falls back to the other, and they are checked against different catalogs.

## In the Semantic Model

Binding and execution are separate checks. The executable semantic model can bind metadata that represents one scalar value with a portable type, such as `occurred`, `sequenceNumber` or `causedBy.subject`. It normalizes supported paths to camelCase: a concept's trailing `.value` and an uppercased first letter bind as the plain member. `eventSourceId` binds as event source identity.

It does not admit:

- the collections `causation` and `tags`,
- `eventType` and `causedBy` without a member below them,
- `occurred.Week`, which is a function Chronicle computes rather than a value the event carries,
- paths through `causedBy.onBehalfOf`, which is optional,
- `observationState`, which has no portable type and differs between live processing and replay.

The reference evaluator executes fewer paths than the binder accepts. It supports event source identity and `$eventContext.occurred` property assignments in `from`, `every` (including `all`) and `children` scopes. Other event-context reads, including `sequenceNumber` and `correlationId`, are unsupported by the evaluator even when binding succeeds.

For an occurrence-time assignment, supply an explicit timestamp on the fact. Use `given clock` for command-produced or `when append` facts; [Specifications](../specifications.md) explains the clock syntax. The evaluator represents the timestamp as invariant UTC round-trip text, for example `2026-10-02T09:00:00.1234567Z`. It never substitutes the wall clock, a sequence number or a default timestamp. A missing occurrence fails projection even for an optional target, without publishing partial read-model or pending-child state.

A deferred child keeps its own occurrence, not its parent's. An `every include children` assignment also uses the triggering child's occurrence.

The evaluator does not support `occurred` in keys, including parent, removal, composite, flat-part and join keys. It is also unsupported in arithmetic, join property mappings, `every` mappings on a scope with joins and nested-object scopes.

## Common Patterns

These patterns show Chronicle metadata mappings. For executable Screenplay specifications, apply the narrower path and scope limits above.

### Audit Fields

Track when things happen:

```pdl
from RecordCreated
  Name = name
  CreatedAt = $eventContext.occurred

from RecordUpdated
  Name = name
  UpdatedAt = $eventContext.occurred
```

### Correlation Tracking

Link related operations:

```pdl
from OrderPlaced
  OrderNumber = orderNumber
  CorrelationId = $eventContext.correlationId
  PlacedAt = $eventContext.occurred
```

### Sequence Tracking

Record an event's position within its event sequence. This is not a per-document revision counter:

```pdl
from StateChanged
  CurrentState = state
  SequenceNumber = $eventContext.sequenceNumber
  ChangedAt = $eventContext.occurred
```

### Parent-Child Relationships

Use the event source ID for relationships:

```pdl
children members identified by userId
  from UserAddedToGroup key userId
    parent $eventContext.eventSourceId
    Role = role
    AddedAt = $eventContext.occurred
```

## In Composite Keys

Event context can be used in composite keys:

```pdl
from EventProcessed
  key ProcessingKey
    EventId = eventId
    SequenceNumber = $eventContext.sequenceNumber
    CorrelationId = $eventContext.correlationId
  ProcessedAt = $eventContext.occurred
```

## In Templates

Chronicle can interpolate event context into display strings. Templates do not bind to Screenplay's executable semantic model:

```pdl
from OrderShipped
  TrackingInfo = `Shipped at ${$eventContext.occurred}`
  Reference = `${orderNumber}-${$eventContext.sequenceNumber}`
```

## Examples

### User Activity Log

```pdl
projection UserActivity => UserActivityReadModel
  from UserLoggedIn
    LastLogin = $eventContext.occurred
    LastLoginSequence = $eventContext.sequenceNumber
    count LoginCount

  from UserAction
    LastAction = actionType
    LastActionTime = $eventContext.occurred
    LastCorrelation = $eventContext.correlationId
```

### Versioned Document

Here `Version` stores an event-sequence position. Numbers can skip for an individual document because the sequence contains other events; the mapping does not calculate a document-specific revision.

```pdl
projection Document => DocumentReadModel
  from DocumentCreated
    Title = title
    Content = content
    Version = $eventContext.sequenceNumber
    CreatedAt = $eventContext.occurred

  from DocumentUpdated
    Content = content
    Version = $eventContext.sequenceNumber
    UpdatedAt = $eventContext.occurred
```

### Group Membership with Audit

```pdl
projection Group => GroupReadModel
  from GroupCreated
    Name = name
    CreatedAt = $eventContext.occurred
    CreatedBy = $eventContext.eventSourceId

  children members identified by userId
    from UserAddedToGroup key userId
      parent groupId
      Name = userName
      Role = role
      AddedAt = $eventContext.occurred
      AddedSequence = $eventContext.sequenceNumber
```

In this group example, `CreatedBy` receives the event source ID, not the identity of the person who created the group. Use a `causedBy` member when the property is meant to record that person.

### Order Processing

```pdl
projection Order => OrderReadModel
  from OrderPlaced
    OrderNumber = orderNumber
    CustomerId = customerId
    Total = total
    PlacedAt = $eventContext.occurred
    TrackingId = $eventContext.correlationId

  from OrderShipped
    ShippedAt = $eventContext.occurred
    ShipmentSequence = $eventContext.sequenceNumber
    Status = "Shipped"
```

## Best Practices

Choose metadata by its recorded meaning. `occurred` is the event's timestamp, `sequenceNumber` is its position in an event sequence, and `correlationId` connects related work. None substitutes for the others or for an identity in the event payload.

Most context describes the recorded event. `observationState` instead describes how an observer sees it, so it can differ on replay and should not be treated as stable read model data.

Continue with [Property Mapping](property-mapping.md) to assign the values to your view, or [Keys](keys.md) if metadata is intended to select an instance rather than fill a property.
