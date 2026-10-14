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
            (SemanticValidationRuleKind.NotEmpty, _) => NotEmpty(shape),
            (SemanticValidationRuleKind.Minimum, SemanticNumberValue number) => Length(shape, length >= number.Value),
            (SemanticValidationRuleKind.Maximum, SemanticNumberValue number) => Length(shape, length <= number.Value),
            (SemanticValidationRuleKind.Length, SemanticNumberValue number) => Length(shape, length == number.Value),
            (SemanticValidationRuleKind.Equal, SemanticTextValue text) => Equal(shape, text.Value, false),
            (SemanticValidationRuleKind.NotEqual, SemanticTextValue text) => Equal(shape, text.Value, true),
            (SemanticValidationRuleKind.Matches, SemanticTextValue text) => Match(shape, text.Value),
            _ => RuleConsistencyResult.Unproven("unsupported rule")
        };
    }

    /// <summary>
    /// Checks a finite pattern without admitting the strategy into an executable semantic model.
    /// </summary>
    /// <param name="pattern">The authored finite pattern.</param>
    /// <param name="checksum">The optional conditioning checksum.</param>
    /// <param name="rule">The concept validation rule.</param>
    /// <param name="assessmentDate">A fixed UTC assessment date for genuine date-bearing counterexamples.</param>
    /// <returns>The proof status; approximate-only witnesses never refute the rule.</returns>
    public static RuleConsistencyResult CheckPattern(string pattern, string? checksum, SemanticValidationRule rule, DateOnly? assessmentDate = null) =>
        FiniteGenerationRules.Check(GenerationPattern.Parse(pattern, checksum), rule, assessmentDate);

    /// <summary>
    /// Checks numeric minimum and maximum rules against an exact inclusive Int64 generation range.
    /// </summary>
    /// <param name="range">The authored inclusive range.</param>
    /// <param name="rule">The concept validation rule.</param>
    /// <returns>The proof status, with an exact integer witness on refutation.</returns>
    public static RuleConsistencyResult CheckRandom(string range, SemanticValidationRule rule) => FiniteGenerationRules.Check(GenerationIntegerRange.Parse(range), rule);

    internal static RuleConsistencyResult CrossCheck(GenerationShape shape, RuleConsistencyResult proof, Func<string, bool> matches, bool complement = false) =>
        proof.Status == RuleConsistencyStatus.Proven ? Samples(shape, matches, complement, null) : proof;

    static RuleConsistencyResult NotEmpty(GenerationShape shape)
    {
        var positions = shape.Positions;

        return positions.All(characters => characters.Contains(' ', StringComparison.Ordinal))
            ? RuleConsistencyResult.Refuted(new string(' ', positions.Length))
            : RuleConsistencyResult.Proven;
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
        catch (RegexStateBudgetExceeded error)
        {
            return RuleConsistencyResult.Unproven(error.Message);
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

        if (expression is null) return Samples(shape, runtime.IsMatch, complement, reason);
        var proof = RegexContainment.Check(shape, expression, complement, requireDeterministic: true);
        if (proof.Status == RuleConsistencyStatus.Proven)
        {
            try
            {
                // Length clipping is valid for containment, not for runtime ambiguity: retain every authored branch.
                var authored = new RegexFragmentR(pattern, shape.Positions.Length, boundToShape: false).Parse();
                if (authored.HasNullableRepeat || !new GlushkovAutomaton(authored).IsDeterministic) return RuleConsistencyResult.Unproven(RegexContainment.AmbiguousPatternReason);
            }
            catch (RegexStateBudgetExceeded error)
            {
                return RuleConsistencyResult.Unproven(error.Message);
            }
        }

        return CrossCheck(shape, proof, runtime.IsMatch, complement);
    }

    static RuleConsistencyResult Samples(GenerationShape shape, Func<string, bool> matches, bool complement, string? reason)
    {
        try
        {
            foreach (var sample in shape.Samples())
            {
                if (matches(sample) == complement) return RuleConsistencyResult.Refuted(sample);
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return RuleConsistencyResult.Unproven("runtime regex timed out");
        }

        return reason is null ? RuleConsistencyResult.Proven : RuleConsistencyResult.Unproven(reason);
    }
}
