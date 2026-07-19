// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>produces when</c> conditions, such as <c>status == "sent" or status == "paid"</c>.
/// </summary>
internal static partial class ConditionParser
{
    /// <summary>
    /// Parses a condition.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="text">The condition text.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the condition.</param>
    /// <returns>The parsed <see cref="ConditionSyntax"/>, or <c>null</c> when the condition is malformed.</returns>
    public static ConditionSyntax? Parse(ParserContext context, string text, SourceLocation location)
    {
        var tokens = Tokenize(text);
        var position = 0;
        var condition = ParseOr(context, tokens, ref position, location);
        if (condition is not null && position < tokens.Count)
        {
            context.Error($"Unexpected '{tokens[position]}' in condition", location);
        }

        return condition;
    }

    static ConditionSyntax? ParseOr(ParserContext context, IReadOnlyList<string> tokens, ref int position, SourceLocation location)
    {
        var left = ParseAnd(context, tokens, ref position, location);
        while (left is not null && position < tokens.Count && tokens[position] == "or")
        {
            position++;
            var right = ParseAnd(context, tokens, ref position, location);
            if (right is null)
            {
                return null;
            }

            left = new LogicalConditionSyntax(left, LogicalOperator.Or, right, location);
        }

        return left;
    }

    static ConditionSyntax? ParseAnd(ParserContext context, IReadOnlyList<string> tokens, ref int position, SourceLocation location)
    {
        var left = ParsePrimary(context, tokens, ref position, location);
        while (left is not null && position < tokens.Count && tokens[position] == "and")
        {
            position++;
            var right = ParsePrimary(context, tokens, ref position, location);
            if (right is null)
            {
                return null;
            }

            left = new LogicalConditionSyntax(left, LogicalOperator.And, right, location);
        }

        return left;
    }

    static ConditionSyntax? ParsePrimary(ParserContext context, IReadOnlyList<string> tokens, ref int position, SourceLocation location)
    {
        if (position >= tokens.Count)
        {
            context.Error("Expected a condition", location);
            return null;
        }

        if (tokens[position] == "(")
        {
            position++;
            var condition = ParseOr(context, tokens, ref position, location);
            if (position < tokens.Count && tokens[position] == ")")
            {
                position++;
            }
            else
            {
                context.Error("Expected ')' in condition", location);
            }

            return condition;
        }

        var left = tokens[position++];
        if (position >= tokens.Count || ParseOperator(tokens[position]) is not { } comparison)
        {
            context.Error($"Expected a comparison operator after '{left}'", location);
            return null;
        }

        position++;
        if (position >= tokens.Count)
        {
            context.Error("Expected a value to compare against", location);
            return null;
        }

        var right = ExpressionParser.ParseMappingSource(tokens[position++], location);
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

    [GeneratedRegex("\"[^\"]*\"|==|!=|>=|<=|>|<|\\(|\\)|[\\w.$-]+", RegexOptions.None, 1000)]
    private static partial Regex TokenRegex();
}
