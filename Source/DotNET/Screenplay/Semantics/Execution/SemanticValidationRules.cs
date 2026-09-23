// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Defines the reference meaning of every declarative validation rule the evaluator executes.
/// </summary>
/// <remarks>
/// Chronicle has no comparison, length or pattern rule algebra; these rules are an application-layer concern
/// whose meaning ESM owns. Every rule except not-empty is satisfied by an absent (null) value, so presence is
/// stated only by not-empty. A bound on text constrains its length in UTF-16 code units - the length both
/// .NET and JavaScript report - and a bound on a number constrains its value.
/// </remarks>
static class SemanticValidationRules
{
    /// <summary>
    /// Gets whether the reference evaluator executes a rule kind. Any other kind blocks plan creation.
    /// </summary>
    /// <param name="kind">The rule kind.</param>
    /// <returns><c>true</c> when the kind is executed.</returns>
    internal static bool Evaluates(SemanticValidationRuleKind kind) => kind is
        SemanticValidationRuleKind.NotEmpty or
        SemanticValidationRuleKind.Maximum or
        SemanticValidationRuleKind.Minimum or
        SemanticValidationRuleKind.Equal or
        SemanticValidationRuleKind.NotEqual or
        SemanticValidationRuleKind.GreaterThan or
        SemanticValidationRuleKind.GreaterThanOrEqual or
        SemanticValidationRuleKind.LessThan or
        SemanticValidationRuleKind.LessThanOrEqual or
        SemanticValidationRuleKind.Length or
        SemanticValidationRuleKind.AllGreaterThan or
        SemanticValidationRuleKind.AllGreaterThanOrEqual or
        SemanticValidationRuleKind.Matches;

    /// <summary>
    /// Decides whether a value satisfies a rule.
    /// </summary>
    /// <param name="rule">The contract-validated rule.</param>
    /// <param name="value">The contract-validated value.</param>
    /// <returns><c>true</c> when the value satisfies the rule.</returns>
    internal static bool Satisfies(SemanticValidationRule rule, SemanticValue value) => rule.Kind switch
    {
        SemanticValidationRuleKind.NotEmpty => !SemanticEvaluator.IsEmpty(value),
        _ when value is SemanticNullValue => true,
        SemanticValidationRuleKind.Maximum => Measure(value) <= Number(rule.Operand),
        SemanticValidationRuleKind.Minimum => Measure(value) >= Number(rule.Operand),
        SemanticValidationRuleKind.Equal => SemanticValueRules.AreEqual(value, rule.Operand!),
        SemanticValidationRuleKind.NotEqual => !SemanticValueRules.AreEqual(value, rule.Operand!),
        SemanticValidationRuleKind.GreaterThan => Measure(value) > Number(rule.Operand),
        SemanticValidationRuleKind.GreaterThanOrEqual => Measure(value) >= Number(rule.Operand),
        SemanticValidationRuleKind.LessThan => Measure(value) < Number(rule.Operand),
        SemanticValidationRuleKind.LessThanOrEqual => Measure(value) <= Number(rule.Operand),
        SemanticValidationRuleKind.Length => value is SemanticTextValue text ? text.Value.Length == Number(rule.Operand) : throw SemanticValueRules.Malformed(),
        SemanticValidationRuleKind.Matches => Matches(value, rule.Operand),
        SemanticValidationRuleKind.AllGreaterThan => Elements(value).All(_ => Measure(_) > Number(rule.Operand)),
        SemanticValidationRuleKind.AllGreaterThanOrEqual => Elements(value).All(_ => Measure(_) >= Number(rule.Operand)),
        _ => throw new InvalidSemanticContract($"Validation rule '{rule.Kind}' is not executable by the reference evaluator.")
    };

    /// <summary>
    /// Describes a failed rule that carries no authored message.
    /// </summary>
    /// <param name="rule">The failed rule.</param>
    /// <param name="value">The value that failed it.</param>
    /// <param name="empty">The message for a failed not-empty rule.</param>
    /// <returns>The rejection message.</returns>
    internal static string DefaultMessage(SemanticValidationRule rule, SemanticValue value, string empty) => rule.Kind switch
    {
        SemanticValidationRuleKind.NotEmpty => empty,
        SemanticValidationRuleKind.Maximum when value is SemanticTextValue => $"A value must be at most {Format(rule.Operand)} characters long.",
        SemanticValidationRuleKind.Minimum when value is SemanticTextValue => $"A value must be at least {Format(rule.Operand)} characters long.",
        SemanticValidationRuleKind.Maximum => $"A value must be at most {Format(rule.Operand)}.",
        SemanticValidationRuleKind.Minimum => $"A value must be at least {Format(rule.Operand)}.",
        SemanticValidationRuleKind.Equal => $"A value must equal {Format(rule.Operand)}.",
        SemanticValidationRuleKind.NotEqual => $"A value must not equal {Format(rule.Operand)}.",
        SemanticValidationRuleKind.GreaterThan => $"A value must be greater than {Format(rule.Operand)}.",
        SemanticValidationRuleKind.GreaterThanOrEqual => $"A value must be at least {Format(rule.Operand)}.",
        SemanticValidationRuleKind.LessThan => $"A value must be less than {Format(rule.Operand)}.",
        SemanticValidationRuleKind.LessThanOrEqual => $"A value must be at most {Format(rule.Operand)}.",
        SemanticValidationRuleKind.Length => $"A value must be exactly {Format(rule.Operand)} characters long.",
        SemanticValidationRuleKind.Matches => "A value must match the required pattern.",
        SemanticValidationRuleKind.AllGreaterThan => $"Every value must be greater than {Format(rule.Operand)}.",
        SemanticValidationRuleKind.AllGreaterThanOrEqual => $"Every value must be at least {Format(rule.Operand)}.",
        _ => $"A value does not satisfy the '{rule.Kind}' rule."
    };

    static bool Matches(SemanticValue value, SemanticValue? operand)
    {
        if (value is not SemanticTextValue text || operand is not SemanticTextValue pattern)
        {
            throw SemanticValueRules.Malformed();
        }

        try
        {
            return SemanticMatchPattern.Create(pattern.Value).IsMatch(text.Value);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    static decimal Measure(SemanticValue value) => value switch
    {
        SemanticTextValue text => text.Value.Length,
        SemanticNumberValue number => number.Value,
        _ => throw SemanticValueRules.Malformed()
    };

    static ImmutableArray<SemanticValue> Elements(SemanticValue value) =>
        value is SemanticArrayValue array ? array.Values : throw SemanticValueRules.Malformed();

    static decimal Number(SemanticValue? operand) =>
        operand is SemanticNumberValue number ? number.Value : throw SemanticValueRules.Malformed();

    static string Format(SemanticValue? operand) => operand switch
    {
        SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture),
        SemanticTextValue text => $"'{text.Value}'",
        SemanticBooleanValue boolean => boolean.Value ? "true" : "false",
        _ => "null"
    };
}
