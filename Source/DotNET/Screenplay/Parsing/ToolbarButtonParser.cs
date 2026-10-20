// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the <c>button</c> lines of a <c>toolbar</c> - buttons that invoke State Change slices.
/// </summary>
/// <remarks>
/// A button is <c>button &lt;id&gt; title "..." [slice &lt;Slice&gt;] [emphasis primary|secondary|quiet] [icon X]</c>, optionally followed by an indented <c>authorize</c> line. The parser reads
/// the grammar only; whether the slice exists and is a State Change slice is checked by <see cref="ToolbarButtonValidator"/>.
/// </remarks>
internal static partial class ToolbarButtonParser
{
    /// <summary>
    /// What a toolbar button line may hold, as it reads in a diagnostic.
    /// </summary>
    public const string Expected = "button <id> title \"<text>\" [slice <Module>.<Feature>.<Slice>] [emphasis primary|secondary|quiet] [icon <name>]";

    /// <summary>
    /// Parses a toolbar button from its already consumed line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the <c>button</c> line.</param>
    /// <returns>The parsed <see cref="ToolbarButtonSyntax"/>, or <c>null</c> when the line has no usable id.</returns>
    public static ToolbarButtonSyntax? Parse(ParserContext context, SourceLine line)
    {
        var head = ButtonRegex().Match(line.Content);
        if (!head.Success)
        {
            context.Error(DiagnosticCodes.InvalidToolbarButton, $"Invalid toolbar button '{line.Content}' - expected {Expected}", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var id = head.Groups[1].Success ? StringLiteral.Unescape(head.Groups[1].Value) : head.Groups[2].Value;
        string? title = null;
        string? slice = null;
        string? icon = null;
        ToolbarEmphasis? emphasis = null;
        var rest = head.Groups[3].Value;
        var position = 0;
        var valid = true;

        while (position < rest.Length)
        {
            var part = PartRegex().Match(rest, position);
            if (!part.Success || part.Index != position)
            {
                context.Error(DiagnosticCodes.InvalidToolbarButton, $"Invalid toolbar button '{line.Content}' - unexpected '{rest[position..].Trim()}'; expected {Expected}", line.Location);
                valid = false;
                break;
            }

            position = part.Groups["end"].Index;
            var key = part.Groups[1].Value;
            var quoted = part.Groups[2].Success;
            var localized = part.Groups[3].Success;
            var value = Value(part);
            if (IsRepeated(key, title, slice, icon, emphasis))
            {
                context.Error(DiagnosticCodes.InvalidToolbarButton, $"Toolbar button '{id}' states '{key}' more than once", line.Location);
                continue;
            }

            switch (key)
            {
                case "title":
                    if (quoted || localized)
                    {
                        title = value;
                    }
                    else
                    {
                        context.Error(DiagnosticCodes.InvalidToolbarButton, $"Invalid title '{value}' on toolbar button '{id}' - expected a quoted string or a $strings.<key> token", line.Location);
                        valid = false;
                    }

                    break;
                case "slice":
                    if (!quoted && !localized && SliceRegex().IsMatch(value))
                    {
                        slice = value;
                    }
                    else
                    {
                        context.Error(DiagnosticCodes.InvalidToolbarButton, $"Invalid slice '{value}' on toolbar button '{id}' - expected <Module>.<Feature>.<Slice>", line.Location);
                        valid = false;
                    }

                    break;
                case "emphasis":
                    if (!quoted && !localized && ParseEmphasis(value) is { } parsed)
                    {
                        emphasis = parsed;
                    }
                    else
                    {
                        context.Error(DiagnosticCodes.InvalidToolbarButton, $"Invalid emphasis '{value}' on toolbar button '{id}' - expected primary, secondary or quiet", line.Location);
                        valid = false;
                    }

                    break;
                default:
                    icon = value;
                    break;
            }
        }

        if (title is null && valid)
        {
            context.Error(DiagnosticCodes.InvalidToolbarButton, $"Toolbar button '{id}' has no title - expected 'button {id} title \"<text>\"'", line.Location);
        }

        AuthorizeSyntax? authorize = null;
        string? confirm = null;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) == "authorize" && authorize is null)
            {
                authorize = AuthorizeParser.Parse(context, child);
                continue;
            }

            if (confirm is null && ConfirmRegex().Match(child.Content) is { Success: true } confirmMatch)
            {
                confirm = confirmMatch.Groups[1].Success ? StringLiteral.Unescape(confirmMatch.Groups[1].Value) : confirmMatch.Groups[2].Value;
                continue;
            }

            context.Error(DiagnosticCodes.InvalidToolbarButton, $"Unexpected '{child.Content}' under toolbar button '{id}' - a button takes one 'authorize' line, one 'confirm' line and nothing else", child.Location);
            context.SkipBlock(child.Indent);
        }

        return new(id, title ?? string.Empty, line.Location) { Slice = slice, Emphasis = emphasis, Icon = icon, Authorize = authorize, Confirm = confirm };
    }

    static ToolbarEmphasis? ParseEmphasis(string value) => value switch
    {
        "primary" => ToolbarEmphasis.Primary,
        "secondary" => ToolbarEmphasis.Secondary,
        "quiet" => ToolbarEmphasis.Quiet,
        _ => null
    };

    static bool IsRepeated(string key, string? title, string? slice, string? icon, ToolbarEmphasis? emphasis) => key switch
    {
        "title" => title is not null,
        "slice" => slice is not null,
        "icon" => icon is not null,
        _ => emphasis is not null
    };

    static string Value(Match part)
    {
        if (part.Groups[2].Success)
        {
            return StringLiteral.Unescape(part.Groups[2].Value);
        }

        return part.Groups[3].Success ? part.Groups[3].Value : part.Groups[4].Value;
    }

    [GeneratedRegex("^button\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|([A-Za-z_][\\w-]*))(?:\\s+(.*))?$", RegexOptions.NonBacktracking)]
    private static partial Regex ButtonRegex();

    [GeneratedRegex("\\s*(title|slice|emphasis|icon)\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*)|([A-Za-z_][\\w.-]*))(?<end>\\s|$)", RegexOptions.NonBacktracking)]
    private static partial Regex PartRegex();

    [GeneratedRegex(@"^[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*$", RegexOptions.NonBacktracking)]
    private static partial Regex SliceRegex();

    [GeneratedRegex("^confirm\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*))$", RegexOptions.NonBacktracking)]
    private static partial Regex ConfirmRegex();
}
