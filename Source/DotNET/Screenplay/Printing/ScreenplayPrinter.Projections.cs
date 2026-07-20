// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of the projection sub-language - the body of a <c>projection</c> declaration.
/// </summary>
public partial class ScreenplayPrinter
{
    void WriteProjection(ScreenplayWriter writer, ProjectionSyntax projection)
    {
        var header = projection.ReadModel is null
            ? $"projection {projection.Name}"
            : $"projection {projection.Name} => {projection.ReadModel}";
        writer.Line(header);

        using (writer.Indent())
        {
            if (projection.Sequence is not null)
            {
                writer.Line($"sequence {projection.Sequence}");
            }

            WriteAutoMap(writer, projection.AutoMap);

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
                    WriteAutoMap(writer, all.AutoMap);
                    WriteMappings(writer, all.Mappings);
                }

                break;
            case JoinSyntax join:
                WriteJoin(writer, join);
                break;
            case ChildrenSyntax children:
                writer.Line($"children {children.Property} identified by {ScreenplaySyntaxText.Expression(children.IdentifiedBy)}");
                using (writer.Indent())
                {
                    WriteAutoMap(writer, children.AutoMap);
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
                    WriteAutoMap(writer, nested.AutoMap);
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
        }
    }

    void WriteFrom(ScreenplayWriter writer, FromSyntax from)
    {
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
                writer.Line($"parent {ScreenplaySyntaxText.Expression(from.ParentKey)}");
            }

            WriteMappings(writer, from.Mappings);
        }
    }

    void WriteEvery(ScreenplayWriter writer, EverySyntax every)
    {
        writer.Line("every");
        using (writer.Indent())
        {
            WriteAutoMap(writer, every.AutoMap);
            if (!every.IncludeChildren)
            {
                writer.Line("exclude children");
            }

            WriteMappings(writer, every.Mappings);
        }
    }

    void WriteJoin(ScreenplayWriter writer, JoinSyntax join)
    {
        writer.Line($"join {join.Property} on {join.On}");
        using (writer.Indent())
        {
            foreach (var joined in join.Events)
            {
                writer.Line($"with {joined.Event}");
                using (writer.Indent())
                {
                    WriteAutoMap(writer, joined.AutoMap);
                    WriteMappings(writer, joined.Mappings);
                }
            }
        }
    }

    void WriteRemoveWith(ScreenplayWriter writer, RemoveWithSyntax remove)
    {
        writer.Line(remove.Key is null
            ? $"remove with {remove.Event}"
            : $"remove with {remove.Event} key {ScreenplaySyntaxText.Expression(remove.Key)}");

        if (remove.ParentKey is null)
        {
            return;
        }

        using (writer.Indent())
        {
            writer.Line($"parent {ScreenplaySyntaxText.Expression(remove.ParentKey)}");
        }
    }

    void WriteKey(ScreenplayWriter writer, KeySyntax key)
    {
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
                        writer.Line($"{part.Property} = {ScreenplaySyntaxText.Expression(part.Expression)}");
                    }
                }

                break;
        }
    }

    void WriteAutoMap(ScreenplayWriter writer, AutoMapMode autoMap)
    {
        switch (autoMap)
        {
            case AutoMapMode.Enabled:
                writer.Line("automap");
                break;
            case AutoMapMode.Disabled:
                writer.Line("no automap");
                break;
        }
    }

    void WriteMappings(ScreenplayWriter writer, IEnumerable<MappingSyntax> mappings)
    {
        foreach (var mapping in mappings)
        {
            writer.Line(mapping switch
            {
                SetMappingSyntax set => $"{set.Property} = {ScreenplaySyntaxText.Expression(set.Source)}",
                IncrementMappingSyntax increment => $"increment {increment.Property}",
                DecrementMappingSyntax decrement => $"decrement {decrement.Property}",
                CountMappingSyntax count => $"count {count.Property}",
                AddMappingSyntax add => $"add {add.Property} by {ScreenplaySyntaxText.Expression(add.Value)}",
                SubtractMappingSyntax subtract => $"subtract {subtract.Property} by {ScreenplaySyntaxText.Expression(subtract.Value)}",
                _ => mapping.Property
            });
        }
    }
}
