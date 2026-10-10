---
id: 0055
title: Admit production routes and observer filters as ESM v10
status: proposed
stage: none
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Contracts/**
  - Documentation/screenplay/interoperability.md
---

## Context

[0053](0053-per-production-route-overrides.md) and [0054](0054-observer-source-and-stream-filters.md) add portable meaning and canonical members. ESM v9 was released in Screenplay 4.117.0. Decisions 0004 and 0025 therefore permit the next single claimed, unreleased version, v10.

## Decision

A model using a production route or observer filter selects language and semantic version 10.0 and schemaVersion 10. Models without those forms keep their previous version, bytes, revision and outcomes.

A produced event may carry `route` as its last member, with the complete command-route shape. A reaction or reducer may carry `from: { source, stream? }`, using catalog identities. Members are omitted when unset. The strict reader refuses them below v10 and refuses production routes on reaction productions. Programmatically built models must also reject foreign streams, key-shape mismatches and destination type mismatches.

The reference evaluator resolves every route in the existing route phase, replaces each production's whole route, and applies observer filters to routed facts. This does not admit Stage rendering or any other consumer automatically.

## Options considered

- Byte-preserving extension of v9: rejected because new members and observer behavior change the contract.
- Two new versions: rejected because the same route-aware fact contract supplies both features and one increment can pin their evidence together.
- Composite read-model keys in v10: deferred to #599. They need a separate portable instance-key contract.

## Default if unanswered

v10 remains claimed, unreleased until the release checkpoint. These records remain proposed until a human accepts them. Consumers that have not admitted v10 must refuse it.

## Timeline and scope

Holds until superseded. Covers only production routes and observer source/stream filters. The other #302 increments retain their current dispositions.

## Verification

**Done when:** 0004 evidence includes a v10 golden, both source-backed corpus vectors and runtime outcomes, strict-reader refusals and byte-identical v1 through v9 goldens and outcomes.

**Verify by:** `full-esm-v10.json`, `EventRoutesCorpus.ProductionRoutesV10`, `EventRoutesCorpus.ObserverFiltersV10`, compatibility specifications, and the CI-equivalent local gates.

## Related

Builds on 0004, 0025, 0036, 0053 and 0054.
