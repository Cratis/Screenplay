---
id: 0148
title: Name ordered paths through a model with `storyline`, and trace a value's lineage both ways from any property or screen field
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-11-09
decider: Accepted Screenplay#1189 and Screenplay#1190 (delegated)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Tool/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

## Context

A user journey such as "a customer returns an item" crosses several modules, features and slices. A model could not name that
path, so it could not be kept with the model, reviewed, exported or drawn by a board (Studio, Cratis/StudioIssues#1369).

`trace-lineage` (decision 0099) answered only "where does this read-model property come from?". Studio's lineage view
(Cratis/StudioIssues#1364) starts from any field a person points at: a command input, an event property, a read-model property or
a field on a screen, and asks both where the value comes from and where it goes. The CLI had no lineage at all.

## Decision

1. **A top-level `storyline` declaration.**

   ```screenplay
   storyline ReturnAnItem "A customer returns an item"
     description "From the request to the refund"
     command RequestReturn "The customer asks"
     event ReturnRequested
     readmodel PendingReturn
   ```

   A name, an optional title and description, and ordered steps. Each step is `<kind> <reference> ["<note>"]` with kind `module`,
   `feature`, `slice`, `command`, `event`, `readmodel`, `query`, `screen` or `reaction`. A storyline is a language construct rather
   than a model-notes sidecar (decision 0045) so that it is reviewed, diffed and exported with the model it describes.
2. **A step is a typed reference.** It resolves by kind from the application root like a qualified dependency, so a command and a
   query of the same name never collide. Renames and moves rewrite steps through the same reference bindings as every other
   reference; correspondence across an edit is by kind and text, not position, so reordering steps never rebinds them.
3. **Authoring metadata, outside the executable model.** No ESM version, no canonical bytes. A model without storylines compiles
   as before.
4. **Diagnostics.** `PLAY0940` malformed declaration, `PLAY0941` malformed step or a nested line, `PLAY0942` (warning) a step that
   names nothing, `PLAY0943` repeated name; an ambiguous step reports `PLAY0198`. A stale storyline warns rather than fails, because
   it never changes what runs. The TypeScript compiler reports the parse diagnostics; the C# compiler reports all of them.
5. **MCP `list-storylines`** reads storylines with every step resolved to an element `declaration-details` accepts. Editing goes
   through `propose-ast` (`StorylineSyntax`, `StorylineStepSyntax`) and `propose-source`, which already guard references.
6. **`trace-lineage` accepts any property or a screen field, and a `direction`.** The subject is a command, event or read-model
   property, or a screen field addressed as the screen's or form's address followed by the bound property. `direction` is
   `upstream`, `downstream` or `both` (default). Downstream reuses the mapping links forward; the hop shape, roles, break statuses
   and paging of decision 0099 are unchanged, with one new role, `screen-field`, for a field subject. A command with a handler is an
   `opaque` break downstream, as it is upstream.
7. **`screenplay lineage <address> [<folder>]`** prints the same trace as text or JSON, exiting 0 complete, 1 with breaks, 2 could
   not run.

## Consequences

- Studio can store, list and edit storylines through the language and MCP, and start lineage from any field it shows.
- A read-model property traced with the default `both` gives the same hops as before: its readers, then its sources. Callers that
  only want sources pass `direction: upstream`.
