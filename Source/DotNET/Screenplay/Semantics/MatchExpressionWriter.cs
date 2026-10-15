// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics;

// Export for renderer conformance only. Screenplay never runs this text through a platform engine.
internal static class MatchExpressionWriter
{
    internal static string DotNet(MatchExpression expression) => expression switch
    {
        MatchCharacterExpression character => CharacterClass(character.Characters),
        MatchAssertionExpression assertion => assertion.IsStart ? @"\A" : @"\z",
        MatchSequenceExpression sequence => string.Concat(sequence.Items.Select(DotNet)),
        MatchChoiceExpression choice => "(?:" + string.Join('|', choice.Alternatives.Select(DotNet)) + ")",
        MatchRepeatExpression repeat => "(?:" + DotNet(repeat.Body) + ")" + Quantifier(repeat),
        _ => throw new InvalidSemanticContract("Unknown portable match expression.")
    };

    static string CharacterClass(MatchCharacterSet characters)
    {
        var ranges = characters.Ranges.Select(range => range.First == range.Last ? Code(range.First) : Code(range.First) + "-" + Code(range.Last)).ToArray();

        return ranges.Length == 0 ? @"[^\u0000-\uffff]" : "[" + string.Concat(ranges) + "]";
    }

    static string Code(int character) => "\\u" + character.ToString("x4", CultureInfo.InvariantCulture);

    static string Quantifier(MatchRepeatExpression repeat) => repeat.Maximum switch
    {
        null when repeat.Minimum == 0 => "*",
        null when repeat.Minimum == 1 => "+",
        null => "{" + repeat.Minimum.ToString(CultureInfo.InvariantCulture) + ",}",
        var maximum when maximum == repeat.Minimum => "{" + repeat.Minimum.ToString(CultureInfo.InvariantCulture) + "}",
        var maximum => "{" + repeat.Minimum.ToString(CultureInfo.InvariantCulture) + "," + maximum.Value.ToString(CultureInfo.InvariantCulture) + "}"
    };
}
