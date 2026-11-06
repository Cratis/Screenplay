---
title: "Cratis Screenplay graph projections: project events onto a property graph"
description: "A Screenplay graph projection builds nodes and edges from events, with the same mappings and counters a read-model projection uses."
---

# Graph projections

A graph projection builds a [graph](../graphs.md) from events the way a [projection](index.md) builds a read model. Write `=> graph <Name>` after the projection name, then describe each node label and edge type the events produce.

```screenplay
concept PersonId : Uuid
concept GroupId : Uuid
concept PersonName : String

module Organization
  feature Chart
    slice StateView OrganizationChart
      event PersonRegistered
        name PersonName

      event PersonRemoved

      event GroupCreated
        name String

      event MemberAdded
        groupId GroupId

      event MemberRemoved
        groupId GroupId

      graph Organization
        node Person
          personId PersonId key
          name     PersonName
        node Group
          groupId GroupId key
          name    String
        edge MEMBER_OF source Person target Group
          since DateTime

      projection OrganizationChart => graph Organization
        node Person
          from PersonRegistered key $eventSourceId
            name = name
          remove with PersonRemoved key $eventSourceId detach
        node Group
          from GroupCreated key $eventSourceId
            name = name
        edge MEMBER_OF
          from MemberAdded source $eventSourceId target groupId
            since = $eventContext.occurred
          remove with MemberRemoved source $eventSourceId target groupId
```

A graph projection is declared in a slice, next to the graph it builds. It holds `node` and `edge` blocks and nothing else; a standalone projection document cannot declare one (`PLAY0877`).

## Syntax

```screenplay
projection <Name> => graph <Graph>
  [description "<text>"]
  node <Label>
    from <Event> key <expression>
      <mapping>
    every
      <mapping>
    remove with <Event> key <expression> [detach]
  edge <TYPE>
    from <Event> source <expression> target <expression> [identified by <expression>]
      <mapping>
    remove with <Event> source <expression> target <expression> [identified by <expression>]
```

## Node blocks

`node <Label>` names a label the graph declares (`PLAY0867`). Inside it:

- `from <Event> key <expression>` creates the node the expression names, or updates it when it exists, and applies the mappings under it. The expression is any [projection expression](expressions.mdx): an event property, `$eventSourceId`, a template or an `$eventContext` member.
- `every` applies its mappings whenever any `from` of the block handles an event, so properties shared by every event are written once.
- `remove with <Event> key <expression>` deletes the node. Add `detach` to delete the edges attached to the node with it. A node removal without `detach` while the graph declares edges that attach to the label is a warning (`PLAY0870`): the edges would be left dangling.

## Edge blocks

`edge <TYPE>` names an edge type the graph declares. Inside it:

- `from <Event> source <expression> target <expression>` creates the edge between the two nodes and applies the mappings. If either endpoint node does not exist it is created bare, with only its key.
- `remove with <Event> source <expression> target <expression>` deletes that edge. `detach` has no meaning on an edge and is refused (`PLAY0869`).
- When the edge declares `identified by`, every handler writes `identified by <expression>` after `target`, and the edge is identified by that value as well (`PLAY0871`, `PLAY0873`).

## Mappings

A mapping inside `from` or `every` is the [property mapping](property-mapping.md) of a read-model projection: `property = expression`, `$eventContext.<member>`, and the [counters](counters.mdx) `increment`, `decrement`, `count`, `add ... by` and `subtract ... by`. They set or change node and edge properties. A property the node or edge does not declare is `PLAY0868`. As with a read model, properties an event shares with the node or edge by name are mapped automatically, so `name = name` above can be left out.

## What a graph projection cannot say

The read-model constructs `children`, `nested`, `join`, `all`, `clear with`, `remove via join`, `variant`, a projection-level `key` or `sequence`, and `automap` blocks have no meaning for a graph and are refused (`PLAY0872`). A malformed block, or a handler without its `key` (node) or `source` and `target` (edge), is `PLAY0873`. A node or edge built twice by one projection is `PLAY0874`. A projection for a graph that is not declared is `PLAY0866`.

## Executable model

Graphs and graph projections are part of the executable semantic model from ESM version 38. A projection yields five idempotent operations on the graph; every sink, whatever the engine, implements the same five:

| Operation | Produced by |
| --- | --- |
| `UpsertNode` | a node `from` |
| `SetProperties` | the mappings of a node or edge `from` and `every` |
| `DeleteNode` (with `detach` or without) | a node `remove with` |
| `UpsertEdge` | an edge `from` |
| `DeleteEdge` | an edge `remove with` |

A node is identified by its label and key; an edge by its type, its source and target keys, and the `identified by` value when the edge declares one. An edge operation ensures its two endpoint nodes exist. A projection owns the edges and the node properties it declares and never deletes a node it did not declare.

The reference executor keeps an in-memory graph and applies these operations, which is what `given graph` and `then graph` specifications run against.

## What the tools do not follow yet

The event model board, dependency and lineage queries, find-references and rename follow read-model projections only. A graph projection is compiled, printed, bound and executed like any other construct, but those tools do not draw it or rewrite its event references; renaming an event leaves a graph projection that the compiler then reports as unknown.

See also [graph declarations](../graphs.md).

## Reading a graph

A query that ends in `graph <Graph>` follows a path over the graph's declared nodes and edges and returns an ordinary read model (decision 0139, ESM v40):

```screenplay
concept PersonId : Uuid
concept GroupId  : Uuid
concept Name     : String

module Organization
  feature Chart
    slice StateView OrganizationChart
      graph Organization
        node Person
          personId PersonId key
          name     Name
        node Group
          groupId GroupId key
          title   Name
        edge MEMBER_OF source Person target Group
          since DateTime optional

      readmodel Membership
        personId PersonId key
        person   Name
        since    DateTime optional

      query MembersOf => Membership[] graph Organization
        filter groupId GroupId
        match person:Person -link:MEMBER_OF-> group:Group
        where group.groupId = $groupId
        return
          personId = person.personId
          person   = person.name
          since    = link.since
```

- `match` is a path of `alias:Label`, `-alias:TYPE->` (outgoing) and `<-alias:TYPE-` (incoming) steps; an alias is needed only where `where` or `return` names it. `optional match` follows a path that may be absent, so what it returns must go to optional read-model properties (`PLAY0883`).
- `where` joins equality tests with `and`; each compares a node or edge property with a `$argument` or a literal.
- `return` assigns every required read-model property from a node or edge property of the pattern.
- Not in this version (`PLAY0885`): variable-length paths, `or`, ranges, functions and aggregates. There is no engine-native query text.
- A specification asserts it with `then query <Name>` after `given graph`, or after events a projection turns into nodes and edges. `given clock "<instant>"` sets `$eventContext.occurred` for those events; without it the reference runner uses a fixed instant.

