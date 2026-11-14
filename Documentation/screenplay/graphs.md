---
title: "Cratis Screenplay graphs: declare the shape of a property graph"
description: "A Screenplay graph declares node labels, edge types and their typed properties. Events build the graph through a projection; nobody writes to it directly."
---

# Graphs

A Cratis Screenplay `graph` declares the shape of a **property graph**: the node labels it holds, the edge types that connect them, and the typed properties on both. Like a [read model](readmodels.md), a graph is derived state. Nothing writes to it directly; a [graph projection](projections/graphs.md) builds it from events.

The language describes the logical graph only. It never mentions Cypher, Gremlin or any other engine, and it has no engine-native escape hatch. Whatever the language cannot say is not supported until a construct is added for it.

## Syntax

```screenplay
graph <Name>
  [description "<text>"]
  [file <path>]
  [sink <Sink>]
  node <Label>
    <property> <Type> [optional]
    <keyProperty> <Type> key
  edge <TYPE> source <Label> target <Label> [identified by <property>]
    <property> <Type> [optional]
```

A graph holds `node` and `edge` declarations. `description`, `documentation` and `file` behave as they do on a [read model](readmodels.md). A `sink <Name>` line stores the graph in a sink declared `for graphs` instead of the default graph sink; see [Sinks](sinks.md).

## An organization

The graph below describes people, the groups they belong to, and how groups nest. It is the graph the [Organization sample](https://github.com/Cratis/Screenplay/tree/main/Samples/Organization) builds.

```screenplay
concept PersonId : Uuid
concept GroupId : Uuid
concept PersonName : String

module Organization
  feature Chart
    slice StateView OrganizationChart
      graph Organization
        node Person
          personId PersonId key
          name     PersonName
        node Group
          groupId GroupId key
          name    String
        edge MEMBER_OF source Person target Group
          since DateTime
        edge PART_OF source Group target Group
```

## Nodes

A `node` declares a label. Its properties are ordinary property lines, and **exactly one** of them carries the trailing `key` modifier. The key identifies the node: a projection names the key of every node it creates, and a [specification](#specifying-a-graph) names it when it asserts one. A node with no key, or with several, is `PLAY0864`.

The key must be a required, single value. It cannot combine with `optional`, `generated`, `identifier` or `subject`.

## Edges

An `edge` declares a directed relationship from a `source` label to a `target` label. Both labels must be declared as nodes in the same graph (`PLAY0865`). An edge type is written in the graph's own convention; `UPPER_SNAKE_CASE` reads well and is what the samples use.

An edge has no key. Its identity is the triple of **type, source node key and target node key**, so a second `MEMBER_OF` between the same person and group is the same edge. When the model needs several edges of one type between the same two nodes, name an edge property with `identified by`:

```screenplay
concept PersonId : Uuid
concept GroupId : Uuid
concept AssignmentId : Uuid

module Organization
  feature Roles
    slice StateView Roles
      graph Organization
        node Person
          personId PersonId key
        node Group
          groupId GroupId key
        edge HAS_ROLE source Person target Group identified by assignmentId
          assignmentId AssignmentId
          role         String
```

The edge is then identified by type, source, target **and** the value of that property, so one person can hold two roles in one group as two edges. The property must be declared on the edge (`PLAY0871`). A sink that cannot hold several edges between the same pair rejects such an edge when the projection is deployed; the language does not.

## Property types

A graph property is a primitive (`Uuid`, `String`, `Int`, `Decimal`, `Bool`, `Date`, `DateTime`), a [concept](concepts.md) over a primitive, or an array of those (`String[]`). Nested objects are a compile error: a [`type`](types.md), a read model or an array of either is `PLAY0863`. Model what you would nest as another node and an edge instead.

## The graph is ensured by its edges

An edge operation creates a bare endpoint node when the node does not exist yet, so an event may draw an edge before the events that create its nodes have arrived. A projection owns the edges and the node properties it declares; it never deletes a node it did not declare.

## Specifying a graph

A [specification](specifications.md) seeds and asserts a graph with `given graph` and `then graph`. Each line states a node or an edge by its key; the lines under it assert property values.

```screenplay
then graph Organization
  has node Person "6fa459ea-ee8a-4ca4-894e-db77e160355e"
    name = "Ada Lovelace"
  has edge MEMBER_OF from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e"
  has edge HAS_ROLE from "6fa459ea-ee8a-4ca4-894e-db77e160355e" to "0f8fad5b-d9cb-469f-a165-70867728950e" identified by "9c858901-8a57-4791-81fe-4c455b099bc9"
    role = "Lead"
  no edge MEMBER_OF from "0f8fad5b-d9cb-469f-a165-70867728950e" to "6fa459ea-ee8a-4ca4-894e-db77e160355e"
  no node Person "9c858901-8a57-4791-81fe-4c455b099bc9"
```

| Step | Meaning |
| --- | --- |
| `has node <Label> <key>` | The node exists, and the properties listed under it hold the given values. |
| `has edge <TYPE> from <source> to <target> [identified by <value>]` | The edge exists, and the properties listed under it hold the given values. |
| `no node <Label> <key>` | The node does not exist. `then graph` only. |
| `no edge <TYPE> from <source> to <target> [identified by <value>]` | The edge does not exist. `then graph` only. |

`given graph` accepts only the `has` forms: it seeds what exists before the action. A `has` assertion lists the properties it checks; the others are not examined. A malformed step is `PLAY0875`, and a step that names an undeclared graph, node or edge is `PLAY0876`. A property the node or edge does not declare is `PLAY0868`.

## What the executable model does with a graph

See [the executable model](projections/graphs.md#executable-model) for how graphs and graph projections are admitted and executed.

## Reading a graph

A query that ends in `graph <Graph>` reads the graph and returns an ordinary read model; see [reading a graph](projections/graphs.md#reading-a-graph). With `collect` it returns nested rows - a person with the groups they belong to, a group with its members - as a graph view; see [collect nested rows](projections/graphs.md#collect-nested-rows).

## Diagnostics

| Code | Meaning |
| --- | --- |
| `PLAY0862` | A graph, node or edge declaration is malformed. |
| `PLAY0863` | A property type is not a primitive, a concept over one, or an array of those. |
| `PLAY0864` | A node declares no `key` property or more than one. |
| `PLAY0865` | An edge names a source or target label the graph does not declare. |
| `PLAY0871` | `identified by` names a property the edge does not declare. |
| `PLAY0874` | A graph, label, edge type or property is declared twice. |
