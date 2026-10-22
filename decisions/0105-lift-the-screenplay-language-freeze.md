---
id: 0105
title: Lift the Screenplay language freeze
status: accepted
stage: none
decided: 2026-10-22
decider: woksin
class: product
reversibility: costly
supersedes: 0092
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - decisions/**
---

## Context

Decision 0092 froze the language at ESM v28 while consumers caught up. On 2026-10-22,
woksin lifted that freeze: “We can stop the Screenplay language freeze.”

## Decision

The language freeze is lifted. New language changes no longer need an exception to 0092.
Decisions 0004 and 0066 remain intact: changed canonical bytes require a new ESM version,
and consumers explicitly admit the versions they support.

## Options considered

- Lift the freeze. Chosen by woksin so blocked models can gain language support.
- Retain the freeze and require individual exceptions. Rejected because the freeze is lifted.

## Default if unanswered

Without this ruling, 0092 would remain in force and each new admission would need an exception.

## Timeline and scope

Effective 2026-10-22 until superseded. This replaces the freeze, not the compatibility or
consumer-admission contracts, and does not itself admit a version in any downstream consumer.

## Verification

Done when 0092 is marked superseded and this record is indexed with a two-way pointer.
Verify by checking both records and decisions/README.md; language changes continue to pin
changed canonical bytes under a new version.

## Consequences

Language fixes can proceed without freeze exceptions. Consumers must still admit each new
ESM version explicitly; unsupported versions remain a visible refusal.
