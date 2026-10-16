// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal static class TextRuleExpression
{
    internal static RegexExpression Create(SemanticValidationRuleKind kind, string operand, IEnumerable<string> positions)
    {
        // Include every code unit, not just printable ASCII. This also makes complement proofs sound
        // when the literal is outside the generator's alphabet (including unpaired surrogates).
        var alphabet = new string([.. string.Concat(positions.Append(operand)).Distinct()]);
        var any = RegexExpression.Repeat(RegexExpression.Character(alphabet), 0, null);
        var literal = RegexExpression.Empty;
        foreach (var character in operand) literal = RegexExpression.Concat(literal, RegexExpression.Character(character.ToString()));
        var prefix = kind == SemanticValidationRuleKind.StartsWith ? literal : RegexExpression.Concat(any, literal);

        return kind == SemanticValidationRuleKind.EndsWith ? prefix : RegexExpression.Concat(prefix, any);
    }
}
