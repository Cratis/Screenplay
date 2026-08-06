// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>policy</c> declarations with their <c>require</c> conditions or inline code.
/// </summary>
internal static partial class PolicyParser
{
    /// <summary>
    /// Parses a policy from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>policy</c> header.</param>
    /// <returns>The parsed <see cref="PolicySyntax"/>.</returns>
    public static PolicySyntax Parse(ParserContext context, SourceLine header)
    {
        var name = HeaderRegex().Match(header.Content);
        if (!name.Success)
        {
            context.Error(DiagnosticCodes.InvalidPolicyDeclaration, $"Invalid policy declaration '{header.Content}' - expected 'policy <Name>'", header.Location);
        }

        PolicyConditionSyntax? condition = null;
        CodeBlockSyntax? code = null;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "require")
            {
                var text = line.Content["require".Length..].Trim();
                while (context.TryPeekChild(line.Indent, out var continuation))
                {
                    context.Reader.TakeSignificant();
                    text += $" {continuation.Content}";
                }

                condition = ParseCondition(context, text, line.Location);
            }
            else if (CodeBlockParser.Languages.Contains(line.Content))
            {
                code = CodeBlockParser.Parse(context, line.Content, line);
            }
            else
            {
                context.Error(DiagnosticCodes.UnknownPolicyDirective, $"Unexpected '{line.Content}' in policy body - expected 'require ...' or an inline code block", line.Location);
                context.SkipBlock(line.Indent);
            }
        }

        if (condition is null && code is null)
        {
            context.Error(DiagnosticCodes.PolicyWithoutRequirement, $"Policy '{name.Groups[1].Value}' must declare a 'require' condition or an inline code block", header.Location);
        }

        return new(name.Groups[1].Value, condition, code, header.Location);
    }

    static PolicyConditionSyntax? ParseCondition(ParserContext context, string text, SourceLocation location)
    {
        var tokens = Tokenize(text);
        var position = 0;
        var condition = ParseCombined(context, tokens, ref position, location);
        if (condition is not null && position < tokens.Count)
        {
            context.Error(DiagnosticCodes.UnexpectedTokenInPolicyCondition, $"Unexpected '{tokens[position]}' in policy condition", location);
        }

        return condition;
    }

    static PolicyConditionSyntax? ParseCombined(ParserContext context, IReadOnlyList<string> tokens, ref int position, SourceLocation location)
    {
        var left = ParsePrimary(context, tokens, ref position, location);
        while (left is not null && position < tokens.Count && (tokens[position] == "and" || tokens[position] == "or"))
        {
            var combinator = tokens[position++] == "and" ? LogicalOperator.And : LogicalOperator.Or;
            var right = ParsePrimary(context, tokens, ref position, location);
            if (right is null)
            {
                return null;
            }

            left = new LogicalPolicyConditionSyntax(left, combinator, right, location);
        }

        return left;
    }

    static PolicyConditionSyntax? ParsePrimary(ParserContext context, IReadOnlyList<string> tokens, ref int position, SourceLocation location)
    {
        if (position >= tokens.Count)
        {
            context.Error(DiagnosticCodes.ExpectedPolicyCondition, "Expected a policy condition", location);
            return null;
        }

        switch (tokens[position])
        {
            case "(":
                position++;
                var condition = ParseCombined(context, tokens, ref position, location);
                if (position < tokens.Count && tokens[position] == ")")
                {
                    position++;
                }
                else
                {
                    context.Error(DiagnosticCodes.UnclosedPolicyConditionGroup, "Expected ')' in policy condition", location);
                }

                return condition;

            case "authenticated":
                position++;
                return new AuthenticatedConditionSyntax(location);

            case "role":
                position++;
                if (position >= tokens.Count || !IsString(tokens[position]))
                {
                    context.Error(DiagnosticCodes.ExpectedRoleName, "Expected a quoted role name after 'role'", location);
                    return null;
                }

                return new RoleConditionSyntax(Unquote(tokens[position++]), location);

            case "claim":
                position++;
                if (position >= tokens.Count || !IsString(tokens[position]))
                {
                    context.Error(DiagnosticCodes.ExpectedClaimName, "Expected a quoted claim name after 'claim'", location);
                    return null;
                }

                var claim = Unquote(tokens[position++]);
                if (position >= tokens.Count || tokens[position] != "matches")
                {
                    context.Error(DiagnosticCodes.ExpectedClaimMatches, "Expected 'matches' after the claim name", location);
                    return null;
                }

                position++;
                if (position >= tokens.Count)
                {
                    context.Error(DiagnosticCodes.ExpectedClaimMatchTarget, "Expected 'subject' or a value after 'matches'", location);
                    return null;
                }

                var target = tokens[position++];
                return target == "subject"
                    ? new ClaimConditionSyntax(claim, true, null, location)
                    : new ClaimConditionSyntax(claim, false, ExpressionParser.ParseMappingSource(context, target, location), location);

            default:
                context.Error(DiagnosticCodes.UnexpectedTokenInPolicyCondition, $"Unexpected '{tokens[position]}' in policy condition", location);
                return null;
        }
    }

    static bool IsString(string token) => token.Length >= 2 && token.StartsWith('"') && token.EndsWith('"');

    static string Unquote(string token) => StringLiteral.Unescape(token[1..^1]);

    static List<string> Tokenize(string text) =>
        [.. TokenRegex().Matches(text).Select(_ => _.Value)];

    [GeneratedRegex(@"^policy\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex("\"" + StringLiteral.BodyPattern + "\"|\\(|\\)|[\\w.$]+", RegexOptions.None, 1000)]
    private static partial Regex TokenRegex();
}
