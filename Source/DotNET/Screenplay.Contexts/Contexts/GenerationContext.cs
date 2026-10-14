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
/// </remarks>
public record GenerationContext(DateTimeOffset Now, TenantId Tenant);
