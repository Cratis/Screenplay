---
id: 0163
title: A graph query collects nested rows along an edge from its root, as a graph view (ESM v54)
status: accepted
stage: designed
class: contract
reversibility: costly
decided: 2026-11-14
decider: Direct request for the cross-repository graph views feature (shared specification, Screenplay section 6)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.Conformance/**
  - Source/DotNET/Screenplay.Contracts/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/Organization/**
---

## Context

[0139](0139-graph-reads-collection-iteration-and-event-context-esm-v40.md) gave a graph query a typed path match whose `return` assigns flat scalars. A consumer that wants a person with the groups they belong to, or a group with its members, gets one flat row per path and has to fold them itself.

The graph views work, shared by Chronicle, Arc, Stage and the clients, defines a **graph view**: a read model anchored at one node label. Each member is a property of that node, a relation along a declared edge to another view, or a property of the followed edge. Nesting is finite, and a view only reads. That work fixes the declaration rules, a conformance fixture (the Organization views) and the query semantics every reference evaluator must reproduce. Screenplay must express such a view so that it maps one-to-one onto the shared declaration rules, and Stage lowers it through the Arc graph session.

## Decision

### 1. `collect` inside `return`

```screenplay
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
```

- `<property> = collect <root> -[alias:]TYPE-> [alias:]Label` (or `<-[alias:]TYPE-` for an incoming edge) assigns a read-model property the rows reached along one edge. The pattern words and arrows are the ones 0139 introduced, so a collect reads like the rest of the query.
- The lines under it are `<property> = <alias>.<property>`, one per property of the row's composite type. Each line reads the reached node, which is a shared-rule `property` member, or the named followed edge, which is an `edgeProperty` member.
- Cardinality comes from the read-model property's type, as in the shared rules: a list of a composite type (`Row[]`) is `many`, and an optional composite type (`Row optional`) is `one`. A required single composite is refused, because absence must be expressible.
- Direction is always explicit in the arrow, so a self edge (`PART_OF` from `Group` to `Group`) is never ambiguous. The shared `AmbiguousDirection` rule cannot arise.

### 2. A query that collects has the shape of a view

It has exactly one `match <alias>:<Label>` of its root alone: no hops and no `optional match`. Its scalar returns come from the root. A `where` condition names the root, as the shared `Property` clause does, or a collected node, as the shared `Related` clause does with its nested equality clauses. A condition never names a collected edge. Flat graph queries keep every form 0139 allows.

### 3. Semantics (the shared query semantics)

- Rows are root nodes, distinct by key and ordered by key ascending: numerically when both keys are numbers, otherwise ordinally, so a Guid orders by its string form. A single-row query returns the first root or none.
- Conditions select roots. The conditions on one collected alias must all hold on the same reached node, and they never narrow what is collected.
- A collect holds every row reached along its edge, ordered by the reached node's key and then by the edge's identity. An identified edge yields one row per edge. A `many` collect with nothing reached is an empty list, and a `one` collect is null.
- A bare node, created only by an edge, reads its absent properties as null.
- The reference graph runner implements this, and `then query` asserts it with structured values (`item`, `none`, `= null`). The shared query vectors run through the runner in a data-driven specification.

### 4. Nesting

A collect inside a collect is refused with `PLAY1144`. Nesting is one level by construction, which also rules out the shared `RecursiveView`. Deeper nesting is an addition to this grammar, not a change to it.

### 5. ESM v54

The executable model carries `collects` on a graph query. Each collect holds the read-model property, the root alias (`from`), the followed `hop` (alias, edge type, `outgoing`, reached node), `cardinality` (`many` or `one`), the row's composite type (`row`) and the ordered `members`. Each member holds the row property, a `kind` of `property` or `edgeProperty`, and a `source`. A model that uses a collect selects 54.0. A model that does not keeps its version and every serialized byte. Existing flat graph queries still select 40.0. Stage maps a collect onto the shared view definition: the relation is the edge type, the direction is `outgoing` when `outgoing` is true and `incoming` otherwise, and the cardinality is the collect's. The nested view is the row type rooted at the reached label.

### 6. Diagnostics

- `PLAY1142`: a malformed collect.
- `PLAY1143`: a collect that does not fit its read-model property or row type.
- `PLAY1144`: a nested collect.
- `PLAY1145`: a query that is not view-shaped.

Shared rules that already have a Screenplay code keep it. An unknown edge, label or property is `PLAY0879`. An edge that is not incident on the root, or a reached label that does not match, is `PLAY0880`. An alias used twice is `PLAY0884`.

### 7. Strictly additive

No existing construct, diagnostic code, serialized byte or behavior changes. `GraphQuerySyntax.collects` is omitted from the syntax JSON when empty, so the syntax goldens of existing documents are unchanged. This is a minor release.

## Consequences

- Every surface changes together: both compilers and the syntax JSON, the printer, the binder, the validator and the serializer, the reference runner, the completeness checks, the MCP description, Monaco, the VS Code grammar, the documentation, the Organization sample and the conformance documents.
- The PLAY0534 completeness check treats a read model that a collecting graph query returns as served. A flat graph query is unchanged.
- Stage lowers a collecting query through the Arc graph session (`Cratis.Arc.Neo4j`) and must return the runner's rows for the shared vectors. The `Cratis/AI` corpus teaches `collect` in the read-surface and projection skills.

## Options considered

- **A body-level `collect` block beside `return`.** Not taken: a nested property is a read-model property, and `return` is where every read-model property is assigned.
- **A `many` or `one` keyword on the collect.** Not taken: the read-model property's type already says it, as the shared rules require.
- **Several hops in one collect, or a collect inside a collect.** Not taken in this version. It needs a nested view definition on every surface, including Stage and the Cypher translation.
