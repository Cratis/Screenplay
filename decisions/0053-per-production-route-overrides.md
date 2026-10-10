---
id: 0053
title: Replace the command route per event production
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

[#302](https://github.com/Cratis/Screenplay/issues/302) needs events from one command to land in different streams. Decision [0036](0036-admit-event-sources-streams-and-command-routes.md) classifies every command production with one route. Arc's per-event wrapper override is available, but its sentinel fallback cannot clear an inherited stream id.

## Decision

A command event production may declare one `stream Source.Stream` with the command route's scalar or composite `streamId` mappings. It replaces the source, stream and stream id together. Other productions keep the command route. An override does not supply `for`, and its destination type must match its source's identifier type. The command route's destination check excludes overridden productions.

Plain, conditional and inline event productions accept this form. A line containing `=` remains payload, including a payload named `stream`. Reaction and refusal-branch productions refuse it with PLAY0636. Duplicate or invalid routes use PLAY0504. A route identical to the command route warns with PLAY0637 and has a typed removal repair.

All routes resolve eagerly after validation and requirements, before generation. A formatting failure in a skipped production rejects the command atomically as Contract. Mapping inputs follow 0036: direct required non-generated scalar command inputs or literals. Paths remain refused with PLAY0268 (#574); generated inputs use PLAY0273.

An unkeyed override under a keyed command is valid portable semantics. Stage must refuse it until its Arc adapter can prevent sentinel inheritance. Handler commands retain their command route.

## Options considered

- Partial metadata inheritance: rejected because the authored route would not describe the complete fact route.
- `no stream` override: rejected because Arc cannot express clearing metadata.
- Lazy conditional route resolution: rejected because conditions can depend on generated values and would change the route phase.
- Routes on reaction direct productions: deferred. Reactions have no command route and 0036 keeps their productions unrouted.

## Default if unanswered

Keep these forms at the status the implementing pull request merges them in. Acceptance remains a human verdict. Consumers do not gain support from a package update alone.

## Timeline and scope

Holds until superseded. This increment covers production classification and routing only. Occurred-at routing, concurrency flags and stream closing are outside it.

## Verification

**Done when:** both compilers, printing, repairs and rename agree; runtime overrides replace the whole route; a skipped production's formatting failure leaves the world unchanged.

**Verify by:** the production-route parser, compiler, binder and evaluator specifications, v10 golden, and `EventRoutesCorpus.ProductionRoutesV10` across its four source forms.

## Related

Builds on 0036 and 0033. Admission is recorded in [0055](0055-admit-production-routes-and-observer-filters-as-esm-v10.md).
