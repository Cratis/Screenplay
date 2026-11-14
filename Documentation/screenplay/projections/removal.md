---
title: "Screenplay projection removal: delete instances or children"
description: "Use Screenplay remove with to delete a read-model instance or one child. Match the right keys and understand where remove via join is supported."
---

# Removal

Screenplay's `remove with` deletes a read-model instance, or one collection item when declared inside `children`. Choose the scope first: removing a member from a group should leave the group and its other members intact. To clear one optional object instead, use [`clear with` in a nested block](nested).

The language also parses `remove via join`, but its execution depends on scope. Chronicle supports it for removing children across parents; it does not execute root-level `remove via join`. Screenplay's reference evaluator rejects the root form as well. The root `remove via join` examples below illustrate declarations, not working cascade-deletion paths. Root-level reference semantics are tracked in [#563](https://github.com/Cratis/Screenplay/issues/563).

## Remove With

At the root, declare the event that removes the entire instance. Without an explicit `key`, removal uses the event source identity:

```pdl
remove with {EventType}
```

### Basic Example

```pdl
projection User => UserReadModel
  from UserCreated
    Name = name
    Email = email

  remove with UserDeleted
```

`UserDeleted` removes the `UserReadModel` whose key matches the event source identity. It does not delete every user instance. A later `from` event for that key can create the read-model instance again.

### With Explicit Key

If creation takes the user ID from an event field, use the same key rule for removal. The two events must resolve to the same stored instance:

```pdl
projection User => UserReadModel
  from UserCreated key userId
    Name = name

  remove with UserDeleted key userId
```

### With named key parts

For a read model identified by several top-level `key` properties, name every part in a child `key` block. Use the same part names as creation so removal selects the complete identity:

```pdl
remove with PaymentRemoved
  key
    invoiceId = invoiceId
    method = method
```

All parts must match: removing one invoice-and-method pair leaves another method on the same invoice intact. A later `from` event with the removed key creates it again. Missing parts report `PLAY0661`; unknown, repeated, or ill-typed parts report `PLAY0662`.

This root form has no scalar or parent key. The parser represents it as `RemoveWithSyntax.PartsKey`; an execution target must explicitly support the composite-key executable contract before running it.

### In Children

Inside `children`, removal targets one element. Carry the same child key and parent key through addition and removal:

```pdl
children members identified by userId
  from UserAddedToGroup key userId
    parent groupId
    Role = role

  remove with UserRemovedFromGroup key userId
    parent groupId
```

`key userId` selects the member and `parent groupId` selects the group containing it. That user's membership in another group remains unchanged.

## Remove Via Join

Inside a child collection, `remove via join` removes elements whose identity matches the removal event's key, across all parents. Its name does not mean it follows an arbitrary reference property.

At the root, the following syntax parses but does not remove an instance in Chronicle. It is not executable yet ([#563](https://github.com/Cratis/Screenplay/issues/563)): `validate` accepts it, but `test` refuses the whole projection with `UnsupportedProjectionBlock`, so every specification that depends on that projection is reported as unsupported. Leave it out of a model you want to run:

```pdl
remove via join on {EventType}
```

### Basic Example

```pdl
projection UserProfile => UserProfileReadModel
  from ProfileCreated
    UserId = userId

  join User on UserId
    with UserCreated
      Name = name

  remove via join on UserDeleted
```

This declaration expresses a profile's dependency on a user, but `UserDeleted` does not delete root profiles through `remove via join`. If the deletion event carries the profile's own key, use `remove with` and that key instead. A user ID reference alone is not a working root-deletion rule.

### With Key

An explicit key selects a value from the removal event. It does not enable root-level join removal:

```pdl
remove via join on UserDeleted key userId
```

### In Children

A child collection can use the related entity's identity as its own identity. In this user's groups collection, `groupId` is the child identity, so a group deletion can remove that group from every user's collection:

```pdl
children groups identified by groupId
  from UserAddedToGroup
    parent userId
    GroupId = groupId

  join Group on GroupId
    with GroupCreated
      Name = name
    with GroupRenamed
      Name = name

  remove via join on GroupDeleted
```

`GroupDeleted` removes matching group children across parent instances. It uses the removal event's source identity by default, or its explicit key if supplied. The child-creation shorthand above also needs to resolve its key to `groupId`; `parent userId` only selects the user.

## Examples

### User Account Deletion

With the default key, registration, email changes, and deletion all refer to the user identified by the event source:

```pdl
projection User => UserReadModel
  from UserRegistered
    Name = name
    Email = email
    CreatedAt = $eventContext.occurred

  from UserEmailChanged
    Email = email
    UpdatedAt = $eventContext.occurred

  remove with UserDeleted
```

### Group with Member Removal

A role change updates the member. A removal event deletes that member from the selected group without deleting the group itself:

```pdl
projection Group => GroupReadModel
  from GroupCreated
    Name = name

  children members identified by userId
    from UserAddedToGroup key userId
      parent groupId
      Name = userName
      Role = role

    from UserRoleChanged key userId
      parent groupId
      Role = role

    remove with UserRemovedFromGroup key userId
      parent groupId
```

### Product with Category Removal

This root-level declaration does not execute category-driven product deletion. The category join can enrich products, but its removal clause does not make that relationship a cascade:

```pdl
projection Product => ProductReadModel
  from ProductCreated
    Name = name
    CategoryId = categoryId

  join Category on CategoryId
    with CategoryCreated
      CategoryName = name

  remove via join on CategoryDeleted
```

Deleting a category does not remove these product instances through the root `remove via join` clause.

### Order Line Item Removal

Removing a child does not calculate the parent's totals for you. This order declares separate arithmetic mappings for adding and removing a line. Those root handlers use event source identity, so the line events must identify the order by that source as well as by their child-level `parent orderId`:

```pdl
projection Order => OrderReadModel
  from OrderPlaced
    OrderNumber = orderNumber
    Total = 0

  children items identified by lineNumber
    from LineItemAdded key lineNumber
      parent orderId
      ProductId = productId
      Quantity = quantity
      LineTotal = total

    from LineItemQuantityChanged key lineNumber
      parent orderId
      Quantity = quantity

    remove with LineItemRemoved key lineNumber
      parent orderId

  from LineItemAdded
    add Total by total

  from LineItemRemoved
    subtract Total by total
```

### User Profile with Account Deletion

Profile updates and user enrichment have distinct mappings. As in the earlier profile example, the root `remove via join` declaration does not execute account-driven profile deletion:

```pdl
projection UserProfile => UserProfileReadModel
  from ProfileCreated
    UserId = userId
    Bio = bio
    Avatar = avatarUrl

  from ProfileUpdated
    Bio = bio
    UpdatedAt = $eventContext.occurred

  join User on UserId
    with UserRegistered
      Name = name
      Email = email
    with UserUpdated
      Name = name
      Email = email

  remove via join on UserAccountDeleted
```

### Subscription with Plan Removal

Direct cancellation can remove this subscription. Plan discontinuation is a different case: the root join-removal declaration below is not executed by Chronicle:

```pdl
projection Subscription => SubscriptionReadModel
  from SubscriptionCreated
    UserId = userId
    PlanId = planId
    StartDate = startDate

  join SubscriptionPlan on PlanId
    with PlanCreated
      PlanName = name
      Price = price
    with PlanUpdated
      PlanName = name
      Price = price

  remove with SubscriptionCancelled

  remove via join on PlanDiscontinued
```

`SubscriptionCancelled` removes the subscription using its own instance key. `PlanDiscontinued` does not remove it through this root `remove via join` clause.

### Task with Project Deletion

A task can display both project and assignee details. Those joins do not make project deletion remove root task instances; the removal clause below is syntax only at this scope:

```pdl
projection Task => TaskReadModel
  from TaskCreated
    Title = title
    ProjectId = projectId
    AssignedTo = assigneeId

  join Project on ProjectId
    with ProjectCreated
      ProjectName = name

  join User on AssignedTo
    with UserCreated
      AssigneeName = name

  remove via join on ProjectDeleted
```

`ProjectDeleted` does not remove the root tasks through this declaration.

### Multi-Level Removal

Employee unassignment and department disbanding act at different scopes. The company join adds display data, but its root join-removal clause does not cascade company dissolution into department deletion:

```pdl
projection Department => DepartmentReadModel
  from DepartmentCreated
    Name = name
    CompanyId = companyId

  join Company on CompanyId
    with CompanyCreated
      CompanyName = name

  children employees identified by employeeId
    from EmployeeAssigned key employeeId
      parent deptId
      Name = name

    remove with EmployeeUnassigned key employeeId
      parent deptId

  remove with DepartmentDisbanded

  remove via join on CompanyDissolved
```

`EmployeeUnassigned` removes one child, while `DepartmentDisbanded` removes the department read-model instance, including its embedded employee collection. `CompanyDissolved` does not remove that root instance through `remove via join`.

## Parent Keys in Child Removal

Use the same parent expression for child removal as for the events that populate the collection:

```pdl
remove with ChildRemoved key childId
  parent parentId
```

This removes the child with `childId` from the parent identified by `parentId`.

## Key Matching

Root `remove with` matches the instance key against the event source identity or explicit event key. Within `children`, it also resolves the containing parent. Child `remove via join` instead matches the child identity across parents; it does not need a parent expression or follow a foreign-key field on the child.

## Multiple Removal Conditions

You can declare several removal events. In this example, direct document deletion works, but closing the owner's account does not delete the document through the root join-removal clause:

```pdl
projection Document => DocumentReadModel
  from DocumentCreated
    Title = title
    OwnerId = ownerId

  join User on OwnerId
    with UserCreated
      OwnerName = name

  remove with DocumentDeleted
  remove via join on UserAccountClosed
```

`DocumentDeleted` removes the document. `UserAccountClosed` does not remove it through this root declaration.

## Best Practices

Decide whether readers should stop seeing the instance or continue seeing it marked as deleted. Removal changes the projected read model; it is not a declaration to erase the source events. Use a status field when the deleted state itself needs to remain queryable.

For direct child removal, verify both keys against the add event. For join removal, check that deleting the same child identity across every parent is really the intended result. Include a later `from` event in lifecycle specifications when recreation would matter: removal alone does not prevent it.

## Common Patterns

### Soft Delete Alternative

Keep an instance queryable by mapping a deletion flag instead of declaring removal. Queries can then decide whether to include it:

```pdl
projection User => UserReadModel
  from UserCreated
    Name = name
    IsDeleted = false

  from UserDeleted
    IsDeleted = true
    DeletedAt = $eventContext.occurred
```

### Cascade Delete

Removing a root instance also removes its embedded collections from that read model. It does not require a separate child removal rule.

The two join-removal clauses below illustrate different limits. `ProductDiscontinued` matches the child `itemId`, not the mapped `ProductId`, so it only removes items with that matching identity. `CustomerAccountClosed` does not remove orders because root join removal is not executed:

```pdl
projection Order => OrderReadModel
  from OrderPlaced
    CustomerId = customerId

  join Customer on CustomerId
    with CustomerCreated
      CustomerName = name

  children items identified by itemId
    from ItemAdded key itemId
      parent orderId
      ProductId = productId

    remove via join on ProductDiscontinued

  remove via join on CustomerAccountClosed
```

### Conditional Removal

Separate events can each remove the same instance. These are event-triggered removal rules; there is no additional predicate on the removal itself:

```pdl
projection Subscription => SubscriptionReadModel
  from SubscriptionCreated
    Status = "Active"

  remove with SubscriptionCancelled
  remove with PaymentFailed
  remove with TrialExpired
```

## Differences Between Remove With and Remove Via Join

| Scope | `remove with` | `remove via join` |
|-------|---------------|-------------------|
| Root instance | Removes the instance matching the event key | Parses, but Chronicle does not execute it; the reference evaluator rejects it |
| Child collection | Removes the child matching the event key within the selected parent | Removes children with the matching identity across parents |
| Nested object | Use `clear with` to express clearing the value | Not supported |

## See Also

- [Joins](joins.md) - Separate related-data updates from lifecycle decisions.
- [Children](children) - Keep the same child and parent identities throughout a collection's lifecycle.
- [Keys](keys.md) - Match removal events to the instances they should delete.
