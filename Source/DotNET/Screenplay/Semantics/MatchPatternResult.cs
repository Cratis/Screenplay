// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal sealed record MatchPatternResult(string JavaScriptPattern, string? DotNetPattern, string? Construct, int Offset, bool Invalid, string? Suggestion)
{
    internal bool Accepted => Construct is null;

    internal string Description => $"{Construct} at offset {Offset}. {Suggestion}".TrimEnd();
}
