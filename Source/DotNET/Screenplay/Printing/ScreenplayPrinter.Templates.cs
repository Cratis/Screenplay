// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

/// <content>
/// Printing of layouts, screen and dialog templates, slots and arrangements.
/// </content>
public sealed partial class ScreenplayPrinter
{
    void WriteLayout(ScreenplayWriter writer, LayoutSyntax layout)
    {
        using var anchor = writer.Anchor(layout);
        writer.Line($"layout {layout.Name}");
        using (writer.Indent())
        {
            WriteTemplateMetadata(writer, new(layout.Category, layout.TemplateType, layout.Exposes, layout.Outlets));
            WriteTemplateScopes(writer, layout.RestrictsScopes, layout.Scopes);
            WriteSlots(writer, layout.Slots);
            WriteArrangement(writer, layout.Arrangement);
            WriteAttachments(writer, layout.Behaviors, layout.UsedBehaviors);
        }
    }

    void WriteScreenTemplate(ScreenplayWriter writer, ScreenTemplateSyntax template)
    {
        using var anchor = writer.Anchor(template);
        writer.Line($"screen template {template.Name}");
        using (writer.Indent())
        {
            if (template.FitsSlot is not null)
            {
                writer.FitsSlotLine($"fits slot {template.FitsSlot}", template);
                writer.Blank();
            }

            WriteTemplateMetadata(writer, new(template.Category, template.TemplateType, template.Exposes, template.Outlets));
            WriteTemplatePicker(writer, template.DisplayName, template.Description, template.RestrictsScopes, template.Scopes);
            WriteSlots(writer, template.Slots);
            WriteTemplateContent(writer, template.Content);
            WriteArrangement(writer, template.Arrangement);
            WriteAttachments(writer, template.Behaviors, template.UsedBehaviors);
        }
    }

    void WriteDialogTemplate(ScreenplayWriter writer, DialogTemplateSyntax template)
    {
        using var anchor = writer.Anchor(template);
        writer.Line($"dialog template {template.Name}");
        using (writer.Indent())
        {
            WriteTemplateMetadata(writer, new(template.Category, template.TemplateType, template.Exposes, template.Outlets));
            WriteTemplatePicker(writer, template.DisplayName, template.Description, template.RestrictsScopes, template.Scopes);
            WriteSlots(writer, template.Slots);
            WriteTemplateContent(writer, template.Content);
            WriteArrangement(writer, template.Arrangement);
            WriteAttachments(writer, template.Behaviors, template.UsedBehaviors);
        }
    }

    void WriteTemplateMetadata(ScreenplayWriter writer, TemplateMetadata metadata)
    {
        if (metadata.Category is not null)
        {
            writer.Line($"category {metadata.Category}");
        }

        if (metadata.TemplateType is not null)
        {
            writer.Line($"type {metadata.TemplateType}");
        }

        foreach (var exposed in metadata.Exposes)
        {
            writer.Line(
                exposed.Type is null
                    ? $"exposes {exposed.Name}"
                    : $"exposes {exposed.Name} {ScreenplaySyntaxText.TypeRef(exposed.Type)}",
                exposed);
        }

        foreach (var outlet in metadata.Outlets)
        {
            writer.Line($"outlet {outlet.Name}", outlet);
        }
    }

    /// <summary>
    /// Writes every slot the structure declares, one per line.
    /// </summary>
    /// <param name="writer">The <see cref="ScreenplayWriter"/> to write to.</param>
    /// <param name="slots">The <see cref="SlotSyntax">slots</see> to write.</param>
    /// <remarks>
    /// Every slot is written, including one only an arrangement named - so a printed document always states
    /// its slots explicitly, and says each of them exactly once.
    /// </remarks>
    void WriteSlots(ScreenplayWriter writer, IEnumerable<SlotSyntax> slots)
    {
        foreach (var slot in slots)
        {
            writer.Line(slot.Contributes is null ? slot.Name : $"{slot.Name} contributes {slot.Contributes}", slot);
        }
    }

    void WriteArrangement(ScreenplayWriter writer, ArrangementSyntax? arrangement)
    {
        if (arrangement is null)
        {
            return;
        }

        writer.Blank();
        using var anchor = writer.Anchor(arrangement);
        writer.Line(arrangement.Mode == ArrangementMode.Freeform ? "arrangement freeform" : "arrangement flow");
        using (writer.Indent())
        {
            if (arrangement.Mode == ArrangementMode.Freeform)
            {
                WriteVariants(writer, arrangement);
                return;
            }

            WriteFlowArrangement(writer, arrangement);
        }
    }

    void WriteVariants(ScreenplayWriter writer, ArrangementSyntax arrangement)
    {
        var first = true;
        foreach (var variant in arrangement.Variants ?? [])
        {
            if (!first)
            {
                writer.Blank();
            }

            first = false;
            WriteVariant(writer, variant);
        }
    }

    void WriteFlowArrangement(ScreenplayWriter writer, ArrangementSyntax arrangement)
    {
        if (arrangement.Root is not null)
        {
            WriteArrangementChildren(writer, arrangement.Root);
        }

        foreach (var arrangementOverride in arrangement.Overrides ?? [])
        {
            writer.Blank();
            WriteArrangementOverride(writer, arrangementOverride);
        }
    }

    void WriteArrangementChildren(ScreenplayWriter writer, ArrangementNodeSyntax node)
    {
        if (node is ArrangementContainerSyntax { Kind: ArrangementContainerKind.Flat } flat)
        {
            foreach (var child in flat.Children)
            {
                WriteArrangementNode(writer, child);
            }

            return;
        }

        WriteArrangementNode(writer, node);
    }

    void WriteArrangementNode(ScreenplayWriter writer, ArrangementNodeSyntax node)
    {
        using var anchor = writer.Anchor(node);
        switch (node)
        {
            case ArrangementSlotSyntax slot:
                writer.Line(WriteArrangementSlotLine(slot));
                break;
            case ArrangementContainerSyntax container:
                WriteArrangementContainer(writer, container);
                break;
            default:
                throw new UnsupportedSyntaxForPrinting("arrangement node", node.GetType().Name);
        }
    }

    void WriteArrangementContainer(ScreenplayWriter writer, ArrangementContainerSyntax container)
    {
        var keyword = container.Kind switch
        {
            ArrangementContainerKind.Row => "row",
            ArrangementContainerKind.Column => "column",
            ArrangementContainerKind.Grid => "grid",
            _ => throw new UnsupportedSyntaxForPrinting("nested arrangement container kind", container.Kind.ToString()),
        };
        writer.Line(WriteArrangementContainerLine(keyword, container));
        using (writer.Indent())
        {
            foreach (var child in container.Children)
            {
                WriteArrangementNode(writer, child);
            }
        }
    }

    void WriteArrangementOverride(ScreenplayWriter writer, ArrangementOverrideSyntax arrangementOverride)
    {
        using var anchor = writer.Anchor(arrangementOverride);
        writer.Line($"when {WriteOverrideCondition(arrangementOverride)}");
        using (writer.Indent())
        {
            WriteArrangementChildren(writer, arrangementOverride.Root);
        }
    }

    void WriteVariant(ScreenplayWriter writer, VariantSyntax variant)
    {
        using var anchor = writer.Anchor(variant);
        writer.Line($"variant width {variant.Width}, height {variant.Height}");
        using (writer.Indent())
        {
            foreach (var place in variant.Places)
            {
                writer.Line(WritePlaceLine(place), place);
            }
        }
    }

    string WriteArrangementSlotLine(ArrangementSlotSyntax slot)
    {
        var line = slot.Name;
        if (slot.Width is not null)
        {
            line += $" width {slot.Width}";
        }

        if (slot.Height is not null)
        {
            line += $" height {slot.Height}";
        }

        if (slot.Grow)
        {
            line += slot.GrowFactor is null ? " grow" : $" grow {WriteNumber(slot.GrowFactor.Value)}";
        }

        if (slot.Span is not null)
        {
            line += $" span {slot.Span}";
        }

        return line;
    }

    string WriteOverrideCondition(ArrangementOverrideSyntax arrangementOverride)
    {
        if (arrangementOverride.Width is not null && arrangementOverride.Height is not null)
        {
            return $"width {arrangementOverride.Width}, height {arrangementOverride.Height}";
        }

        if (arrangementOverride.Width is not null)
        {
            return $"width {arrangementOverride.Width}";
        }

        return $"height {arrangementOverride.Height}";
    }

    string WritePlaceLine(PlaceSyntax place) =>
        place.Hidden
            ? $"place {place.SlotName} hidden"
            : $"place {place.SlotName} at {place.X},{place.Y} size {place.SizeWidth},{place.SizeHeight}";

    readonly record struct TemplateMetadata(
        string? Category,
        string? TemplateType,
        IEnumerable<TemplateExposedValueSyntax> Exposes,
        IEnumerable<TemplateOutletSyntax> Outlets);
}
