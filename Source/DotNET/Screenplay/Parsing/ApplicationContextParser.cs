// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses named application time-zone context members (decision 0132).
/// </summary>
internal static partial class ApplicationContextParser
{
    internal static ApplicationContextSyntax? Parse(ParserContext context, SourceLine header, ApplicationContextSyntax? existing, PlayPlacement placement)
    {
        if (header.Content != "context" || !placement.IsDocument || existing is not null)
        {
            context.Error(DiagnosticCodes.InvalidContextDeclaration, "Declare 'context' once at the application root.", header.Location);
            context.SkipBlock(header.Indent);
            return existing;
        }

        var members = new List<ContextMemberSyntax>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        string? description = null;
        var directives = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (DescriptionParser.TryParse(context, line, ref description, "Context", directives)) continue;
            ParseMember(context, line, members, names);
        }

        return new(members, header.Location) { Description = description, DirectiveLocations = directives };
    }

    static void ParseMember(ParserContext context, SourceLine line, List<ContextMemberSyntax> members, HashSet<string> names)
    {
        if (context.TryPeekChild(line.Indent, out _))
        {
            context.Error(DiagnosticCodes.InvalidContextDeclaration, "A context member cannot have a body.", line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        var match = MemberRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidContextDeclaration, $"Invalid context member '{line.Content}' - expected '<name> time zone'.", line.Location);
            return;
        }

        if (!MemberTypeRegex().IsMatch(match.Groups[2].Value))
        {
            context.Error(DiagnosticCodes.UnknownContextMemberType, $"Unknown context member type '{match.Groups[2].Value}' - expected 'time zone'.", line.Location);
            return;
        }

        var name = LineText.Unescape(match.Groups[1].Value);
        if (name == "timeZone" || ContextExpressionSyntax.KnownRoots.Contains(name, StringComparer.Ordinal))
        {
            context.Error(DiagnosticCodes.ReservedContextMemberName, $"The context member name '{name}' is reserved.", line.Location);
            return;
        }

        if (!names.Add(name))
        {
            context.Error(DiagnosticCodes.DuplicateContextMember, $"The context member '{name}' is already declared.", line.Location);
            return;
        }

        members.Add(new(name, line.Location));
    }

    [GeneratedRegex(@"^(@?[a-z_]\w*)\s+(.+)$", RegexOptions.NonBacktracking)]
    private static partial Regex MemberRegex();

    [GeneratedRegex(@"^time\s+zone$", RegexOptions.NonBacktracking)]
    private static partial Regex MemberTypeRegex();
}
