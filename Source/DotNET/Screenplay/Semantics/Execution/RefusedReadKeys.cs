// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Decides whether a protected command read key has a shape the event store refuses to decide over.
/// </summary>
/// <remarks>
/// Refused shapes are empty, whitespace-only, exactly <c>*</c>, containing <c>#</c> anywhere, and leading or trailing whitespace.
/// A protected read of such a key behaves as a read of a missing view (decision 0091).
/// </remarks>
internal static class RefusedReadKeys
{
    /// <summary>
    /// Determines whether a key, or any part of a composite key, has a refused shape.
    /// </summary>
    /// <param name="key">The <see cref="SemanticValue"/> key to check.</param>
    /// <returns><see langword="true"/> when the key is refused; otherwise <see langword="false"/>.</returns>
    internal static bool IsRefused(SemanticValue key) => key switch
    {
        SemanticTextValue text => IsRefused(text.Value),
        SemanticCompositeValue composite => composite.Properties.Any(property => IsRefused(property.Value)),
        _ => false
    };

    static bool IsRefused(string value) =>
        string.IsNullOrWhiteSpace(value) ||
        value == "*" ||
        value.Contains('#', StringComparison.Ordinal) ||
        char.IsWhiteSpace(value[0]) ||
        char.IsWhiteSpace(value[^1]);
}
