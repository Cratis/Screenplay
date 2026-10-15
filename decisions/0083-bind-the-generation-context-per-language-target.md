---
id: 0083
title: Bind the version-1 generation context to each language target's native type
status: accepted
stage: none
decided: 2026-10-15
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay.Contexts/**
  - Source/DotNET/Screenplay/Semantics/SemanticTypedContextCatalog.cs
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/Monaco/**
  - Documentation/screenplay/context.md
  - Documentation/screenplay/generated-values/strategies.md
---

## Context

[Screenplay#799](https://github.com/Cratis/Screenplay/issues/799) found that a `generate rule` body receives `Cratis.Screenplay.Contexts.GenerationContext(DateTimeOffset Now, TenantId Tenant)`, a record class in the slim `Cratis.Screenplay.Contexts` package. Fundamentals has a different type, `Cratis.Concepts.GenerationContext`, a `readonly record struct (DateTimeOffset Now, string Tenant)` with a `DefaultTenant` constant, shipped since Fundamentals v7.24.0. Every `GenerateValue.*(context)` overload takes the Fundamentals type. A body that passes `context` to a Fundamentals API therefore compiles in Arc-generated code and not against Screenplay's type. Arc already recovers a bare built-in call such as `return GenerateValue.Ulid(context);` as `generate ulid`; a compound or otherwise opaque body, such as `return $"{context.Tenant}:{GenerateValue.Ulid(context)}";`, would have to be recovered as a `generate rule`, and until this is settled Arc withholds such bodies with SP0073 ([Arc#3116](https://github.com/Cratis/Arc/issues/3116)).

[Decision 0054](0054-concept-generation-strategies-and-freshness.md) item 9 gave `ValueGeneration` a closed context of `Now` and the portable `TenantId`, and says these are the same names as Arc's context, which Arc adapts. [Decision 0012](0012-typed-context-descriptor-and-command-handler-role.md) makes the typed-context descriptor language-neutral, with providers generating the typed code. The descriptor for `ValueGeneration` lists `Now` as a `DateTime` runtime token and `Tenant` as a `TenantId` runtime token, and the catalog requires context/result version 1 (`SemanticTypedContextCatalog.cs`). Descriptors are sidecars: canonical ESM bytes and revisions do not include them (`Documentation/screenplay/context.md`).

[Decision 0019](0019-publish-the-context-family-separately.md) requires the context package to depend on the BCL only, and `verify-package.py` asserts an empty dependency set for every target framework. Fundamentals references eight packages, including `Microsoft.Extensions.DependencyInjection` and `System.Reactive`. [Decision 0002](0002-implementation-attachments-envelope-and-reducer-role.md) keeps Screenplay free of Roslyn: it cannot compile a body and enforces no API allowlist, so each provider checks bodies against its own types. The reference runner never invokes a rule body.

## Decision

The normative version-1 `GenerationContext` is the language-neutral descriptor: `Now` (an instant), `Tenant` (the portable `TenantId`) and a `String` result. Each language provider binds that descriptor to its target's native type. Screenplay does not choose one runtime type for all targets.

- **Cratis C# target (Stage `Rendering.Cratis`, and Arc).** The binding is `Cratis.Concepts.GenerationContext` from Fundamentals. The generated wrapper hands the body a Fundamentals context, so `GenerateValue.*(context)` compiles unchanged.
- **`Cratis.Screenplay.Contexts.GenerationContext`.** It stays BCL-only, with its current shape and namespace. It is name-compatible with the Fundamentals type (`Now`, `Tenant`), not type-identical. It remains the typed shape for tooling and for hosts that do not use Fundamentals.
- **Portable C# subset.** A body that uses only `context.Now`, the tenant in string interpolation or string assignment, and BCL types means the same on both types. String formatting of the tenant relies on [#798](https://github.com/Cratis/Screenplay/issues/798).
- **Target-bound C#.** A body that passes `context` to `GenerateValue.*`, names `GenerationContext` or `GenerationContext.DefaultTenant`, constructs a context, or reads `context.Tenant.Value` is bound to one binding. Passing `context` to Fundamentals APIs is valid only on the Cratis C# target.
- **Neutrality.** The selected provider, not the fence's language tag, determines the binding: a `csharp` tag does not distinguish the Cratis C# target from a C# host using the Screenplay record, and file attachments carry no tag. Each provider compiles or admits a body against its own binding and rejects a body that does not fit it. A TypeScript or Kotlin provider binds the same descriptor to its own types. No manifest change is needed for this.
- **No contract change.** ESM bytes, golden vectors, the attachment manifest and the descriptor stay as they are, and the context version stays 1. The change is in documentation and provider behavior.

## Options considered

1. **Per-target binding of the neutral descriptor (chosen).** It follows decision 0012, keeps 0019 intact, changes no bytes, and makes the Cratis C# case compile.
2. **A. Screenplay.Contexts uses the Fundamentals type directly.** Rejected. The package would take Fundamentals' dependencies and fail the empty-dependency assertion of 0019. It would also tie every target to a C# package and drop `TenantId`, whose `Default` and `NotSet` values providers translate to. The record class would also change shape, which breaks binaries compiled against it (0019 forwarding).
3. **B. Mirror the Fundamentals shape.** Rejected. A mirror in the namespace `Cratis.Concepts` with the same name makes a project referencing both packages hit CS0433, and a mirror in any other namespace still fails to compile `GenerateValue.Ulid(context)` because the overload takes Fundamentals' type. Changing the existing type's `Tenant` to `string` or the type to a struct breaks published binaries and the spec that pins `Tenant` as `TenantId` (`for_GenerationContext`).
4. **C. A bridge (conversion or helper).** Rejected. A body would need extra text, such as a conversion call, on one target only, so the same body would not mean the same everywhere. Screenplay never rewrites opaque bodies (`context.md`), and the bridge could not live in the BCL-only package.
5. **D. Document as unsupported.** Rejected. Arc would keep SP0073 for the case [Arc#3116](https://github.com/Cratis/Arc/issues/3116) defines as self-contained, which includes `GenerateValue` calls with `context`, and rule bodies would stay file references.

## Default if unanswered

The docs keep presenting the Screenplay record as the one type bodies receive. Arc keeps SP0073 for bodies that pass `context` to `GenerateValue`. Stage has no stated binding for the wrapper, so it chooses privately, and Cratis C# bodies and Screenplay's documented examples can disagree.

## Timeline and scope

Settle this before Arc#3116 lifts SP0073 for these bodies. It holds until superseded.

In scope: the binding rule, the portable and target-bound subsets, the Screenplay documentation and editor wording, and consumer follow-ups.

Out of scope: a new context version or ESM version, a Fundamentals dependency in `Cratis.Screenplay.Contexts`, any bridge or helper API, TypeScript or Kotlin bindings (each provider owns its own), checking bodies inside Screenplay (0002), input-derived generation (#308), and changes to other roles' contexts.

## Verification

**Done when:** the documentation states the descriptor as normative and lists the Cratis C# binding, with the portable and target-bound subsets. The `strategies.md` rule example uses a compound body such as `return $"{context.Tenant}:{GenerateValue.Ulid(context)}";` and is marked as Cratis C# target-bound. The `GenerationContext.cs` remarks, the Monaco hover and completion text, and the MCP `syntax-schema` description say the same. `Cratis.Screenplay.Contexts` still has no dependencies, canonical ESM bytes and golden vectors are unchanged, and the context version is 1. Stage hands a rule body a `Cratis.Concepts.GenerationContext`, and Arc lifts SP0073 for self-contained bodies.

**Verify by:** `verify-package.py` and the `for_ContextPackage` and `for_GenerationContext` specs pass unchanged; the golden and corpus comparisons show no byte or revision change; a documentation search finds no text claiming bodies always receive the Screenplay record type; a Stage render of the compound `generate rule` above compiles against Fundamentals with the body unchanged; the Arc#3116 spec recovers that body as an inline `generate rule` with unchanged content instead of reporting SP0073; and equivalent-input specs show the portable subset yields the same values on both bindings for the default, a named and an absent tenant (after #798).

## Consequences

Cratis C# rule bodies can use Fundamentals' generation APIs unchanged, and the context package stays slim. Other targets stay possible because the contract is the descriptor and not a C# type.

A rule body is only portable across targets when it stays in the portable subset, and authors must know which uses are target-bound. A name-compatible type can drift from the Fundamentals type, so the pair needs a conformance check.

Screenplay changes: `Documentation/screenplay/context.md` (a binding table), `Documentation/screenplay/generated-values/strategies.md` (a compound `GenerateValue.Ulid(context)` example), the `GenerationContext.cs` remarks, `Source/Screenplay/Monaco/screenplay-language/hover-content.ts` and `completion-planner.ts`, and the `syntax-schema` description in `Source/DotNET/Screenplay.Mcp/McpToolCatalog.cs`. [#798](https://github.com/Cratis/Screenplay/issues/798) is a prerequisite for tenant formatting.

Consumer work: Arc#3116 lifts SP0073 for self-contained compound bodies that use `Now`, `Tenant` and `GenerateValue.*(context)`, recovering them as inline `generate rule` bodies. Stage wraps `ValueGeneration` bodies with `IGeneratableInContext<T>.New(global::Cratis.Concepts.GenerationContext)`, which `SemanticCommonArtifactRenderer` already uses for other strategies. Cratis/AI corpus skills state the binding and the portable subset. Each consumer issue is filed with the implementation.

## Related issues

Screenplay: [#799](https://github.com/Cratis/Screenplay/issues/799), [#798](https://github.com/Cratis/Screenplay/issues/798), [#614](https://github.com/Cratis/Screenplay/issues/614), [#616](https://github.com/Cratis/Screenplay/issues/616). Arc: [#3116](https://github.com/Cratis/Arc/issues/3116).
