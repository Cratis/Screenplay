// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_ScreenplaySyntaxWalker;

public class when_walking_structured_values : Specification
{
    given.a_counting_walker _walker;
    Specifications.SpecificationSyntax _specification;

    void Establish()
    {
        _walker = new();
        _specification = new ScreenplayCompiler().CompileSpecification("specification Placing\n  when Place\n    lines = [{\"sku\":\"A-1\"}]").Value!;
    }

    void Because() => _walker.VisitSpecification(_specification);

    [Fact] void should_descend_into_the_list() => _walker.Nodes.OfType<ListExpressionSyntax>().Count().ShouldEqual(1);
    [Fact] void should_descend_into_the_object() => _walker.Nodes.OfType<ObjectExpressionSyntax>().Count().ShouldEqual(1);
    [Fact] void should_visit_the_key() => _walker.Nodes.OfType<ObjectMemberSyntax>().Single().Name.ShouldEqual("sku");
    [Fact] void should_visit_the_value() => _walker.Nodes.OfType<LiteralExpressionSyntax>().Single().Value.ShouldEqual("A-1");
}
