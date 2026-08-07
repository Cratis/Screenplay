// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>require</c> rules - a condition the whole artifact must satisfy, with its message in the body.
/// </summary>
internal static partial class RequirementParser
{
    /// <summary>
    /// Parses a requirement from its already consumed <c>require</c> line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the <c>require</c> line.</param>
    /// <returns>The parsed <see cref="RequirementSyntax"/>, or <c>null</c> when the rule is malformed.</returns>
    public static RequirementSyntax? Parse(ParserContext context, SourceLine line)
    {
        var match = RequireRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(
                DiagnosticCodes.InvalidRequirement,
                $"Invalid requirement '{line.Content}' - expected 'require <condition>'",
                line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var condition = ConditionParser.Parse(context, match.Groups[1].Value, line.Location);
        string? message = null;

        // The message goes in the body rather than on the end of the line - a condition is as long as the
        // rule it states, and a message pushed out past it is the part nobody reads.
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (MessageRegex().Match(child.Content) is { Success: true } text)
            {
                message = StringLiteral.Unescape(text.Groups[1].Value);
                continue;
            }

            context.Error(
                DiagnosticCodes.UnknownRequirementDirective,
                $"Unexpected '{child.Content}' in requirement body - expected 'message \"<text>\"'",
                child.Location);
            context.SkipBlock(child.Indent);
        }

        return condition is null ? null : new RequirementSyntax(condition, message, line.Location);
    }

    [GeneratedRegex(@"^require\s+(\S.*)$", RegexOptions.None, 1000)]
    private static partial Regex RequireRegex();

    [GeneratedRegex("^message\\s+\"(" + StringLiteral.BodyPattern + ")\"$", RegexOptions.None, 1000)]
    private static partial Regex MessageRegex();
}
