---
id: 0052
title: Declare one caller detail shape as authoring metadata before executable admission
status: accepted
stage: implemented
decided: 2026-10-09
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Compiler/**
  - Source/Screenplay/Monaco/**
  - Source/Screenplay/VSCodeExtension/**
  - Documentation/screenplay/identity.md
  - Samples/Invoicing/**
---

## Context

[The ruling on #119](https://github.com/Cratis/Screenplay/issues/119#issuecomment-6090476382)
separates the caller expression root from declaring additional caller details. Token built-ins already
have executable equivalents. Additional sources have no admitted portable runtime contract yet. This
record preserves the step-2 ruling without claiming the executable admission reserved for #600.

## Decision

An application declares at most one top-level `identity` block, separate from `authentication`. Each
identity detail has a unique name, a type and exactly one source: a claim, a keyed single-result query
with `by`, an opaque inline implementation or a file. Built-ins cannot be redeclared. Query keys use
only token built-ins, claims or literals, never other details; query authorization cannot depend on
additional details. Result types and optionality match the detail. The block is authoring metadata:
it survives syntax transport, validation, printing, editing and folder layout but is absent from the
ESM. Executable reads of `$identity.<detail>` refuse with `PLAY0268` naming #600. `$context.identity`
stays built-ins only, and `scoped to` stays an opaque name, not a detail reference.

## Options considered

- Per-provider detail sets: not taken. The application has one caller shape, and the runtime provider
  context does not carry a provider discriminator. A later source qualifier can be additive.
- Resolve `scoped to` against detail declarations: not taken. It would break existing opaque scopes.
- Permit dependent query sources or refresh/cache bodies: not taken. They introduce resolution cycles
  or deployment/runtime decisions into the application model.
- Add ESM fields or run the escapes now: not taken. Admission needs its own accepted runtime contract.

## Default if unanswered

The accepted ruling already supplies the default: retain token built-ins as executable, additional
caller details as authoring metadata. Consumers cannot execute those detail sources; #600 owns the
admission decision and its cost.

## Timeline and scope

Holds until superseded by an admission record. In scope: C# and TypeScript syntax, parser, walker,
validators, binder refusal, printer, AST mirrors and contract, folder layout, MCP navigation, editors,
documentation and living samples. Out of scope: ESM members or versions, the Contexts runtime contract,
reference execution, Stage providers, source refresh/caching and per-provider shapes.

## Verification

**Done when:** all sources round-trip, folder assembly has one block, declared paths resolve without
PLAY0155, undeclared paths still warn, metadata preserves executable bytes and executable detail reads
refuse with PLAY0268 naming the detail and #600.

**Verify by:** identity parser/validator, printer, walker, folder and binder specifications; shared
syntax and diagnostic conformance vectors; editor specifications; the CI-equivalent Tier 1 gate.

## Consequences

Authors can describe caller data without confusing source validity with runtime support. Renderers
must read syntax metadata and cannot infer details from an ESM. Details are unavailable to executable
clauses until #600 is admitted.
