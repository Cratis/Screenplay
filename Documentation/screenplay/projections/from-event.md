---
title: "Screenplay projections: handle events with from"
description: "Use a from block in a Screenplay projection to select the read model instance an event updates, assign its values, and maintain counters."
---

<a id="from-event"></a>

A `from` block in a Screenplay projection describes how one event creates or updates a read model instance. Use it to connect a recorded fact, such as a user registering, to the values your query should return.

## Basic Syntax

```pdl
from {EventType}
  {mappings and operations}
```

## Simple Example

```pdl
projection User => UserReadModel
  from UserRegistered
    Name = name
    Email = email
    IsActive = true
```

Each `UserRegistered` event selects a `UserReadModel` instance by its event source ID because this block has no explicit key. The assignments copy the user's name and email and mark the user active. Other compatible, same-named properties can map automatically.

## Multiple Events (Compact Syntax)

When several event types make the same change, list them on one `from` line. They share the mappings below it:

```pdl
projection TransportRoute => TransportRouteReadModel
  automap
  from HubRouteAdded key id, WarehouseRouteAdded key id
```

This is equivalent to:

```pdl
projection TransportRoute => TransportRouteReadModel
  automap
  from HubRouteAdded key id
  from WarehouseRouteAdded key id
```

Each event can select its instance differently. Here `EventA` uses `idA`, `EventB` uses `idB`, and `EventC` uses its event source ID. Any shared mapping, such as `Property = value`, must be valid for every event listed:

```pdl
from EventA key idA, EventB key idB, EventC
  Property = value
```

## With Keys

Use an explicit key when the event carries the identity of the instance it should update. This event belongs to a group but updates the user identified by its `userId`:

```pdl
from UserAssignedToGroup key userId
  GroupId = $eventContext.eventSourceId
  AssignedAt = $eventContext.occurred
```

`GroupId` records the event's source ID; it does not select the user instance. The `key userId` on `from` does that. [Keys](keys.md) covers default, constant and composite identities.

## With AutoMap

AutoMap is on by default, so matching properties are copied without asking for it:

```pdl
from UserRegistered
  IsActive = true
```

AutoMap matches names case-insensitively and leaves explicitly mapped targets alone, so `IsActive = true` wins even if the event has an `isActive` property. A block containing only counter or arithmetic operations does not auto-map other properties.

A `from` block cannot contain `automap` or `no automap` (`PLAY0075`). Set the policy at the projection or enclosing scope instead. [Auto-Map](auto-map) explains compatible types, scope settings and join behavior.

## Multiple From Blocks

Give each event its own block when it changes different values. These handlers all use the default event-source key, so events for the same source update the same user:

```pdl
projection User => UserReadModel
  from UserRegistered
    Name = name
    Email = email
    CreatedAt = $eventContext.occurred

  from UserEmailChanged
    Email = email
    UpdatedAt = $eventContext.occurred

  from UserDeactivated
    IsActive = false
    DeactivatedAt = $eventContext.occurred
```

## One use of an event per projection level

Chronicle allows one use of an event type per projection level. Ordinary compilation reports
`PLAY0801` at the second and each later reference, even if the keys are different or identical.
The check compares authored event names ordinally; it does not resolve qualifications or generations.
One set covers `from` (including comma-separated events), `remove with`, `remove via join` and
`clear with`. Joins have a separate set: an event can occur once in `from` and once in a join.
Each `children`, `nested` and variant block starts fresh sets; different projections are independent.
Variants still do not bind to the executable model.

One event cannot update two rows of the same level by appearing in two `from` blocks. Combine
mappings that update the same row into one block. Model a genuinely second domain fact as its own
event, produced by the command or a reaction when that fact actually occurs; do not invent events
solely for a read-side update. Two keys do not turn one fact into two facts.

## With Parent Key

When used within [Children](children) blocks, you can specify the parent relationship:

```pdl
children members identified by userId
  from UserAddedToGroup key userId
    parent groupId
    Role = role
```

## Operations

After selecting an instance, assign values with `Name = value`, change counters with `increment`, `decrement` or `count`, or accumulate amounts with `add` and `subtract`. [Property Mapping](property-mapping.md) explains assignments; [Arithmetic](arithmetic.md) covers running totals. AutoMap supplies matching properties where enabled, without a directive inside `from`.

## Event Context

Use `$eventContext` for values recorded alongside the event payload, such as when a login occurred and its position in the event sequence:

```pdl
from UserLoggedIn
  LastLogin = $eventContext.occurred
  LastSequenceNumber = $eventContext.sequenceNumber
  count LoginCount
```

This example uses Chronicle's metadata mappings. The reference evaluator supports the occurrence-time assignment but not `sequenceNumber`; a fact read through `occurred` must carry an explicit timestamp. [Event Context](event-context.md) separates the available paths from what executable specifications can evaluate.

## Nested Indentation

All mappings and operations within a `from` block must be indented:

```pdl
from UserCreated
  Name = name          # Indented
  Email = email        # Indented
  IsActive = true      # Indented
```

## Examples

### Simple Property Mapping

```pdl
from OrderPlaced
  CustomerId = customerId
  Total = total
  Status = "Pending"
```

### Using Templates

```pdl
from PersonRegistered
  FullName = `${firstName} ${lastName}`
  Email = email
```

### Counter Operations

```pdl
from PageViewed
  count ViewCount
  LastViewedAt = $eventContext.occurred
```

### Arithmetic Operations

```pdl
from PaymentReceived
  add Balance by amount
  LastPaymentDate = $eventContext.occurred
```

### Complex Example

The key selects an order, the assignments fill its view, and the counter belongs to that order instance, not to a global order total:

```pdl
from OrderPlaced key orderId
  CustomerId = customerId
  OrderNumber = orderNumber
  Total = total
  Status = "Pending"
  PlacedAt = $eventContext.occurred
  increment TotalOrders
```
