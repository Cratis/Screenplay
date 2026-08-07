// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Names the diagnostics a construct reports for the parts of a condition that belong to the shared grammar.
/// </summary>
/// <param name="UnexpectedToken">The code reported when tokens are left over after the condition.</param>
/// <param name="UnclosedGroup">The code reported when a parenthesised group is never closed.</param>
/// <param name="Subject">What the construct calls its condition in a message - <c>condition</c>, <c>policy condition</c>.</param>
internal sealed record LogicalConditionDiagnostics(string UnexpectedToken, string UnclosedGroup, string Subject);

/// <summary>
/// Parses the <c>and</c> / <c>or</c> layer shared by every condition in the language.
/// </summary>
/// <remarks>
/// The language has one condition grammar, not one per construct. Constructs differ only in what an operand
/// is - a policy matches a role or a claim, a <c>produces when</c> compares a property against a value - so
/// each supplies its own operand parser and its own node type, and the rules for combining them live here
/// once. Two grammars is how the language ended up meaning two different things by the same text.
/// <para>
/// <c>and</c> binds tighter than <c>or</c> and parentheses override that, which is what a general purpose
/// language does and therefore what a reader already expects: <c>a or b and c</c> is <c>a or (b and c)</c>.
/// Both operators are left associative, so <c>a or b or c</c> is <c>(a or b) or c</c>.
/// </para>
/// </remarks>
internal static class LogicalConditionParser
{
    /// <summary>
    /// Parses a single operand - everything a condition is made of that is not <c>and</c>, <c>or</c> or a group.
    /// </summary>
    /// <typeparam name="T">The type of condition node the construct builds.</typeparam>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="tokens">The tokens of the condition.</param>
    /// <param name="position">The position to read from, left on the token after the operand.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the condition.</param>
    /// <returns>The parsed operand, or <c>null</c> when it is malformed.</returns>
    internal delegate T? ParseOperand<T>(ParserContext context, IReadOnlyList<string> tokens, ref int position, SourceLocation location)
        where T : class;

    /// <summary>
    /// Parses a condition.
    /// </summary>
    /// <typeparam name="T">The type of condition node the construct builds.</typeparam>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="tokens">The tokens of the condition.</param>
    /// <param name="location">The <see cref="SourceLocation"/> of the condition.</param>
    /// <param name="operand">Parses one operand of the construct's condition.</param>
    /// <param name="combine">Builds the construct's node for two conditions combined with an operator.</param>
    /// <param name="diagnostics">The <see cref="LogicalConditionDiagnostics"/> the construct reports.</param>
    /// <returns>The parsed condition, or <c>null</c> when it is malformed.</returns>
    public static T? Parse<T>(
        ParserContext context,
        IReadOnlyList<string> tokens,
        SourceLocation location,
        ParseOperand<T> operand,
        Func<T, LogicalOperator, T, SourceLocation, T> combine,
        LogicalConditionDiagnostics diagnostics)
        where T : class
    {
        var position = 0;
        var condition = ParseOr(context, tokens, ref position, location, operand, combine, diagnostics);
        if (condition is not null && position < tokens.Count)
        {
            context.Error(diagnostics.UnexpectedToken, $"Unexpected '{tokens[position]}' in {diagnostics.Subject}", location);
        }

        return condition;
    }

    static T? ParseOr<T>(
        ParserContext context,
        IReadOnlyList<string> tokens,
        ref int position,
        SourceLocation location,
        ParseOperand<T> operand,
        Func<T, LogicalOperator, T, SourceLocation, T> combine,
        LogicalConditionDiagnostics diagnostics)
        where T : class
    {
        var left = ParseAnd(context, tokens, ref position, location, operand, combine, diagnostics);
        while (left is not null && position < tokens.Count && tokens[position] == "or")
        {
            position++;
            var right = ParseAnd(context, tokens, ref position, location, operand, combine, diagnostics);
            if (right is null)
            {
                return null;
            }

            left = combine(left, LogicalOperator.Or, right, location);
        }

        return left;
    }

    static T? ParseAnd<T>(
        ParserContext context,
        IReadOnlyList<string> tokens,
        ref int position,
        SourceLocation location,
        ParseOperand<T> operand,
        Func<T, LogicalOperator, T, SourceLocation, T> combine,
        LogicalConditionDiagnostics diagnostics)
        where T : class
    {
        var left = ParseGroupOrOperand(context, tokens, ref position, location, operand, combine, diagnostics);
        while (left is not null && position < tokens.Count && tokens[position] == "and")
        {
            position++;
            var right = ParseGroupOrOperand(context, tokens, ref position, location, operand, combine, diagnostics);
            if (right is null)
            {
                return null;
            }

            left = combine(left, LogicalOperator.And, right, location);
        }

        return left;
    }

    static T? ParseGroupOrOperand<T>(
        ParserContext context,
        IReadOnlyList<string> tokens,
        ref int position,
        SourceLocation location,
        ParseOperand<T> operand,
        Func<T, LogicalOperator, T, SourceLocation, T> combine,
        LogicalConditionDiagnostics diagnostics)
        where T : class
    {
        if (position >= tokens.Count || tokens[position] != "(")
        {
            return operand(context, tokens, ref position, location);
        }

        position++;
        var condition = ParseOr(context, tokens, ref position, location, operand, combine, diagnostics);
        if (position < tokens.Count && tokens[position] == ")")
        {
            position++;
        }
        else
        {
            context.Error(diagnostics.UnclosedGroup, $"Expected ')' in {diagnostics.Subject}", location);
        }

        return condition;
    }
}
