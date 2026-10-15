// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts;

/// <summary>
/// Represents the closed context supplied to a ValueGeneration implementation.
/// Inputs, caller identity and random state are not part of this contract.
/// </summary>
/// <param name="Now">The generation instant. Date segments are interpreted in UTC.</param>
/// <param name="Tenant">The portable tenant, translated by the provider at the context boundary.</param>
/// <remarks>
/// Providers map their default namespace to <see cref="TenantId.Default"/> and reject ambiguous named-tenant
/// mappings. A value derived from command input is not generation and needs a separate derivation contract.
/// <para>
/// The normative version-1 context is the language-neutral descriptor of Now, Tenant and a String result. Each
/// language provider binds it to its own native type, and the provider, not the fence's language tag, decides the
/// binding. The Cratis C# target (Stage, Arc) binds Cratis.Concepts.GenerationContext from Fundamentals. This record
/// stays BCL-only and is name-compatible with that type, not type-identical.
/// </para>
/// <para>
/// A body that uses only Now, the tenant in string formatting and BCL types is portable. Passing the context to
/// Fundamentals APIs, naming GenerationContext or reading the tenant's Value is bound to one binding.
/// </para>
/// </remarks>
public record GenerationContext(DateTimeOffset Now, TenantId Tenant);
