// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of the capture sub-language - the change data capture body.
/// </summary>
public partial class ScreenplayPrinter
{
    void WriteCapture(ScreenplayWriter writer, CaptureSyntax capture)
    {
        writer.Line($"capture {capture.Name}");
        using (writer.Indent())
        {
            if (capture.Source is not null)
            {
                WriteCaptureSource(writer, capture.Source);
            }

            if (capture.Key is not null)
            {
                writer.Line($"key {capture.Key}");
            }

            WriteCaptureMap(writer, capture.Map);

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
        writer.Line($"source {source.Kind}");
        using (writer.Indent())
        {
            foreach (var setting in source.Settings)
            {
                writer.Line(setting.Value.Length == 0 ? setting.Name : $"{setting.Name} {setting.Value}");
            }
        }
    }

    void WriteCaptureMap(ScreenplayWriter writer, IEnumerable<CaptureMapOperationSyntax> operations)
    {
        var list = operations.ToList();
        if (list.Count == 0)
        {
            return;
        }

        writer.Line("map");
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
                        writer.Line($"\"{translation.From}\" => {translation.To}");
                    }
                }

                break;
            case CaptureSplitSyntax split:
                writer.Line($"split {ScreenplaySyntaxText.Expression(split.Source)} by \"{split.Separator}\"");
                using (writer.Indent())
                {
                    foreach (var target in split.Targets)
                    {
                        writer.Line(target);
                    }
                }

                break;
        }
    }

    void WriteCaptureAppend(ScreenplayWriter writer, CaptureAppendSyntax append)
    {
        writer.Line($"append {append.Event}");
        using (writer.Indent())
        {
            WriteTags(writer, append.Tags);

            if (append.When is null)
            {
                WriteMappings(writer, append.Mappings);
                return;
            }

            writer.Line($"when {ScreenplaySyntaxText.CaptureWhen(append.When)}");
            using (writer.Indent())
            {
                WriteMappings(writer, append.Mappings);
            }
        }
    }

    void WriteCaptureChildren(ScreenplayWriter writer, CaptureChildrenSyntax children)
    {
        writer.Line($"children {children.Property} identified by {children.IdentifiedBy}");
        using (writer.Indent())
        {
            WriteCaptureMap(writer, children.Map);
            foreach (var append in children.Appends)
            {
                WriteCaptureAppend(writer, append);
            }
        }
    }

    void WriteCaptureNested(ScreenplayWriter writer, CaptureNestedSyntax nested)
    {
        writer.Line($"nested {nested.Property}");
        using (writer.Indent())
        {
            WriteCaptureMap(writer, nested.Map);
            foreach (var append in nested.Appends)
            {
                WriteCaptureAppend(writer, append);
            }
        }
    }
}
