// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Reads structured values - objects and lists - written as indented blocks.
/// </summary>
/// <remarks>
/// Screenplay has no JSON. An object is a header line followed by one indented <c>member = value</c> line per
/// member; a member holding an object or a list opens another indented block. A list is a header line followed by
/// one <c>item</c> line per element: <c>item &lt;value&gt;</c> for a scalar and a bare <c>item</c> with an indented
/// block for an object or a nested list. A header - a property, a member or an <c>item</c> - ending in <c>none</c> is
/// an empty list and one ending in <c>empty</c> an object without members; any other header opens a block, so a stray
/// word is never mistaken for an empty value. A block whose first line is an <c>item</c> is a list; a member named
/// <c>item</c> is written <c>@item</c>, and a member name that is not an identifier is quoted.
/// </remarks>
internal static partial class StructuredBlockParser
{
    /// <summary>
    /// The keyword introducing one element of a list.
    /// </summary>
    public const string ItemKeyword = "item";

    /// <summary>
    /// The keyword stating an empty list.
    /// </summary>
    public const string NoneKeyword = "none";

    /// <summary>
    /// The keyword stating an object without members.
    /// </summary>
    public const string EmptyKeyword = "empty";

    /// <summary>
    /// Tries to read a <c>&lt;property&gt;</c>, <c>&lt;property&gt; none</c> or <c>&lt;property&gt; empty</c> header line - a property mapping whose value is structured.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to read the block from and report diagnostics to.</param>
    /// <param name="line">The header <see cref="SourceLine"/>, already taken from the reader.</param>
    /// <param name="mapping">The parsed <see cref="PropertyMappingSyntax"/> when the line is a header.</param>
    /// <returns>Whether the line is a structured value header.</returns>
    public static bool TryParseMapping(ParserContext context, SourceLine line, out PropertyMappingSyntax mapping)
    {
        var header = MappingHeaderRegex().Match(line.Content);

        // A bare word with nothing under it is a malformed line, not an empty object - an object always has members.
        if (!header.Success || header.Groups["name"].Value == "streamId" || (Marker(header) is null && !context.TryPeekChild(line.Indent, out _)))
        {
            mapping = null!;
            return false;
        }

        mapping = new(LineText.Unescape(header.Groups["name"].Value), ParseValue(context, line, Marker(header)), line.Location);
        return true;
    }

    /// <summary>
    /// Reads the structured value opened by a header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to read the block from and report diagnostics to.</param>
    /// <param name="header">The header <see cref="SourceLine"/>, already taken from the reader.</param>
    /// <param name="marker"><c>none</c> or <c>empty</c> when the header ends in one, stating an empty list or an object without members; otherwise <c>null</c>.</param>
    /// <returns>The <see cref="ObjectExpressionSyntax"/> or <see cref="ListExpressionSyntax"/>.</returns>
    public static ExpressionSyntax ParseValue(ParserContext context, SourceLine header, string? marker)
    {
        if (marker == NoneKeyword)
        {
            RejectChildren(context, header, "An empty list ('none') has no lines under it.");
            return new ListExpressionSyntax([], header.Location);
        }

        if (marker == EmptyKeyword)
        {
            RejectChildren(context, header, "An object without members ('empty') has no lines under it.");
            return new ObjectExpressionSyntax([], header.Location);
        }

        if (!context.TryPeekChild(header.Indent, out var first))
        {
            context.Error(DiagnosticCodes.InvalidStructuredValue, $"'{header.Content}' opens a structured value - indent its 'member = value' lines or 'item' lines under it, or end it in 'none' for an empty list or 'empty' for an object without members", header.Location);
            return new ObjectExpressionSyntax([], header.Location);
        }

        return IsItem(first.Content) ? ParseList(context, header) : ParseObject(context, header);
    }

    /// <summary>
    /// Reads a scalar written in a structured block, a member value or an <c>item</c> value.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="text">The scalar text.</param>
    /// <param name="location">The <see cref="SourceLocation"/> where the scalar starts.</param>
    /// <returns>The parsed literal, or a recovered expression after reporting a diagnostic.</returns>
    internal static ExpressionSyntax ParseScalar(ParserContext context, string text, SourceLocation location)
    {
        if (text.StartsWith('{') || text.StartsWith('['))
        {
            return ExpressionParser.ParseMappingSource(context, text, location);
        }

        if (text.StartsWith("case.", StringComparison.Ordinal))
        {
            context.Error(DiagnosticCodes.InvalidSpecificationCaseReference, "A case reference fills a whole value position; it cannot occur inside a structured value or expression.", location);
            return new RawExpressionSyntax(text, location);
        }

        if (ExpressionParser.ParseLiteral(context, text, location) is { } literal)
        {
            return literal is LiteralExpressionSyntax value ? value with { RawLocation = location, RawLength = text.Length } : literal;
        }

        context.Error(DiagnosticCodes.InvalidStructuredValue, $"Expected a string, number, true, false or null in a structured value, got '{text}'", location);
        return new RawExpressionSyntax(text, location);
    }

    /// <summary>
    /// Gets the <c>none</c> or <c>empty</c> marker a header match captured in its <c>marker</c> group.
    /// </summary>
    /// <param name="match">The header <see cref="Match"/>.</param>
    /// <returns>The marker, or <c>null</c> when the header opens a block.</returns>
    internal static string? Marker(Match match) => match.Groups["marker"].Success ? match.Groups["marker"].Value : null;

    /// <summary>
    /// Gets the <c>none</c> or <c>empty</c> marker ending a line, such as a typed declaration <c>tags String[] none</c>.
    /// </summary>
    /// <param name="content">The line content.</param>
    /// <returns>The marker, or <c>null</c> when the line does not end in one.</returns>
    internal static string? TrailingMarker(string content)
    {
        if (content.EndsWith($" {NoneKeyword}", StringComparison.Ordinal)) return NoneKeyword;
        return content.EndsWith($" {EmptyKeyword}", StringComparison.Ordinal) ? EmptyKeyword : null;
    }

    static bool IsItem(string content) =>
        content == ItemKeyword || (content.StartsWith(ItemKeyword + " ", StringComparison.Ordinal) && !content[ItemKeyword.Length..].TrimStart().StartsWith('='));

    static ListExpressionSyntax ParseList(ParserContext context, SourceLine header)
    {
        var items = new List<ExpressionSyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (!IsItem(line.Content))
            {
                context.Error(DiagnosticCodes.InvalidStructuredValue, $"A list holds one 'item' line per element, got '{line.Content}'", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            var rest = line.Content[ItemKeyword.Length..].TrimStart();
            if (rest.Length == 0)
            {
                items.Add(ParseValue(context, line, marker: null));
            }
            else if (string.Equals(rest, NoneKeyword, StringComparison.Ordinal) || string.Equals(rest, EmptyKeyword, StringComparison.Ordinal))
            {
                items.Add(ParseValue(context, line, rest));
            }
            else
            {
                items.Add(ParseScalar(context, rest, line.LocationAt(line.Content.Length - rest.Length)));
                RejectChildren(context, line, "An 'item' with a value has no lines under it.");
            }
        }

        return new(items, header.Location);
    }

    static ObjectExpressionSyntax ParseObject(ParserContext context, SourceLine header)
    {
        var members = new List<ObjectMemberSyntax>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var match = MemberRegex().Match(line.Content);
            if (!match.Success)
            {
                context.Error(
                    DiagnosticCodes.InvalidStructuredValue,
                    IsItem(line.Content)
                        ? $"An object holds 'member = value' lines; an 'item' line belongs in a list, got '{line.Content}'"
                        : $"Expected 'member = value', a member opening an indented block, 'member none' or 'member empty', got '{line.Content}'",
                    line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            var nameToken = match.Groups["name"].Value;
            var name = nameToken.StartsWith('"') ? StringLiteral.Unescape(nameToken[1..^1]) : LineText.Unescape(nameToken);
            ExpressionSyntax value;
            if (match.Groups["value"].Success)
            {
                var text = match.Groups["value"].Value;
                value = ParseScalar(context, text, line.LocationAt(match.Groups["value"].Index));
                RejectChildren(context, line, "A member with a value has no lines under it.");
            }
            else
            {
                value = ParseValue(context, line, Marker(match));
            }

            if (!names.Add(name))
            {
                context.Error(DiagnosticCodes.DuplicateStructuredValueMember, $"Duplicate property '{name}' in structured value", line.Location);
                continue;
            }

            members.Add(new ObjectMemberSyntax(name, value, line.Location)
            {
                RawLocation = line.Location,
                RawLength = nameToken.Length
            });
        }

        return new(members, header.Location);
    }

    static void RejectChildren(ParserContext context, SourceLine line, string message)
    {
        if (context.TryPeekChild(line.Indent, out var child))
        {
            context.Error(DiagnosticCodes.InvalidStructuredValue, message, child.Location);
            context.SkipBlock(line.Indent);
        }
    }

    [GeneratedRegex(@"^(?<name>@?[\p{L}_][\p{L}\p{N}_]*(?:\.@?[\p{L}_][\p{L}\p{N}_]*)*)(?:\s+(?<marker>none|empty))?$", RegexOptions.NonBacktracking)]
    private static partial Regex MappingHeaderRegex();

    [GeneratedRegex("^(?<name>@?[\\p{L}_][\\p{L}\\p{N}_]*|\"" + StringLiteral.BodyPattern + "\")(?:\\s*=\\s*(?<value>\\S.*)|(?:\\s+(?<marker>none|empty))?)$", RegexOptions.NonBacktracking)]
    private static partial Regex MemberRegex();
}
