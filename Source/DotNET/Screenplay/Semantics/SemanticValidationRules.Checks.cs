// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

public static partial class SemanticValidationRules
{
    /// <summary>
    /// Checks one rule against a value without an execution plan.
    /// </summary>
    /// <param name="rule">The contract-validated rule.</param>
    /// <param name="value">The value, or <see cref="SemanticValue.Null"/> for absence.</param>
    /// <returns>The rule outcome, message and optional pattern admission details.</returns>
    /// <exception cref="InvalidSemanticContract">The value or operand is incompatible with the rule.</exception>
    public static SemanticRuleCheck Check(SemanticValidationRule rule, SemanticValue value) =>
        Check(rule, value, "A required value is empty.");

    /// <summary>
    /// Checks every rule on a concept in declaration order.
    /// </summary>
    /// <param name="concept">The concept whose rules constrain the value.</param>
    /// <param name="value">The value, or <see cref="SemanticValue.Null"/> for absence.</param>
    /// <returns>One result per rule, carrying the concept identity.</returns>
    /// <exception cref="InvalidSemanticContract">The value or operand is incompatible with a rule.</exception>
    public static ImmutableArray<SemanticRuleCheck> Check(SemanticConcept concept, SemanticValue value) =>
        [.. concept.Validations.Select(rule => Check(rule, value, "A required concept value is empty.") with { Concept = concept.Id })];

    static SemanticRuleCheck Check(SemanticValidationRule rule, SemanticValue value, string empty)
    {
        ValidateKinds(rule, value);

        var pattern = rule is { Kind: SemanticValidationRuleKind.Matches, Operand: SemanticTextValue text }
            ? PortableMatchPattern.Check(text.Value)
            : null;
        if (!Evaluates(rule.Kind))
        {
            return new(SemanticRuleStatus.NotEvaluable, rule, $"Validation rule '{rule.Kind}' is not executable by the reference evaluator.");
        }

        if (pattern is { Status: PortableMatchStatus.Refused or PortableMatchStatus.Invalid })
        {
            return new(SemanticRuleStatus.NotEvaluable, rule, pattern.Description, Pattern: pattern);
        }

        return new(
            Satisfies(rule, value) ? SemanticRuleStatus.Satisfied : SemanticRuleStatus.Failed,
            rule,
            rule.Message ?? DefaultMessage(rule, value, empty),
            Pattern: pattern);
    }
}
