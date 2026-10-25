// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Recognises identity-typed strings in the canonical semantic model.
/// </summary>
internal static class SemanticIdentityText
{
    /// <summary>
    /// Checks whether a string is any of Screenplay's identity types.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns><see langword="true"/> when one of the identity parsers accepts it; otherwise, <see langword="false"/>.</returns>
    public static bool IsIdentity(string? value) =>
        ApplicationIdentity.TryParse(value, out _) ||
        DocumentId.TryParse(value, out _) ||
        SemanticId.TryParse(value, out _) ||
        EventContractId.TryParse(value, out _) ||
        SemanticRevision.TryParse(value, out _) ||
        CatalogRevision.TryParse(value, out _);

    /// <summary>
    /// Checks whether a string defines a declaration when it is the value of <c language="text">id</c>.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns><see langword="true"/> when it is a semantic or application identity; otherwise, <see langword="false"/>.</returns>
    public static bool IsDefinition(string? value) =>
        SemanticId.TryParse(value, out _) || ApplicationIdentity.TryParse(value, out _);

    /// <summary>
    /// Checks whether a string is identity-assignment provenance that is dropped.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns><see langword="true"/> when it is a catalog revision; otherwise, <see langword="false"/>.</returns>
    public static bool IsProvenance(string? value) => CatalogRevision.TryParse(value, out _);

    /// <summary>
    /// Checks whether a string property holds a requirement identity, a hash over the identity of its owner.
    /// </summary>
    /// <param name="property">The property name.</param>
    /// <returns><see langword="true"/> for <c language="text">requirementId</c> and <c language="text">requirement</c>; otherwise, <see langword="false"/>.</returns>
    public static bool IsRequirement(string? property) =>
        string.Equals(property, "requirementId", StringComparison.Ordinal) || string.Equals(property, "requirement", StringComparison.Ordinal);
}
