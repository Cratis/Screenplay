// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed record RegexExpression(string Kind, string Characters = "", RegexExpression? Left = null, RegexExpression? Right = null, int Minimum = 0, int? Maximum = 0)
{
    internal static RegexExpression Empty => new("empty");
    internal static RegexExpression None => new("none");
    internal static RegexExpression Any => new("characters", RegexCharacters.Printable);

    // Child nodes are immutable and already constructed, so this is constant-time even for a flat literal.
    internal int MinimumLength { get; } = Kind switch
    {
        "empty" => 0,
        "none" => int.MaxValue,
        "characters" => Characters.Length == 0 ? int.MaxValue : 1,
        "alternate" => Math.Min(Left!.MinimumLength, Right!.MinimumLength),
        "concat" => (int)Math.Min(int.MaxValue, (long)Left!.MinimumLength + Right!.MinimumLength),
        "repeat" => (int)Math.Min(int.MaxValue, (long)Left!.MinimumLength * Minimum),
        _ => throw new InvalidSemanticContract("Unknown regular expression node.")
    };

    internal static RegexExpression Character(string characters) => new("characters", characters);
    internal static RegexExpression Alternate(RegexExpression left, RegexExpression right) => new("alternate", Left: left, Right: right);
    internal static RegexExpression Concat(RegexExpression left, RegexExpression right) => new("concat", Left: left, Right: right);
    internal static RegexExpression Repeat(RegexExpression expression, int minimum, int? maximum) => new("repeat", Left: expression, Minimum: minimum, Maximum: maximum);
}
