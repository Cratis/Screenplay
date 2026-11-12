// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// Resolves a reference as a model writes it to the full reference a package source looks up.
/// </summary>
/// <remarks>
/// A reference is a catalog short name (<c>cratis/stripe</c>), a full OCI reference (<c>ghcr.io/acme/extension-ledger</c>)
/// or an npm package (<c>npm:@acme/charts</c>). The catalog is a naming convention, not a service the compiler calls: a short
/// <c>owner/name</c> resolves to <c>ghcr.io/owner/extension-name</c>. Only restore resolves; the compiler reads the lock.
/// </remarks>
public static class ExtensionCatalog
{
    /// <summary>The registry a short name resolves into.</summary>
    public const string DefaultRegistry = "ghcr.io";

    /// <summary>
    /// Resolves a reference.
    /// </summary>
    /// <param name="reference">The reference as the model writes it.</param>
    /// <returns>The full reference.</returns>
    public static string Resolve(string reference)
    {
        if (reference.StartsWith("npm:", StringComparison.Ordinal)) return reference;
        var parts = reference.Split('/');
        if (parts.Length == 2 && !parts[0].Contains('.') && !parts[0].Contains(':')) return $"{DefaultRegistry}/{parts[0]}/extension-{parts[1]}";
        return reference;
    }
}
