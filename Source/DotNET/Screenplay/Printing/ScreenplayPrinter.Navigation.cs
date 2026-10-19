// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

/// <content>
/// Printing of navigation regions, their logo and items.
/// </content>
public sealed partial class ScreenplayPrinter
{
    void WriteNavigationRegion(ScreenplayWriter writer, NavigationRegionSyntax region)
    {
        using var anchor = writer.Anchor(region);
        var mode = region.Mode switch
        {
            NavigationRegionMode.Inherit => " inherits",
            NavigationRegionMode.Extend => " extends",
            NavigationRegionMode.Override => " overrides",
            _ => string.Empty
        };
        writer.Line($"navigation {(region.Region == NavigationRegionKind.Sidebar ? "sidebar" : "topbar")}{mode}");
        using (writer.Indent())
        {
            if (region.Logo is { } logo)
            {
                writer.Line($"logo image {StringLiteral.Quote(logo.Image)} title {ScreenplaySyntaxText.LocalizableString(logo.Title)}", logo);
            }

            foreach (var item in region.Items)
            {
                WriteNavigationItemNode(writer, item);
            }

            foreach (var button in region.Buttons)
            {
                WriteToolbarButton(writer, button);
            }
        }
    }

    void WriteNavigationItemNode(ScreenplayWriter writer, NavigationItemSyntax item)
    {
        var line = $"item {WriteItemId(item.Id)} title {ScreenplaySyntaxText.LocalizableString(item.Title)}";
        if (item.Icon is not null) line += $" icon {WriteItemValue(item.Icon)}";
        if (item.Target is { } target) line += $" target {target.Module}{(target.Feature is null ? string.Empty : "." + target.Feature)}";
        if (item.Slot is not null) line += $" slot {item.Slot}";
        writer.Line(line, item);
        using (writer.Indent())
        {
            foreach (var child in item.Children)
            {
                WriteNavigationItemNode(writer, child);
            }
        }
    }
}
