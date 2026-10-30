// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Result version 1 for Snapshot Many: a non-null ordered array of typed rows of the declared read model.
/// Empty is success. A missing result, default array, null or malformed rows are Unsupported, never empty success.
/// This provider contract does not write stored read models or admit performer queries to the executable model.
/// </summary>
/// <param name="Rows">The ordered rows, or default when no answer was supplied.</param>
/// <param name="Diagnostic">An explicit Unsupported diagnostic, if supplied.</param>
public sealed record SemanticPerformerBodyResult(ImmutableArray<SemanticCompositeValue> Rows = default, string? Diagnostic = null)
{
    /// <summary>Answers an ordered array; an empty array is a successful answer.</summary>
    /// <param name="rows">The typed rows of the declared read model.</param>
    /// <returns>The snapshot answer.</returns>
    public static SemanticPerformerBodyResult Answered(ImmutableArray<SemanticCompositeValue> rows) => new(rows);

    /// <summary>Declines execution explicitly rather than answering empty rows.</summary>
    /// <param name="diagnostic">The host-owned explanation.</param>
    /// <returns>An unsupported result.</returns>
    public static SemanticPerformerBodyResult Unsupported(string diagnostic) => new(Diagnostic: diagnostic);
}
