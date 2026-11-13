// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Binds the <c>audience</c>, <c>availability</c> and <c>available</c> syntax to its ESM v53 form.
/// </summary>
/// <remarks>
/// Binding is purely structural: a name that is not declared stays a name, and the semantic validator refuses the model, so an
/// unknown audience or availability can never silently open a gate.
/// </remarks>
internal static class SemanticAvailabilityBinder
{
    const string ContextPrefix = "$context.";

    /// <summary>
    /// Binds the audiences of an application.
    /// </summary>
    /// <param name="audiences">The <see cref="AudienceSyntax">audiences</see>.</param>
    /// <returns>The bound audiences.</returns>
    internal static ImmutableArray<SemanticAudience> Audiences(IEnumerable<AudienceSyntax> audiences) =>
        [.. audiences.Select(audience => new SemanticAudience(audience.Name))];

    /// <summary>
    /// Binds the reusable availabilities of an application.
    /// </summary>
    /// <param name="availabilities">The <see cref="AvailabilitySyntax">availabilities</see>.</param>
    /// <returns>The bound availabilities.</returns>
    internal static ImmutableArray<SemanticAvailability> Availabilities(IEnumerable<AvailabilitySyntax> availabilities) =>
        [.. availabilities.Select(availability => new SemanticAvailability(availability.Name, [.. availability.Requirements.Select(Requirement)]))];

    /// <summary>
    /// Binds the gates of a construct.
    /// </summary>
    /// <param name="gates">The <see cref="AvailableSyntax">gates</see>.</param>
    /// <returns>The bound gates, one per <c>available</c> line.</returns>
    internal static ImmutableArray<SemanticAvailabilityRequirement> Gates(IEnumerable<AvailableSyntax> gates) =>
        [.. gates.Select(gate => Requirement(gate.Requirement))];

    /// <summary>
    /// Binds a context operand: a <c>$context</c> path or a literal.
    /// </summary>
    /// <param name="expression">The <see cref="ExpressionSyntax"/> to bind.</param>
    /// <returns>The bound operand, or <see langword="null"/> for an expression that is neither.</returns>
    internal static SemanticContextOperand? Operand(ExpressionSyntax expression) => expression switch
    {
        ContextExpressionSyntax context => new(context.Path, null),
        LiteralExpressionSyntax { Value: string text } => new(null, SemanticValue.Text(text)),
        LiteralExpressionSyntax { Value: bool flag } => new(null, SemanticValue.Boolean(flag)),
        LiteralExpressionSyntax { Value: double number } => new(null, SemanticValue.Number((decimal)number)),
        _ => null
    };

    static SemanticAvailabilityRequirement Requirement(AvailabilityRequirementSyntax requirement) => requirement switch
    {
        AudienceAvailabilityRequirementSyntax audience => new SemanticAudienceRequirement(audience.Audience),
        AvailabilityReferenceSyntax reference => new SemanticAvailabilityReference(reference.Availability),
        ConditionAvailabilityRequirementSyntax condition => new SemanticConditionRequirement(Condition(condition.Condition)),
        LogicalAvailabilityRequirementSyntax logical => new SemanticLogicalAvailability(Requirement(logical.Left), Operator(logical.Operator), Requirement(logical.Right)),
        _ => throw new InvalidSemanticContract($"Availability requirement '{requirement.GetType().Name}' is unsupported during semantic binding.")
    };

    static SemanticAvailabilityCondition Condition(ConditionSyntax condition) => condition switch
    {
        ComparisonConditionSyntax comparison => new SemanticAvailabilityComparison(
            Path(comparison.Left),
            Comparison(comparison.Operator),
            Operand(comparison.Right) ?? throw new InvalidSemanticContract("An availability condition compares a $context value with a literal or another $context value.")),
        LogicalConditionSyntax logical => new SemanticLogicalAvailabilityCondition(Condition(logical.Left), Operator(logical.Operator), Condition(logical.Right)),
        _ => throw new InvalidSemanticContract($"Availability condition '{condition.GetType().Name}' is unsupported during semantic binding.")
    };

    static string Path(string left) => left.StartsWith(ContextPrefix, StringComparison.Ordinal) ? left[ContextPrefix.Length..] : left;

    static SemanticLogicalOperator Operator(LogicalOperator @operator) => @operator == LogicalOperator.And ? SemanticLogicalOperator.And : SemanticLogicalOperator.Or;

    static SemanticComparisonOperator Comparison(ComparisonOperator @operator) => @operator switch
    {
        ComparisonOperator.Equal => SemanticComparisonOperator.Equal,
        ComparisonOperator.NotEqual => SemanticComparisonOperator.NotEqual,
        ComparisonOperator.GreaterThan => SemanticComparisonOperator.GreaterThan,
        ComparisonOperator.GreaterThanOrEqual => SemanticComparisonOperator.GreaterThanOrEqual,
        ComparisonOperator.LessThan => SemanticComparisonOperator.LessThan,
        ComparisonOperator.LessThanOrEqual => SemanticComparisonOperator.LessThanOrEqual,
        _ => throw new InvalidSemanticContract("An availability condition compares with ==, !=, >, >=, < or <=.")
    };
}
