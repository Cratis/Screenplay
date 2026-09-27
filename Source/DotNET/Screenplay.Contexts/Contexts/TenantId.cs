// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Contexts;

/// <summary>
/// Represents the portable identifier of the tenant a code body is executing for.
/// Providers translate their runtime tenant or namespace at the context boundary, rejecting ambiguous mappings.
/// </summary>
/// <param name="Value">The underlying value of the identifier.</param>
public record TenantId(string Value)
{
    /// <summary>
    /// The portable default tenant, the zero GUID. Providers map the Arc/Chronicle default namespace
    /// <c>Default</c> to this value without conflating it with a named tenant.
    /// </summary>
    public static readonly TenantId Default = new("00000000-0000-0000-0000-000000000000");

    /// <summary>
    /// The absence of a tenant, represented by an empty string. Providers must distinguish it from named tenants.
    /// </summary>
    public static readonly TenantId NotSet = new(string.Empty);

    /// <summary>
    /// Converts a <see cref="TenantId"/> to its underlying value.
    /// </summary>
    /// <param name="tenant">The <see cref="TenantId"/> to convert.</param>
    /// <returns>The underlying value.</returns>
    public static implicit operator string(TenantId tenant) => tenant.Value;

    /// <summary>
    /// Converts a value to a <see cref="TenantId"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted <see cref="TenantId"/>.</returns>
    public static implicit operator TenantId(string value) => new(value);
}
