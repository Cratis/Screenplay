// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Prints outlets - the item lists layouts and templates offer - and the items lower levels add to them.
/// </summary>
public sealed partial class ScreenplayPrinter
{
    static string TitledLine(OutletItemSyntax item) => item switch
    {
        OutletFilterSyntax filter => $"filter {filter.Id} title {ScreenplaySyntaxText.LocalizableString(filter.Title)}{(filter.Query is null ? string.Empty : $" sets {filter.Query}.{filter.Parameter}")}",
        OutletTabSyntax tab => $"tab {tab.Id} title {ScreenplaySyntaxText.LocalizableString(tab.Title)}{(tab.Shows is null ? string.Empty : $" shows {tab.Shows}")}",
        OutletStatSyntax stat => $"stat {stat.Id} title {ScreenplaySyntaxText.LocalizableString(stat.Title)}{(stat.Query is null ? string.Empty : $" value from {stat.Query}.{stat.Field}")}",
        _ => throw new ArgumentOutOfRangeException(nameof(item), item.GetType().Name, "Not an outlet item with a title line")
    };

    /// <summary>
    /// Writes a default item as <c>default &lt;id&gt; …</c> - its outlet's item form without the item's keyword.
    /// </summary>
    static string ItemHead(OutletItemSyntax item, string line) => item.IsDefault ? $"default {line[(line.IndexOf(' ', StringComparison.Ordinal) + 1)..]}" : line;

    static void WriteOutletPosition(ScreenplayWriter writer, OutletItemSyntax item)
    {
        if (item.Position is { } position)
        {
            writer.Line($"{(position.Placement == OutletPlacement.Before ? "before" : "after")} {position.Id}", position);
        }
    }

    static string KindWord(OutletKind kind) => kind switch
    {
        OutletKind.Actions => "actions",
        OutletKind.Filters => "filters",
        OutletKind.Tabs => "tabs",
        OutletKind.Stats => "stats",
        _ => "navigation"
    };

    void WriteOutlets(ScreenplayWriter writer, IEnumerable<OutletSyntax> outlets)
    {
        foreach (var outlet in outlets)
        {
            WriteOutlet(writer, outlet);
        }
    }

    void WriteOutlet(ScreenplayWriter writer, OutletSyntax outlet)
    {
        using var anchor = writer.Anchor(outlet);
        var header = $"outlet {outlet.Name}";
        if (outlet.Kind is { } kind)
        {
            header += $" of {KindWord(kind)}";
            if (outlet.Subject is { } subject) header += subject == ScreenActionSubjectKind.Item ? " for item" : " for screen";
            if (outlet.Slot is not null) header += $" in {outlet.Slot}";
        }
        else
        {
            header += outlet.Mode switch
            {
                OutletMode.Inherit => " inherits",
                OutletMode.Override => " overrides",
                _ => " extends"
            };
        }

        writer.Line(header);
        using (writer.Indent())
        {
            if (outlet.Logo is { } logo)
            {
                writer.Line($"logo image {StringLiteral.Quote(logo.Image)} title {ScreenplaySyntaxText.LocalizableString(logo.Title)}", logo);
            }

            foreach (var hide in outlet.Hides)
            {
                writer.Line($"hide {hide.Id}", hide);
            }

            foreach (var item in outlet.Items)
            {
                WriteOutletItem(writer, item);
            }
        }
    }

    void WriteOutletItem(ScreenplayWriter writer, OutletItemSyntax item)
    {
        switch (item)
        {
            case ToolbarButtonSyntax button:
                WriteToolbarButton(writer, button);
                break;
            case NavigationItemSyntax navigation:
                WriteNavigationItemNode(writer, navigation);
                break;
            case OutletGroupSyntax group:
                var line = $"group {group.Id} title {ScreenplaySyntaxText.LocalizableString(group.Title)}";
                if (group.Icon is not null) line += $" icon {WriteItemValue(group.Icon)}";
                writer.Line(line, group);
                using (writer.Indent())
                {
                    foreach (var button in group.Buttons)
                    {
                        WriteToolbarButton(writer, button);
                    }

                    WriteOutletPosition(writer, group);
                }

                break;
            default:
                writer.Line(ItemHead(item, TitledLine(item)), item);
                using (writer.Indent())
                {
                    WriteOutletPosition(writer, item);
                }

                break;
        }
    }

    void WriteNavigationItemNode(ScreenplayWriter writer, NavigationItemSyntax item)
    {
        var line = $"item {WriteItemId(item.Id)} title {ScreenplaySyntaxText.LocalizableString(item.Title)}";
        if (item.Icon is not null) line += $" icon {WriteItemValue(item.Icon)}";
        if (item.Target is { } target) line += $" target {target.Module}{(target.Feature is null ? string.Empty : "." + target.Feature)}";
        if (item.Slot is not null) line += $" slot {item.Slot}";
        writer.Line(ItemHead(item, line), item);
        using (writer.Indent())
        {
            foreach (var child in item.Children)
            {
                WriteNavigationItemNode(writer, child);
            }

            WriteOutletPosition(writer, item);
        }
    }
}
