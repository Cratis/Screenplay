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
        if (position >= tokens.Count || ParseOperator(tokens[position]) is not { } comparison)
        {
            context.Error(DiagnosticCodes.ExpectedComparisonOperator, $"Expected a comparison operator after '{left}'", location);
            return null;
        }

        position++;
        if (position >= tokens.Count)
        {
            context.Error(DiagnosticCodes.ExpectedComparisonValue, "Expected a value to compare against", location);
            return null;
        }

        var right = ExpressionParser.ParseMappingSource(context, tokens[position++], location);
        return new ComparisonConditionSyntax(left, comparison, right, location);
    }

    static ComparisonOperator? ParseOperator(string token) => token switch
    {
        "==" => ComparisonOperator.Equal,
        "!=" => ComparisonOperator.NotEqual,
        ">" => ComparisonOperator.GreaterThan,
        ">=" => ComparisonOperator.GreaterThanOrEqual,
        "<" => ComparisonOperator.LessThan,
        "<=" => ComparisonOperator.LessThanOrEqual,
        _ => null
    };

    static List<string> Tokenize(string text) =>
        [.. TokenRegex().Matches(text).Select(_ => _.Value)];

    [GeneratedRegex("\"" + StringLiteral.BodyPattern + "\"|==|!=|>=|<=|>|<|\\(|\\)|[\\w.$-]+", RegexOptions.None, 1000)]
    private static partial Regex TokenRegex();
}
