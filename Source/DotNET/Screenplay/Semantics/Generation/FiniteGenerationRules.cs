// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Semantics.Generation;

internal static class FiniteGenerationRules
{
    const string ApproximateReason = "checksum or UTC date shape is an over-approximation";

    internal static RuleConsistencyResult Check(GenerationIntegerRange range, SemanticValidationRule rule) => (rule.Kind, rule.Operand) switch
    {
        (SemanticValidationRuleKind.Minimum, SemanticNumberValue number) => range.Minimum >= number.Value ? RuleConsistencyResult.Proven : RuleConsistencyResult.Refuted(range.Minimum.ToString(CultureInfo.InvariantCulture)),
        (SemanticValidationRuleKind.Maximum, SemanticNumberValue number) => range.Maximum <= number.Value ? RuleConsistencyResult.Proven : RuleConsistencyResult.Refuted(range.Maximum.ToString(CultureInfo.InvariantCulture)),
        (SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation, _) => RuleConsistencyResult.Unproven("opaque"),
        _ => RuleConsistencyResult.Unproven("unsupported rule")
    };

    internal static RuleConsistencyResult Check(GenerationPattern pattern, SemanticValidationRule rule, DateOnly? assessmentDate = null)
    {
        if (rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation) return RuleConsistencyResult.Unproven("opaque");
        var approximate = pattern.HasDates || pattern.Checksum is not null;
        return (rule.Kind, rule.Operand) switch
        {
            (SemanticValidationRuleKind.Minimum, SemanticNumberValue number) => Length(pattern, length => length >= number.Value, pattern.MinimumLength >= number.Value, assessmentDate),
            (SemanticValidationRuleKind.Maximum, SemanticNumberValue number) => Length(pattern, length => length <= number.Value, pattern.MaximumLength <= number.Value, assessmentDate),
            (SemanticValidationRuleKind.Length, SemanticNumberValue number) => Length(pattern, length => length == number.Value, pattern.MinimumLength == number.Value && pattern.MaximumLength == number.Value, assessmentDate),
            (SemanticValidationRuleKind.NotEmpty, _) when pattern.Checksum is not null || pattern.HasDates => RuleConsistencyResult.Proven,
            (SemanticValidationRuleKind.NotEmpty, _) => FinitePatternContainment.Check(pattern, RegexExpression.Repeat(RegexExpression.Character(" "), 0, null), complement: true),
            (SemanticValidationRuleKind.Equal, SemanticTextValue text) when !approximate => FinitePatternContainment.Check(pattern, Literal(text.Value)),
            (SemanticValidationRuleKind.NotEqual, SemanticTextValue text) when !approximate => FinitePatternContainment.Check(pattern, Literal(text.Value), complement: true),
            (SemanticValidationRuleKind.Matches, SemanticTextValue text) => Match(pattern, text.Value, assessmentDate),
            _ => RuleConsistencyResult.Unproven(approximate ? ApproximateReason : "unsupported rule")
        };
    }

    internal static RuleConsistencyResult CheckChecksum(GenerationPattern pattern, string algorithm)
    {
        _ = GenerationChecksum.Width(algorithm);

        return pattern.Checksum == algorithm ? RuleConsistencyResult.Proven : RuleConsistencyResult.Unproven("checksum arithmetic requires a conditioned-support proof");
    }

    static RegexExpression Literal(string text)
    {
        var expression = RegexExpression.Empty;
        foreach (var character in text) expression = RegexExpression.Concat(expression, RegexExpression.Character(character.ToString()));

        return expression;
    }

    static RuleConsistencyResult Length(GenerationPattern pattern, Func<int, bool> satisfies, bool allLengths, DateOnly? date)
    {
        if (allLengths) return RuleConsistencyResult.Proven;
        foreach (var sample in FiniteGenerationSamples.Create(pattern, date))
        {
            if (!satisfies(sample.Length)) return RuleConsistencyResult.Refuted(sample);
        }

        return RuleConsistencyResult.Unproven(ApproximateReason);
    }

    static RuleConsistencyResult Match(GenerationPattern pattern, string authored, DateOnly? date)
    {
        RegexExpression? expression;
        string? reason = null;
        try
        {
            expression = new RegexFragmentR(authored, pattern.MaximumLength).Parse();
        }
        catch (OutsideRegexFragment error)
        {
            expression = null;
            reason = error.Message;
        }
        catch (RegexStateBudgetExceeded error)
        {
            return RuleConsistencyResult.Unproven(error.Message);
        }
        Regex runtime;
        try
        {
            runtime = SemanticMatchPattern.Create(authored);
        }
        catch (ArgumentException)
        {
            return RuleConsistencyResult.Unproven(reason ?? "invalid regex");
        }
        var approximate = pattern.HasDates || pattern.Checksum is not null;
        var proof = expression is null ? RuleConsistencyResult.Unproven(reason!) : FinitePatternContainment.Check(pattern, expression);
        if (proof.Status == RuleConsistencyStatus.Refuted && !approximate) return proof;
        if (proof.Status == RuleConsistencyStatus.Proven && !approximate)
        {
            try
            {
                var full = new RegexFragmentR(authored, pattern.MaximumLength, boundToShape: false).Parse();
                if (full.HasNullableRepeat || !new GlushkovAutomaton(full).IsDeterministic) return RuleConsistencyResult.Unproven(RegexContainment.AmbiguousPatternReason);
            }
            catch (RegexStateBudgetExceeded error)
            {
                return RuleConsistencyResult.Unproven(error.Message);
            }
        }
        try
        {
            foreach (var sample in FiniteGenerationSamples.Create(pattern, date))
            {
                if (!runtime.IsMatch(sample)) return RuleConsistencyResult.Refuted(sample);
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return RuleConsistencyResult.Unproven("runtime regex timed out");
        }

        return approximate ? RuleConsistencyResult.Unproven(ApproximateReason) : proof;
    }
}
