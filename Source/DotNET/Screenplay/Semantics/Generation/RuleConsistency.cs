// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Semantics.Generation;

/// <summary>
/// Proves whether every generated value satisfies a concept validation rule, independently of ESM version.
/// </summary>
public static class RuleConsistency
{
    /// <summary>
    /// Checks the exact built-in generation shape against one rule.
    /// </summary>
    /// <param name="generation">The admitted strategy, or null for implicit UUID generation.</param>
    /// <param name="primitive">The concept primitive.</param>
    /// <param name="rule">The validation rule; severity does not affect proof status.</param>
    /// <returns>The proof status, with a violating value or a reason when applicable.</returns>
    public static RuleConsistencyResult Check(SemanticGeneration? generation, SemanticPrimitiveType primitive, SemanticValidationRule rule)
    {
        if (rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation) return RuleConsistencyResult.Unproven("opaque");
        var shape = GenerationShape.Create(generation);
        if (shape is null) return RuleConsistencyResult.Unproven("opaque generation strategy");
        if (primitive is not (SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid)) return RuleConsistencyResult.Unproven("unsupported primitive");
        var length = shape.Positions.Length;
        return (rule.Kind, rule.Operand) switch
        {
            (SemanticValidationRuleKind.NotEmpty, _) => Length(shape, length >= 1),
            (SemanticValidationRuleKind.Minimum, SemanticNumberValue number) => Length(shape, length >= number.Value),
            (SemanticValidationRuleKind.Maximum, SemanticNumberValue number) => Length(shape, length <= number.Value),
            (SemanticValidationRuleKind.Length, SemanticNumberValue number) => Length(shape, length == number.Value),
            (SemanticValidationRuleKind.Equal, SemanticTextValue text) => Equal(shape, text.Value, false),
            (SemanticValidationRuleKind.NotEqual, SemanticTextValue text) => Equal(shape, text.Value, true),
            (SemanticValidationRuleKind.Matches, SemanticTextValue text) => Match(shape, text.Value),
            _ => RuleConsistencyResult.Unproven("unsupported rule")
        };
    }

    static RuleConsistencyResult Length(GenerationShape shape, bool satisfied) => satisfied ? RuleConsistencyResult.Proven : RuleConsistencyResult.Refuted(shape.Samples().First());

    // The literal language of ^escaped$ is exact even when the operand contains non-ASCII
    // or control characters excluded from authored fragment R patterns.
    static RuleConsistencyResult Equal(GenerationShape shape, string text, bool complement)
    {
        var expression = RegexExpression.Empty;
        foreach (var character in text) expression = RegexExpression.Concat(expression, RegexExpression.Character(character.ToString()));

        return RegexContainment.Check(shape, expression, complement);
    }

    static RuleConsistencyResult Match(GenerationShape shape, string pattern, bool complement = false)
    {
        RegexExpression? expression = null;
        string? reason = null;
        try
        {
            expression = new RegexFragmentR(pattern, shape.Positions.Length).Parse();
        }
        catch (OutsideRegexFragment error)
        {
            reason = error.Message;
        }

        Regex runtime;
        try
        {
            runtime = SemanticMatchPattern.Create(pattern);
        }
        catch (ArgumentException)
        {
            return RuleConsistencyResult.Unproven(reason ?? "invalid regex");
        }

        return expression is null ? Samples(shape, runtime, complement, reason!) : RegexContainment.Check(shape, expression, complement);
    }

    static RuleConsistencyResult Samples(GenerationShape shape, Regex regex, bool complement, string reason)
    {
        try
        {
            foreach (var sample in shape.Samples())
            {
                if (regex.IsMatch(sample) == complement) return RuleConsistencyResult.Refuted(sample);
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return RuleConsistencyResult.Unproven(reason + "; runtime regex timeout");
        }

        return RuleConsistencyResult.Unproven(reason);
    }
}
