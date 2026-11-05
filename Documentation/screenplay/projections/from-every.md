---
title: "Screenplay every: share projection mappings"
description: "Use every in a Screenplay projection to share timestamp and audit mappings across subscribed events, including child events unless excluded."
---

<a id="from-every"></a>

The `every` directive shares mappings across events that already affect a Cratis Screenplay projection. Use it when a field such as `LastUpdated` should change with each relevant event, without repeating the mapping in each `from` block.

Unlike [`all`](from-all.md), `every` does not subscribe the projection to unrelated event types. It covers the projection's subscribed events, including child events by default. Neither block has a single event contract, so explicit assignments from event properties belong in the relevant `from` handler.

<a id="basic-syntax"></a>
<a id="simple-example"></a>

## Keep a profile's audit fields together

In this projection excerpt, registration sets the profile fields and an email change replaces only the email. Both events also update the shared metadata:

```pdl
projection User => UserReadModel
  every
    LastUpdated = $eventContext.occurred
    UpdatedBy = $eventContext.eventSourceId

  from UserRegistered
    Name = name
    Email = email

  from UserEmailChanged
    Email = email
```

A `UserEmailChanged` event updates `Email`, `LastUpdated`, and `UpdatedBy` without assigning `Name` again. Despite its name, `UpdatedBy` here stores the event source identifier, not the identity of the person who made the change. Use the [event context reference](event-context.md) to choose the metadata your read model needs.

<a id="with-automap"></a>
<a id="product-with-global-automap"></a>

## Share automatic property mapping

An `automap` directive copies compatible, same-named event properties to read model properties. You can place it alongside an explicit timestamp mapping:

```pdl
projection Product => ProductReadModel
  every
    automap
    LastModified = $eventContext.occurred
```

This is a mapping choice, not a subscription to all event types. Add the relevant `from` blocks to the projection; use [AutoMap](auto-map.mdx) to control which properties they copy.

<a id="exclude-children"></a>
<a id="scope-and-application"></a>
<a id="parent-level-only-with-exclude-children"></a>
<a id="including-children-default"></a>

## Decide what counts as a parent update

An order can change because it ships or because an item is added to its child collection. By default, either event can update the parent's shared timestamp. If `LastUpdated` should describe only the order's own events, add `exclude children` inside `every`:

```pdl
every
  LastUpdated = $eventContext.occurred
  exclude children
```

Omit `exclude children` when child activity should count as parent activity. The option changes which events run the parent's shared mappings; child-specific mappings still belong in their own blocks.

<a id="common-patterns"></a>
<a id="audit-fields"></a>
<a id="event-source-tracking"></a>
<a id="version-tracking"></a>
<a id="examples"></a>
<a id="user-profile-with-audit"></a>
<a id="document-versioning"></a>

## Record the latest event's metadata

A sequence number records which event last updated the profile. It is not a count of profile changes: other events can occupy positions in the sequence. This excerpt keeps that metadata in `every` while leaving profile changes in their specific handlers:

```pdl
projection User => UserReadModel
  every
    LastUpdated = $eventContext.occurred
    LastSequenceNumber = $eventContext.sequenceNumber
    exclude children

  from UserRegistered
    Name = name
    Email = email
    CreatedAt = $eventContext.occurred

  from UserEmailChanged
    Email = email

  from UserNameChanged
    Name = name
```

For a number that increases once per relevant event, use [`count`](counters.mdx) in `every` instead of assigning `$eventContext.sequenceNumber`. The reference evaluator cannot execute `sequenceNumber` mappings. See the [event context reference](event-context.md) for metadata availability and supported evaluation paths.

<a id="order-with-tracking"></a>
<a id="with-multiple-children"></a>

## Keep child activity independent

The same choice applies when a parent has several collections. In this excerpt, adding a member or creating a post leaves `LastActivity` unchanged because both are child events:

```pdl
projection Group => GroupReadModel
  every
    LastActivity = $eventContext.occurred
    exclude children

  from GroupCreated
    Name = name

  children members identified by userId
    from UserAdded
      Name = userName

  children posts identified by postId
    from PostCreated
      Title = title
```

`GroupCreated` still sets `LastActivity` along with `Name`. Remove `exclude children` if the timestamp should instead answer “when was there any activity in this group?” See [child collections](children.mdx) for their identity and parent-key rules.

<a id="best-practices"></a>
<a id="when-to-use"></a>

Keep a mapping in its [`from` block](from-event.md) when it applies to only one event type. Choose [`all`](from-all.md) when events outside the projection's declared subscriptions must also run the shared mappings. Use only one `every` or `all` block at a given level.
