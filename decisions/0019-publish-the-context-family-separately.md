---
id: 0019
title: Publish the context family in a slim package with compiler type forwarding
status: accepted
stage: implemented
decided: 2026-09-26
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay.Contexts/**
  - Source/DotNET/Screenplay/ContextTypeForwarders.cs
  - Source/DotNET/Screenplay/Screenplay.csproj
  - Screenplay.slnx
---

## Context

Generated applications that host code bodies need the public context contracts without pulling in the compiler. All contexts currently live in `Cratis.Screenplay`, so moving them naively would break binaries compiled against the old assembly.

## Decision

Publish the whole cohesive context family, including `Claim`, payload helpers and context records, in `Cratis.Screenplay.Contexts`, retaining namespaces and public signatures. The compiler depends transitively on the slim package and forwards every moved public type, preserving resolution of old assembly-qualified references. The slim package depends on the BCL only, not on the compiler, Arc or Chronicle.

## Options considered

- Move the family and forward public types (chosen): keeps one source of truth and binary compatibility.
- Copy a subset of tokens into a second package: rejected because the contracts would drift and typed helpers would remain compiler-bound.
- Break compatibility by simply moving the types: rejected for existing compiled consumers.
- Keep the family in the compiler: rejected because generated applications would retain an unnecessary compiler dependency.

## Default if unanswered

Hosts keep temporary copies or pull in the whole compiler, and their contracts can diverge.

## Timeline and scope

Apply before providers replace temporary context copies and retain forwarding while compiler consumers can reference the old assembly. In scope: project, package, type forwarding and package assertions. Out of scope: context API changes and a new publishing pipeline; existing `publish.yml` already packs all packages.

## Verification

**Done when:** The new package contains the complete context family without compiler dependencies and an old-style consumer compiled against compiler-hosted types still resolves its types, constructors, operators and helpers at runtime.

**Verify by:** Run context specs and binary/assembly-qualified compatibility tests; pack Release and inspect `.nupkg` contents and dependency groups; keep the compiler's API-compatibility pack validation active.

## Consequences

Context-only consumers can install a small runtime dependency. The compiler package pulls it transitively, and forwarding must stay until a deliberate breaking release.
