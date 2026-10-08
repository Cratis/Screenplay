// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_preserving_example_route_parity
{
    [Theory]
    [InlineData("concept Shape : String")]
    [InlineData("type Shape\n  value String")]
    void should_not_apply_command_and_read_model_route_diagnostics_to_other_types(string declaration)
    {
        var result = new ScreenplayCompiler().Compile(declaration + "\nexample Fixture : Shape\n  no stream");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0526").ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0520").ShouldBeTrue();
    }

    [Fact]
    void should_dispatch_example_and_locator_markers_through_the_overridable_visitor()
    {
        var result = new ScreenplayCompiler().Compile("module M\n  feature F\n    slice Automation S\n      event E\n      reaction Observer\n        when E\n      example Expected : E\n        no stream\n      specification X\n        given E\n        when redelivered E to Observer\n          no stream\n        then no events");
        result.Diagnostics.ShouldBeEmpty();
        var walker = new RouteWalker();
        walker.VisitApplication(result.Value!);
        walker.Markers.Count.ShouldEqual(2);
    }

    sealed class RouteWalker : ScreenplaySyntaxWalker
    {
        public List<SpecificationNoStreamSyntax> Markers { get; } = [];

        public override void VisitSpecificationNoStream(SpecificationNoStreamSyntax syntax)
        {
            Markers.Add(syntax);
            base.VisitSpecificationNoStream(syntax);
        }
    }
}
