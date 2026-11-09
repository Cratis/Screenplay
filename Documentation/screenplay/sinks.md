---
title: "Cratis Screenplay sinks: choose where read models and graphs are stored"
description: "Every Screenplay read model and graph uses a default sink. Declare named sinks to keep reporting read models or graphs apart and map each to storage."
---

# Sinks

A sink is where a read model or a graph is stored. A Cratis Screenplay model never names a database. It names **logical sinks**, and the deployment maps each name to real storage, such as a MongoDB, PostgreSQL or SQL Server database for read models, or a Neo4j database for graphs.

Use a named sink when part of the state belongs somewhere else: reporting read models in a separate database, or one graph kept apart from another. A model that names no sinks needs nothing from this page.

## Default sinks

Every application has two implicit sinks: one for read models and one for graphs. A read model or graph that names no sink uses the default for its kind. You never declare a default sink, so every existing model keeps its meaning.

## Declare a named sink

Declare each named sink once, at the application level, with the kind of thing it stores:

```screenplay
sink Reporting for readmodels
sink People for graphs
```

A sink declared `for readmodels` stores read models only, and one declared `for graphs` stores graphs only. Sink names are unique across the whole application, compared ignoring case: `Reporting` and `reporting` cannot both be declared.

## Choose a sink

A read model or a graph chooses a sink with a bare `sink <Name>` line in its body, the same way a read model says [`passive`](readmodels.md#passive-decision-state):

```screenplay
sink Reporting for readmodels

concept Month : String
concept Money : Decimal

module Finance
  feature Revenue
    slice StateView MonthlyRevenue
      readmodel MonthlyRevenue
        sink Reporting
        month Month
        total Money
```

```screenplay
sink People for graphs

concept PersonId : Uuid

module Organization
  feature Chart
    slice StateView People
      graph Organization
        sink People
        node Person
          personId PersonId key
```

The line names the sink exactly as it is declared, and may sit anywhere among the body's directives and properties.

Nothing else in the language names a sink. Whatever builds or reads a read model or graph follows it:

- A projection or reducer writes to its read model's sink, and a graph projection writes to its graph's sink.
- A query reads from its read model's sink, and a graph query reads from its graph's sink.

### A property named `sink`

A read model has always been able to declare a property called `sink`, and it still can. A `sink <Name>` line in a read model is the directive only when `<Name>` is a sink the application declares, spelled exactly as declared. Otherwise it is the property it always was: `sink Customer` declares a property `sink` of type `Customer` when no sink is called `Customer`. Where the sink is declared does not matter; a sink declared in another file of the folder counts once the files are merged.

A line is always a property when it cannot be the directive: an escaped name (`@sink Reporting`), a collection (`sink Reporting[]`), an optional type or a modifier. Use `@sink <Type>` when you want a property whose type has the same name as a sink; the printer writes it that way too. A property whose type differs from a declared sink only in case, such as `sink reporting` beside `sink Reporting for readmodels`, keeps its meaning and warns (`PLAY0939`).

In a graph every `sink` line is the directive, since a graph body has no properties of its own, and naming a sink that is not declared is an error.

### Passive read models have no sink

A [`passive`](readmodels.md#passive-decision-state) read model is never materialized: it is replayed for command decisions. It has nowhere to be stored, so naming a sink on it is an error.

## How a deployment maps names to storage

The model says which sink each read model or graph belongs to; the deployment says what each sink is. The application host binds every declared sink name to a store of the right kind, the same way it supplies the [environment](environments.md) values a model needs, and binds the two default sinks to its default stores. Moving the reporting read models to another database changes the deployment, not the model.

A sink is **realization metadata**, not part of the executable semantic model (ESM). Adding, removing or changing a `sink` line leaves the ESM bytes and its revision unchanged. The compiler reports each `sink` with the information code `PLAY0270`, and a compilation exposes the choices beside the ESM:

| `SemanticCompilation` member | Type | Holds |
| --- | --- | --- |
| `Sinks` | `ImmutableArray<SemanticSink>` | Each declared sink's `Name` and `Kind` (`ReadModels` or `Graphs`), in declaration order. The defaults are never listed. |
| `ReadModelSinks` | `ImmutableDictionary<SemanticId, string>` | The sink name of every read model that names one, keyed by the read model's canonical identity. |
| `GraphSinks` | `ImmutableDictionary<string, string>` | The sink name of every graph that names one, keyed by the graph's name. |

Because the ESM revision does not change when a sink changes, a renderer must include these members in its hashed render input, or a changed sink would not cause a new render.

In the syntax tree, `ApplicationSyntax.Sinks` holds the declarations (`SinkSyntax` with `name` and `stores`), and `ReadModelSyntax.Sink` and `GraphSyntax.Sink` hold the chosen name. Syntax JSON omits all three when they are empty, so models without sinks serialize exactly as before. Older tools reject syntax JSON that carries them, because their reader refuses unknown members.

## Rename a sink

Renaming a sink with `propose-rename` renames its declaration and every `sink` line that names it, in every file of the model, including read models in other files than the declaration. A `sink <Type>` line that names no declared sink is a property and is left alone. `find-references` finds the same lines. A new name that matches another sink ignoring case is refused.

## Diagnostics

| Code | Severity | Meaning |
| --- | --- | --- |
| `PLAY0931` | Error | A sink declaration or a graph's `sink` line is malformed, or a declaration has lines under it. |
| `PLAY0932` | Error | Two sinks share a name, compared ignoring case. |
| `PLAY0933` | Error | A graph names a sink that is not declared. A read model's line naming no declared sink is a property. |
| `PLAY0934` | Error | A read model names a `for graphs` sink, or a graph names a `for readmodels` sink. |
| `PLAY0935` | Error | A `passive` read model names a sink. |
| `PLAY0936` | Error | A read model or graph names two different sinks. |
| `PLAY0937` | Warning | A read model or graph repeats the same `sink` line. |
| `PLAY0938` | Warning | No read model or graph names a declared sink. |
| `PLAY0939` | Warning | A read model's property `sink` has a type that differs only in case from a declared sink. |

Both the C# and the TypeScript compiler report all of them. See the [diagnostics catalogue](diagnostics.md#sinks).
