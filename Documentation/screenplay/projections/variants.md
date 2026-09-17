# Variants

A `variant` block declares one of several **mutually exclusive** named read models sharing the enclosing projection's logical identity. An entity is in exactly one variant at a time. Entering one variant removes it from every other variant of the same projection — the compiler wires that exclusion for you; you never write a `remove with` for a sibling's entering event yourself.

This is for a shape like a Kanban board, where a work item is a backlog item, then a development item, then a pull request item — never more than one of those at once, and each stage has its own properties.

## Basic Example

```pdl
projection WorkItem
  variant BacklogItem
    enters on IssueCreated

  variant DevelopmentItem
    enters on IssueStarted

  variant PullRequestItem
    enters on PullRequestCreated
```

Here `WorkItem` is not itself a read model — it is the shared identity the three variants are grouped under. Each variant produces its **own** read model, named after the variant: `BacklogItem`, `DevelopmentItem`, `PullRequestItem`. A `query` or a command's `reads` names a variant exactly as it would name any other projection's read model.

## `enters on`

`enters on <EventType>` declares the event that **activates** a variant — the only event allowed to create that variant's instance. A variant must declare at least one.

```pdl
variant PullRequestItem
  enters on PullRequestCreated
```

Like an ordinary `from` event, `enters on` accepts an explicit key:

```pdl
variant PullRequestItem
  enters on PullRequestCreated key issueId
```

## A variant's own events are update-only

Everything a variant declares beyond its entering event is **update-only** — it can bring an already-active variant up to date, but it can never create one, and it can never resurrect an entity into a variant it has since left:

```pdl
projection WorkItem
  variant PullRequestItem
    enters on PullRequestCreated

    from BuildCompleted
      buildStatus = status
```

If `BuildCompleted` arrives for an issue that never had a pull request created, no `PullRequestItem` is created. This is not a rule you have to remember to apply carefully — it is what makes a variant a variant, rather than an ordinary projection that happens to share a name with some others.

## Shared handlers

A block declared at the **projection level**, outside every `variant`, is a shared handler applied to every variant that has the property it maps. There is no separate keyword for "shared" — being outside a `variant` block is what makes it one:

```pdl
projection WorkItem
  from TitleChanged
    title = title

  variant BacklogItem
    enters on IssueCreated

  variant PullRequestItem
    enters on PullRequestCreated
```

Here `TitleChanged` updates `title` on whichever variant is currently active, and creates none of the others — exactly like a variant's own non-entering events, a shared handler is update-only.

If a variant does not have the property a shared handler maps, that is a declaration error, not a silent skip or a runtime failure — the same discipline the Chronicle client SDKs enforce for the equivalent `[GlobalFor<T>]` attribute (with a compile-time analyzer for the model-bound authoring style).

## Full Example

```pdl
projection WorkItem
  from TitleChanged
    title = title

  variant BacklogItem
    enters on IssueCreated

  variant DevelopmentItem
    enters on IssueStarted

  variant PullRequestItem
    enters on PullRequestCreated

    from BuildCompleted
      buildStatus = status
```

- `title` is kept up to date on whichever variant is currently active.
- An issue starts as a `BacklogItem`.
- `IssueStarted` moves it to `DevelopmentItem`, removing it from `BacklogItem`.
- `PullRequestCreated` moves it to `PullRequestItem`, removing it from `DevelopmentItem`.
- `BuildCompleted` updates `buildStatus` on `PullRequestItem` only, and never resurrects the item into that variant on its own.

## Best Practices

1. **Name variants for what the entity *is* at that stage**, not for the event that created it.
2. **Keep the entering event minimal.** It is the one event allowed to create the variant, so the fewer properties it needs to set, the less there is to get wrong.
3. **Prefer a shared handler over repeating the same mapping in every variant** when every variant genuinely carries the property.
4. **Don't reach for variants when a single projection with a `status` property would do.** Variants are for genuinely different shapes with different properties, not for a single read model that merely changes one field.

## See Also

- [From Event](from-event.md) - The ordinary create-or-update mapping a variant's `enters on` event resembles.
- [Removal](removal.md) - The `remove with` mechanism variants build mutual exclusion out of, without you writing it.
- [Grammar (EBNF)](grammar.md) - The formal `variant` and `enters on` productions.
