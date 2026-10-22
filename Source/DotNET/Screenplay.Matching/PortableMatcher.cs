// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Matching;

/// <summary>
/// An immutable, thread-safe matcher for Screenplay's portable linear dialect.
/// </summary>
public sealed class PortableMatcher
{
    readonly PortableMatchMachine _machine;

    PortableMatcher(PortableMatchMachine machine) => _machine = machine;

    /// <summary>
    /// Compiles a decoded pattern without delimiters or flags.
    /// </summary>
    /// <param name="pattern">The portable pattern.</param>
    /// <returns>A reusable matcher.</returns>
    /// <exception cref="InvalidPortableMatchPattern">The pattern is not admitted.</exception>
    public static PortableMatcher Compile(string pattern)
    {
        var parsed = new PortableMatchParser(pattern).Parse();
        if (!parsed.Accepted) throw new InvalidPortableMatchPattern(parsed.Description);

        return new(new MatchAutomatonBuilder().Compile(parsed.Expression!));
    }

    /// <summary>
    /// Searches the value using UTF-16 code-unit semantics and strict input anchors.
    /// </summary>
    /// <param name="value">The text to match.</param>
    /// <returns>Whether the pattern matches.</returns>
    public bool IsMatch(string value) => _machine.IsMatch(value);
}
