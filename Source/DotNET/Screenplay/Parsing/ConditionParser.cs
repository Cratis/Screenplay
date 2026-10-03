// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>produces when</c> conditions, such as <c>status == "sent" or status == "paid"</c>.
/// </summary>
internal static partial class ConditionParser
{
    static readonly LogicalConditionDiagnostics _diagnostics = new(
        DiagnosticCodes.UnexpectedTokenInCondition,
        DiagnosticCodes.UnclosedConditionGroup,
        "condition");

    /// <summary>
    /// Parses a condition.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="text">The condition text.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the condition.</param>
    /// <returns>The parsed <see cref="ConditionSyntax"/>, or <c>null</c> when the condition is malformed.</returns>
    public static ConditionSyntax? Parse(ParserContext context, string text, SourceLocation location) =>
        LogicalConditionParser.Parse<ConditionSyntax>(
            context,
            Tokenize(text),
            location,
            ParseComparison,
            static (left, @operator, right, location) => new LogicalConditionSyntax(left, @operator, right, location),
            _diagnostics);

    static ConditionSyntax? ParseComparison(ParserContext context, IReadOnlyList<string> tokens, ref int position, SourceLocation location)
    {
        if (position >= tokens.Count)
        {
            context.Error(DiagnosticCodes.ExpectedCondition, "Expected a condition", location);
            return null;
        }

        var left = tokens[position++];
        if (position >= tokens.Count || ParseOperator(tokens, position, out var width) is not { } comparison)
        {
            context.Error(DiagnosticCodes.ExpectedComparisonOperator, $"Expected a comparison operator after '{left}'", location);
            return null;
        }

        position += width;
        if (position >= tokens.Count)
        {
            context.Error(DiagnosticCodes.ExpectedComparisonValue, "Expected a value to compare against", location);
            return null;
        }

        var right = ExpressionParser.ParseMappingSource(context, tokens[position++], location);
        return new ComparisonConditionSyntax(left, comparison, right, location);
    }

    /// <summary>
    /// Reads the operator at a position, and how many tokens it spans.
    /// </summary>
    /// <param name="tokens">The tokens of the condition.</param>
    /// <param name="position">Where the operator starts.</param>
    /// <param name="width">How many tokens the operator spans.</param>
    /// <returns>The <see cref="ComparisonOperator"/>, or <c>null</c> when no operator is there.</returns>
    /// <remarks>
    /// The word operators are how a condition about text reads aloud - <c>starts with</c> is two words
    /// because that is the phrase, not because the grammar needed a separator. So an operator is not always
    /// one token, and the width says how far to step.
    /// </remarks>
    static ComparisonOperator? ParseOperator(IReadOnlyList<string> tokens, int position, out int width)
    {
        width = 1;
        if (tokens[position] == "starts" && position + 1 < tokens.Count && tokens[position + 1] == "with")
        {
            width = 2;
            return ComparisonOperator.StartsWith;
        }

        return tokens[position] switch
        {
            "==" => ComparisonOperator.Equal,
            "!=" => ComparisonOperator.NotEqual,
            ">" => ComparisonOperator.GreaterThan,
            ">=" => ComparisonOperator.GreaterThanOrEqual,
            "<" => ComparisonOperator.LessThan,
            "<=" => ComparisonOperator.LessThanOrEqual,
            "contains" => ComparisonOperator.Contains,
            _ => null
        };
    }

    static List<string> Tokenize(string text) =>
        [.. TokenRegex().Matches(text).Select(_ => _.Value)];

    [GeneratedRegex("\"" + StringLiteral.BodyPattern + "\"|==|!=|>=|<=|>|<|\\(|\\)|[\\w.$-]+", RegexOptions.None, 1000)]
    private static partial Regex TokenRegex();
}
