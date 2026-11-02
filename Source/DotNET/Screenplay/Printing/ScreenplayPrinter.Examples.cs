// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Printing;

public sealed partial class ScreenplayPrinter
{
    // An inline value stays on the header line only when it is a scalar; an object or a list moves under the header as a block.
    static string? ScalarInline(IEnumerable<PropertyMappingSyntax> values, string? inlineProperty) =>
        inlineProperty is not null && values.SingleOrDefault(value => value.Property == inlineProperty) is { } inline && !ScreenplaySyntaxText.IsStructured(inline.Source) ? inlineProperty : null;

    static string SpecificationHeader(ScreenplayWriter writer, string header, IEnumerable<PropertyMappingSyntax> values, string? inlineProperty)
    {
        var inline = inlineProperty is null ? null : values.SingleOrDefault(value => value.Property == inlineProperty);

        return inline is null ? header : $"{header} {inline.Property} = {writer.Expression(inline.Source)}";
    }

    void WriteSpecificationExample(ScreenplayWriter writer, SpecificationExampleSyntax example)
    {
        using var anchor = writer.Anchor(example);
        writer.Line($"example {example.Name} : {example.Type}");
        using (writer.Indent())
        {
            WriteDescription(writer, example.Description, example);
            WriteSpecificationEventSource(writer, example.For);
            WriteSpecificationRoute(writer, example.Stream, example.NoStream);
            WriteSpecificationValues(writer, example.Values);
            foreach (var fixture in example.GeneratedValues)
            {
                writer.Assignment($"generated {fixture.Property}", fixture.Source, fixture, ScreenplaySyntaxText.ResponseValue);
            }
        }
    }
}
