// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the <c>navigation topbar</c> and <c>navigation sidebar</c> blocks of a <c>layout</c> (the application's
/// regions) and of a <c>module</c> (what the module inherits, extends or overrides).
/// </summary>
/// <remarks>
/// A region holds an optional <c>logo</c> and <c>item</c> lines; an item may nest further <c>item</c> lines. The parser
/// only reads the grammar. Whether the combination makes sense - modes at application level, logos in modules,
/// targets that exist - is checked by <see cref="NavigationValidator"/> over the whole document.
/// </remarks>
internal static partial class NavigationParser
{
    /// <summary>
    /// What a navigation region body may hold, as it reads in a diagnostic.
    /// </summary>
    public const string Expected = "logo image \"<path>\" title \"<text>\" or item <id> title \"<text>\" [icon <name>] [target <Module>[.<Feature>]] [slot <name>]";

    /// <summary>
    /// Determines whether a line inside a layout opens a navigation region rather than declaring a slot named <c>navigation</c>.
    /// </summary>
    /// <param name="content">The content of the line.</param>
    /// <returns><c>true</c> when the line is <c>navigation</c> followed by something other than <c>contributes</c>.</returns>
    public static bool IsRegionHeader(string content) => RegionStartRegex().IsMatch(content) && !ContributionStartRegex().IsMatch(content);

    /// <summary>
    /// Parses a navigation region from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>navigation</c> header.</param>
    /// <returns>The parsed <see cref="NavigationRegionSyntax"/>.</returns>
    public static NavigationRegionSyntax ParseRegion(ParserContext context, SourceLine header)
    {
        var match = RegionHeaderRegex().Match(header.Content);
        var region = NavigationRegionKind.Topbar;
        var mode = NavigationRegionMode.Unspecified;
        if (match.Success)
        {
            region = match.Groups[1].Value == "sidebar" ? NavigationRegionKind.Sidebar : NavigationRegionKind.Topbar;
            mode = match.Groups[2].Value switch
            {
                "inherits" => NavigationRegionMode.Inherit,
                "extends" => NavigationRegionMode.Extend,
                "overrides" => NavigationRegionMode.Override,
                _ => NavigationRegionMode.Unspecified
            };
        }
        else
        {
            context.Error(DiagnosticCodes.InvalidNavigationRegionDeclaration, $"Invalid navigation declaration '{header.Content}' - expected 'navigation topbar' or 'navigation sidebar', optionally followed by 'inherits', 'extends' or 'overrides'", header.Location);
        }

        var items = new List<NavigationItemSyntax>();
        NavigationLogoSyntax? logo = null;
        while (context.TryPeekChild(header.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "logo":
                    var parsed = ParseLogo(context, child);
                    if (logo is not null)
                    {
                        context.Error(DiagnosticCodes.NavigationLogoNotAllowed, "This navigation region already declares a 'logo' - the application has one logo", child.Location);
                    }
                    else
                    {
                        logo = parsed;
                    }

                    break;
                case "item":
                    if (ParseItem(context, child) is { } item)
                    {
                        items.Add(item);
                    }

                    break;
                default:
                    context.Error(DiagnosticCodes.InvalidNavigationItem, $"Unexpected '{child.Content}' in a navigation region - expected {Expected}", child.Location);
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        return new(region, mode, items, header.Location) { Logo = logo };
    }

    static NavigationLogoSyntax? ParseLogo(ParserContext context, SourceLine line)
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

    static NavigationItemSyntax? ParseItem(ParserContext context, SourceLine line)
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

            context.Error(DiagnosticCodes.InvalidNavigationItem, $"Unexpected '{child.Content}' under navigation item '{id}' - only sub-items are allowed: item <id> title \"<text>\" ...", child.Location);
            context.SkipBlock(child.Indent);
        }

        return new(id, title ?? string.Empty, line.Location) { Icon = icon, Target = target, Slot = slot, Children = children };
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

    [GeneratedRegex(@"^navigation\s+\S", RegexOptions.NonBacktracking)]
    private static partial Regex RegionStartRegex();

    [GeneratedRegex(@"^navigation\s+contributes\b", RegexOptions.NonBacktracking)]
    private static partial Regex ContributionStartRegex();

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
