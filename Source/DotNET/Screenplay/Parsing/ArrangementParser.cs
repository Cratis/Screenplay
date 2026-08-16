// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the body shared by <c>layout</c>, <c>screen template</c> and <c>dialog template</c> - the slots
/// they declare and the <c>arrangement</c> block arranging those slots.
/// </summary>
/// <remarks>
/// The three declarations differ only in where they sit and what they may say about their parent, so the
/// body they hold is parsed once here. Slots are declared by plain name lines; the <c>arrangement</c> block
/// then positions them, either as a responsive <c>flow</c> tree or as pixel-precise <c>freeform</c> variants.
/// </remarks>
internal static partial class ArrangementParser
{
    /// <summary>
    /// Parses the body of a layout, screen template or dialog template from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the declaration's header.</param>
    /// <param name="keyword">The construct being parsed, used in diagnostics.</param>
    /// <param name="name">The name of the construct, used in diagnostics.</param>
    /// <param name="allowsFitsSlot">Whether the construct may declare <c>fits slot</c>.</param>
    /// <returns>The parsed <see cref="Body"/>.</returns>
    public static Body ParseBody(ParserContext context, SourceLine header, string keyword, string name, bool allowsFitsSlot)
    {
        var slots = new List<SlotSyntax>();
        ArrangementSyntax? arrangement = null;
        string? fitsSlot = null;

        while (context.TryPeekChild(header.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "arrangement":
                    if (arrangement is not null)
                    {
                        context.Error(DiagnosticCodes.DuplicateArrangement, $"This {keyword} already declares 'arrangement' - a {keyword} can have at most one", child.Location);
                        context.SkipBlock(child.Indent);
                        break;
                    }

                    arrangement = ParseArrangement(context, child, keyword, name, slots);
                    break;
                case "fits":
                    ParseFitsSlot(context, child, keyword, allowsFitsSlot, ref fitsSlot);
                    break;
                default:
                    AddSlot(context, child, slots);
                    break;
            }
        }

        return new(slots, arrangement, fitsSlot);
    }

    static void ParseFitsSlot(ParserContext context, SourceLine line, string keyword, bool allowsFitsSlot, ref string? fitsSlot)
    {
        if (!allowsFitsSlot)
        {
            context.Error(
                DiagnosticCodes.FitsSlotNotAllowed,
                $"A {keyword} cannot declare 'fits slot' - it occupies no slot of the structure it opens over",
                line.Location);
            return;
        }

        var match = FitsSlotRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidFitsSlotDeclaration, $"Invalid 'fits slot' declaration '{line.Content}' - expected 'fits slot <name>'", line.Location);
            return;
        }

        if (fitsSlot is not null)
        {
            context.Error(DiagnosticCodes.DuplicateFitsSlot, $"This {keyword} already declares 'fits slot' - a {keyword} fills at most one slot of its parent", line.Location);
            return;
        }

        fitsSlot = match.Groups[1].Value;
    }

    static void AddSlot(ParserContext context, SourceLine line, List<SlotSyntax> slots)
    {
        var match = SlotDeclarationRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(
                DiagnosticCodes.InvalidLayoutSlotName,
                $"Invalid slot declaration '{line.Content}' - expected an identifier, optionally followed by 'contributes <Point>'",
                line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        if (context.TryPeekChild(line.Indent, out _))
        {
            context.Error(
                DiagnosticCodes.UnknownLayoutDirective,
                $"Unexpected block under '{line.Content}' - a slot declaration names a slot and nothing more; arrange slots in the 'arrangement' block",
                line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        AddSlot(slots, new(match.Groups[1].Value, match.Groups[2].Success ? match.Groups[2].Value : null, line.Location));
    }

    static void AddSlot(List<SlotSyntax> slots, SlotSyntax slot)
    {
        if (!slots.Exists(existing => existing.Name == slot.Name))
        {
            slots.Add(slot);
        }
    }

    static ArrangementSyntax ParseArrangement(ParserContext context, SourceLine header, string keyword, string name, List<SlotSyntax> slots)
    {
        var match = ArrangementRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidArrangementDeclaration, $"Invalid arrangement declaration '{header.Content}' - expected 'arrangement flow' or 'arrangement freeform'", header.Location);
            context.SkipBlock(header.Indent);
            return new(ArrangementMode.Flow, header.Location, new ArrangementContainerSyntax(ArrangementContainerKind.Flat, [], header.Location), []);
        }

        return match.Groups[1].Value == "freeform"
            ? ParseFreeform(context, header, keyword, name, slots)
            : ParseFlow(context, header, keyword, name, slots);
    }

    static ArrangementSyntax ParseFlow(ParserContext context, SourceLine header, string keyword, string name, List<SlotSyntax> slots)
    {
        var children = new List<ArrangementNodeSyntax>();
        var overrides = new List<ArrangementOverrideSyntax>();
        var seenOverrides = new HashSet<(string? Width, string? Height)>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "when":
                    var overrideNode = ParseOverride(context, line);
                    if (!seenOverrides.Add((overrideNode.Width, overrideNode.Height)))
                    {
                        context.Error(DiagnosticCodes.DuplicateArrangementOverride, "This arrangement already declares a 'when' override for this width/height combination", line.Location);
                        continue;
                    }

                    overrides.Add(overrideNode);
                    continue;
                case "variant":
                    context.Error(
                        DiagnosticCodes.ArrangementDirectiveMismatch,
                        $"The {keyword} '{name}' arranges by 'flow' but declares a 'variant' - variants belong to 'arrangement freeform'",
                        line.Location);
                    context.SkipBlock(line.Indent);
                    continue;
                default:
                    if (ParseNode(context, line) is { } node)
                    {
                        children.Add(node);
                    }

                    continue;
            }
        }

        var root = new ArrangementContainerSyntax(ArrangementContainerKind.Flat, children, header.Location);
        CollectSlots(root, slots);
        return new(ArrangementMode.Flow, header.Location, root, overrides);
    }

    static ArrangementSyntax ParseFreeform(ParserContext context, SourceLine header, string keyword, string name, List<SlotSyntax> slots)
    {
        var variants = new List<VariantSyntax>();
        var seenVariants = new HashSet<(string Width, string Height)>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) != "variant")
            {
                context.Error(
                    DiagnosticCodes.ArrangementDirectiveMismatch,
                    $"Unexpected '{line.Content}' - the {keyword} '{name}' arranges by 'freeform', whose body is made of 'variant' blocks",
                    line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            var variant = ParseVariant(context, line, slots);
            if (!seenVariants.Add((variant.Width, variant.Height)))
            {
                context.Error(DiagnosticCodes.DuplicateVariant, $"This arrangement already declares a 'variant' for width {variant.Width}, height {variant.Height}", line.Location);
                continue;
            }

            variants.Add(variant);
        }

        return new(ArrangementMode.Freeform, header.Location, Variants: variants);
    }

    static ArrangementOverrideSyntax ParseOverride(ParserContext context, SourceLine header)
    {
        var match = OverrideRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(
                DiagnosticCodes.InvalidArrangementOverride,
                $"Invalid 'when' declaration '{header.Content}' - expected 'when width <compact|regular>[, height <compact|regular>]' or 'when height <compact|regular>'",
                header.Location);
            context.SkipBlock(header.Indent);
            return new(null, null, new ArrangementContainerSyntax(ArrangementContainerKind.Flat, [], header.Location), header.Location);
        }

        var width = match.Groups[1].Success ? match.Groups[1].Value : null;
        var height = match.Groups[2].Success ? match.Groups[2].Value : null;
        height ??= match.Groups[3].Success ? match.Groups[3].Value : null;

        var children = new List<ArrangementNodeSyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (ParseNode(context, line) is { } node)
            {
                children.Add(node);
            }
        }

        var root = new ArrangementContainerSyntax(ArrangementContainerKind.Flat, children, header.Location);
        return new(width, height, root, header.Location);
    }

    static ArrangementNodeSyntax? ParseNode(ParserContext context, SourceLine line)
    {
        var firstWord = LineText.FirstWord(line.Content);
        return firstWord == "row" || firstWord == "column" || firstWord == "grid"
            ? ParseContainer(context, line, firstWord)
            : ParseSlotLeaf(context, line);
    }

    static ArrangementContainerSyntax? ParseContainer(ParserContext context, SourceLine line, string keyword)
    {
        var match = ContainerRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidArrangementContainer, $"Invalid '{keyword}' declaration '{line.Content}' - expected '{keyword}', optionally followed by 'gap <number>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var gap = match.Groups[2].Success ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : (int?)null;
        var kind = keyword switch
        {
            "row" => ArrangementContainerKind.Row,
            "column" => ArrangementContainerKind.Column,
            _ => ArrangementContainerKind.Grid,
        };

        var children = new List<ArrangementNodeSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (ParseNode(context, child) is { } node)
            {
                children.Add(node);
            }
        }

        return new(kind, children, line.Location, gap);
    }

    static ArrangementSlotSyntax? ParseSlotLeaf(ParserContext context, SourceLine line)
    {
        var match = SlotLeafRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(
                DiagnosticCodes.InvalidArrangementSlotAttributes,
                $"Invalid slot '{line.Content}' - expected an identifier, optionally followed by 'width <n>', 'height <n>', 'grow' or 'span <n>'",
                line.Location);
            return null;
        }

        return new(
            match.Groups[1].Value,
            line.Location,
            match.Groups[2].Success ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : null,
            match.Groups[3].Success ? int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) : null,
            match.Groups[4].Success,
            match.Groups[5].Success ? int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture) : null);
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
            AddSlot(slots, new(place.SlotName, null, place.Location));
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

    /// <summary>
    /// Adds every slot the tree positions that the body did not already declare.
    /// </summary>
    /// <param name="node">The <see cref="ArrangementNodeSyntax"/> to walk.</param>
    /// <param name="slots">The slots declared so far, added to in place.</param>
    /// <remarks>
    /// The flat list is deliberately kept separate from the tree: it is what a slot <em>is</em>, while the
    /// tree is where it sits. Positioning a slot the body never named still declares it, so an arrangement
    /// on its own remains a complete declaration.
    /// </remarks>
    static void CollectSlots(ArrangementNodeSyntax node, List<SlotSyntax> slots)
    {
        switch (node)
        {
            case ArrangementSlotSyntax slot:
                AddSlot(slots, new(slot.Name, null, slot.Location));
                break;
            case ArrangementContainerSyntax container:
                foreach (var child in container.Children)
                {
                    CollectSlots(child, slots);
                }

                break;
        }
    }

    [GeneratedRegex(@"^arrangement\s+(flow|freeform)$", RegexOptions.None, 1000)]
    private static partial Regex ArrangementRegex();

    [GeneratedRegex(@"^fits\s+slot\s+([a-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex FitsSlotRegex();

    [GeneratedRegex(@"^([a-z_]\w*)(?:\s+contributes\s+([A-Za-z_]\w*))?$", RegexOptions.None, 1000)]
    private static partial Regex SlotDeclarationRegex();

    [GeneratedRegex(@"^(row|column|grid)(?:\s+gap\s+(\d+))?$", RegexOptions.None, 1000)]
    private static partial Regex ContainerRegex();

    [GeneratedRegex(@"^([a-z_]\w*)(?:\s+width\s+(\d+))?(?:\s+height\s+(\d+))?(?:\s+(grow))?(?:\s+span\s+(\d+))?$", RegexOptions.None, 1000)]
    private static partial Regex SlotLeafRegex();

    [GeneratedRegex(@"^when\s+(?:width\s+(compact|regular)(?:\s*,\s*height\s+(compact|regular))?|height\s+(compact|regular))$", RegexOptions.None, 1000)]
    private static partial Regex OverrideRegex();

    [GeneratedRegex(@"^variant\s+width\s+(compact|regular)\s*,\s*height\s+(compact|regular)$", RegexOptions.None, 1000)]
    private static partial Regex VariantRegex();

    [GeneratedRegex(@"^place\s+([a-z_]\w*)\s+(?:(hidden)|at\s+(\d+)\s*,\s*(\d+)\s+size\s+(fill|\d+)\s*,\s*(fill|\d+))$", RegexOptions.None, 1000)]
    private static partial Regex PlaceRegex();

    /// <summary>
    /// Holds what the body of a layout, screen template or dialog template declares.
    /// </summary>
    /// <param name="Slots">The slots declared, in declaration order.</param>
    /// <param name="Arrangement">The <c>arrangement</c> block, or <c>null</c> when the body only names slots.</param>
    /// <param name="FitsSlot">The slot named by <c>fits slot</c>, or <c>null</c> when the body does not say.</param>
    internal sealed record Body(IReadOnlyList<SlotSyntax> Slots, ArrangementSyntax? Arrangement, string? FitsSlot);
}
