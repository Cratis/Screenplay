// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>layout</c> declarations - a reusable screen template with named slots, arranged either as a
/// responsive <c>flow</c> template tree or as pixel-precise <c>freeform</c> variants.
/// </summary>
internal static partial class LayoutParser
{
    /// <summary>
    /// Parses a layout from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>layout</c> header.</param>
    /// <returns>The parsed <see cref="LayoutSyntax"/>.</returns>
    public static LayoutSyntax Parse(ParserContext context, SourceLine header)
    {
        var name = header.Content["layout".Length..].Trim();
        var arrangement = LayoutArrangement.Flow;
        var hasArrangement = false;
        TemplateSyntax? template = null;
        var variants = new List<VariantSyntax>();
        var seenVariants = new HashSet<(string Width, string Height)>();
        var slots = new List<SlotSyntax>();

        while (context.TryPeekChild(header.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "arrangement":
                    if (hasArrangement)
                    {
                        context.Error(DiagnosticCodes.DuplicateArrangement, "This layout already declares 'arrangement' - a layout can have at most one", child.Location);
                        break;
                    }

                    hasArrangement = true;
                    arrangement = ParseArrangement(context, child);
                    break;
                case "template":
                    if (template is not null)
                    {
                        context.Error(DiagnosticCodes.UnknownLayoutDirective, "This layout already declares 'template' - a layout can have at most one", child.Location);
                        context.SkipBlock(child.Indent);
                        break;
                    }

                    template = ParseTemplate(context, child, slots);
                    break;
                case "variant":
                    var variant = ParseVariant(context, child, slots);
                    if (!seenVariants.Add((variant.Width, variant.Height)))
                    {
                        context.Error(DiagnosticCodes.DuplicateVariant, $"This layout already declares a 'variant' for width {variant.Width}, height {variant.Height}", child.Location);
                        break;
                    }

                    variants.Add(variant);
                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownLayoutDirective, $"Unexpected '{child.Content}' in layout body - expected arrangement, template or variant", child.Location);
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        if (template is not null && arrangement == LayoutArrangement.Freeform)
        {
            context.Error(DiagnosticCodes.ArrangementDirectiveMismatch, $"Layout '{name}' declares 'arrangement freeform' but has a 'template' block - freeform layouts use 'variant' blocks instead", header.Location);
        }

        if (variants.Count > 0 && arrangement == LayoutArrangement.Flow)
        {
            context.Error(DiagnosticCodes.ArrangementDirectiveMismatch, $"Layout '{name}' has 'variant' blocks but its arrangement is 'flow' (the default) - freeform layouts declare 'arrangement freeform'", header.Location);
        }

        return new(name, slots, header.Location, arrangement, template, variants.Count > 0 ? variants : null);
    }

    static LayoutArrangement ParseArrangement(ParserContext context, SourceLine line)
    {
        var match = ArrangementRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidArrangementDeclaration, $"Invalid arrangement declaration '{line.Content}' - expected 'arrangement flow' or 'arrangement freeform'", line.Location);
            return LayoutArrangement.Flow;
        }

        return match.Groups[1].Value == "freeform" ? LayoutArrangement.Freeform : LayoutArrangement.Flow;
    }

    static TemplateSyntax ParseTemplate(ParserContext context, SourceLine header, List<SlotSyntax> slots)
    {
        var children = new List<TemplateNodeSyntax>();
        var overrides = new List<TemplateOverrideSyntax>();
        var seenOverrides = new HashSet<(string? Width, string? Height)>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "when")
            {
                var overrideNode = ParseOverride(context, line);
                if (!seenOverrides.Add((overrideNode.Width, overrideNode.Height)))
                {
                    context.Error(DiagnosticCodes.DuplicateTemplateOverride, "This template already declares a 'when' override for this width/height combination", line.Location);
                    continue;
                }

                overrides.Add(overrideNode);
                continue;
            }

            var node = ParseTemplateNode(context, line);
            if (node is not null)
            {
                children.Add(node);
            }
        }

        var root = new TemplateContainerSyntax(TemplateContainerKind.Flat, children, header.Location);
        CollectSlots(root, slots);
        return new(root, overrides, header.Location);
    }

    static TemplateOverrideSyntax ParseOverride(ParserContext context, SourceLine header)
    {
        var match = OverrideRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(
                DiagnosticCodes.InvalidTemplateOverride,
                $"Invalid 'when' declaration '{header.Content}' - expected 'when width <compact|regular>[, height <compact|regular>]' or 'when height <compact|regular>'",
                header.Location);
            context.SkipBlock(header.Indent);
            return new(null, null, new TemplateContainerSyntax(TemplateContainerKind.Flat, [], header.Location), header.Location);
        }

        var width = match.Groups[1].Success ? match.Groups[1].Value : null;
        var height = match.Groups[2].Success ? match.Groups[2].Value : null;
        height ??= match.Groups[3].Success ? match.Groups[3].Value : null;

        var children = new List<TemplateNodeSyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var node = ParseTemplateNode(context, line);
            if (node is not null)
            {
                children.Add(node);
            }
        }

        var root = new TemplateContainerSyntax(TemplateContainerKind.Flat, children, header.Location);
        return new(width, height, root, header.Location);
    }

    static TemplateNodeSyntax? ParseTemplateNode(ParserContext context, SourceLine line)
    {
        var firstWord = LineText.FirstWord(line.Content);
        return firstWord == "row" || firstWord == "column" || firstWord == "grid"
            ? ParseContainer(context, line, firstWord)
            : ParseTemplateSlot(context, line);
    }

    static TemplateContainerSyntax? ParseContainer(ParserContext context, SourceLine line, string keyword)
    {
        var match = ContainerRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidTemplateContainer, $"Invalid '{keyword}' declaration '{line.Content}' - expected '{keyword}', optionally followed by 'gap <number>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var gap = match.Groups[2].Success ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : (int?)null;
        var kind = keyword switch
        {
            "row" => TemplateContainerKind.Row,
            "column" => TemplateContainerKind.Column,
            _ => TemplateContainerKind.Grid,
        };

        var children = new List<TemplateNodeSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var node = ParseTemplateNode(context, child);
            if (node is not null)
            {
                children.Add(node);
            }
        }

        return new(kind, children, line.Location, gap);
    }

    static TemplateSlotSyntax? ParseTemplateSlot(ParserContext context, SourceLine line)
    {
        var match = TemplateSlotRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(
                DiagnosticCodes.InvalidTemplateSlotAttributes,
                $"Invalid slot declaration '{line.Content}' - expected an identifier, optionally followed by 'contributes <Point>', 'width <n>', 'height <n>', 'grow' or 'span <n>'",
                line.Location);
            return null;
        }

        return new(
            match.Groups[1].Value,
            line.Location,
            match.Groups[2].Success ? match.Groups[2].Value : null,
            match.Groups[3].Success ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) : null,
            match.Groups[4].Success ? int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture) : null,
            match.Groups[5].Success,
            match.Groups[6].Success ? int.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture) : null);
    }

    static VariantSyntax ParseVariant(ParserContext context, SourceLine header, List<SlotSyntax> slots)
    {
        var match = VariantRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(
                DiagnosticCodes.InvalidVariantDeclaration,
                $"Invalid variant declaration '{header.Content}' - expected 'variant width <compact|regular>, height <compact|regular>'",
                header.Location);
            context.SkipBlock(header.Indent);
            return new(string.Empty, string.Empty, [], header.Location);
        }

        var width = match.Groups[1].Value;
        var height = match.Groups[2].Value;
        var places = new List<PlaceSyntax>();
        var seenSlots = new HashSet<string>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var place = ParsePlace(context, line);
            if (place is null)
            {
                continue;
            }

            if (!seenSlots.Add(place.SlotName))
            {
                context.Error(DiagnosticCodes.DuplicatePlaceInVariant, $"Slot '{place.SlotName}' is already placed in this variant", line.Location);
                continue;
            }

            places.Add(place);
            if (!slots.Exists(existing => existing.Name == place.SlotName))
            {
                slots.Add(new(place.SlotName, null, place.Location));
            }
        }

        return new(width, height, places, header.Location);
    }

    static PlaceSyntax? ParsePlace(ParserContext context, SourceLine line)
    {
        var match = PlaceRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(
                DiagnosticCodes.InvalidPlaceDeclaration,
                $"Invalid place declaration '{line.Content}' - expected 'place <Slot> hidden' or 'place <Slot> at <x>,<y> size <w>,<h>'",
                line.Location);
            return null;
        }

        var slotName = match.Groups[1].Value;
        if (match.Groups[2].Success)
        {
            return new(slotName, line.Location, Hidden: true);
        }

        return new(
            slotName,
            line.Location,
            false,
            int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture),
            match.Groups[5].Value,
            match.Groups[6].Value);
    }

    static void CollectSlots(TemplateNodeSyntax node, List<SlotSyntax> slots)
    {
        switch (node)
        {
            case TemplateSlotSyntax slot when !slots.Exists(existing => existing.Name == slot.Name):
                slots.Add(new(slot.Name, slot.Contributes, slot.Location));
                break;
            case TemplateContainerSyntax container:
                foreach (var child in container.Children)
                {
                    CollectSlots(child, slots);
                }

                break;
        }
    }

    [GeneratedRegex(@"^arrangement\s+(flow|freeform)$", RegexOptions.None, 1000)]
    private static partial Regex ArrangementRegex();

    [GeneratedRegex(@"^(row|column|grid)(?:\s+gap\s+(\d+))?$", RegexOptions.None, 1000)]
    private static partial Regex ContainerRegex();

    [GeneratedRegex(@"^([a-z_]\w*)(?:\s+contributes\s+([A-Za-z_]\w*))?(?:\s+width\s+(\d+))?(?:\s+height\s+(\d+))?(?:\s+(grow))?(?:\s+span\s+(\d+))?$", RegexOptions.None, 1000)]
    private static partial Regex TemplateSlotRegex();

    [GeneratedRegex(@"^when\s+(?:width\s+(compact|regular)(?:\s*,\s*height\s+(compact|regular))?|height\s+(compact|regular))$", RegexOptions.None, 1000)]
    private static partial Regex OverrideRegex();

    [GeneratedRegex(@"^variant\s+width\s+(compact|regular)\s*,\s*height\s+(compact|regular)$", RegexOptions.None, 1000)]
    private static partial Regex VariantRegex();

    [GeneratedRegex(@"^place\s+([a-z_]\w*)\s+(?:(hidden)|at\s+(\d+)\s*,\s*(\d+)\s+size\s+(fill|\d+)\s*,\s*(fill|\d+))$", RegexOptions.None, 1000)]
    private static partial Regex PlaceRegex();
}
