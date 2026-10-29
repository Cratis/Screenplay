// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>Represents a code validation's messages, or an unsupported execution.</summary>
/// <param name="Messages">An empty array accepts; each message rejects. Null or a default array means unsupported.</param>
/// <param name="Diagnostic">An execution diagnostic. A diagnostic always makes the result unsupported.</param>
public sealed record SemanticValidationBodyResult(ImmutableArray<string>? Messages, string? Diagnostic = null)
{
    /// <summary>Creates a successful validation with no messages.</summary>
    /// <returns>An evaluated, valid result.</returns>
    public static SemanticValidationBodyResult Valid() => new([]);

    /// <summary>Creates a validation rejection preserving each message verbatim.</summary>
    /// <param name="messages">The messages in evaluation order.</param>
    /// <returns>An evaluated, invalid result.</returns>
    public static SemanticValidationBodyResult Invalid(params string[] messages) => new([.. messages]);

    /// <summary>Creates an unsupported execution without validation messages.</summary>
    /// <param name="diagnostic">The reason execution is unsupported.</param>
    /// <returns>An unsupported result.</returns>
    public static SemanticValidationBodyResult Unsupported(string diagnostic) => new(null, diagnostic);
}
