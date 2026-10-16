// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Checks admitted command comparisons using calendar dates and UTC instants.
/// </summary>
public static class SemanticDateComparisons
{
    /// <summary>
    /// Finds the command properties participating in date comparisons, including properties typed through concepts.
    /// </summary>
    /// <param name="application">The contract-validated application declaring the concepts.</param>
    /// <param name="command">The command whose requirements, conditional productions and validation rules are inspected.</param>
    /// <returns>The participating property identities and their Date or DateTime primitive types.</returns>
    public static IReadOnlyDictionary<SemanticId, SemanticPrimitiveType> Participants(SemanticApplication application, SemanticCommand command)
    {
        var conditions = command.Requirements.Select(requirement => requirement.Condition)
            .Concat(command.Produces.Select(production => production.When).OfType<SemanticCondition>());
        var ids = conditions.SelectMany(Operands).Where(operand => operand.Read is null && operand.Property.IsSet).Select(operand => operand.Property)
            .Concat(command.Validations.Where(rule => SemanticDateComparison.IsComparison(rule.Kind)).Select(rule => rule.Property)).ToHashSet();

        return command.Properties.Where(property => ids.Contains(property.Id) && SemanticDateComparison.IsDate(Primitive(application, property.Type)))
            .ToDictionary(property => property.Id, property => Primitive(application, property.Type));
    }

    /// <summary>
    /// Parses an ISO 8601 instant with an explicit offset or Z, with minutes or seconds and up to seven fractional second digits.
    /// </summary>
    /// <param name="value">The instant spelling to parse using invariant culture.</param>
    /// <param name="instant">The parsed instant on success; otherwise the default value.</param>
    /// <returns>Whether the spelling is one of the admitted explicit-offset formats.</returns>
    public static bool TryParseInstant(string value, out DateTimeOffset instant) => DateTimeOffset.TryParseExact(
        value,
        ["yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", "yyyy-MM-dd'T'HH:mmzzz", "yyyy-MM-dd'T'HH:mm'Z'"],
        CultureInfo.InvariantCulture,
        DateTimeStyles.AssumeUniversal,
        out instant);

    /// <summary>
    /// Compares calendar dates or UTC instants at tick precision without applying a comparison operator.
    /// </summary>
    /// <param name="left">The left text value containing a calendar date or an explicitly offset instant.</param>
    /// <param name="right">The right text value containing a calendar date or an explicitly offset instant.</param>
    /// <param name="primitive">The declared Date or DateTime primitive shared by both operands.</param>
    /// <returns>A negative value, zero or a positive value when the left value is earlier than, equal to or later than the right value.</returns>
    /// <exception cref="InvalidSemanticContract">An operand is not text, cannot be parsed as the declared date primitive, or the primitive is not Date or DateTime.</exception>
    public static int Compare(SemanticValue left, SemanticValue right, SemanticPrimitiveType primitive)
    {
        if (left is not SemanticTextValue first || right is not SemanticTextValue second)
            throw new InvalidSemanticContract("A date comparison requires text values of its declared date primitive.");
        if (primitive == SemanticPrimitiveType.Date &&
            DateOnly.TryParseExact(first.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var firstDate) &&
            DateOnly.TryParseExact(second.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var secondDate))
        {
            return firstDate.CompareTo(secondDate);
        }
        if (primitive == SemanticPrimitiveType.DateTime && TryParseInstant(first.Value, out var firstInstant) && TryParseInstant(second.Value, out var secondInstant))
            return firstInstant.UtcTicks.CompareTo(secondInstant.UtcTicks);

        throw new InvalidSemanticContract("A date comparison requires valid dates or explicitly offset instants.");
    }

    /// <summary>
    /// Checks a command validation rule chronologically for Date and DateTime comparisons, or delegates other rules to the value-rule check.
    /// </summary>
    /// <param name="rule">The contract-validated command rule.</param>
    /// <param name="value">The value, or <see cref="SemanticValue.Null"/> for absence, which satisfies date comparisons.</param>
    /// <param name="primitive">The subject property's primitive type, resolved through any concept.</param>
    /// <returns>The rule outcome with its authored message or the reference evaluator's default message.</returns>
    /// <exception cref="InvalidSemanticContract">The value or operand is incompatible with the rule.</exception>
    public static SemanticRuleCheck Check(SemanticValidationRule rule, SemanticValue value, SemanticPrimitiveType primitive) =>
        SemanticDateComparison.IsDate(primitive) && SemanticDateComparison.IsComparison(rule.Kind)
            ? new SemanticRuleCheck(
                Satisfies(rule, value, primitive) ? SemanticRuleStatus.Satisfied : SemanticRuleStatus.Failed,
                rule,
                rule.Message ?? SemanticValidationRules.DefaultMessage(rule, value, "A required value is empty."))
            : SemanticValidationRules.Check(rule, value);

    /// <summary>
    /// Checks supplied DateTime comparison inputs before any command rule or condition runs.
    /// </summary>
    /// <param name="application">The contract-validated application declaring the concepts.</param>
    /// <param name="command">The command whose participating properties are checked in declaration order.</param>
    /// <param name="values">The supplied inputs; null entries and absent values are ignored.</param>
    /// <returns>The first invalid text input's error, or null when no participating DateTime text input is invalid.</returns>
    public static SemanticValidationFailure? CheckInputs(SemanticApplication application, SemanticCommand command, IEnumerable<SemanticPropertyValue?> values)
    {
        var dateTypes = Participants(application, command);
        var inputs = values.ToArray();
        var invalid = command.Properties.FirstOrDefault(property =>
            dateTypes.GetValueOrDefault(property.Id) == SemanticPrimitiveType.DateTime &&
            inputs.FirstOrDefault(value => value?.TargetProperty == property.Id)?.Value is SemanticTextValue text && !TryParseInstant(text.Value, out _));

        return invalid is null ? null : new SemanticValidationFailure($"DateTime command property '{invalid.Name}' requires an explicit offset or Z.", SemanticValidationSeverity.Error);
    }

    static SemanticPrimitiveType Primitive(SemanticApplication application, SemanticTypeReference type) => type.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => type.Primitive,
        SemanticTypeReferenceKind.Concept => application.Concepts.FirstOrDefault(concept => concept.Id == type.Target)?.Primitive ?? SemanticPrimitiveType.Unknown,
        _ => SemanticPrimitiveType.Unknown
    };

    static bool Satisfies(SemanticValidationRule rule, SemanticValue value, SemanticPrimitiveType primitive)
    {
        if (value is SemanticNullValue) return true;
        var comparison = Compare(value, rule.Operand!, primitive);

        return rule.Kind switch
        {
            SemanticValidationRuleKind.Equal => comparison == 0,
            SemanticValidationRuleKind.NotEqual => comparison != 0,
            SemanticValidationRuleKind.GreaterThan => comparison > 0,
            SemanticValidationRuleKind.GreaterThanOrEqual => comparison >= 0,
            SemanticValidationRuleKind.LessThan => comparison < 0,
            SemanticValidationRuleKind.LessThanOrEqual => comparison <= 0,
            _ => throw new InvalidSemanticContract("Unknown date comparison rule.")
        };
    }

    static IEnumerable<SemanticConditionOperand> Operands(SemanticCondition condition) => condition switch
    {
        SemanticComparison comparison => [comparison.Left, comparison.Right],
        SemanticLogicalCondition logical => Operands(logical.Left).Concat(Operands(logical.Right)),
        _ => []
    };
}
