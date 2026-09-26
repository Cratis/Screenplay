// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of the capture sub-language - the change data capture body.
/// </summary>
public partial class ScreenplayPrinter
{
    void WriteCapture(ScreenplayWriter writer, CaptureSyntax capture)
    {
        using var anchor = writer.Anchor(capture);
        writer.Line($"capture {capture.Name}");
        using (writer.Indent())
        {
            if (capture.Source is not null)
            {
                WriteCaptureSource(writer, capture.Source);
            }

            if (capture.Key is not null)
            {
                writer.DirectiveLine($"key {capture.Key}", capture, "key");
            }

            WriteCaptureMap(writer, capture.Map, capture);

            foreach (var append in capture.Appends)
            {
                WriteCaptureAppend(writer, append);
            }

            foreach (var children in capture.Children)
            {
                WriteCaptureChildren(writer, children);
            }

            foreach (var nested in capture.Nested)
            {
                WriteCaptureNested(writer, nested);
            }
        }
    }

    void WriteCaptureSource(ScreenplayWriter writer, CaptureSourceSyntax source)
    {
        using var anchor = writer.Anchor(source);
        writer.Line($"source {source.Kind}");
        using (writer.Indent())
        {
            foreach (var setting in source.Settings)
            {
                writer.Line(setting.Value.Length == 0 ? setting.Name : $"{setting.Name} {setting.Value}", setting);
            }
        }
    }

    void WriteCaptureMap(ScreenplayWriter writer, IEnumerable<CaptureMapOperationSyntax> operations, SyntaxNode owner)
    {
        var list = operations.ToList();
        if (list.Count == 0)
        {
            return;
        }

        writer.DirectiveLine("map", owner, "map");
        using (writer.Indent())
        {
            foreach (var operation in list)
            {
                WriteCaptureMapOperation(writer, operation);
            }
        }
    }

    void WriteCaptureMapOperation(ScreenplayWriter writer, CaptureMapOperationSyntax operation)
    {
        using var anchor = writer.Anchor(operation);
        switch (operation)
        {
            case CaptureMapEntrySyntax entry:
                var translations = entry.Translations.ToList();
                var suffix = translations.Count > 0 ? " translate" : string.Empty;
                writer.Line($"{entry.Property} = {ScreenplaySyntaxText.Expression(entry.Source)}{suffix}");
                using (writer.Indent())
                {
                    foreach (var translation in translations)
                    {
                        writer.Line($"{StringLiteral.Quote(translation.From)} => {translation.To}", translation);
                    }
                }

                break;
            case CaptureSplitSyntax split:
                writer.Line($"split {ScreenplaySyntaxText.Expression(split.Source)} by {StringLiteral.Quote(split.Separator)}");
                using (writer.Indent())
                {
                    var targets = split.Targets.ToList();
                    for (var index = 0; index < targets.Count; index++)
                    {
                        writer.DirectiveLine(targets[index], split, $"target:{index}");
                    }
                }

                break;
            default:
                throw new UnsupportedSyntaxForPrinting("capture map operation", operation.GetType().Name);
        }
    }

    void WriteCaptureAppend(ScreenplayWriter writer, CaptureAppendSyntax append)
    {
        using var anchor = writer.Anchor(append);
        writer.Line($"append {append.Event}");
        using (writer.Indent())
        {
            WriteTags(writer, append.Tags);

            if (append.When is null)
            {
                WriteMappings(writer, append.Mappings, ReservedWords.MappingBlock);
                return;
            }

            writer.Line($"when {ScreenplaySyntaxText.CaptureWhen(append.When)}", append.When);
            using (writer.Indent())
            {
                WriteMappings(writer, append.Mappings, ReservedWords.MappingBlock);
            }
        }
    }

    void WriteCaptureChildren(ScreenplayWriter writer, CaptureChildrenSyntax children)
    {
        using var anchor = writer.Anchor(children);
        writer.Line($"children {children.Property} identified by {children.IdentifiedBy}");
        using (writer.Indent())
        {
            WriteCaptureMap(writer, children.Map, children);
            foreach (var append in children.Appends)
            {
                WriteCaptureAppend(writer, append);
            }
        }
    }

    void WriteCaptureNested(ScreenplayWriter writer, CaptureNestedSyntax nested)
    {
        using var anchor = writer.Anchor(nested);
        writer.Line($"nested {nested.Property}");
        using (writer.Indent())
        {
            WriteCaptureMap(writer, nested.Map, nested);
            foreach (var append in nested.Appends)
            {
                WriteCaptureAppend(writer, append);
            }
        }
    }
}
