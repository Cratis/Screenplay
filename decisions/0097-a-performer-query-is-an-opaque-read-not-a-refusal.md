---
id: 0097
title: A performer query is an opaque read, not a refusal of the application
status: proposed
stage: none
class: contract
reversibility: cheap
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Documentation/screenplay/**
  - Samples/AcmeCommerce/**
---

## Context

A query with a `performer` block was refused with `PLAY0268` ("uses a performer, which is not admitted into the
executable model"). Binding is all-or-nothing for an application, so one performer query made `screenplay test`,
`cratis screenplay validate --executable` and every specification of the whole application `unbound`
(Cratis/Screenplay#862, found building the Acme Commerce sample). The language documents `performer` as realization
metadata ("a query is complete without a performer") and the executable model has no way to run it.

## Decision

1. A performer-backed query is an opaque read. Binding records it as code the model needs (`QueryPerformer`),
   reports `PLAY0270` information (`Query '<name>' is performed by code`) and leaves it out of the executable model.
2. A specification whose `then query` reads such a query is left out of the executable specifications with the same
   information. Every other declaration and specification of the application stays executable.
3. A read model a performer query returns is identified like any other (a `by` key, an explicit key, or the
   conventional `*Id` property), so it binds and can coexist with projections.
4. `scoped to`, context-sourced `by` and `filter ... from` stay refused with `PLAY0268`.
5. No new construct, ESM version or diagnostic code, and no ESM byte change: the freeze of 0092 is respected. The
   change is a bug fix of the binder's admission scope.

## Options considered

- Keep refusing. Rejected: it makes a documented construct unusable in any executable application.
- Refuse only the specifications that touch the query. Equivalent to this decision for specifications, but would still
  lose the read model and the plan entry for the query.

## Related issues

Cratis/Screenplay#862; decisions 0004, 0092.
