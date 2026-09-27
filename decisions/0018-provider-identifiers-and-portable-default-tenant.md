---
id: 0018
title: Use provider-generated identifiers in code bodies and keep the portable default tenant
status: accepted
stage: partially implemented
decided: 2026-09-26
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Documentation/screenplay/readmodels.md
  - Documentation/screenplay/context.md
  - Source/DotNET/Screenplay.Contexts/Contexts/**
---

## Context

Authored Screenplay property names can be camelCase, while Stage's generated C# records use PascalCase. Documentation that writes `context.Event.amount` does not compile against the generated wrapper. Screenplay's `TenantId.Default` is the zero GUID, while Arc and Chronicle use the namespace `"Default"` for the default tenant.

## Decision

Code bodies refer to provider-generated identifiers: C# providers expose PascalCase members. Screenplay retains names exactly as authored in the DSL and providers never rewrite opaque body text. Screenplay context-contract v1 retains the zero-GUID `TenantId.Default`. Providers map Arc/Chronicle's `"Default"` namespace to this portable default at their boundary. Named tenants remain distinct; an ambiguous mapping (including a named tenant colliding with the zero GUID) is rejected rather than collapsed.

## Options considered

- Use generated names and translate at the provider boundary (chosen): body code compiles without redefining the published context contract.
- Rewrite bodies to match DSL names: rejected because opaque code is not a safe syntax tree for Screenplay or its providers to rewrite.
- Change `TenantId.Default` to `"Default"`: rejected because existing clients rely on the published zero-GUID value.
- Treat every unknown or ambiguous tenant as default: rejected because it can cross tenant boundaries.

## Default if unanswered

Examples continue to fail compilation; providers either refuse tenant-aware bodies or guess a mapping, risking tenant collisions.

## Timeline and scope

Applies from context-contract v1 through a deliberate new contract revision. This record clarifies body identifiers, examples, and provider-boundary tenancy. It does not alter ESM bytes, add provider code to Screenplay, or change the DSL's property names.

## Verification

**Done when:** Documented C# body members compile against generated wrappers, body text remains unchanged, and providers translate default/unset/named tenants without conflating an ambiguous named tenant with the default.

**Verify by:** Compile the documented example with a generated C# wrapper; exercise provider tests for default, unset, named tenant and zero-GUID-name collisions and check source-mapped casing diagnostics.

## Consequences

C# authors must use generated PascalCase names in bodies even when the DSL used camelCase. Providers own explicit tenancy translation and reject collisions; the portable context contract and existing ESM stay stable.

## Status notes

**2026-09-27 — partially implemented, not verified.** Screenplay owns the portable tenant contract and the corrected C# body examples. Provider tests for default, unset, named and zero-GUID-colliding tenants, plus source-mapped casing diagnostics, are consumer follow-up work in Stage/Arc; they have not shipped in this repository. The original verification criteria remain outstanding until provider evidence is available.
