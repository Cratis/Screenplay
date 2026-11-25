---
id: 0170
title: Graph projections name their declaring slice by semantic identity
status: accepted
stage: none
class: contract
reversibility: costly
decided: 2026-11-24
decider: Sindre Alstad Wilting (direct implementation request, Cratis/Screenplay#1375)
applies-to:
  - Source/DotNET/Screenplay/Semantics/**
  - Source/DotNET/Screenplay.Conformance/**
  - Documentation/screenplay/**
---

## Context

Graph projections are stored at application scope. Their names and graph names do not identify the slice that declared them, so authoring tools cannot attach a projection to its owner without guessing.

## Decision

- `SemanticGraphProjection.DeclaringSlice` is an optional `SemanticId`, assigned from the declaring slice's existing identity catalog entry. It is not a display name or a newly invented identity.
- A newly bound graph projection carries this identity and selects language and semantic version **60.0**. Canonical JSON appends `declaringSlice` after `edges` only when it is present.
- The identity must resolve to a slice, including slices in nested features and the default module. A dangling identity or an identity of another declaration is invalid.
- Models before v60 omit the member and read it as null. Readers do not infer an owner, and serializers refuse to write a scoped projection as an older version. A v60 model must contain at least one scoped graph projection.
- Normalization treats `declaringSlice` as an identity reference. Reassigned identities normalize equivalently; moving a projection to another slice changes its meaning. Unknown references fail closed.
- Slice ownership is the complete scope reference: separate read-model, module, feature and slice-name fields are not added. The slice identity retains its ancestry through the ordinary semantic model.

## Consequences

Published v1–v59 golden and corpus bytes remain unchanged. New v60 schemas and golden bytes demonstrate the contract. This adds no Screenplay syntax, diagnostic, execution rule or MCP response; the MCP ownership consumer is separate work.
