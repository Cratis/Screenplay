---
id: 0092
title: The language is frozen at ESM v28
status: superseded
superseded-by: 0105
stage: none
decided: 2026-10-17
decider: Sindre Wilting (delegated to the L0 coordinator)
class: product
reversibility: cheap
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/Compiler/**
  - Documentation/screenplay/**
  - decisions/**
---

## Context

Between ESM v1 and v28 the language grew quickly: protected command reads (0085), composite read-model keys (0088),
date and Uuid comparisons (0087, 0090) and many more. Each version must be admitted again by every consumer: the Stage
executor, host and renderer, the Arc generator, the `cratis` CLI, the AI skills and Studio. They have fallen behind. A
construct that Screenplay admits but Stage refuses or Studio cannot round-trip is worse for a user than one that does not
exist: the model compiles, and then Play, code generation or publication fails.

The language can now represent the models we build. The next gains for users come from making what exists work end to
end, above all in Studio: modelling, Play, code generation and publication.

## Decision

The Screenplay language is frozen at executable semantic model (ESM) version 28.

1. No new ESM versions and no new language constructs, keywords or grammar.
2. Screenplay work continues on:
   - bugs, including semantic rulings that define behaviour an admitted version already has (for example 0091);
   - diagnostics: clearer messages, typed arguments (0086), specific codes and repairs;
   - the MCP server and editor tooling;
   - host APIs that remove logic Stage, Arc or Studio would otherwise duplicate (for example 0089).
3. Records that propose new constructs stay `proposed` (parked). They are not rejected; they wait for the freeze to end.
4. Exception: a construct may be admitted only when a real model is blocked and no reasonable model-side alternative
   exists. The request names the model and the alternatives tried, and the decider accepts it in its own record.
5. Consumers catch up to v1-v28: Stage conforms fully (every admitted construct runs and renders, or a typed refusal is
   tracked as a bug), and the Arc generator, the CLI, the AI skills and Studio admit and round-trip everything up to v28.
   Catch-up is measured by the canonical corpus conformance gates, starting with Stage
   (https://github.com/Cratis/Stage/issues/491). Stage runs one engine, the executable semantic model; its separate
   event-model path is removed (https://github.com/Cratis/Stage/issues/23).

## Options considered

- Keep adding constructs while consumers catch up. Rejected: the gap grows faster than it closes, and every new version
  multiplies admission work across five consumers.
- Freeze per consumer (each picks its own ceiling). Rejected: a model that works in one tool and not another is the
  failure the freeze exists to remove.

## How it ends

The freeze ends with a superseding record from the decider, once the catch-up in item 5 is done or a blocked real model
justifies reopening the language. Until then, item 4 is the only path to a new construct.

## Consequences

- Diagnostic codes, MCP tools and repairs may still be added; ESM bytes for v1-v28 models do not change.
- The AI skills cite this record when an agent asks for a construct that does not exist: model it with what exists.
