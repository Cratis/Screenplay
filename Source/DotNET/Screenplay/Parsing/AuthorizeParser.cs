// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>authorize</c> declarations - the policies a caller has to satisfy, and how they combine.
/// </summary>
internal static partial class AuthorizeParser
{
    static readonly LogicalConditionDiagnostics _diagnostics = new(
        DiagnosticCodes.UnexpectedTokenInAuthorize,
        DiagnosticCodes.UnclosedAuthorizeGroup,
        "authorize");

    /// <summary>
    /// Parses an <c>authorize</c> declaration from its already consumed first line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the <c>authorize</c> keyword.</param>
    /// <returns>The parsed <see cref="AuthorizeSyntax"/>, or <c>null</c> when it requires nothing.</returns>
    public static AuthorizeSyntax? Parse(ParserContext context, SourceLine line)
    {
        var text = line.Content["authorize".Length..];
        while (context.TryPeekChild(line.Indent, out var child) && ContinuationRegex().IsMatch(child.Content))
        {
            context.Reader.TakeSignificant();
            text += " " + child.Content;
        }

        var tokens = Tokenize(text);
        if (tokens.Count == 0)
        {
            context.Error(DiagnosticCodes.AuthorizeWithoutPolicy, "Expected at least one policy after 'authorize'", line.Location);
            return null;
        }

        var requirement = LogicalConditionParser.Parse<PolicyRequirementSyntax>(
            context,
            tokens,
            line.Location,
            ParseReference,
            static (left, @operator, right, location) => new LogicalPolicyRequirementSyntax(left, @operator, right, location),
            _diagnostics);

        return requirement is null ? null : new AuthorizeSyntax(requirement, line.Location);
    }

    static PolicyRequirementSyntax? ParseReference(ParserContext context, IReadOnlyList<string> tokens, ref int position, SourceLocation location)
    {
        if (position >= tokens.Count)
        {
            context.Error(DiagnosticCodes.AuthorizeWithoutPolicy, "Expected a policy name", location);
            return null;
        }

        var token = tokens[position++];
        if (!NameRegex().IsMatch(token))
        {
            context.Error(DiagnosticCodes.InvalidPolicyReference, $"Invalid policy reference '{token}' - policy names are PascalCase identifiers", location);
            return null;
        }

        return new PolicyReferenceSyntax(token, location);
    }

    /// <summary>
    /// Splits the text of an authorize into tokens, making the conjunction between adjacent policies explicit.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    /// <returns>The tokens, with a synthesized <c>and</c> wherever two operands sit side by side.</returns>
    /// <remarks>
    /// <c>authorize A B</c> has always meant both, by writing them next to each other. Turning that into the
    /// <c>and</c> it already means lets the one condition grammar parse this too, so <c>A or B and C</c>
    /// groups here exactly as it groups in a policy - which is the whole point: two policies next to each
    /// other and two policies joined by <c>or</c> used to produce a flat list that could not say which.
    /// </remarks>
    static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        foreach (var token in TokenRegex().Matches(text).Select(_ => _.Value))
        {
            if (tokens.Count > 0 && IsOperandEnd(tokens[^1]) && IsOperandStart(token))
            {
                tokens.Add("and");
            }

            tokens.Add(token);
        }

        return tokens;
    }

    static bool IsOperandEnd(string token) => token == ")" || IsName(token);

    static bool IsOperandStart(string token) => token == "(" || IsName(token);

    static bool IsName(string token) => token is not ("(" or ")" or "and" or "or");

    [GeneratedRegex(@"^(?:(?:or|and)\s+)?[A-Za-z_(][\w\s()]*$", RegexOptions.None, 1000)]
    private static partial Regex ContinuationRegex();

    [GeneratedRegex(@"\(|\)|[A-Za-z_]\w*", RegexOptions.None, 1000)]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"^[A-Z]\w*$", RegexOptions.None, 1000)]
    private static partial Regex NameRegex();
}
