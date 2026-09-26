// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of the projection sub-language - the body of a <c>projection</c> declaration.
/// </summary>
public partial class ScreenplayPrinter
{
    void WriteProjection(ScreenplayWriter writer, ProjectionSyntax projection)
    {
        using var anchor = writer.Anchor(projection);
        var header = projection.ReadModel is null
            ? $"projection {projection.Name}"
            : $"projection {projection.Name} => {projection.ReadModel}";
        writer.Line(header);

        using (writer.Indent())
        {
            WriteFile(writer, projection.File);

            if (projection.Sequence is not null)
            {
                writer.DirectiveLine($"sequence {projection.Sequence}", projection, "sequence");
            }

            WriteAutoMap(writer, projection.AutoMap, projection);

            if (projection.Key is not null)
            {
                WriteKey(writer, projection.Key);
            }

            foreach (var block in projection.Blocks)
            {
                WriteProjectionBlock(writer, block);
            }
        }
    }

    void WriteProjectionBlock(ScreenplayWriter writer, ProjectionBlockSyntax block)
    {
        using var anchor = writer.Anchor(block);
        switch (block)
        {
            case FromSyntax from:
                WriteFrom(writer, from);
                break;
            case EverySyntax every:
                WriteEvery(writer, every);
                break;
            case AllSyntax all:
                writer.Line("all");
                using (writer.Indent())
                {
                    WriteAutoMap(writer, all.AutoMap, all);
                    WriteMappings(writer, all.Mappings, ReservedWords.None);
                }

                break;
            case JoinSyntax join:
                WriteJoin(writer, join);
                break;
            case ChildrenSyntax children:
                writer.Line($"children {children.Property} identified by {ScreenplaySyntaxText.Expression(children.IdentifiedBy)}");
                using (writer.Indent())
                {
                    WriteAutoMap(writer, children.AutoMap, children);
                    foreach (var nested in children.Blocks)
                    {
                        WriteProjectionBlock(writer, nested);
                    }
                }

                break;
            case NestedSyntax nested:
                writer.Line($"nested {nested.Property}");
                using (writer.Indent())
                {
                    WriteAutoMap(writer, nested.AutoMap, nested);
                    foreach (var child in nested.Blocks)
                    {
                        WriteProjectionBlock(writer, child);
                    }
                }

                break;
            case ClearWithSyntax clear:
                writer.Line($"clear with {clear.Event}");
                break;
            case RemoveWithSyntax remove:
                WriteRemoveWith(writer, remove);
                break;
            case RemoveViaJoinSyntax removeViaJoin:
                writer.Line(removeViaJoin.Key is null
                    ? $"remove via join on {removeViaJoin.Event}"
                    : $"remove via join on {removeViaJoin.Event} key {ScreenplaySyntaxText.Expression(removeViaJoin.Key)}");
                break;
            case ProjectionVariantSyntax variant:
                WriteProjectionVariant(writer, variant);
                break;
            default:
                throw new UnsupportedSyntaxForPrinting("projection block", block.GetType().Name);
        }
    }

    void WriteProjectionVariant(ScreenplayWriter writer, ProjectionVariantSyntax variant)
    {
        using var anchor = writer.Anchor(variant);
        writer.Line($"variant {variant.Name}");
        using (writer.Indent())
        {
            foreach (var entersOn in variant.EntersOn)
            {
                writer.Line(
                    entersOn.Key is null
                        ? $"enters on {entersOn.Event}"
                        : $"enters on {entersOn.Event} key {ScreenplaySyntaxText.Expression(entersOn.Key)}",
                    entersOn);
            }

            foreach (var block in variant.Blocks)
            {
                WriteProjectionBlock(writer, block);
            }
        }
    }

    void WriteFrom(ScreenplayWriter writer, FromSyntax from)
    {
        using var anchor = writer.Anchor(from);
        var events = from.Events.Select(spec => spec.Key is null
            ? spec.Event
            : $"{spec.Event} key {ScreenplaySyntaxText.Expression(spec.Key)}");
        writer.Line($"from {string.Join(", ", events)}");

        using (writer.Indent())
        {
            if (from.Key is not null)
            {
                WriteKey(writer, from.Key);
            }

            if (from.ParentKey is not null)
            {
                writer.Line($"parent {ScreenplaySyntaxText.Expression(from.ParentKey)}", from.ParentKey);
            }

            WriteMappings(writer, from.Mappings, ReservedWords.ProjectionFromBlock);
        }
    }

    void WriteEvery(ScreenplayWriter writer, EverySyntax every)
    {
        using var anchor = writer.Anchor(every);
        writer.Line("every");
        using (writer.Indent())
        {
            WriteAutoMap(writer, every.AutoMap, every);
            if (!every.IncludeChildren)
            {
                writer.DirectiveLine("exclude children", every, "exclude children");
            }

            WriteMappings(writer, every.Mappings, ReservedWords.None);
        }
    }

    void WriteJoin(ScreenplayWriter writer, JoinSyntax join)
    {
        using var anchor = writer.Anchor(join);
        writer.Line($"join {join.Property} on {join.On}");
        using (writer.Indent())
        {
            foreach (var joined in join.Events)
            {
                writer.Line($"with {joined.Event}", joined);
                using (writer.Indent())
                {
                    WriteAutoMap(writer, joined.AutoMap, joined);
                    WriteMappings(writer, joined.Mappings, ReservedWords.None);
                }
            }
        }
    }

    void WriteRemoveWith(ScreenplayWriter writer, RemoveWithSyntax remove)
    {
        using var anchor = writer.Anchor(remove);
        writer.Line(remove.Key is null
            ? $"remove with {remove.Event}"
            : $"remove with {remove.Event} key {ScreenplaySyntaxText.Expression(remove.Key)}");

        if (remove.ParentKey is null)
        {
            return;
        }

        using (writer.Indent())
        {
            writer.Line($"parent {ScreenplaySyntaxText.Expression(remove.ParentKey)}", remove.ParentKey);
        }
    }

    void WriteKey(ScreenplayWriter writer, KeySyntax key)
    {
        using var anchor = writer.Anchor(key);
        switch (key)
        {
            case ExpressionKeySyntax expression:
                writer.Line($"key {ScreenplaySyntaxText.Expression(expression.Expression)}");
                break;
            case CompositeKeySyntax composite:
                writer.Line($"key {composite.Type}");
                using (writer.Indent())
                {
                    foreach (var part in composite.Parts)
                    {
                        writer.Line($"{part.Property} = {ScreenplaySyntaxText.Expression(part.Expression)}", part);
                    }
                }

                break;
            default:
                throw new UnsupportedSyntaxForPrinting("projection key", key.GetType().Name);
        }
    }

    void WriteAutoMap(ScreenplayWriter writer, AutoMapMode autoMap, SyntaxNode owner)
    {
        if (autoMap != AutoMapMode.Inherit)
        {
            foreach (var previous in owner.DirectiveLocations
                .Where(entry => entry.Key.StartsWith("automap previous:", StringComparison.Ordinal))
                .OrderBy(entry => entry.Value.Line))
            {
                var text = previous.Key.StartsWith("automap previous:Enabled:", StringComparison.Ordinal) ? "automap" : "no automap";
                writer.DirectiveLine(text, owner, previous.Key);
            }
        }

        switch (autoMap)
        {
            case AutoMapMode.Enabled:
                writer.DirectiveLine("automap", owner, "automap");
                break;
            case AutoMapMode.Disabled:
                writer.DirectiveLine("no automap", owner, "automap");
                break;
            case AutoMapMode.Inherit:
                break;
            default:
                throw new UnsupportedSyntaxForPrinting("automap mode", autoMap.ToString());
        }
    }

    void WriteMappings(ScreenplayWriter writer, IEnumerable<MappingSyntax> mappings, IReadOnlySet<string> reserved)
    {
        foreach (var mapping in mappings)
        {
            writer.Line(
                mapping switch
                {
                    SetMappingSyntax set => $"{ReservedWords.Escape(set.Property, reserved)} = {ScreenplaySyntaxText.Expression(set.Source)}",
                    ClearMappingSyntax clear => $"clear {ReservedWords.Escape(clear.Property, ReservedWords.ClearMapping)}",
                    IncrementMappingSyntax increment => $"increment {increment.Property}",
                    DecrementMappingSyntax decrement => $"decrement {decrement.Property}",
                    CountMappingSyntax count => $"count {count.Property}",
                    AddMappingSyntax add => $"add {add.Property} by {ScreenplaySyntaxText.Expression(add.Value)}",
                    SubtractMappingSyntax subtract => $"subtract {subtract.Property} by {ScreenplaySyntaxText.Expression(subtract.Value)}",
                    _ => throw new UnsupportedSyntaxForPrinting("projection mapping", mapping.GetType().Name)
                },
                mapping);
        }
    }
}
