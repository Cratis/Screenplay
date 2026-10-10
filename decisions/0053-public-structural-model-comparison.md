---
id: 0053
title: Share typed structural model comparison from the Screenplay library
status: accepted
stage: implemented
decided: 2026-10-10
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Comparison/**
  - Source/DotNET/Screenplay/Indexing/**
  - Source/DotNET/Screenplay.Mcp/McpSemanticDiff.cs
  - Source/DotNET/Screenplay.Mcp/McpRevisionDiff.cs
  - Documentation/screenplay/model-comparison.md
---

## Context

[Screenplay #555](https://github.com/Cratis/Screenplay/issues/555) exposes the
structural comparison currently available through MCP to library consumers.
[CLI #259](https://github.com/Cratis/cli/issues/259) needs to compare an authored
workspace against sources generated from code, where persisted identities do
not exist. Reimplementing comparison in each consumer would diverge on event
generations, effective specification outcomes and incomplete authoring sources.

## Decision

`ModelComparison` and its typed result live in `Cratis.Screenplay.Comparison`
in the `Cratis.Screenplay` library. The required authoring index cluster moves
with the engine as internal `Cratis.Screenplay.Indexing` types. A thin internal
MCP snapshot adapter retains the existing Tool/MCP boundary without exposing
the library's indexes. MCP delegates to the engine and retains its
protocol-specific revision validation, JSON projection and paging. The existing MCP semantic-diff JSON bytes are frozen,
including field order, null emission, record ordering and limit statements.

Inputs explicitly declare whether their catalogs are authoritative:

- Identity matching applies only when both inputs have persisted identities
  and share an application identity. Different identity-bearing applications
  are incompatible and do not silently fall back.
- Address matching applies otherwise. Keys contain normalized kind and every
  typed semantic address part except Application, retaining OwnerKind and
  Generation. With two executable models, catalog addresses provide
  property-level comparison. If either is not executable, both fall back to
  exact authoring declaration keys with `DeclarationLevelOnly`.

Address matching never infers renames or owner moves: each becomes removal plus
addition. It skips document moves, exposes no semantic IDs and computes no
identity records. Identity coverage is incomplete with `IdentitiesNotCompared`.
A generated catalog's origin does not choose the mode.

Typed gap kinds, their statements and the `NotCompared` limit statements are
public contracts. Unknown coverage remains unknown; a known structural change
may still be reported when other sections are incomplete. This API does not
claim the execution/equivalence verdict defined by [decision 0013](0013-equivalence-for-screenplay-code-round-trips.md).

## Options considered

- **Library comparison with internal shared indexes (taken):** consumers get
  typed, unpaged results without depending on MCP, and MCP uses the same engine.
- **Public API in Screenplay.Mcp (rejected):** it forces compiler consumers to
  depend on a protocol server and leaves the algorithm in the wrong package.
- **Consumer-specific comparison implementations (rejected):** event,
  specification and coverage behavior would diverge.
- **Guess identities from catalog origin or infer address renames (rejected):**
  generated IDs and coincidental addresses cannot prove persisted continuity.

## Default if unanswered

The agreed design supplies the default: keep MCP's byte contract and expose
one library engine with explicit input identity authority. Without the API,
consumers remain dependent on MCP transport or duplicate comparison logic;
neither is an acceptable substitute for this scope.

## Timeline and scope

Hold until superseded. In scope: the .NET public comparison contract, shared
internal indexing, both matching modes, existing MCP compatibility, specs and
documentation. Out of scope: compiler language changes, TypeScript APIs,
canonical vectors, package version changes, execution/equivalence predicates,
code recovery, transitive/runtime impact and external implementation content.

## Verification

**Done when:** public specs demonstrate both matching modes, generation risk and
coverage, effective outcomes, direct dependants and incomplete sources; all
three characterization goldens remain byte-identical before and after the
refactor; MCP comparison/index/connection specs remain green.

**Verify by:** affected library and MCP specifications, the whole-solution
Debug test command, the Release build on net8/net9/net10 and canonical vector
specifications on supported runtimes.

## Consequences

Library consumers classify typed structural changes themselves. Address-mode
consumers can compare generated sources without pretending renames preserve
identity, but must retain incomplete identity coverage in their verdicts.
Future comparison changes must preserve the public gap vocabulary and MCP
transport compatibility rather than quietly changing consumer meaning.
