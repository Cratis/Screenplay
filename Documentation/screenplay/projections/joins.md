---
title: "Screenplay projection joins: enrich related data"
description: "Use Screenplay projection joins to keep related data current on existing read models. Learn key matching, backfill, and child and nested join limits."
---

# Joins

A Screenplay projection join keeps related data current on an existing read model. For an order that displays a customer's name, the order event supplies the customer ID; customer events supply the name. A root join updates orders with a matching reference without creating orders from customer events.

Use `from` for events that create or update the order itself, and `join` for events about a related entity. If an event must create the order under a key other than its event source identity, use [property keys](keys.md) on `from`. The [`PLAY0690`](../diagnostics.md#reducer-instance-identity) diagnostic can warn when an event's source identity belongs to a different kind of entity than the instance being projected.

## Basic Syntax

```pdl
join {Name} on {Property}
  with {EventType}
    {mappings}
  with {EventType}
    {mappings}
```

In `join <Name> on <Property>`, `<Property>` is the read-model property used to match the related event's source identity. `<Name>` is a label for readers only; Chronicle discards it when translating the declaration into a projection definition.

## Simple Example

`OrderPlaced` records which customer the order belongs to. The join uses that stored `CustomerId` to route customer changes to the order:

```pdl
projection Order => OrderReadModel
  from OrderPlaced
    OrderNumber = orderNumber
    CustomerId = customerId

  join Customer on CustomerId
    with CustomerCreated
      CustomerName = name
      CustomerEmail = email
    with CustomerUpdated
      CustomerName = name
      CustomerEmail = email
```

When `CustomerCreated` or `CustomerUpdated` arrives from the customer stream identified by `CustomerId`, the join updates every matching existing order. The customer event's source identity supplies the match; a payload field named `customerId` alone does not establish it. When `OrderPlaced` sets `CustomerId`, the engine also reads the latest matching event of each joined type to fill in customer data already recorded. This history lookup is called backfill.

## With AutoMap

Use `automap` when the joined event's property names match the read-model fields you want to fill. This group example uses the same matching rule as the order's customer join:

```pdl
join Group on GroupId
  with GroupCreated
    automap
  with GroupRenamed
    automap
```

## Multiple Mappings

Explicit mappings make the relationship clear when names differ. Here product fields fill the product-specific fields on the read model. Chronicle also maps the event occurrence time; Screenplay's reference evaluator does not support `$eventContext.occurred` in join mappings, so it cannot execute this timestamp example:

```pdl
join Product on ProductId
  with ProductCreated
    ProductName = name
    ProductDescription = description
    ProductPrice = price
    LastProductUpdate = $eventContext.occurred
  with ProductUpdated
    ProductName = name
    ProductDescription = description
    ProductPrice = price
    LastProductUpdate = $eventContext.occurred
```

## Multiple Events

List each event type that can change the related data. A customer verification event only changes `IsVerified`; it does not need to repeat the name mapping:

```pdl
join Customer on CustomerId
  with CustomerRegistered
    CustomerName = name
    IsVerified = verified
  with CustomerUpdated
    CustomerName = name
  with CustomerVerified
    IsVerified = verified
```

## Join with From Events

Keep the order's own lifecycle in `from` blocks. Shipping changes the order's status, while customer events continue to update its displayed customer name:

```pdl
projection Order => OrderReadModel
  from OrderPlaced
    OrderNumber = orderNumber
    CustomerId = customerId
    Total = total

  from OrderShipped
    ShippedAt = $eventContext.occurred
    Status = "Shipped"

  join Customer on CustomerId
    with CustomerCreated
      CustomerName = name
    with CustomerUpdated
      CustomerName = name
```

## Multiple Joins

An order can carry independent references to a customer and a product. Give each reference its own join so changes are matched against the appropriate property:

```pdl
projection Order => OrderReadModel
  from OrderPlaced
    OrderNumber = orderNumber
    CustomerId = customerId
    ProductId = productId

  join Customer on CustomerId
    with CustomerCreated
      CustomerName = name

  join Product on ProductId
    with ProductCreated
      ProductName = name
      ProductPrice = price
```

## Joins in Children

Inside a `children` block, distinguish the child's identity from its other references. The group member below is identified by the user ID, so user events can enrich that same member. Child joins match the child's identity; they do not provide arbitrary foreign-key matching against any child property.

The join label does not populate a child field. The `with` mappings, or compatible fields supplied by auto-map at that level, do. An event field consumed by an explicit join mapping is not auto-mapped to another child field:

```pdl
projection Group => GroupReadModel
  from GroupCreated
    Name = name

  children members identified by userId
    from UserAddedToGroup
      UserId = userId
      Role = role

    join User on UserId
      with UserCreated
        UserName = name
        UserEmail = email
      with UserUpdated
        UserName = name
        UserEmail = email
```

For a single optional object instead of a collection, use a [nested block](nested). Chronicle 19.99.1 and Screenplay's reference evaluator support joins inside root-level `nested` objects, including further nested objects. A `from` mapping that sets the nested join reference backfills the latest matching joined event into that object. Later joined events update only existing nested objects whose reference matches; they do not create root instances. Joins inside `children` are also supported, but joins inside nested objects under `children`, children inside nested objects and `remove via join` inside nested objects remain unsupported.

There is a backfill difference to account for when writing executable specifications. Chronicle also backfills when a nested join's `on` property is the nested schema's key or likely key (for example, `details.id`), even if that nested `from` block does not map it. The reference evaluator does not resolve the nested schema's key here: it uses the root read-model identifier for the key-property check. A nested `on` property therefore qualifies for backfill through that `from` block's own mapping of it, not merely because it is named `id`. An already populated nested key alone does not trigger this backfill in the reference evaluator; live joined events still match existing nested objects by their `on` value.

The compiler's population check still reports a required field filled only by a nested join as unpopulated (`PLAY0284`). This check has not yet caught up with Chronicle's nested-join support. Populate a required field from a `from` event that the nested block handles, or move its join out of the nested block.

## Examples

### Order with Customer Details

This variation keeps order placement fields explicit and uses matching names for customer data:

```pdl
projection Order => OrderReadModel
  from OrderPlaced
    OrderNumber = orderNumber
    CustomerId = customerId
    Total = total
    PlacedAt = $eventContext.occurred

  join Customer on CustomerId
    with CustomerCreated
      automap
    with CustomerUpdated
      automap
```

### Product with Category Information

The same relationship applies to a product's category. Renaming the category updates its displayed details on products that reference it:

```pdl
projection Product => ProductReadModel
  from ProductCreated
    ProductId = productId
    Name = name
    CategoryId = categoryId

  join Category on CategoryId
    with CategoryCreated
      CategoryName = name
      CategoryDescription = description
    with CategoryRenamed
      CategoryName = name
      CategoryDescription = description
```

### User Profile with Organization

Profile events own the user's bio and avatar. Organization events own the organization name shown alongside them:

```pdl
projection UserProfile => UserProfileReadModel
  from ProfileCreated
    UserId = userId
    Name = name
    OrganizationId = organizationId

  from ProfileUpdated
    Bio = bio
    Avatar = avatarUrl

  join Organization on OrganizationId
    with OrganizationCreated
      OrganizationName = name
      OrganizationType = type
    with OrganizationRenamed
      OrganizationName = name
      OrganizationType = type
```

### Task with Assignee Details

At the root level, `TaskAssigned` can change the reference used by a join. Setting `AssignedTo` lets the engine fill in details for the new assignee from existing user events:

```pdl
projection Task => TaskReadModel
  from TaskCreated
    Title = title
    Description = description
    AssignedTo = assigneeId

  from TaskAssigned
    AssignedTo = assigneeId
    AssignedAt = $eventContext.occurred

  join User on AssignedTo
    with UserRegistered
      AssigneeName = name
      AssigneeEmail = email
    with UserUpdated
      AssigneeName = name
      AssigneeEmail = email
```

### Reservation with Room and Guest

A reservation illustrates two references with different lifecycles: room details and guest contact details can change independently of the booking:

```pdl
projection Reservation => ReservationReadModel
  from ReservationMade
    ReservationNumber = number
    RoomId = roomId
    GuestId = guestId
    CheckIn = checkInDate
    CheckOut = checkOutDate

  join Room on RoomId
    with RoomCreated
      RoomNumber = number
      RoomType = type
      RoomFloor = floor
    with RoomUpdated
      RoomNumber = number
      RoomType = type
      RoomFloor = floor

  join Guest on GuestId
    with GuestRegistered
      GuestName = name
      GuestEmail = email
      GuestPhone = phone
    with GuestUpdated
      GuestName = name
      GuestEmail = email
      GuestPhone = phone
```

### Children with Join

The project below has a root join for its manager and a child join declared for task assignees. These have different matching rules. The manager join matches `ManagerId`, but the child join matches the task's `taskId` identity. It does not match an unrelated `AssignedTo` value. Do not use this child shape to look up arbitrary assignees unless the user event's source identity also matches the task identity:

```pdl
projection Project => ProjectReadModel
  from ProjectCreated
    Name = name
    ManagerId = managerId

  join User on ManagerId
    with UserCreated
      ManagerName = name

  children tasks identified by taskId
    from TaskAdded
      Title = title
      AssignedTo = assigneeId

    join User on AssignedTo
      with UserCreated
        AssigneeName = name
      with UserUpdated
        AssigneeName = name
```

## How Joins Work

A live joined event updates matching existing instances. When a root `from` event sets a joined reference, the engine can also fill in data from the latest matching event of each joined type. This history lookup is called backfill: the customer can already exist when the order is placed.

Neither path makes a customer event create an order. See [Chronicle projections](/chronicle/projections/) for the runtime execution model.

## Join Key Requirements

The `on` property must exist on the read model. For a root join, its stored value must match the joined event's source identity. Several instances can reference the same customer, and several projections can join the same customer stream.

For a child join, check the collection's `identified by` property as well: the child identity determines which element receives the update.

## Best Practices

Start with the reference your reader needs, such as `CustomerId` on an order, then list the events that can change the displayed data. Use explicit mappings when event names and read-model names differ; reserve `automap` for fields that really correspond.

When writing specifications, cover both arrival orders: customer data recorded before order placement, and customer data changed afterward. That checks both backfill and live updates without treating a join as an instance-creation event.

## Common Patterns

### Reference Data

Reference data can use the same key-matching approach. A status ID connects the instance to its current display name and color:

```pdl
join Status on StatusId
  with StatusCreated
    StatusName = name
    StatusColor = color
  with StatusUpdated
    StatusName = name
    StatusColor = color
```

### User Information

Use a user reference when the read model needs current profile details rather than a copy fixed at creation time:

```pdl
join User on UserId
  with UserRegistered
    UserName = name
    UserAvatar = avatarUrl
  with UserProfileUpdated
    UserName = name
    UserAvatar = avatarUrl
```

### Nested Relationships

For a collection, identity still governs child updates. In this fragment, `ProductId` is mapped onto an item identified by `itemId`. The child join does not turn that product reference into a foreign-key lookup: a live product event must match the item's identity to update it. If product IDs and item IDs differ, this shape does not enrich the item as a root product join would:

```pdl
children items identified by itemId
  from ItemAdded
    ProductId = productId

  join Product on ProductId
    with ProductCreated
      ProductName = name
```

## See Also

- [Children](children) - Choose the identity and parent key for each collection element.
- [Removal](removal.md) - Decide whether a related deletion removes the read-model instance.
- [Keys](keys.md) - Choose which event value identifies an instance.
