---
title: "Cratis Screenplay event seeding for initial data"
description: "Declare initial application data as Cratis Screenplay seed events, grouped by event source ID, with the event declarations and values a runtime needs."
---

# Event seeding

Cratis Screenplay seeding describes initial application data as events grouped by event source ID, the identity of the entity whose history they belong to. Use a top-level `seed` block for reference data, demo customers or test-environment fixtures so the model states which facts establish its starting state.

## Before you seed

Declare the [events](events.md) you will seed, or make their names available through an [import](imports.md). Declare any [types](types.md) used for nested records too, so their members can be checked. Choose an event source ID for each entity and supply values that match the event's properties. Compiling the document records these declarations; it does not append events to a running application.

The example below assumes `CustomerRegistered` and `CustomerUpgraded` already exist. The `for` ID selects the event history; it does not fill a `customerId` property automatically. Supply that property separately wherever the event requires it. To execute the seeds, the consuming runtime must also resolve the events and accept their values; an imported name alone does not establish that.

## Syntax

```screenplay
seed ["<name>"]
  for "<event source id>"
    <EventType>
      <property> = <value>
      ...
    ...
  ...
  [position <x> <y>]
```

Place `seed` at the top level, alongside concepts, policies and modules. Multiple blocks accumulate. An optional quoted name, such as `seed "Starter catalogue"`, lets authoring tools identify a seed collection.

Within the block, each `for "<event source id>"` group holds the events for that quoted ID. Name an event on its own line, then indent its property assignments underneath. An unknown event name produces a warning.

Scalar assignments use `<property> = <value>`, with the same expression grammar as `produces` mappings and specification events. For an object or list, use an indented [structured value](structured-values.md) under the property name.

The optional `position <x> <y>` line accepts two whole numbers, including negatives, at most once per block. It is canvas authoring metadata, not part of the executable model.

An empty `seed` block — one without any `for` group — is a compile error.

[`propose-property`](mcp/reference.md#property-change-planner) treats seed assignments as fixtures of the event's current generation: rename and remove update them, required additions need `fixtureValue` for every seed entry, and optional additions leave seeds unchanged.

## Example

```screenplay
seed
  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    CustomerRegistered
      customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
      name       = "Acme Corp"
    CustomerUpgraded
      tier = "gold"
  for "9c858901-8a57-4791-81fe-4c455b099bc9"
    CustomerRegistered
      name = "Globex"
```

The first group describes a customer registration followed by an upgrade in the same history. The second describes another customer's registration. Event order within each group is declaration order.

## Executing seed events

Seeding behavior belongs to the runtime consuming the model. [Cratis Stage](/stage/) accepts literal seed values, including objects and lists of literals, and resolves each event against its execution plan. It checks that the seed history can be replayed before appending it to Chronicle; a group that fails that replay check is left out with a diagnostic.

Stage skips a source that already has events, so changing a seed block does not update that source's existing history. If starting data is missing, check the seed diagnostics and whether the chosen source already contains events before changing the model. Use [specifications](specifications.md) when you need isolated Given/When/Then examples rather than application-wide starting data.
