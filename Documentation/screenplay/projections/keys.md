---
title: "Screenplay projection keys: select a read model instance"
description: "Choose which read model instance an event updates in Screenplay. Put keys on each from block, or use the event source ID when no key is specified."
---

<a id="keys"></a>

A Screenplay projection key selects the read model instance an event creates or updates. Put it on each relevant `from` block or on the individual events in a shared block. Without an explicit key, the event selects the instance identified by its event source ID.

Start by asking which view should change when the event arrives. If the answer is the order named by `orderId`, use that property as the key on every handler that updates the order. If every event should contribute to one summary, give all those handlers the same constant string key.

A `key` directly on the projection is retained in syntax but does not route events in Chronicle or the executable semantic model. It supplies no default for `from`, and the parser warns with `PLAY0381`.

A declared [read-model key](../readmodels.md#keys) describes instance identity and lookup parts. A projection still declares its own event-to-instance key on each relevant `from` block. Align that mapping with the read model's key parts. A read-model key does not supply a projection route. For several flat key properties, use an untyped key block naming every part exactly once. This is distinct from a single property whose value has a declared composite type; both shapes can coexist in one application.

Reducers have no property-key option: their instances follow each event's event-source id, not its stream id. If [`PLAY0690`](../diagnostics.md#reducer-instance-identity) reports a reducer/query identity mismatch, use a projection with a property key on every relevant `from`, or [joins](joins.md) for related instances that already exist.

## Explicit Keys

When an event's payload carries the instance identity, name that property after `key`:

```pdl
from UserRegistered key userId
  Name = name
  Email = email
```

The `userId` from the event becomes the key for the `UserReadModel` instance.

## Inline Key Syntax

Keys can be specified inline on the `from` statement:

```pdl
from OrderPlaced key orderId
  Total = total
  Status = "Pending"
```

## Multiple Events with Keys

A shared `from` can select the same kind of instance from differently named event properties. Here `EventA` uses `idA`, `EventB` uses `idB`, and `EventC` falls back to its event source ID:

```pdl
from EventA key idA, EventB key idB, EventC
```

## Block Key Syntax

The same simple key can sit on an indented line when that reads more clearly with the mappings:

```pdl
from OrderPlaced
  key orderId
  Total = total
```

## Composite Keys

When one identifier is a value made of several named parts, use a typed composite key. This form constructs an `OrderKey` value from the event:

```pdl
from OrderCreated
  key OrderKey
    CustomerId = customerId
    OrderNumber = orderNumber
  Total = total
```

### Composite Key Structure

```pdl
key {TypeName}
  {Property} = {expression}
  {Property} = {expression}
  ...
```

The `{TypeName}` must name a declared composite type in the read model schema. Its parts must match that type's properties. This is different from a read model that declares several separate scalar `key` properties; use the untyped form below for that shape.

### Composite Key with Event Context

Chronicle can use event metadata as key parts, including the recorded identity that caused the event:

```pdl
from LineItemAdded
  key LineItemKey
    OrderId = orderId
    LineNumber = lineNumber
    SequenceNumber = $eventContext.sequenceNumber
    CreatedBy = $eventContext.causedBy.subject
  Product = productName
  Quantity = quantity
```

This example selects a distinct instance for each combination of order, line, sequence number and recorded subject; it does not keep updating one instance per order line. Although the semantic model can bind these scalar metadata reads, the reference evaluator does not execute `sequenceNumber` or `causedBy.subject`. See [Event Context](event-context.md#in-the-semantic-model).

### Flat read-model key parts

For a read model with several scalar `key` properties, leave out the composite type name:

```pdl
from PaymentRecorded
  key
    invoiceId = invoiceId
    method = method
  add total by amount
```

The block applies to every event named by `from`. The instance key is the set of named part values, independent of part order. On creation, each part is written to its read-model property even with `no automap`. After mappings run, every part property must still equal its key value; a contradictory mapping fails projection.

Every root `from` and `remove with` on this shape needs a complete untyped block. Missing parts or scalar/default keys report `PLAY0661`; unknown, repeated, ill-typed or non-simple parts report `PLAY0662`. Root `from` and `remove with` are the only admitted projection blocks for flat composite identities. Joins, child/nested scopes, every/all, clear with, variants and remove via join remain refused with `PLAY0268`, as do reducers over these read models.

## Default Key Behavior

For a read model with one identifier, if no key is specified, the event source ID is used as the key:

```pdl
from UserRegistered
  Name = name
```

Equivalent to:

```pdl
from UserRegistered key $eventSourceId
  Name = name
```

## Key in Children

Children must always specify an identifier:

```pdl
children members identified by userId
  from UserAddedToGroup key userId
    parent groupId
    Role = role
```

The `identified by userId` clause names the child identifier property, while `key userId` names the event property whose value supplies it. The `parent groupId` line names the event property that points at the parent instance.

## Key Expressions

Key expressions can read a property path (`userId`, `order.id`), the event source ID (`$eventSourceId`) or an admitted scalar event-context path such as `$eventContext.correlationId`. A constant key must be text, such as `literal "global"`.

Do not use template expressions or non-text literals such as `42`, `true` or `null` as keys or typed composite key parts. The semantic binder rejects templates; Chronicle's key lowering preserves string literals as values but treats other literals as property-path text. A numeric event property is different from a numeric literal: it is a property read, not a fixed key.

Parsing a metadata path is not proof that it can be used in an executable specification. Collections, whole composite metadata values, `occurred.Week` and optional `causedBy.onBehalfOf` paths are outside the semantic model's admitted scalar reads. The reference evaluator supports fewer reads still; [Event Context](event-context.md#in-the-semantic-model) lists the boundary.

## Examples

### Simple Key

```pdl
from ProductCreated key productId
  Name = name
  Price = price
```

### Event Source as Key

```pdl
from AccountCreated key $eventSourceId
  AccountNumber = accountNumber
  Balance = 0.0
```

### Nested Property as Key

```pdl
from OrderPlaced key order.id
  Total = order.total
  CustomerId = customerId
```

### Composite Key Example

```pdl
projection OrderLine => OrderLineReadModel
  from LineItemAdded
    key OrderLineKey
      OrderId = orderId
      ProductId = productId
    Quantity = quantity
    UnitPrice = unitPrice
    LineTotal = total
```

### Composite Key with Multiple Properties

```pdl
from ReservationMade
  key ReservationKey
    HotelId = hotelId
    RoomNumber = roomNumber
    CheckInDate = checkInDate
  GuestName = guestName
  Nights = nights
```

### Children with Keys

```pdl
children orderLines identified by lineNumber
  from LineItemAdded key lineNumber
    parent orderId
    Product = productName
    Quantity = quantity
    Price = price
```

## Literal (Constant) Keys

Use `literal "value"` to fix the key to a constant string. Every event matching that handler updates the same instance regardless of event source. Repeat the constant on each handler that contributes to the same summary:

```pdl
from OrderPlaced
  key literal "global"
  count TotalOrders
```

Every `OrderPlaced` event, from every event source, increments `TotalOrders` on the single document with key `"global"`.

### Inline literal syntax

Literal keys also work inline on the `from` statement:

```pdl
from UserLoggedIn key literal "site-stats"
  count TotalLogins
```

### Combining literal keys with functions

Literal keys are especially useful with counter and arithmetic operations for global aggregates:

```pdl
projection SiteMetrics => SiteMetricsReadModel
  from UserRegistered
    key literal "metrics"
    count TotalUsers

  from UserLoggedIn
    key literal "metrics"
    increment ActiveSessions

  from UserLoggedOut
    key literal "metrics"
    decrement ActiveSessions

  from OrderPlaced
    key literal "metrics"
    count TotalOrders
    add TotalRevenue by amount
```

All events from all users accumulate into the single `SiteMetricsReadModel` document with key `"metrics"`.

## When to Use Each Approach

Use the default event source ID when the source already identifies the view you want to update. The event stream ID is a different value and is not the default instance key.

Choose a property key when the event names another instance, or a constant string for a shared summary. Inline and block syntax express the same simple key. Use a typed composite when the identifier itself is a declared composite value; use an untyped block when the read model has several flat scalar key properties.

A composite key describes identity. It does not by itself specify partitioning or sharding.

## Best Practices

Check all handlers that should update the same instance, not just the one that creates it. An order created with `key orderId` and later updated without that key can select different instances if its event source ID differs from `orderId`.

Once the routing is consistent, continue with [Property Mapping](property-mapping.md) to decide which values change. For keys built from recorded metadata, check [Event Context](event-context.md) before relying on the reference evaluator to execute the example.
