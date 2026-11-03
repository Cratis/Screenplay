---
id: 0129
title: Projections and read models may opt in to explicit per-reference event generations; bare names keep their meaning
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-10-29
decider: woksin (ruling on #642)
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
---

## Context

[#642](https://github.com/Cratis/Screenplay/issues/642) asks whether a projection or read model can refer to a specific event generation. Today such a reference uses the event's bare name, which means its current (highest) generation ([0011](0011-event-generations.md), [0015](0015-event-generations-in-the-executable-model.md)). So a projection cannot stay on an older generation once a newer one is registered.

Chronicle can now deliver events pinned to a generation (Cratis/Chronicle#4747, part of Cratis/Chronicle#4710). Its projection definition language parses PDL with the Screenplay package, which rejects a form such as `from PersonRegistered+1`. Until Screenplay decides, Chronicle uses this rule for opted-in projections: a bare name pins the latest generation registered at registration time, and explicit per-clause generations exist only through the .NET projection builders. Stage accepts only generation 1 today and refuses multi-generation models (Cratis/Stage#335).

## Decision

This record writes down the owner's ruling. It adds no design beyond it. The ruling, quoted verbatim from [woksin on #642, 2026-10-29](https://github.com/Cratis/Screenplay/issues/642#issuecomment-6503427798):

> Decision: Support opt-in explicit per-reference event generations in projections and read models, introduced through a new decision and ESM version; bare names keep their current semantics.
>
> Reason: This adds multi-generation selection without changing existing models, validated against historical shapes.
>
> Next: Write the decision record first. Stage keeps refusing multi-generation models until then (Stage#335).

So:

1. A projection or read model may name the generation of an event it references. The form is opt-in and per reference.
2. A bare event name keeps the meaning that 0011 and 0015 give it. Existing models do not change.
3. The form is introduced through a new ESM version. The version number is allocated when the form is admitted ([0025](0025-allocate-esm-v7-to-responses-and-number-later-versions-at-admission.md)).
4. Stage keeps refusing multi-generation models until it supports this form (Cratis/Stage#335).

## Open for the implementing slice

The ruling does not fix the concrete syntax, the ESM representation or the reference-evaluator semantics. The issue lists these as the work that follows a "yes". The issue asks for them to be written in a decision record, so they are settled in a follow-up decision record before the form is admitted, within items 1 to 4, and checked against historical shapes as the ruling's reason requires.

## Timeline and scope

In scope: the generation-qualified reference in projections and read models, its ESM version, and the events and projections language reference. Downstream: Chronicle's PDL path picks the form up once the package supports it, and Stage renders it or keeps refusing it (Cratis/Stage#335).

## Verification

Done when the opt-in form compiles in both compilers and maps to the new ESM version, bare names compile to unchanged canonical bytes, and the language reference documents it.

## Consequences

Models that need to stay on an older generation during a rollout can say so. Models that do not opt in are unaffected. Until Screenplay ships the form, Chronicle's registration-time rule stands for opted-in projections.
