// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_guarding_composite_stream_id_contracts : given.a_compiler
{
    [Fact]
    void should_refuse_unrepresentable_shapes_at_transport_and_print_boundaries()
    {
        var location = SourceLocation.Start;
        var type = new TypeRefSyntax("String", false, false, location);
        var part = new EventStreamIdPartSyntax("one", type, location);
        var mapping = new PropertyMappingSyntax("one", new LiteralExpressionSyntax("value", location), location);
        foreach (var node in new SyntaxNode[]
        {
            new EventStreamSyntax("S", location) { StreamIdParts = null! },
            new EventStreamSyntax("S", location) { StreamIdParts = [null!] },
            new EventStreamSyntax("S", location) { StreamId = type, StreamIdParts = [part] },
            part with { Name = "bad-name" },
            part with { Type = type with { IsOptional = true } },
            part with { Type = type with { IsCollection = true } },
            new CommandStreamSyntax("A", "S", location) { StreamIdParts = null! },
            new CommandStreamSyntax("A", "S", location) { StreamIdParts = [null!] },
            new CommandStreamSyntax("A", "S", location) { StreamId = mapping with { Property = "streamId" }, StreamIdParts = [mapping] },
            new SpecificationStreamSyntax("A", "S", location) { StreamIdParts = [mapping with { Property = "bad-name" }] }
        })
        {
            Catch.Exception(() => SyntaxJson.Serialize(node)).ShouldBeOfExactType<InvalidSyntaxJson>();
        }
    }

    [Theory]
    [InlineData("one String")]
    [InlineData("one String\n      one String")]
    void should_retain_invalid_but_representable_drafts(string parts)
    {
        var parsed = _compiler.Parse("eventsource A\n  stream S\n    streamId\n      " + parts);
        var decoded = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed.Value!));
        var printed = new ScreenplayPrinter().Print(decoded);
        SyntaxJson.StructurallyEqual(decoded, _compiler.Parse(printed).Value!).ShouldBeTrue();
    }

    [Fact]
    void should_visit_part_declarations_types_and_route_mappings()
    {
        var parsed = _compiler.Parse("eventsource A\n  stream S\n    streamId\n      one String\n      two String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream A.S\n          streamId\n            two = \"b\"\n            one = \"a\"").Value!;
        var walker = new Parts();
        walker.VisitApplication(parsed);
        walker.Declarations.Count.ShouldEqual(2);
        walker.Mappings.Count.ShouldEqual(2);
    }

    sealed class Parts : ScreenplaySyntaxWalker
    {
        internal List<EventStreamIdPartSyntax> Declarations { get; } = [];
        internal List<PropertyMappingSyntax> Mappings { get; } = [];

        public override void VisitEventStreamIdPart(EventStreamIdPartSyntax syntax)
        {
            Declarations.Add(syntax);
            base.VisitEventStreamIdPart(syntax);
        }

        public override void VisitPropertyMapping(PropertyMappingSyntax syntax)
        {
            Mappings.Add(syntax);
            base.VisitPropertyMapping(syntax);
        }
    }
}
