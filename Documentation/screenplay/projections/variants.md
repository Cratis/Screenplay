---
title: "Screenplay projection variants: model mutually exclusive shapes"
description: "Use Screenplay variants for separate read-model shapes that share an identity. Define entering events, update-only handlers, and shared mappings."
---

# Variants

Screenplay projection variants give one entity separate read-model shapes for different stages of its lifecycle. A work item can appear as a backlog item, a development item, or a pull request item, with fields suited to each stage. Use variants when those shapes differ; use a single read model with a status field when only the status changes.

An entering event creates or updates one variant and removes the same identity from its siblings. Before any entering event, the entity can be absent from all of them. Keep the entering events on the shared entity's event source identity so the generated sibling-removal keys match.

Screenplay validates these declarations and lowers them into its executable semantic model. Chronicle's projection declaration-language compiler does not yet lower `variant` blocks; the declarations below are not executable through that path. See [Validation and executable semantics](#validation-and-executable-semantics) for the distinction.

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

`WorkItem` groups the variants under a shared identity; it is not a fourth read model. The three queryable read models are `BacklogItem`, `DevelopmentItem`, and `PullRequestItem`. A `query` or a command's `reads` names the variant read model it needs.

## `enters on`

`enters on <EventType>` names an event allowed to create the variant's instance. Every variant needs at least one entering event. For a pull request item, that event is the creation of the pull request:

```pdl
variant PullRequestItem
  enters on PullRequestCreated
```

Like an ordinary `from` event, `enters on` accepts an explicit key. Here `issueId` supplies the pull request item's identity:

```pdl
variant PullRequestItem
  enters on PullRequestCreated key issueId
```

Generated sibling removals still use the entering event's source identity. If `issueId` differs from that source identity, the example above does not ensure mutual exclusion: creation and sibling removal can target different keys. Keep those identities aligned when choosing an explicit key.

## A variant's own events are update-only

A non-entering `from` event updates an existing variant without creating it. That matters for delayed build results: a build event should update an active pull request item, not bring back one the work item has already left:

```pdl
projection WorkItem
  variant PullRequestItem
    enters on PullRequestCreated

    from BuildCompleted
      buildStatus = status
```

If `BuildCompleted` arrives before `PullRequestCreated`, there is no pull request item to update, so it creates none. The same is true after the work item leaves that variant. This differs from an ordinary projection's `from` handler, which can create an instance.

## Shared handlers

Put a `from` block outside the variants when the same mapping belongs to every shape. A title change is shared because a work item keeps its title through every stage.

Every variant with a known read-model shape must declare the mapped property. A missing property reports `PLAY0383`; validation cannot settle that check for an unknown or imported shape. No separate `shared` keyword is needed:

```pdl
projection WorkItem
  from TitleChanged
    title = title

  variant BacklogItem
    enters on IssueCreated

  variant PullRequestItem
    enters on PullRequestCreated
```

`TitleChanged` updates `title` on an existing variant and creates none of the others. Shared non-entering handlers are update-only, just like a variant's own non-entering `from` blocks.

Chronicle's .NET model-bound authoring uses `[GlobalFor<T>]` for shared handlers. That builder checks that the mapped properties exist on the variant before merging the shared mappings. This is a separate authoring path from Screenplay's declaration-language blocks.

## Full Example

With entering events using the same work-item source identity, the stage changes and shared title mapping fit together as follows:

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

`IssueCreated` puts the issue in `BacklogItem`. `IssueStarted` moves the same identity to `DevelopmentItem`, removing it from the backlog. `PullRequestCreated` moves it again, this time into `PullRequestItem`.

A title change updates whichever variant exists at that point. `BuildCompleted` only changes `buildStatus` on an existing pull request item; it cannot move the work item into that stage.

## Validation and executable semantics

Screenplay checks the declaration before execution:

| Diagnostic | Declaration to correct |
|------------|------------------------|
| `PLAY0382` | A variant has no `enters on` event. |
| `PLAY0383` | A shared mapping targets a property absent from a known variant shape. |
| `PLAY0384` | A variant name is repeated within the projection. |
| `PLAY0385` | An entering event is claimed more than once within the projection. |

The executable semantic model (ESM) is the compiled representation used by Screenplay's reference evaluator. It turns each variant into an independent projection on its own read model. Entering events become create-or-update `from` transitions. Other `from` blocks, including shared mappings merged first, become update-only joins on the variant's identifier, retaining the original key or default event source identity. Each sibling's entering event adds a removal keyed by event source identity.

Chronicle's .NET client-side variant reclassification follows the same mapping approach, but that does not make declaration-language `variant` blocks executable in Chronicle. That lowering is tracked separately in [Cratis/Chronicle#4109](https://github.com/Cratis/Chronicle/issues/4109).

Internally, the ESM projection name and semantic address use `<length of group name>:<group name>:<variant name>`, for example `8:WorkItem:BacklogItem`. The read-model name remains `BacklogItem`, which is the name to use in queries. As with an ordinary projection, the ESM needs an unambiguous identifier. Mark one read-model property as `key` or identify it through a keyed query returning an optional single read model. The binder only falls back to an unambiguous conventional `*Id` property when the same slice declares an admitted list query for that read model. Composite-key variant read models and a projection's `sequence` are not supported in the portable ESM.

## Best Practices

Name a variant for the read model the reader queries, such as `PullRequestItem`, and let its entering event supply what that shape needs at creation. Share a mapping only when every variant actually carries the field.

In executable specifications, follow one identity through each stage and assert that the previous variant is absent. Also send a non-entering event before activation and after departure. Those cases check that update-only handlers do not create or revive a variant. If entering events use explicit keys, check their relationship to event source identity as part of the same scenario.

## See Also

- [From Event](from-event.md) - Compare ordinary create-or-update handlers with variant activation.
- [Removal](removal.md) - Understand the direct removal mechanism used for sibling exclusion.
- [Grammar (EBNF)](grammar.md) - Check the formal `variant` and `enters on` productions.
