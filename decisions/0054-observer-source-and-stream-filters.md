---
id: 0054
title: Filter reactions and reducers by event source and stream
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Documentation/screenplay/**
---

## Context

[#302](https://github.com/Cratis/Screenplay/issues/302) needs observers to distinguish the streams an event belongs to. Chronicle reactors and reducers already filter by event source type and stream type.

## Decision

A reaction or reducer may declare one leaf `from Source` or `from Source.Stream`. It selects facts by the source's stored name and, when supplied, its source-owned stream's stored name. Stream ids are never filtered. An unrouted fact never matches. Observers without a filter retain their existing behavior.

A filtered reaction has only `when Event` triggers on declared events. Invalid, duplicate, unresolved, ambiguous or child-bearing filters, including non-event triggers, use PLAY0638. The C# compiler warns with PLAY0639 when every statically known producer lies outside the filter. Effective command-production routes are known; reactions, captures and public publication are unrouted. Handler commands and foreign origins are unknown and suppress the warning.

The printer places the filter after description and documentation, before conditions and triggers or rules. Rename repairs source and stream references. A filtered opaque reducer is Unsupported when a matching observed fact reaches it, whether the fact comes from `given` history or the `when` step. Matching history cannot prove that reducer-built state is absent without executing the opaque transition.

## Options considered

- Stream-id filters: rejected because Chronicle's observer contract does not filter individual stream ids.
- Payload predicates instead: rejected because the event payload does not own routing metadata.
- Filters on projections or captures: rejected in this increment. Chronicle projections do not filter metadata, and captures have a different source contract.

## Default if unanswered

Keep these forms at the status the implementing pull request merges them in. Acceptance remains a human verdict. An observer without `from` observes the same facts as before.

## Timeline and scope

Holds until superseded. Only reactions and reducers gain the filter. Reaction identity and invokes authorization are unchanged.

## Verification

**Done when:** both compilers resolve the same filters; only matching routed facts fire reactions or reach reducer transitions; stored-name pins survive rename.

**Verify by:** observer-filter compiler and binder specifications, reaction/reducer runtime specifications, rename specifications, and `EventRoutesCorpus.ObserverFiltersV10`.

## Related

Builds on 0036. Admission is recorded in [0055](0055-admit-production-routes-and-observer-filters-as-esm-v10.md).
