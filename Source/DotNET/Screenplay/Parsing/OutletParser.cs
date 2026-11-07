// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses outlets - the item lists a layout or template offers - and the items modules, features, screens and
/// contributions add to them.
/// </summary>
/// <remarks>
/// A layout, screen template or dialog template declares <c>outlet &lt;name&gt; of &lt;kind&gt; [for item|for screen] [in &lt;slot&gt;]</c>
/// with <c>default</c> items. A module, feature or slice screen writes <c>outlet &lt;name&gt; inherits|extends|overrides</c>
/// with its own items and <c>hide &lt;id&gt;</c> lines. Items are recognized by their first word; whether an item fits its
/// outlet's kind is checked once the outlet it changes is known, by <see cref="OutletValidator"/>.
/// </remarks>
internal static partial class OutletParser
{
    /// <summary>
    /// The form of an outlet declaration, as it reads in a diagnostic.
    /// </summary>
    public const string DeclarationExpected = "outlet <name> of actions|filters|tabs|stats|navigation [for item|for screen] [in <slot>]";

    /// <summary>
    /// The form of a lower level's outlet, as it reads in a diagnostic.
    /// </summary>
    public const string OverrideExpected = "outlet <name> inherits|extends|overrides";

    /// <summary>
    /// The item forms, as they read in a diagnostic.
    /// </summary>
    public const string ItemsExpected = "button, group, filter <id> title \"<text>\" sets <Query>.<param>, tab <id> title \"<text>\" shows <Screen>, stat <id> title \"<text>\" value from <Query>.<field> or item <id> title \"<text>\" target <Module>[.<Feature>]";

    static readonly HashSet<string> _itemKeywords = new(StringComparer.Ordinal) { "button", "group", "filter", "tab", "stat", "item" };

    /// <summary>
    /// Gets a value indicating whether the line is an outlet with a kind - <c>outlet &lt;name&gt; of …</c>.
    /// </summary>
    /// <param name="content">The line's content.</param>
    /// <returns><c>true</c> when the line declares an outlet.</returns>
    public static bool IsDeclaration(string content) => DeclarationStartRegex().IsMatch(content);

    /// <summary>
    /// Gets a value indicating whether the line is a lower level's outlet - <c>outlet &lt;name&gt; inherits|extends|overrides</c>.
    /// </summary>
    /// <param name="content">The line's content.</param>
    /// <returns><c>true</c> when the line changes an outlet.</returns>
    public static bool IsOverride(string content) => OverrideRegex().IsMatch(content);

    /// <summary>
    /// Gets a value indicating whether a line starts an outlet item.
    /// </summary>
    /// <param name="content">The line's content.</param>
    /// <returns><c>true</c> for <c>button</c>, <c>group &lt;id&gt; title</c>, <c>filter</c>, <c>tab</c>, <c>stat</c> and <c>item</c> lines.</returns>
    public static bool IsItem(string content)
    {
        var word = LineText.FirstWord(content);
        return word == "group" ? GroupStartRegex().IsMatch(content) : _itemKeywords.Contains(word);
    }

    /// <summary>
    /// Parses an outlet a layout, screen template or dialog template declares, and its <c>default</c> items.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to read from and report to.</param>
    /// <param name="header">The consumed <c>outlet &lt;name&gt; of &lt;kind&gt;</c> line.</param>
    /// <param name="owner">What declares the outlet, as it reads in a diagnostic - for example <c>layout</c>.</param>
    /// <returns>The <see cref="OutletSyntax"/>, or <c>null</c> when the header is malformed.</returns>
    public static OutletSyntax? ParseDeclaration(ParserContext context, SourceLine header, string owner)
    {
        if (OverrideRegex().IsMatch(header.Content))
        {
            context.Error(DiagnosticCodes.InvalidOutletDeclaration, $"A {owner} declares the outlets it offers - write '{DeclarationExpected}'; 'inherits', 'extends' and 'overrides' belong on a module, feature or screen", header.Location);
            context.SkipBlock(header.Indent);
            return null;
        }

        var match = DeclarationRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidOutletDeclaration, $"Invalid outlet declaration '{header.Content}' - expected '{DeclarationExpected}'", header.Location);
            context.SkipBlock(header.Indent);
            return null;
        }

        var kind = Enum.Parse<OutletKind>(match.Groups[2].Value, ignoreCase: true);
        var items = new List<OutletItemSyntax>();
        NavigationLogoSyntax? logo = null;
        while (context.TryPeekChild(header.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "default":
                    if (ParseDefault(context, child, kind) is { } item)
                    {
                        items.Add(item);
                    }

                    break;
                case "logo" when kind == OutletKind.Navigation:
                    var parsed = NavigationParser.ParseLogo(context, child);
                    if (logo is not null)
                    {
                        context.Error(DiagnosticCodes.NavigationLogoNotAllowed, "This outlet already declares a 'logo' - the application has one logo", child.Location);
                    }
                    else
                    {
                        logo = parsed;
                    }

                    break;
                default:
                    context.Error(DiagnosticCodes.InvalidOutletItem, $"Unexpected '{child.Content}' in outlet '{match.Groups[1].Value}' - a {owner} lists the items its outlet shows by default as 'default <id> title \"<text>\" …' lines", child.Location);
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        return new(match.Groups[1].Value, kind, OutletMode.Unspecified, items, header.Location)
        {
            Subject = match.Groups[3].Success ? Enum.Parse<ScreenActionSubjectKind>(match.Groups[3].Value, ignoreCase: true) : null,
            Slot = match.Groups[4].Success ? match.Groups[4].Value : null,
            Logo = logo
        };
    }

    /// <summary>
    /// Parses the outlet a module, feature or slice screen changes, with its own items and <c>hide</c> lines.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to read from and report to.</param>
    /// <param name="header">The consumed <c>outlet &lt;name&gt; …</c> line.</param>
    /// <param name="owner">What changes the outlet, as it reads in a diagnostic - for example <c>feature</c>.</param>
    /// <returns>The <see cref="OutletSyntax"/>, or <c>null</c> when the header is malformed.</returns>
    public static OutletSyntax? ParseOverride(ParserContext context, SourceLine header, string owner)
    {
        if (IsDeclaration(header.Content))
        {
            context.Error(DiagnosticCodes.InvalidOutletDeclaration, $"A {owner} cannot declare an outlet - a layout or template declares 'outlet <name> of <kind>'; a {owner} writes '{OverrideExpected}'", header.Location);
            context.SkipBlock(header.Indent);
            return null;
        }

        var match = OverrideRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidOutletDeclaration, $"Invalid outlet '{header.Content}' - a {owner} writes '{OverrideExpected}'", header.Location);
            context.SkipBlock(header.Indent);
            return null;
        }

        var name = match.Groups[1].Value;
        var mode = match.Groups[2].Value switch
        {
            "inherits" => OutletMode.Inherit,
            "extends" => OutletMode.Extend,
            _ => OutletMode.Override
        };
        var items = new List<OutletItemSyntax>();
        var hides = new List<OutletHideSyntax>();
        while (context.TryPeekChild(header.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) == "hide")
            {
                if (HideRegex().Match(child.Content) is { Success: true } hide)
                {
                    hides.Add(new(hide.Groups[1].Value, child.Location));
                }
                else
                {
                    context.Error(DiagnosticCodes.InvalidOutletItem, $"Invalid '{child.Content}' - expected 'hide <id>'", child.Location);
                }

                if (context.TryPeekChild(child.Indent, out _))
                {
                    context.Error(DiagnosticCodes.InvalidOutletItem, $"Unexpected block under '{child.Content}' - 'hide <id>' stands alone", child.Location);
                    context.SkipBlock(child.Indent);
                }

                continue;
            }

            if (LineText.FirstWord(child.Content) == "logo")
            {
                context.Error(DiagnosticCodes.NavigationLogoNotAllowed, $"The {owner}'s outlet '{name}' declares a logo - the logo is the application's, declared once in a layout's navigation outlet", child.Location);
                context.SkipBlock(child.Indent);
                continue;
            }

            if (IsItem(child.Content))
            {
                if (ParseItem(context, child, allowPosition: true) is { } item)
                {
                    items.Add(item);
                }

                continue;
            }

            context.Error(DiagnosticCodes.InvalidOutletItem, $"Unexpected '{child.Content}' in outlet '{name}' - expected hide <id> or an item: {ItemsExpected}", child.Location);
            context.SkipBlock(child.Indent);
        }

        if (mode == OutletMode.Inherit && (items.Count > 0 || hides.Count > 0))
        {
            context.Error(DiagnosticCodes.InvalidOutletDeclaration, $"The {owner}'s outlet '{name}' inherits, so it lists nothing of its own - write 'outlet {name} extends' to add items or hide inherited ones", header.Location);
        }

        return new(name, null, mode, items, header.Location) { Hides = hides };
    }

    /// <summary>
    /// Parses the outlet a module, feature or slice screen changes and adds it to the ones it already changes.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to read from and report to.</param>
    /// <param name="header">The consumed <c>outlet</c> line.</param>
    /// <param name="owner">What changes the outlet, as it reads in a diagnostic.</param>
    /// <param name="outlets">The outlets the owner already changes.</param>
    public static void AddOverride(ParserContext context, SourceLine header, string owner, List<OutletSyntax> outlets)
    {
        if (ParseOverride(context, header, owner) is not { } outlet)
        {
            return;
        }

        if (outlets.Exists(existing => existing.Name == outlet.Name))
        {
            context.Error(DiagnosticCodes.DuplicateOutlet, $"This {owner} already changes outlet '{outlet.Name}' - write its items under one 'outlet {outlet.Name}' line", header.Location);
            return;
        }

        outlets.Add(outlet);
    }

    /// <summary>
    /// Parses one outlet item line - <c>button</c>, <c>group</c>, <c>filter</c>, <c>tab</c>, <c>stat</c> or <c>item</c>.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to read from and report to.</param>
    /// <param name="line">The consumed item line.</param>
    /// <param name="allowPosition">Whether the item may say <c>before &lt;id&gt;</c> or <c>after &lt;id&gt;</c>.</param>
    /// <returns>The <see cref="OutletItemSyntax"/>, or <c>null</c> when the line is malformed.</returns>
    public static OutletItemSyntax? ParseItem(ParserContext context, SourceLine line, bool allowPosition) => LineText.FirstWord(line.Content) switch
    {
        "button" => ToolbarButtonParser.Parse(context, line, allowPosition),
        "group" => ParseGroup(context, line, allowPosition),
        "item" => NavigationParser.ParseItem(context, line, allowPosition),
        _ => ParseTitled(context, line, allowPosition)
    };

    /// <summary>
    /// Tries to read a <c>before &lt;id&gt;</c> or <c>after &lt;id&gt;</c> child line.
    /// </summary>
    /// <param name="line">The child line.</param>
    /// <param name="position">The parsed <see cref="OutletPositionSyntax"/>.</param>
    /// <returns><c>true</c> when the line is a position.</returns>
    public static bool TryParsePosition(SourceLine line, out OutletPositionSyntax? position)
    {
        var match = PositionRegex().Match(line.Content);
        position = match.Success
            ? new(match.Groups[1].Value == "before" ? OutletPlacement.Before : OutletPlacement.After, match.Groups[2].Value, line.Location)
            : null;
        return match.Success;
    }

    /// <summary>
    /// Reports a second <c>before</c> or <c>after</c> under one item.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report to.</param>
    /// <param name="line">The repeated line.</param>
    /// <param name="id">The id of the item.</param>
    public static void ReportRepeatedPosition(ParserContext context, SourceLine line, string id) =>
        context.Error(DiagnosticCodes.InvalidOutletItem, $"Item '{id}' already says where it stands - write one 'before <id>' or 'after <id>'", line.Location);

    static OutletItemSyntax? ParseDefault(ParserContext context, SourceLine line, OutletKind kind)
    {
        var match = DefaultRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidOutletItem, $"Invalid default '{line.Content}' - expected 'default <id> title \"<text>\" …'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var keyword = kind switch
        {
            OutletKind.Actions => "button",
            OutletKind.Filters => "filter",
            OutletKind.Tabs => "tab",
            OutletKind.Stats => "stat",
            _ => "item"
        };

        // A default is written in its outlet's item form without the item's keyword; read it as that item.
        var item = ParseItem(context, line with { Content = $"{keyword} {match.Groups[1].Value}" }, allowPosition: false);
        return item is null ? null : item with { IsDefault = true };
    }

    static OutletGroupSyntax? ParseGroup(ParserContext context, SourceLine line, bool allowPosition)
    {
        var match = GroupRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidOutletItem, $"Invalid group '{line.Content}' - expected 'group <id> title \"<text>\" [icon <name>]'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var id = match.Groups[1].Value;
        var buttons = new List<ToolbarButtonSyntax>();
        OutletPositionSyntax? position = null;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) == "button")
            {
                if (ToolbarButtonParser.Parse(context, child, allowPosition: false) is { } button)
                {
                    buttons.Add(button);
                }

                continue;
            }

            if (allowPosition && TryParsePosition(child, out var parsed))
            {
                if (position is not null)
                {
                    ReportRepeatedPosition(context, child, id);
                }

                position ??= parsed;
                continue;
            }

            context.Error(DiagnosticCodes.InvalidOutletItem, $"Unexpected '{child.Content}' under group '{id}' - a group holds 'button' lines", child.Location);
            context.SkipBlock(child.Indent);
        }

        return new(id, Text(match, 2), buttons, line.Location)
        {
            Icon = GroupIcon(match),
            Position = position
        };
    }

    static OutletItemSyntax? ParseTitled(ParserContext context, SourceLine line, bool allowPosition)
    {
        var keyword = LineText.FirstWord(line.Content);
        var expected = keyword switch
        {
            "filter" => "filter <id> title \"<text>\" [sets <Query>.<param>]",
            "tab" => "tab <id> title \"<text>\" [shows <Screen>]",
            _ => "stat <id> title \"<text>\" [value from <Query>.<field>]"
        };
        var match = TitledRegex().Match(line.Content);
        var binding = match.Success && match.Groups[5].Success ? match.Groups[5].Value : null;
        var bound = binding is null ? null : keyword switch
        {
            "filter" => SetsRegex().Match(binding),
            "tab" => ShowsRegex().Match(binding),
            _ => ValueFromRegex().Match(binding)
        };
        if (!match.Success || match.Groups[1].Value != keyword || bound is { Success: false })
        {
            context.Error(DiagnosticCodes.InvalidOutletItem, $"Invalid {keyword} '{line.Content}' - expected '{expected}'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var id = match.Groups[2].Value;
        OutletPositionSyntax? position = null;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (allowPosition && TryParsePosition(child, out var parsed))
            {
                if (position is not null)
                {
                    ReportRepeatedPosition(context, child, id);
                }

                position ??= parsed;
                continue;
            }

            context.Error(DiagnosticCodes.InvalidOutletItem, $"Unexpected '{child.Content}' under {keyword} '{id}'{(allowPosition ? " - expected 'before <id>' or 'after <id>'" : string.Empty)}", child.Location);
            context.SkipBlock(child.Indent);
        }

        var title = Text(match, 3);
        return keyword switch
        {
            "filter" => new OutletFilterSyntax(id, title, line.Location) { Query = bound?.Groups[1].Value, Parameter = bound?.Groups[2].Value, Position = position },
            "tab" => new OutletTabSyntax(id, title, line.Location) { Shows = bound?.Groups[1].Value, Position = position },
            _ => new OutletStatSyntax(id, title, line.Location) { Query = bound?.Groups[1].Value, Field = bound?.Groups[2].Value, Position = position }
        };
    }

    static string? GroupIcon(Match match)
    {
        if (match.Groups[4].Success)
        {
            return StringLiteral.Unescape(match.Groups[4].Value);
        }

        return match.Groups[5].Success ? match.Groups[5].Value : null;
    }

    static string Text(Match match, int quotedGroup) => match.Groups[quotedGroup].Success
        ? StringLiteral.Unescape(match.Groups[quotedGroup].Value)
        : match.Groups[quotedGroup + 1].Value;

    [GeneratedRegex(@"^outlet\s+\S+\s+of\b", RegexOptions.NonBacktracking)]
    private static partial Regex DeclarationStartRegex();

    [GeneratedRegex(@"^outlet\s+([A-Za-z_]\w*)\s+of\s+(actions|filters|tabs|stats|navigation)(?:\s+for\s+(item|screen))?(?:\s+in\s+([a-z_]\w*))?$", RegexOptions.NonBacktracking)]
    private static partial Regex DeclarationRegex();

    [GeneratedRegex(@"^outlet\s+([A-Za-z_]\w*)\s+(inherits|extends|overrides)$", RegexOptions.NonBacktracking)]
    private static partial Regex OverrideRegex();

    [GeneratedRegex(@"^hide\s+([A-Za-z_][\w-]*)$", RegexOptions.NonBacktracking)]
    private static partial Regex HideRegex();

    [GeneratedRegex(@"^(before|after)\s+([A-Za-z_][\w-]*)$", RegexOptions.NonBacktracking)]
    private static partial Regex PositionRegex();

    [GeneratedRegex(@"^default\s+(\S.*)$", RegexOptions.NonBacktracking)]
    private static partial Regex DefaultRegex();

    [GeneratedRegex(@"^group\s+\S+\s+title\b", RegexOptions.NonBacktracking)]
    private static partial Regex GroupStartRegex();

    [GeneratedRegex("^group\\s+([A-Za-z_][\\w-]*)\\s+title\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*))(?:\\s+icon\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|([A-Za-z_][\\w-]*)))?$", RegexOptions.NonBacktracking)]
    private static partial Regex GroupRegex();

    [GeneratedRegex("^(filter|tab|stat)\\s+([A-Za-z_][\\w-]*)\\s+title\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*))(?:\\s+(\\S.*))?$", RegexOptions.NonBacktracking)]
    private static partial Regex TitledRegex();

    [GeneratedRegex(@"^sets\s+([A-Za-z_]\w*)\.([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex SetsRegex();

    [GeneratedRegex(@"^shows\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ShowsRegex();

    [GeneratedRegex(@"^value\s+from\s+([A-Za-z_]\w*)\.([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ValueFromRegex();
}
