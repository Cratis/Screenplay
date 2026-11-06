---
id: 0140
title: Run a graph projection handler once per item of an event collection (ESM v41)
status: accepted
stage: designed
class: contract
reversibility: costly
decided: 2026-11-06
decider: Einar Ingebrigtsen (approved in conversation on Cratis/Screenplay#1170)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
---

## Context

[0139](0139-graph-reads-collection-iteration-and-event-context-esm-v40.md) decided `foreach` in graph projections and planned it into ESM v40. v40 shipped (5.8.0) with graph queries and the specification clock only, because Chronicle's graph projection could not iterate a collection ([Chronicle#5708](https://github.com/Cratis/Chronicle/issues/5708)). Chronicle now can ([Chronicle#5714](https://github.com/Cratis/Chronicle/pull/5714)): a graph mapping names a collection of the event and runs once per item.

## Decision

The grammar and semantics of 0139 section 2 stand, unchanged:

```screenplay
edge SCORES
  from ModelDefined foreach slots source $eventSourceId target slots.series identified by slots.slot
    slot = slots.slot
```

- `foreach <collection>` follows the event name in a graph `from` or `remove with`; the collection is a property of the event that is a list of a composite type. Anything else is refused (`PLAY0886`), and so is a dotted collection (nested iteration).
- Inside the handler, `<collection>.<property>` is the item's property and every other expression is the event's. An absent or empty collection does nothing.
- Because v40 is published, the construct selects a new version: the executable model carries `forEach`, the identity of the collection property, on a graph handler, and a model that uses it selects 41.0. A model that does not keeps its version and bytes. v39 stays earmarked for [0137](0137-admit-event-subject-lineage-as-esm-v39.md).

## Consequences

- Both compilers, the printer and syntax JSON, the binder, validator, serializer and reference evaluator change together, as for 0139.
- The reference evaluator runs each item as the event with the collection replaced by the item, which is what Chronicle's graph changeset builder does, so a specification and a kernel agree.
- Stage lowers the handler to the kernel's `ForEach` (Stage follows in its repository).
