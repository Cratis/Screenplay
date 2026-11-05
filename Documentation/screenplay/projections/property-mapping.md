---
title: "Screenplay projections: map event values to properties"
description: "Map event data, constants and metadata to read model properties in Screenplay projections. Use explicit assignments for values AutoMap cannot supply."
---

<a id="property-mapping"></a>

Property mapping in a Screenplay projection assigns an event value, a constant or event metadata to a read model property. Use `Property = expression` when a value needs a different name, an initial value or a source that AutoMap cannot choose.

## Basic Mapping

Map an event property to a read model property:

```pdl
from UserRegistered
  Name = name
  Email = email
```

The left side names a read model property; the right side reads from the event. With AutoMap enabled, these two case-insensitive name matches need no explicit assignments. Keep assignments where they explain a choice, or use them with `no automap` at the projection level when every mapped value should be stated. See [Auto-Map](auto-map).

## Nested Properties

Use a dotted source path to read inside an event object. These assignments flatten contact and address data into the read model:

```pdl
from UserRegistered
  Email = contactInfo.email
  Phone = contactInfo.phone
  City = address.city
```

## Literal Values

Use literals for values known at the point this event is handled. Here registration activates the user and sets an initial status and priority:

```pdl
from UserRegistered
  Name = name
  IsActive = true
  Status = "Pending"
  Priority = 5
  CreatedAt = null
```

Literals include booleans (`true`, `false`), double-quoted strings (`"text"`), numbers (`42`, `3.14`) and `null`. The target property's type must accept the value. `CreatedAt = null` clears the value rather than assigning an occurrence time.

## Clearing a Value

Assigning a value and removing one are different acts, and `=` hides the difference. Use `clear` to remove the value a property holds:

```pdl
clear {Property}
```

### Example

```pdl
from NoteCleared
  clear Note
  ClearedAt = $eventContext.occurred
```

A dotted path clears a property on a nested object:

```pdl
from OwnerNoteCleared
  clear Owner.Note
```

`Property = null` has the same meaning as `clear Property`. Prefer `clear` when writing a new mapping so the intent to remove a value is visible.

`clear <property>` is a mapping line inside a `from`, `every`, `all` or `with` block. It is not the same directive as `clear with <EventType>`, which nulls a whole child object — see [Nested Objects](nested).

Because the two read alike, a bare `clear with` — the directive without its event type — is reported as an invalid mapping rather than read as clearing a property named `with`. If a read model really does have a property called `with`, escape it: `clear @with`.

## String Templates

Create formatted strings using template literals:

```pdl
from PersonRegistered
  FullName = `${firstName} ${lastName}`
  DisplayInfo = `${name} (${email})`
```

Backticks mark the template, and each `${expression}` inserts a value. This lets `FullName` combine two event properties without requiring a separate field on the event. Templates are projection-language expressions supported by Chronicle; the Screenplay executable semantic model does not bind them.

## Event Source ID

`$eventSourceId` reads the identity of the source the event was appended to. `$eventContext.eventSourceId` reads the same value:

```pdl
from UserAssignedToGroup
  GroupId = $eventContext.eventSourceId
  UserId = $eventSourceId
```

Both assignments receive the same ID; neither reads a user ID from the payload. Mapping an ID into a property also does not change which instance the event updates. Select that instance with the `from` [key](keys.md).

## Event Context

Use `$eventContext` for metadata stored alongside the event rather than in its payload:

```pdl
from UserRegistered
  Name = name
  CreatedAt = $eventContext.occurred
  SequenceNumber = $eventContext.sequenceNumber
  CorrelationId = $eventContext.correlationId
```

Chronicle can read these metadata paths. The Screenplay reference evaluator supports `occurred` assignments in supported scopes, but not the `sequenceNumber` and `correlationId` assignments shown here. [Event Context](event-context.md) explains the available paths and how to supply occurrence times in specifications.

## Multiple Mappings

Define multiple mappings in a single `from` block:

```pdl
from OrderPlaced
  OrderNumber = orderNumber
  CustomerId = customerId
  Total = total
  Status = "New"
  PlacedAt = $eventContext.occurred
  TaxRate = 0.08
```

## Property Types

The read model property type determines what values are valid:

```pdl
from ProductCreated
  Name = name              # string
  Price = price            # decimal/number
  IsAvailable = true       # boolean
  Stock = 0                # integer
  Category = null          # nullable
```

## Examples

### User Profile

```pdl
from UserProfileUpdated
  FirstName = firstName
  LastName = lastName
  Email = email
  Bio = bio
  UpdatedAt = $eventContext.occurred
```

### Order with Calculated Fields

`Subtotal`, `Tax` and `Total` below are copied from the event, not calculated by this mapping. The template formats `Reference` for display:

```pdl
from OrderPlaced
  OrderId = orderId
  CustomerId = customerId
  Subtotal = subtotal
  Tax = tax
  Total = total
  Status = "Pending"
  Reference = `ORD-${orderNumber}`
  PlacedAt = $eventContext.occurred
```

### Nested Data

```pdl
from CompanyRegistered
  CompanyName = name
  Email = contactInfo.email
  Phone = contactInfo.phone
  Street = address.street
  City = address.city
  State = address.state
  ZipCode = address.zipCode
```

### With Literals and Context

```pdl
from AccountCreated
  AccountNumber = accountNumber
  Balance = 0.0
  IsActive = true
  AccountType = "Standard"
  OpenedAt = $eventContext.occurred
  OpenedBy = $eventContext.eventSourceId
```

In the account example, `OpenedBy` holds the event source ID, not the identity of the person who opened it. To record that person in Chronicle, use the appropriate `$eventContext.causedBy` member described in [Event Context](event-context.md#causedby).

## Best Practices

Choose the source by what the property means: event data for business values, a literal for a known initial state, and event context for metadata. For a running calculation rather than a copied value, continue with [Arithmetic](arithmetic.md).

Before adding assignments for every field, check [Auto-Map](auto-map). Explicit mappings take precedence, so you can keep direct name matches automatic while spelling out the values that need a decision.
