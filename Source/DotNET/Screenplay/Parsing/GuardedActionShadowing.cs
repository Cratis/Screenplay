// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Proves shadowing by comparison-set inclusion, bounding each DNF expansion to 64 disjuncts.
/// </summary>
internal static class GuardedActionShadowing
{
    const int MaximumDisjuncts = 64;

    internal static void Validate(ScreenGuardedActionSyntax action, ParserContext context) =>
        Validate(action.Alternatives.Select(alternative => (alternative.Condition, alternative.Location, $"Alternative executing '{alternative.Command}' is shadowed by earlier alternatives in this action")), context);

    internal static void Validate(InteractionBindingSyntax binding, ParserContext context) =>
        Validate(binding.Alternatives.Select(alternative => (alternative.Condition, alternative.Location, "This 'when' alternative is shadowed by earlier alternatives in this interaction")), context);

    static void Validate(IEnumerable<(ConditionSyntax Condition, SourceLocation Location, string Message)> alternatives, ParserContext context)
    {
        var earlier = new List<HashSet<Comparison>>();
        foreach (var alternative in alternatives)
        {
            var terms = Normalize(alternative.Condition);
            if (terms is null) continue;
            if (terms.Count > 0 && terms.TrueForAll(term => earlier.Exists(previous => previous.IsSubsetOf(term))))
            {
                context.Warning(DiagnosticCodes.UnreachableActionAlternative, alternative.Message, alternative.Location);
            }

            earlier.AddRange(terms);
        }
    }

    static List<HashSet<Comparison>>? Normalize(ConditionSyntax condition)
    {
        if (condition is ComparisonConditionSyntax { Right: LiteralExpressionSyntax literal } comparison)
        {
            return [[new(comparison.Left, comparison.Operator, literal.Value)]];
        }

        if (condition is not LogicalConditionSyntax logical) return null;
        var left = Normalize(logical.Left);
        var right = Normalize(logical.Right);
        if (left is null || right is null) return null;
        if (logical.Operator == LogicalOperator.Or)
        {
            return left.Count + right.Count > MaximumDisjuncts ? null : [.. left, .. right];
        }

        if (logical.Operator != LogicalOperator.And || left.Count * right.Count > MaximumDisjuncts) return null;
        return [.. left.SelectMany(first => right.Select(second => new HashSet<Comparison>(first.Concat(second))))];
    }

    sealed record Comparison(string Path, ComparisonOperator Operator, object? Value);
}
