// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Matching;

internal sealed record MatchPatternResult(string JavaScriptPattern, MatchExpression? Expression, string? Construct, int Offset, bool Invalid, string? Suggestion)
{
    internal bool Accepted => Construct is null;

    internal int ExpandedSize { get; init; }

    // An on-demand renderer export, never the reference matcher's execution path.
    internal string NormalizedDotNetPattern => Expression is { } node ? MatchExpressionWriter.DotNet(node) : throw new InvalidOperationException("A refused match pattern has no normalized form.");

    internal string Description => $"{Construct} at offset {Offset}. {Suggestion}".TrimEnd();
}
