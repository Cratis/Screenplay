---
id: 0138
title: Admit property graphs and graph projections (ESM v38)
status: accepted
stage: implemented
class: contract
reversibility: costly
decided: 2026-11-05
decider: Einar Ingebrigtsen (requested in Cratis/Screenplay#1159 and #1160)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.Contracts/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

A model projects events onto documents and tables. Some domains are a graph first: people belong to groups, groups nest, a person holds several roles in one group. Chronicle is adding a graph projection engine with a Neo4j sink ([#1159](https://github.com/Cratis/Screenplay/issues/1159)); the language has to say what a graph is and how events build it, without naming an engine.

## Decision

### 1. `graph`

A slice-level declaration, a sibling of `readmodel`: `node <Label>` blocks with typed properties, exactly one marked `key`, and `edge <TYPE> source <Label> target <Label> [identified by <property>]` blocks with typed properties. `description`, `documentation` and `file` behave as on a read model.

- A property is a primitive, a concept over a primitive, or an array of those. A nested object, a `type` or a read model is an error (`PLAY0863`).
- An edge is identified by `(type, source key, target key)`. `identified by` names an edge property that makes edges between the same pair distinct. A sink that cannot hold several edges between one pair rejects it; the language does not.
- `identified by` is written on the edge header, after `target`.

### 2. `projection <Name> => graph <Graph>`

Declared in the same slice family as the graph. It holds `node <Label>` and `edge <TYPE>` blocks:

- node: `from <Event> key <expression>`, `every`, `remove with <Event> key <expression> [detach]`;
- edge: `from <Event> source <expression> target <expression> [identified by <expression>]`, `every`, `remove with <Event> source <expression> target <expression> [identified by <expression>]`.

Mappings, counters, `$eventContext` and automap are those of a read-model projection. `children`, `nested`, `join`, `all`, `clear with`, `remove via join`, `variant`, a projection-level `key` or `sequence`, and `automap` blocks are refused (`PLAY0872`). There is no Cypher or other engine-native block.

### 3. Operations

A graph projection yields five idempotent operations: `UpsertNode`, `SetProperties`, `DeleteNode` (with explicit `detach`), `UpsertEdge` and `DeleteEdge`. An edge operation ensures its two endpoint nodes exist. A projection owns the edges and the node properties it declares and never deletes a node it did not declare.

### 4. Specifications

`given graph <Graph>` seeds nodes and edges (`has node`, `has edge`); `then graph <Graph>` asserts them and also states absence (`no node`, `no edge`). They are written in the slice's ordinary `specification`, with `given <Event>` and `when append <Event>`. The reference evaluator keeps an in-memory graph and applies the five operations. Values read from `$eventContext` are not materialized by the reference evaluator.

### 5. ESM v38

The executable model carries `graphs`, `graphProjections` and `graphSpecifications` at the application, as state machines and workflows are, from language and semantic version 38.0. A model that declares one selects 38.0; a model without one keeps its bytes and version. The version number is provisional until admission: v38 was earmarked for [#1138](https://github.com/Cratis/Screenplay/issues/1138), and the holder of that earmark renumbers if both land.

Graphs and their projections carry no semantic identities of their own: they are identified by name, like state machines. Handlers refer to events by contract identity.

### 6. Diagnostics

`PLAY0862` to `PLAY0877`, listed in the [diagnostics catalogue](../Documentation/screenplay/diagnostics.md).

## Consequences

- Every surface changes together: both compilers, the printer and syntax JSON, the binder, validator, serializer and reference evaluator, the editors, the MCP run of specifications, the documentation and the samples.
- Chronicle, Arc, Stage and the AI corpus adapt in their own repositories; the umbrella issue lists them.
- Reads are out of the language for this version. Applications read the graph through the engine driver.
