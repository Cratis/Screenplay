---
id: 0139
title: Read a graph with a typed path match, iterate event collections, and give graph specifications event context (ESM v40)
status: accepted
stage: designed
class: contract
reversibility: costly
decided: 2026-11-06
decider: Einar Ingebrigtsen (approved in conversation on Cratis/Screenplay#1169, #1170 and #1171)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.Contracts/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
---

## Context

[0138](0138-graphs-and-graph-projections-esm-v38.md) admitted graphs and graph projections and left reads out of the language, expecting "a neutral traversal construct ... anchored on ISO GQL / SQL/PGQ". Rebuilding Bilbo, a naming and data quality application, as a Screenplay-only model showed three gaps that stop a graph being part of an application built from the model alone:

1. nothing reads a graph, so no query, screen or specification can follow `QualityModel -SCORES-> TimeSeries -NAMED_IN-> NamingSchema` ([#1169](https://github.com/Cratis/Screenplay/issues/1169));
2. an event that carries a list cannot produce one node or edge per item ([#1170](https://github.com/Cratis/Screenplay/issues/1170));
3. a graph specification cannot state or assert values taken from `$eventContext` ([#1171](https://github.com/Cratis/Screenplay/issues/1171)).

## Decision

### 1. A graph query returns an ordinary read model

```screenplay
readmodel SchemaSeries
  modelId      ModelId
  slot         SlotName
  timeSeriesId TimeSeriesId

query SeriesScoredInSchema => SchemaSeries[] graph Lineage
  by schemaId SchemaId
  match model:QualityModel -scores:SCORES-> series:TimeSeries -NAMED_IN-> schema:NamingSchema
  where schema.schemaId = $schemaId
  return
    modelId      = model.modelId
    slot         = scores.slot
    timeSeriesId = series.timeSeriesId
```

- `=> <ReadModel>[] graph <Graph>` (or `=> <ReadModel> graph <Graph>` for one row, `optional` as on other queries, `observable` re-evaluates when a projection of that graph writes). The result is the declared read model, so screens, `authorize`, paging and `then query` work as for any query.
- `match` is a **path pattern over declared nodes and edges**: `alias:Label`, then `-alias:TYPE->` or `<-TYPE-` for direction, repeated. An alias is optional on an edge and needed only when `return` or `where` names it. Labels, edge types, direction and property names are checked against the graph (`PLAY0878` to `PLAY0885`).
- `where` is a conjunction (`and`) of equality tests between a node or edge property and a query argument (`$name`) or a literal. No `or`, ranges or functions in this version.
- `return` assigns every required read-model property from a node or edge property of the pattern. Types must be compatible.
- `optional match <pattern>` follows a path that may be absent: its aliases are null in a row without it, so each `return` assignment from it must target an optional read-model property.
- Out of scope, deliberately: variable-length paths, aggregation, ordering inside the graph (use the read model's ordering), shortest path and writes. They are additions to this grammar, not changes to it.
- There is no Cypher or other engine-native block. A sink maps the pattern to its engine, as it maps the five write operations.

### 2. `foreach` in a graph projection

```screenplay
edge SCORES
  from ModelDefined foreach slots source modelId target slots.series
    slot = slots.slot
```

`foreach <collection property>` in a graph `from` or `remove with` block runs the block once per item. The item's properties are written `<collection>.<property>`; every other expression is the event's. The collection is a property that is a list of a composite type. Replay is mark and sweep (0138), so an item that left the collection on a later event that restates it is removed. Nested `foreach` is refused (`PLAY0886`).

### 3. Context in graph specifications

A `given <Event>` and a `when append <Event>` in a graph specification may carry an `occurred` line (a date and time) and a `caused by` line, which the reference evaluator uses for `$eventContext.<path>`. Without them the evaluator uses a fixed clock and an anonymous subject, so a specification that does not care is unchanged. `has node` and `has edge` then assert properties taken from context.

### 4. ESM v40

The executable model carries `graphQueries` at the application. A model that declares none keeps its bytes and version. `foreach` and the event context lines extend the existing graph projection and specification shapes and select 40.0 only when used. v39 stays earmarked for [0137](0137-admit-event-subject-lineage-as-esm-v39.md); the version is provisional until admission, as 0138 was.

### 5. Reference evaluation

The reference graph runner gains `then query` over a graph: it evaluates the pattern over its in-memory graph. A sink must give the same rows for the same graph; Stage and Chronicle verify that against the runner's rows.

### 6. Diagnostics

`PLAY0878` to `PLAY0890`, listed in the diagnostics catalogue when implemented.

## Consequences

- Every surface changes together, as for 0138: both compilers, printer and syntax JSON, binder, validator, serializer, reference evaluator, editors, MCP, documentation and the corpus skills.
- Stage and Chronicle follow in their repositories: Stage lowers a graph query to the engine through the graph session Arc provides (`Cratis.Arc.Neo4j`), and a rendered application exposes it as an ordinary query.
- Fork inheritance by reference (Bilbo) becomes a one-hop `optional match` on `FORKED_FROM`; chains of forks need variable-length paths and wait for a later version.
