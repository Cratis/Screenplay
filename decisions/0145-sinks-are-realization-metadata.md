---
id: 0145
title: Named sinks for read models and graphs are realization metadata outside portable ESM
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-11-08
decider: Accepted Screenplay#1182 design (delegated)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

A model could not say where a read model or graph is stored, so every one went to the single sink the runtime defaults to. Applications need to keep reporting read models in a separate database or two graphs apart. Following [0135](0135-passive-read-models-are-realization-metadata.md), the physical store (MongoDB, PostgreSQL, SQL Server, Neo4j) is a deployment concern. [Screenplay#1182](https://github.com/Cratis/Screenplay/issues/1182) specifies logical sinks that a deployment maps to storage.

## Decision

1. **Defaults need no declaration.** Every application has an implicit default sink for read models and one for graphs. A read model or graph that names no sink uses its kind's default, so existing models, their syntax JSON and their ESM are unchanged.
2. **A named sink is declared once, at the application root, on one line:** `sink <Name> for readmodels` or `sink <Name> for graphs`. It is `ApplicationSyntax.Sinks` (`SinkSyntax` with `Name` and `Stores`, a `SinkKind`). The member is `Stores` rather than `Kind` so its syntax JSON is `stores`, not the discriminator-avoiding `syntaxKind`. Folder merge concatenates the declarations of every file; the validator reports duplicates.
3. **A read model or graph chooses one with a bare body directive** `sink <Name>`, like `passive`. `ReadModelSyntax.Sink` and `GraphSyntax.Sink` are nullable init properties, preserving constructor and `Deconstruct` signatures. Syntax JSON omits all three members when empty.
4. **`sink` stays contextual in a read model.** A body line is the directive only when it is exactly `sink` followed by one identifier that is not a primitive type. `sink String`, `sink Money[]`, `sink Money optional` remain properties named `sink`; `@sink Money` declares one of any type, and the printer escapes every property named `sink` that way. A graph body has no bare properties, so there every `sink` line is the directive.
5. **Projections, reducers and queries follow their target.** Nothing else in the language names a sink.
6. **Names are unique application-wide, compared ordinally ignoring case.** A directive must name its sink with the exact declared spelling; a case-only mismatch is an unknown sink. This is the conservative reading: uniqueness protects case-insensitive stores, and exact references keep rename and binding unambiguous.
7. **Diagnostics** (permanent codes):

   | Code | Severity | Reported by |
   | --- | --- | --- |
   | `PLAY0916` malformed declaration, nested line, or malformed directive | Error | both parsers |
   | `PLAY0917` duplicate sink name (ignoring case) | Error | both validators |
   | `PLAY0918` unknown sink on a read model or graph | Error | both validators |
   | `PLAY0919` kind mismatch (`for graphs` on a read model, `for readmodels` on a graph) | Error | both validators |
   | `PLAY0920` `sink` together with `passive` | Error | both parsers |
   | `PLAY0921` two different `sink` directives on one owner | Error | both parsers |
   | `PLAY0922` repeated identical `sink` directive | Warning | both parsers |
   | `PLAY0923` declared sink nothing names | Warning | both validators |

   The issue listed no code for a malformed line; `PLAY0916` was added for it so a malformed declaration is never silently dropped. A conflicting repeat keeps the first sink. A directive naming a sink of the wrong kind still counts as a use, so it reports only `PLAY0919`.
8. **Realization metadata, not ESM.** The binder reports Information `PLAY0270` for each declaration and directive. The choices are exposed on `SemanticCompilation` as `Sinks` (`ImmutableArray<SemanticSink>`, name and `SemanticSinkKind`, declaration order), `ReadModelSinks` (`ImmutableDictionary<SemanticId, string>`, canonical read-model identity to sink name) and `GraphSinks` (`ImmutableDictionary<string, string>`, graph name, the graph's canonical identity in ESM, to sink name). There is no ESM member, version change or changed canonical byte or revision. Every sidecar-copying operation preserves them (`WithSidecarsOf`).
9. **Renderers must hash these members into render input**, since toggling a sink leaves the ESM revision unchanged. Runtime consumers carry them through application realization; a portable ESM alone cannot recover them. Stage carries the names to Chronicle (Cratis/Chronicle#5735), and Showrunner's Stage Manager maps them to databases.
10. **Rename** of a sink renames its declaration and every directive naming it; a new name equal to another sink ignoring case is refused.

## In scope

Both compilers (parsers, validators, syntax JSON and schema), the C# binder and `SemanticCompilation`, the printer, folder merge, rename and identifier spans, the authoring index, Monaco (tokens, keyword hover, completion), the VS Code TextMate grammar, MCP descriptions, `grammar.md`, the `sinks` page and diagnostics catalogue, conformance vectors, and the Invoicing and Organization samples.

## Alternatives and consequences

Physical store names in the model were rejected: the same model must run against different storage per deployment. A portable ESM member was rejected because this is realization metadata, not execution semantics, and would bump the ESM for every consumer. Treating every `sink X` read-model line as the directive was rejected because it would break a property named `sink` of a primitive type; the remaining edge, a property named `sink` of a concept type written without `@`, now reports `PLAY0918`; the documentation of that code says to write `@sink`.

## Downgrade path

Older tools reject syntax JSON that carries `sinks`, `sink` or `SinkSyntax`, because their reader refuses unknown members. Before downgrading, remove the sink lines or re-export the model with a version that omits them. ESM bytes are unchanged, so only syntax JSON and `.play` source carry the incompatibility.
