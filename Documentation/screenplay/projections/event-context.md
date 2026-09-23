# Event Context

Event context provides access to metadata about the event itself, such as when it occurred, its sequence number, who caused it and how it was correlated.

## Syntax

Access event context members with the `$eventContext.` prefix followed by a dotted path:

```pdl
$eventContext.{path}
```

A path is written camelCase, as in the table below. Chronicle, which runs the projection, also resolves a segment whose first letter is uppercased (`$eventContext.EventType.Id`); no other spelling resolves.

Every path is checked against the event context catalog when the projection compiles. An unlisted member or sub-path is a warning (`PLAY0295`, `PLAY0296`); a runtime reflects over the event context and may resolve more than the catalog lists, but such a path is not portable. A path below a collection or an empty path is an error (`PLAY0297`, `PLAY0298`) because it can never resolve. See [Diagnostics](../diagnostics.md#event-context-paths).

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

The ISO 8601 week the event occurred in:

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

The correlation ID associated with the event:

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

The short form `$causedBy.subject` also parses, and Chronicle compiles it, but Chronicle cannot yet evaluate it when the projection runs ([Cratis/Chronicle#4119](https://github.com/Cratis/Chronicle/issues/4119)). Write `$eventContext.causedBy.<property>` until that is fixed.

### eventSourceId

The event source ID:

```pdl
from UserAssignedToGroup
  GroupId = $eventContext.eventSourceId
```

## `$eventSourceId` and `$eventContext.eventSourceId`

`$eventSourceId` is not shorthand for `$eventContext.eventSourceId`. They are two different expressions - each its own node in the syntax tree, and each its own expression in Chronicle - that resolve to the same value: the identifier of the event source the event was appended to. Prefer `$eventSourceId`, and use it in keys:

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

## Common Patterns

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

Track event order:

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
  key ProcessingKey {
    EventId = eventId
    SequenceNumber = $eventContext.sequenceNumber
    CorrelationId = $eventContext.correlationId
  }
  ProcessedAt = $eventContext.occurred
```

## In Templates

Event context works in string templates:

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

1. **Audit Timestamps**: Use `occurred` for created/updated timestamps
2. **Correlation**: Use `correlationId` to link related operations
3. **Versioning**: Use `sequenceNumber` for version tracking
4. **Relationships**: Use `$eventSourceId` for parent-child relationships
5. **Debugging**: Include correlation IDs and timestamps for troubleshooting
6. **Stability**: Event context values are fixed when the event is appended - except `observationState`, which depends on whether the event is being replayed
