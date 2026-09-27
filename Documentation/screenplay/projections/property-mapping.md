# Property Mapping

Property mapping assigns values from events to read model properties. Mappings use the simple assignment syntax: `Property = expression`.

## Basic Mapping

Map an event property to a read model property:

```pdl
from UserRegistered
  Name = name
  Email = email
```

This maps the `name` and `email` properties from the `UserRegistered` event to the corresponding properties on the read model.

## Nested Properties

Access nested properties using dot notation:

```pdl
from UserRegistered
  Email = contactInfo.email
  Phone = contactInfo.phone
  City = address.city
```

## Literal Values

Assign literal values:

```pdl
from UserRegistered
  Name = name
  IsActive = true
  Status = "Pending"
  Priority = 5
  CreatedAt = null
```

Supported literal types:

- **Boolean**: `true`, `false`
- **String**: `"text"` (double quotes)
- **Number**: `42`, `3.14`, `1e-3`, `2.5E+4`
- **Null**: `null`

A number stays a Double when its exact decimal value equals the nearest Double's exact binary value: `2.50`, `1e17` and `144115188075855872` keep their previous type, printed form and Chronicle storage text. Typed JSON writes finite Doubles as plain JSON numbers, using the normal JSON serializer spelling, and reads all plain JSON numbers as Doubles. An explicit `Double` envelope remains accepted on input. Int64 and Decimal values use typed envelopes to preserve their exact values. When Double would change the value, an integer within Int64 range becomes Int64 (for example `9007199254740993`); otherwise an exactly representable number becomes Decimal (for example `0.1` and `1e-5`). Values neither type can represent exactly retain the Double fallback, including overflow for previously accepted fixed-point numbers. Printing a Double uses exact binary digits when its short spelling would reparse as another type. Specification consistency now distinguishes spellings previously rounded to the same Double, such as `100000000000000020` and `100000000000000016`: those are different numbers, so conflicting outcomes are reported rather than silently matched.

The exact-value rule applies to `.play` source, including numbers nested inside objects and lists. Ordinary typed-workspace JSON numbers instead decode as Doubles; use explicit Int64 and Decimal envelopes when exact CLR values must survive JSON.

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

`Property = null` means the same thing and keeps working, so nothing written against the older spelling breaks. Prefer `clear` in new projections — it names the act instead of assigning nothing.

`clear <property>` is a mapping line inside a `from`, `every`, `all` or `with` block. It is not the same directive as `clear with <EventType>`, which nulls a whole child object — see [Nested Objects](nested).

Because the two read alike, a bare `clear with` — the directive without its event type — is reported as an invalid mapping rather than read as clearing a property named `with`. If a read model really does have a property called `with`, escape it: `clear @with`.

## String Templates

Create formatted strings using template literals:

```pdl
from PersonRegistered
  FullName = `${firstName} ${lastName}`
  DisplayInfo = `${name} (${email})`
```

Template syntax:

- Wrap in backticks: `` `template` ``
- Use `${expression}` for substitutions
- Can combine multiple expressions

## Event Source ID

The special identifier `$eventSourceId` provides the event source ID:

```pdl
from UserAssignedToGroup
  GroupId = $eventContext.eventSourceId
  UserId = $eventSourceId
```

## Event Context

Access event metadata:

```pdl
from UserRegistered
  Name = name
  CreatedAt = $eventContext.occurred
  SequenceNumber = $eventContext.sequenceNumber
  CorrelationId = $eventContext.correlationId
```

See [Event Context](event-context.md) for all available properties.

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

## Best Practices

1. **Be Explicit**: Name properties clearly to show intent
2. **Use Defaults**: Set initial values with literals when appropriate
3. **Leverage Context**: Use event context for audit fields
4. **Templates for Display**: Use templates to create formatted display values
5. **Nested Access**: Directly access nested properties rather than mapping intermediate objects
