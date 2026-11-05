---
title: "Screenplay all: project across event types"
description: "Use all in a Screenplay projection to subscribe to every event type, with default per-source keys; every only shares mappings for subscribed events."
---

<a id="all-block"></a>

A projection-level `all` block in Cratis Screenplay subscribes to every event type, including types not named in a `from` block. Use it to record activity across event types for each event source, the identifier to which an event belongs.

[`every`](from-every.md) shares mappings across events the projection already subscribes to. It does not broaden those subscriptions. That difference matters when an event unrelated to your declared `from` blocks arrives: `all` handles it, while `every` alone does not.

<a id="basic-syntax"></a>
<a id="simple-example"></a>

## Count activity for each event source

This projection excerpt counts events and records the latest occurrence time without naming individual event types:

```pdl
projection EventCounter => EventCounterReadModel
  all
    count totalEvents
    lastOccurred = $eventContext.occurred
```

If source A has two events and source B has one, their read model instances have `totalEvents` values of 2 and 1. `all` widens the event-type subscription; it does not merge event sources into one global total. See [projection keys](keys.md) before choosing how to group an aggregate.

<a id="combining-all-with-from-blocks"></a>

## Add detail for a named event

Combine `all` with `from` when you need a shared activity measure and extra information from one event type:

```pdl
projection ActivityFeed => ActivityFeedModel
  all
    count totalSystemEvents
    lastActivity = $eventContext.occurred

  from UserRegistered
    recentUsers = name
```

`UserRegistered` sets `recentUsers` as well as running the shared mappings. An event type without its own handler still updates the count and timestamp on the instance keyed by its source. Despite the name `totalSystemEvents`, this example keeps per-source counts. `recentUsers` holds the latest mapped `name`, not a collection of users.

When a named handler declares a key, its key selects the affected instance. The event-source default applies to event types without a handler. A join event that matches no instance does not fall back to creating one under its own source. Do not assume that adding `all` overrides your existing routing.

## Difference Between `all` and `every`

| Choice | `all` | `every` |
|--------|-------|---------|
| Event types | Subscribes to every event type | Uses the projection's existing subscriptions |
| Shared mappings | Run across event types, including those without a `from` block | Run for subscribed events, including child events by default |
| Typical question | When did this source last have any activity? | When did this profile last change? |

<a id="example-contrast"></a>

In the following profile excerpt, the declared events are registration and email changes. An unrelated event type does not update `LastUpdated`:

```pdl
# Uses 'every' — only runs for UserRegistered and UserEmailChanged
projection UserProfile => UserProfileModel
  every
    LastUpdated = $eventContext.occurred

  from UserRegistered
    Name = name

  from UserEmailChanged
    Email = email
```

The audit projection instead accepts event types it does not name. Each event source gets its own count and timestamp:

```pdl
# Uses 'all' — runs for every event type in the entire system
projection SystemAuditLog => AuditLogModel
  all
    count totalEvents
    lastEventAt = $eventContext.occurred
```

<a id="when-to-use"></a>

## Keep the subscription at projection level

Use one `all` or `every` block at a given level. Inside a child or nested block, `all` behaves as `every` and produces a diagnostic; it does not create a system-wide subscription.

The reference evaluator supports `all` alongside projection-level `from` and join mappings, but not alongside removals, child collections, or nested objects. Keep those combinations out of a model you intend to evaluate as a specification.

If you only need common fields for the projection's existing events, continue with [`every`](from-every.md). To count those events, use [counters](counters.mdx); to sum amounts carried by them, use [arithmetic](arithmetic.md).
