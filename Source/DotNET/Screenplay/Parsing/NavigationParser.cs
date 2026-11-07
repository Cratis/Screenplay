// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the items of a navigation outlet - <c>item</c> lines, which may nest further <c>item</c> lines - and the
/// application's <c>logo</c>.
/// </summary>
/// <remarks>
/// The parser only reads the grammar. Whether the combination makes sense - logos in modules, targets that exist - is
/// checked by <see cref="OutletValidator"/> over the whole document.
/// </remarks>
internal static partial class NavigationParser
{
    /// <summary>
    /// What a navigation item line holds, as it reads in a diagnostic.
    /// </summary>
    public const string Expected = "item <id> title \"<text>\" [icon <name>] [target <Module>[.<Feature>]] [slot <name>]";

    /// <summary>
    /// Determines whether a line inside a layout or module is a Screenplay 5 navigation region rather than a slot named <c>navigation</c>.
    /// </summary>
    /// <param name="content">The content of the line.</param>
    /// <returns><c>true</c> when the line is <c>navigation topbar</c> or <c>navigation sidebar</c>, with or without a mode.</returns>
    public static bool IsRegionHeader(string content) => RegionHeaderRegex().IsMatch(content);

    /// <summary>
    /// Reports a Screenplay 5 navigation region and reads its block so a migration can rewrite it.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report to.</param>
    /// <param name="header">The consumed <c>navigation topbar|sidebar</c> line.</param>
    /// <param name="owner">What declares it - <c>layout</c> or <c>module</c>.</param>
    /// <returns>The <see cref="RetiredNavigationRegion"/> read.</returns>
    public static RetiredNavigationRegion ReportRegion(ParserContext context, SourceLine header, string owner)
    {
        var match = RegionHeaderRegex().Match(header.Content);
        var region = match.Success ? match.Groups[1].Value : "<name>";
        var replacement = owner == "layout"
            ? $"outlet {region} of navigation"
            : $"outlet {region} {(match.Groups[2].Success ? match.Groups[2].Value : "extends")}";
        context.Error(DiagnosticCodes.RemovedNavigationRegion, SlotsAndOutletsMigration.Replaced($"'{header.Content}' in a {owner}", replacement), header.Location);

        // The region is reported once, as a whole; what it holds is read quietly for the migration to judge.
        var quiet = context.Quiet();
        NavigationLogoSyntax? logo = null;
        var items = new List<OutletItemSyntax>();
        var unrecognized = new List<SourceLocation>();
        while (quiet.TryPeekChild(header.Indent, out var child))
        {
            quiet.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "logo":
                    // A malformed or second logo is not one the region can hold; it is left for review like any other line.
                    var parsed = ParseLogo(quiet, child);
                    if (parsed is not null && logo is null)
                    {
                        logo = parsed;
                    }
                    else
                    {
                        unrecognized.Add(child.Location);
                    }

                    break;
                case "item" when ParseItem(quiet, child) is { } item:
                    items.Add(item);
                    break;
                case "button" when ToolbarButtonParser.Parse(quiet, child, allowPosition: false) is { } button:
                    items.Add(button);
                    break;
                default:
                    unrecognized.Add(child.Location);
                    quiet.SkipBlock(child.Indent);
                    break;
            }
        }

        OutletMode? mode = match.Success && match.Groups[2].Success
            ? match.Groups[2].Value switch
            {
                "inherits" => OutletMode.Inherit,
                "overrides" => OutletMode.Override,
                _ => OutletMode.Extend
            }
            : null;
        return new(region, mode, logo, items, unrecognized, header.Location);
    }

    /// <summary>
    /// Parses a <c>logo image "…" title "…"</c> line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed logo line.</param>
    /// <returns>The <see cref="NavigationLogoSyntax"/>, or <c>null</c> when the line is malformed.</returns>
    public static NavigationLogoSyntax? ParseLogo(ParserContext context, SourceLine line)
    {
        var match = LogoRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidNavigationLogo, $"Invalid logo '{line.Content}' - expected 'logo image \"<path>\" title \"<text>\"'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        return new(StringLiteral.Unescape(match.Groups[1].Value), Text(match, 2), line.Location);
    }

    /// <summary>
    /// Parses an <c>item</c> line and its nested sub-items.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed item line.</param>
    /// <param name="allowPosition">Whether the item may say <c>before &lt;id&gt;</c> or <c>after &lt;id&gt;</c>.</param>
    /// <returns>The <see cref="NavigationItemSyntax"/>, or <c>null</c> when the line is malformed.</returns>
    public static NavigationItemSyntax? ParseItem(ParserContext context, SourceLine line, bool allowPosition = false)
    {
        var head = ItemRegex().Match(line.Content);
        if (!head.Success)
        {
            context.Error(DiagnosticCodes.InvalidNavigationItem, $"Invalid navigation item '{line.Content}' - expected {Expected}", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var id = head.Groups[1].Success ? StringLiteral.Unescape(head.Groups[1].Value) : head.Groups[2].Value;
        string? title = null;
        string? icon = null;
        string? slot = null;
        NavigationTargetSyntax? target = null;
        var rest = head.Groups[3].Value;
        var position = 0;
        var valid = true;

        while (position < rest.Length)
        {
            var part = PartRegex().Match(rest, position);
            if (!part.Success || part.Index != position)
            {
                context.Error(DiagnosticCodes.InvalidNavigationItem, $"Invalid navigation item '{line.Content}' - unexpected '{rest[position..].Trim()}'; expected {Expected}", line.Location);
                valid = false;
                break;
            }

            position = part.Groups["end"].Index;
            var key = part.Groups[1].Value;
            var isQuoted = part.Groups[2].Success;
            var isLocalized = part.Groups[3].Success;
            var value = Text(part, 2, 3, 4);
            var duplicate = key switch { "title" => title is not null, "icon" => icon is not null, "slot" => slot is not null, _ => target is not null };
            if (duplicate)
            {
                context.Error(DiagnosticCodes.InvalidNavigationItem, $"Navigation item '{id}' states '{key}' more than once", line.Location);
                continue;
            }

            switch (key)
            {
                case "title":
                    if (!isQuoted && !isLocalized)
                    {
                        context.Error(DiagnosticCodes.InvalidNavigationItem, $"Invalid title '{value}' on navigation item '{id}' - expected a quoted string or a $strings.<key> token", line.Location);
                        valid = false;
                    }
                    else
                    {
                        title = value;
                    }

                    break;
                case "icon":
                    icon = value;
                    break;
                case "slot":
                    if (isQuoted || isLocalized || !IdentifierRegex().IsMatch(value))
                    {
                        context.Error(DiagnosticCodes.InvalidNavigationItem, $"Invalid slot '{value}' on navigation item '{id}' - expected a slot name", line.Location);
                        valid = false;
                    }
                    else
                    {
                        slot = value;
                    }

                    break;
                default:
                    if (isQuoted || isLocalized || !TargetRegex().IsMatch(value))
                    {
                        context.Error(DiagnosticCodes.InvalidNavigationItem, $"Invalid target '{value}' on navigation item '{id}' - expected <Module> or <Module>.<Feature>", line.Location);
                        valid = false;
                    }
                    else
                    {
                        var dot = value.IndexOf('.', StringComparison.Ordinal);
                        target = new(dot < 0 ? value : value[..dot], dot < 0 ? null : value[(dot + 1)..], line.Location);
                    }

                    break;
            }
        }

        if (title is null && valid)
        {
            context.Error(DiagnosticCodes.InvalidNavigationItem, $"Navigation item '{id}' has no title - expected 'item {id} title \"<text>\"'", line.Location);
        }

        var children = new List<NavigationItemSyntax>();
        OutletPositionSyntax? placement = null;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) == "item")
            {
                if (ParseItem(context, child) is { } nested)
                {
                    children.Add(nested);
                }

                continue;
            }

            if (allowPosition && OutletParser.TryParsePosition(child, out var parsed))
            {
                if (placement is not null)
                {
                    OutletParser.ReportRepeatedPosition(context, child, id);
                }

                placement ??= parsed;
                continue;
            }

            context.Error(DiagnosticCodes.InvalidNavigationItem, $"Unexpected '{child.Content}' under navigation item '{id}' - only sub-items are allowed: item <id> title \"<text>\" ...", child.Location);
            context.SkipBlock(child.Indent);
        }

        return new(id, title ?? string.Empty, line.Location) { Icon = icon, Target = target, Slot = slot, Children = children, Position = placement };
    }

    static string Text(Match match, int quotedGroup) => Text(match, quotedGroup, quotedGroup + 1, quotedGroup + 1);

    static string Text(Match match, int quotedGroup, int localizedGroup, int bareGroup)
    {
        if (match.Groups[quotedGroup].Success)
        {
            return StringLiteral.Unescape(match.Groups[quotedGroup].Value);
        }

        return match.Groups[localizedGroup].Success ? match.Groups[localizedGroup].Value : match.Groups[bareGroup].Value;
    }

    [GeneratedRegex(@"^navigation\s+(topbar|sidebar)(?:\s+(inherits|extends|overrides))?$", RegexOptions.NonBacktracking)]
    private static partial Regex RegionHeaderRegex();

    [GeneratedRegex("^logo\\s+image\\s+\"(" + StringLiteral.BodyPattern + ")\"\\s+title\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*))$", RegexOptions.NonBacktracking)]
    private static partial Regex LogoRegex();

    [GeneratedRegex("^item\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|([A-Za-z_][\\w-]*))(?:\\s+(.*))?$", RegexOptions.NonBacktracking)]
    private static partial Regex ItemRegex();

    [GeneratedRegex("\\s*(title|icon|slot|target)\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*)|([A-Za-z_][\\w.-]*))(?<end>\\s|$)", RegexOptions.NonBacktracking)]
    private static partial Regex PartRegex();

    [GeneratedRegex(@"^[A-Za-z_]\w*$", RegexOptions.NonBacktracking)]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex(@"^[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*$", RegexOptions.NonBacktracking)]
    private static partial Regex TargetRegex();
}
