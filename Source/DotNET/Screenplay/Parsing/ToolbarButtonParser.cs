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
    public const string Expected = "button <id> title \"<text>\" [slice <Module>.<Feature>.<Slice>] [emphasis primary|secondary|quiet] [icon <name>] [runs close|submit|refresh] [for item|for screen] [opens dialog|drawer|in slot <name>]";

    static readonly HashSet<string> _builtIns = new(StringComparer.Ordinal) { "close", "submit", "refresh" };

    /// <summary>
    /// Parses a toolbar button from its already consumed line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the <c>button</c> line.</param>
    /// <param name="allowPosition">Whether the button may say <c>before &lt;id&gt;</c> or <c>after &lt;id&gt;</c> - in an outlet a lower level changes.</param>
    /// <returns>The parsed <see cref="ToolbarButtonSyntax"/>, or <c>null</c> when the line has no usable id.</returns>
    public static ToolbarButtonSyntax? Parse(ParserContext context, SourceLine line, bool allowPosition = false)
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
        string? runs = null;
        ToolbarEmphasis? emphasis = null;
        ScreenActionSubjectKind? subject = null;
        HookOpening? opens = null;
        string? opensSlot = null;
        var rest = head.Groups[3].Value;
        var position = 0;
        var valid = true;

        while (position < rest.Length)
        {
            var slot = OpensSlotRegex().Match(rest, position);
            if (slot.Success && slot.Index == position)
            {
                position = slot.Groups["end"].Index;
                if (opens is not null)
                {
                    context.Error(DiagnosticCodes.InvalidToolbarButton, $"Toolbar button '{id}' states 'opens' more than once", line.Location);
                    continue;
                }

                opens = HookOpening.Slot;
                opensSlot = slot.Groups[1].Value;
                continue;
            }

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
            var repeated = key switch
            {
                "runs" => runs is not null,
                "for" => subject is not null,
                "opens" => opens is not null,
                _ => IsRepeated(key, title, slice, icon, emphasis)
            };
            if (repeated)
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
                case "runs":
                    if (!quoted && !localized && _builtIns.Contains(value))
                    {
                        runs = value;
                    }
                    else
                    {
                        context.Error(DiagnosticCodes.InvalidToolbarButton, $"Invalid runs '{value}' on toolbar button '{id}' - a button runs one of the built-ins 'close', 'submit' or 'refresh'", line.Location);
                        valid = false;
                    }

                    break;
                case "for":
                    if (!quoted && !localized && (string.Equals(value, "item", StringComparison.Ordinal) || string.Equals(value, "screen", StringComparison.Ordinal)))
                    {
                        subject = value == "item" ? ScreenActionSubjectKind.Item : ScreenActionSubjectKind.Screen;
                    }
                    else
                    {
                        context.Error(DiagnosticCodes.InvalidToolbarButton, $"Invalid 'for {value}' on toolbar button '{id}' - a button acts 'for item' or 'for screen'", line.Location);
                        valid = false;
                    }

                    break;
                case "opens":
                    if (!quoted && !localized && (string.Equals(value, "dialog", StringComparison.Ordinal) || string.Equals(value, "drawer", StringComparison.Ordinal)))
                    {
                        opens = value == "dialog" ? HookOpening.Dialog : HookOpening.Drawer;
                    }
                    else
                    {
                        context.Error(DiagnosticCodes.InvalidToolbarButton, $"Invalid 'opens {value}' on toolbar button '{id}' - a button opens dialog, drawer or in slot <name>", line.Location);
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
        ConditionSyntax? enabledWhen = null;
        OutletPositionSyntax? placement = null;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (allowPosition && OutletParser.TryParsePosition(child, out var parsed))
            {
                if (placement is not null)
                {
                    OutletParser.ReportRepeatedPosition(context, child, id);
                }

                placement ??= parsed;
                continue;
            }

            if (LineText.FirstWord(child.Content) == "authorize" && authorize is null)
            {
                authorize = AuthorizeParser.Parse(context, child);
                continue;
            }

            if (child.Content == "runs")
            {
                ReadRuns(context, child, id, runs, ref opens);
                continue;
            }

            if (enabledWhen is null && IsEnabledWhen(child.Content, out var enabled))
            {
                enabledWhen = ParseEnabledWhen(context, child, enabled);
                continue;
            }

            if (confirm is null && ConfirmRegex().Match(child.Content) is { Success: true } confirmMatch)
            {
                confirm = confirmMatch.Groups[1].Success ? StringLiteral.Unescape(confirmMatch.Groups[1].Value) : confirmMatch.Groups[2].Value;
                continue;
            }

            context.Error(DiagnosticCodes.InvalidToolbarButton, $"Unexpected '{child.Content}' under toolbar button '{id}' - a button takes one 'runs', 'enabled when', 'confirm' and 'authorize' line and nothing else", child.Location);
            context.SkipBlock(child.Indent);
        }

        return new(id, title ?? string.Empty, line.Location) { Slice = slice, Emphasis = emphasis, Icon = icon, Runs = runs, Position = placement, Authorize = authorize, Confirm = confirm, Subject = subject, Opens = opens, OpensSlot = opensSlot, EnabledWhen = enabledWhen };
    }

    /// <summary>
    /// Parses the condition of an <c>enabled when</c> line, in the guarded-action condition grammar.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <c>enabled when</c> line.</param>
    /// <param name="text">The condition text.</param>
    /// <returns>The parsed <see cref="ConditionSyntax"/>, or <c>null</c> when it does not parse.</returns>
    internal static ConditionSyntax? ParseEnabledWhen(ParserContext context, SourceLine line, string text)
    {
        var condition = ConditionParser.Parse(context, text, line.Location, strict: true);
        if (condition is not null)
        {
            ScreenParser.ValidateGuardedCondition(context, condition);
        }

        return condition;
    }

    /// <summary>
    /// Gets whether a line is an <c>enabled when &lt;condition&gt;</c> line, and its condition text.
    /// </summary>
    /// <param name="content">The line content.</param>
    /// <param name="condition">The condition text, when it is.</param>
    /// <returns><c>true</c> when the line is an <c>enabled when</c> line.</returns>
    internal static bool IsEnabledWhen(string content, out string condition)
    {
        var match = EnabledWhenRegex().Match(content);
        condition = match.Success ? match.Groups[1].Value : string.Empty;
        return match.Success;
    }

    static void ReadRuns(ParserContext context, SourceLine child, string id, string? builtIn, ref HookOpening? opens)
    {
        if (builtIn is not null)
        {
            context.Error(DiagnosticCodes.InvalidToolbarButton, $"Toolbar button '{id}' runs the built-in '{builtIn}' and its slice - a button runs one or the other", child.Location);
            return;
        }

        if (opens is not null)
        {
            var stated = opens == HookOpening.Runs ? "states 'runs' more than once" : "both runs and opens - a button that runs its slice opens nothing";
            context.Error(DiagnosticCodes.InvalidToolbarButton, $"Toolbar button '{id}' {stated}", child.Location);
            return;
        }

        opens = HookOpening.Runs;
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

    [GeneratedRegex("\\s*(title|slice|emphasis|icon|runs|for|opens)\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*)|([A-Za-z_][\\w.-]*))(?<end>\\s|$)", RegexOptions.NonBacktracking)]
    private static partial Regex PartRegex();

    [GeneratedRegex(@"^[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*$", RegexOptions.NonBacktracking)]
    private static partial Regex SliceRegex();

    [GeneratedRegex(@"\s*opens\s+in\s+slot\s+([A-Za-z_][\w-]*)(?<end>\s|$)", RegexOptions.NonBacktracking)]
    private static partial Regex OpensSlotRegex();

    [GeneratedRegex(@"^enabled\s+when\s+(.+)$", RegexOptions.NonBacktracking)]
    private static partial Regex EnabledWhenRegex();

    [GeneratedRegex("^confirm\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*))$", RegexOptions.NonBacktracking)]
    private static partial Regex ConfirmRegex();
}
