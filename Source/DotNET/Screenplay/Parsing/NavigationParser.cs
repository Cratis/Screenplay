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
    public const string Expected = "item <id> title \"<text>\" [icon <name>] [target <Module>[.<Feature>] [screen <Screen>]] [slot <name>]";

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
                    logo = ReadRegionLogo(quiet, child, logo, unrecognized);
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

        return new(region, ReadRegionMode(match), logo, items, unrecognized, header.Location);
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
        var parts = new ItemParts(context, line, id);
        parts.Read(head.Groups[3].Value);
        var target = parts.ResolveTarget();
        var (children, placement) = ReadItemChildren(context, line, id, allowPosition);
        return new(id, parts.Title ?? string.Empty, line.Location) { Icon = parts.Icon, Target = target, Slot = parts.Slot, Children = children, Position = placement };
    }

    static (List<NavigationItemSyntax> Children, OutletPositionSyntax? Position) ReadItemChildren(ParserContext context, SourceLine line, string id, bool allowPosition)
    {
        var children = new List<NavigationItemSyntax>();
        OutletPositionSyntax? placement = null;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (string.Equals(LineText.FirstWord(child.Content), "item", StringComparison.Ordinal))
            {
                ReadNestedItem(context, child, children);
                continue;
            }

            if (allowPosition && TryReadItemPosition(context, child, id, ref placement))
            {
                continue;
            }

            context.Error(DiagnosticCodes.InvalidNavigationItem, $"Unexpected '{child.Content}' under navigation item '{id}' - only sub-items are allowed: item <id> title \"<text>\" ...", child.Location);
            context.SkipBlock(child.Indent);
        }

        return (children, placement);
    }

    static void ReadNestedItem(ParserContext context, SourceLine line, List<NavigationItemSyntax> children)
    {
        if (ParseItem(context, line) is { } nested)
        {
            children.Add(nested);
        }
    }

    static bool TryReadItemPosition(ParserContext context, SourceLine line, string id, ref OutletPositionSyntax? placement)
    {
        if (!OutletParser.TryParsePosition(line, out var parsed))
        {
            return false;
        }

        if (placement is not null)
        {
            OutletParser.ReportRepeatedPosition(context, line, id);
        }

        placement ??= parsed;
        return true;
    }

    static OutletMode? ReadRegionMode(Match match) => match.Success && match.Groups[2].Success
        ? match.Groups[2].Value switch
        {
            "inherits" => OutletMode.Inherit,
            "overrides" => OutletMode.Override,
            _ => OutletMode.Extend
        }
        : null;

    static NavigationLogoSyntax? ReadRegionLogo(ParserContext context, SourceLine line, NavigationLogoSyntax? logo, List<SourceLocation> unrecognized)
    {
        // A malformed or second logo is not one the region can hold; it is left for review like any other line.
        var parsed = ParseLogo(context, line);
        if (parsed is not null && logo is null)
        {
            return parsed;
        }

        unrecognized.Add(line.Location);
        return logo;
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

    [GeneratedRegex("\\s*(title|icon|slot|target|screen)\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*)|([A-Za-z_][\\w.-]*))(?<end>\\s|$)", RegexOptions.NonBacktracking)]
    private static partial Regex PartRegex();

    [GeneratedRegex(@"^[A-Za-z_]\w*$", RegexOptions.NonBacktracking)]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex(@"^[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*$", RegexOptions.NonBacktracking)]
    private static partial Regex TargetRegex();
}
