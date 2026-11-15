---
id: 0165
title: A then event asserts its tags exactly and in order, or asserts none
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-11-14
decider: Einar Ingebrigtsen (direct request, Cratis/Screenplay#1138)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/EventModels/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

Events declare `tag` lines and Chronicle appends them, but a specification could not say which tags an appended event carries. A slice that depends on tags for its dynamic consistency boundary had no executable check.

## Decision

- A `then` event takes `tag <value>` lines or a single `no tags`. The expectation is exact: the tags must equal the listed ones in order, duplicates included.
- `no tags` asserts the event carries none; a `then` with neither leaves tags unspecified, as before.
- A value is an identifier or string literal. Context expressions are resolved at append time and are refused.
- `given`, `when`, redelivery locators and event examples refuse both forms. Every misuse reports `PLAY1148`.
- The expectations are executable from executable semantic model **v55**; earlier versions keep their semantics.
- The MCP fixture-occurrence view and event-model board remain payload-and-routing views: they do not expose tag expectations. This is a display-only exclusion, not a loss of executable semantics. Read the authored specification or its ESM v55 `expectedTags` to inspect these assertions; the specification runner still enforces them. The MCP board application consumes that same event-model representation.

## Consequences

The C# and TypeScript compilers, the semantic model, runner, Monaco language service, documentation and Invoicing sample change together. A tag change on an event now fails its specifications unless they state the new tags.
