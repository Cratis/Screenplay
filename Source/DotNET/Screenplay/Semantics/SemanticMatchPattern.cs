// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Holds the portable match rule definition shared by binding, contract validation and execution.
/// </summary>
internal static class SemanticMatchPattern
{
    // Exactly one @; nonempty local part without whitespace or @; at least two nonempty domain labels.
    internal const string Email = @"^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$";

    internal static MatchPatternResult Parse(string pattern) => new PortableMatchParser(pattern).Parse();

    internal static PortableMatchMachine Create(string pattern) => Create(Parse(pattern));

    internal static PortableMatchMachine Create(MatchPatternResult parsed)
    {
        if (!parsed.Accepted) throw new InvalidSemanticContract($"Matches pattern is not executable: {parsed.Description}");

        return new MatchAutomatonBuilder().Compile(parsed.Expression!);
    }
}
