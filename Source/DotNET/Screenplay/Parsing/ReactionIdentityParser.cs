// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

internal static partial class ReactionIdentityParser
{
    internal static bool IsDeclarationLine(SourceLine line) => PrefixRegex().IsMatch(line.Content);

    internal static ReactionIdentitySyntax? Parse(ParserContext context, SourceLine line, bool repeated)
    {
        if (repeated)
        {
            context.Error(DiagnosticCodes.InvalidReactionIdentity, "A reaction may declare 'runs as' only once.", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var match = IdentityRegex().Match(line.Content);
        if (!match.Success || context.TryPeekChild(line.Indent, out _))
        {
            context.Error(DiagnosticCodes.InvalidReactionIdentity, "Expected one reaction-level line: 'runs as system [role \"<Role>\" and role \"<Role>\"]'.", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var roles = match.Groups["role"].Captures.Select(capture => StringLiteral.Unescape(capture.Value)).ToArray();
        if (roles.Any(string.IsNullOrEmpty) || roles.Distinct(StringComparer.Ordinal).Count() != roles.Length)
        {
            context.Error(DiagnosticCodes.InvalidReactionIdentity, "Reaction identity roles must be nonempty and distinct quoted literals.", line.Location);
            return null;
        }

        return new("system", roles, line.Location);
    }

    internal static void Misplaced(ParserContext context, SourceLine line)
    {
        context.Error(DiagnosticCodes.InvalidReactionIdentity, "'runs as' belongs directly in a reaction body, not under a trigger or invocation.", line.Location);
        context.SkipBlock(line.Indent);
    }

    [GeneratedRegex(@"^runs\s+as\b", RegexOptions.None, 1000)]
    private static partial Regex PrefixRegex();

    [GeneratedRegex(@"^runs\s+as\s+system(?:\s+role\s+""(?<role>" + StringLiteral.BodyPattern + @")""(?:\s+and\s+role\s+""(?<role>" + StringLiteral.BodyPattern + @")"")*)?$", RegexOptions.None, 1000)]
    private static partial Regex IdentityRegex();
}
