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
    from <Event> [foreach <collection>] key <expression>
      <mapping>
    every
      <mapping>
    remove with <Event> [foreach <collection>] key <expression> [detach]
  edge <TYPE>
    from <Event> [foreach <collection>] source <expression> target <expression> [identified by <expression>]
      <mapping>
    remove with <Event> [foreach <collection>] source <expression> target <expression> [identified by <expression>]
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

## One element per item of a collection

An event that carries a list can draw one node or edge per item. Write `foreach <collection>` after the event name; the handler then runs once per item, the item's properties are written `<collection>.<property>`, and every other expression is the event's:

```screenplay
type ModelSlot
  slot   String
  series Uuid

module Scoring
  feature Models
    slice StateView ModelScores
      event ModelDefined
        slots ModelSlot[]

      event ModelRetired
        slots ModelSlot[]

      graph Scoring
        node Model
          modelId Uuid key
        node Series
          seriesId Uuid key
        edge SCORES source Model target Series identified by slot
          slot String

      projection ModelScores => graph Scoring
        edge SCORES
          from ModelDefined foreach slots source $eventSourceId target slots.series identified by slots.slot
            slot = slots.slot
          remove with ModelRetired foreach slots source $eventSourceId target slots.series identified by slots.slot
```

The collection is a property of the event that is a list of a composite type (`PLAY0886` otherwise), and iteration goes one level deep: `foreach slots.parts` is refused. An absent or empty list does nothing. A model that uses `foreach` selects ESM version 41; see [decision 0140](https://github.com/Cratis/Screenplay/blob/main/decisions/0140-graph-foreach-esm-v41.md). Chronicle runs the handler once per item (Chronicle 19.134 and later).

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

The event model board, dependency and lineage queries, find-references and rename follow read-model projections only. A graph projection is compiled, printed, bound and executed like any other construct, but those tools do not draw it or rewrite its event references; renaming an event leaves a graph projection that the compiler then reports as unknown. Graph queries, flat or collecting, are the same: renaming a read model, a composite type or one of their properties does not rewrite the `return` lines and `collect` rows that name them, and the compiler then reports them (`PLAY0881`, `PLAY1145`).

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
- `return` can also [collect nested rows](#collect-nested-rows) along an edge from the root, with `<property> = collect <root> -TYPE-> <alias>:<Label>`.
- A specification asserts it with `then query <Name>` after `given graph`, or after events a projection turns into nodes and edges. `given clock "<instant>"` sets `$eventContext.occurred` for those events; without it the reference runner uses a fixed instant.


## Collect nested rows

A graph query can return rows that hold other rows: a person with the groups they belong to, a group with its members. Inside `return`, `<property> = collect <root> -[alias:]TYPE-> [alias:]Label` assigns a read-model property the rows reached along one edge from the query's root, and the lines under it fill each row from the reached node or the edge that was followed (decision 0163, ESM v54):

```screenplay
concept PersonId : Uuid
concept GroupId  : Uuid
concept Name     : String

type GroupMembership
  id    GroupId
  name  Name
  since DateTime optional

type GroupSummary
  id   GroupId
  name Name

module Organization
  feature Chart
    slice StateView OrganizationViews
      graph Organization
        node Person
          personId PersonId key
          name     Name
        node Group
          groupId GroupId key
          name    Name
        edge MEMBER_OF source Person target Group
          since DateTime optional
        edge PART_OF source Group target Group

      readmodel PersonWithGroups
        id     PersonId key
        name   Name
        groups GroupMembership[]

      readmodel GroupWithParent
        id     GroupId key
        name   Name
        parent GroupSummary optional

      query PeopleInGroup => PersonWithGroups[] graph Organization
        filter groupId GroupId
        match person:Person
        where group.groupId = $groupId
        return
          id     = person.personId
          name   = person.name
          groups = collect person -membership:MEMBER_OF-> group:Group
            id    = group.groupId
            name  = group.name
            since = membership.since

      query GroupsWithParent => GroupWithParent[] graph Organization
        match group:Group
        return
          id     = group.groupId
          name   = group.name
          parent = collect group -PART_OF-> parent:Group
            id   = parent.groupId
            name = parent.name
```

- **The root.** A query that collects reads like a view: one `match <alias>:<Label>` of its root node alone, no hops and no `optional match`. Its scalar returns come from the root. Every row of the result is one root node, distinct by key and ordered by the key (`PLAY1147` otherwise).
- **The edge.** A collect follows one declared edge from the root, outgoing (`-TYPE->`) or incoming (`<-TYPE-`), to the node it leads to. The arrow gives the direction, so an edge whose source and target are the same label (`PART_OF` above) is never ambiguous. Name the edge (`-membership:MEMBER_OF->`) when a row reads one of its properties.
- **The rows.** The collected property is a list of a [composite type](../types.md) (`GroupMembership[]`) or an optional composite type (`GroupSummary optional`). The lines under the collect assign the type's properties, each from the reached node or the followed edge, and every required property of the type is assigned (`PLAY1145`). A list holds one row per edge reached, ordered by the reached node's key and then by the edge's `identified by` value, so two roles in the same group are two rows; with nothing reached it is empty. An optional row is the first of those, or null.
- **Conditions select roots.** A `where` condition on a collected node keeps the roots with at least one reached node that satisfies every condition on that alias. It never narrows what is collected: `PeopleInGroup` returns everyone in the group, each with all of their groups. A condition on a collected edge is `PLAY1147`.
- **Absent properties.** A node created only by an edge (see [the graph is ensured by its edges](../graphs.md#the-graph-is-ensured-by-its-edges)) has none of its other properties yet; a row reads them as null.
- **One level.** A collect inside a collect is `PLAY1146`; follow the edge from the root in a collect of its own. A malformed collect is `PLAY1144`.

A specification asserts collected rows with structured values under `then query`, one `item` per row and `none` for an empty list:

```screenplay
concept PersonId : Uuid
concept GroupId  : Uuid
concept Name     : String

type GroupMembership
  id    GroupId
  name  Name

module Organization
  feature Chart
    slice StateView OrganizationViews
      graph Organization
        node Person
          personId PersonId key
          name     Name
        node Group
          groupId GroupId key
          name    Name
        edge MEMBER_OF source Person target Group

      readmodel PersonWithGroups
        id     PersonId key
        name   Name
        groups GroupMembership[]

      query PeopleWithGroups => PersonWithGroups[] graph Organization
        match person:Person
        return
          id     = person.personId
          name   = person.name
          groups = collect person -MEMBER_OF-> group:Group
            id   = group.groupId
            name = group.name

      specification SeeingEveryoneWithTheirGroups
        given graph Organization
          has node Person "6fa459ea-ee8a-4ca4-894e-db77e160355e"
            name = "Ada"
          has node Person "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            name = "Linus"
          has node Group "0f8fad5b-d9cb-469f-a165-70867728950e"
            name = "Engineering"
          has edge MEMBER_OF from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e"
        then query PeopleWithGroups
          result
            id   = "6fa459ea-ee8a-4ca4-894e-db77e160355e"
            name = "Ada"
            groups
              item
                id   = "0f8fad5b-d9cb-469f-a165-70867728950e"
                name = "Engineering"
          result
            id   = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            name = "Linus"
            groups none
```

The executable model carries each collect on its graph query as `collects`: the read-model property, the root alias, the followed hop (edge type, `outgoing`, the reached label), the cardinality (`many` or `one`), the row's composite type and its members, each of kind `property` (a property of the reached node) or `edgeProperty` (a property of the followed edge) with its `source`. A query that collects selects ESM v54; a query that does not keeps its version and bytes. A sink answers it with the same rows the reference runner gives, as for any graph query. The Organization sample reads its graph this way; see [the Organization sample](https://github.com/Cratis/Screenplay/tree/main/Samples/Organization).
