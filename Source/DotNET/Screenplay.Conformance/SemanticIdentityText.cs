// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Validates identity definitions after the schema has identified their field.
/// </summary>
internal static class SemanticIdentityText
{
    /// <summary>
    /// Checks whether the value of an identity definition is a semantic or application identity.
    /// </summary>
    /// <param name="value">The definition value.</param>
    /// <returns>Whether the definition has a valid identity type.</returns>
    public static bool IsDefinition(string? value) =>
        SemanticId.TryParse(value, out _) || ApplicationIdentity.TryParse(value, out _);
}
