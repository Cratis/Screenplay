// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query;

public class and_it_returns_a_collection : given.a_performed_query
{
    SemanticPerformedQueryShape _shape;

    void Because() => _shape = ShapeOf("FiguresInRegion");

    [Fact] void should_return_many() => _shape.Cardinality.ShouldEqual(SemanticQueryCardinality.Many);
    [Fact] void should_describe_only_caller_supplied_filters() => _shape.Arguments.Select(_ => _.Name).ShouldEqual(["region", "status"]);
    [Fact] void should_describe_them_as_filters() => _shape.Arguments.ShouldEachConformTo(_ => _.Kind == SemanticPerformedQueryArgumentKind.Filter);
    [Fact] void should_describe_a_concept_filter_by_its_concept() => _shape.Arguments[0].Type!.Kind.ShouldEqual(SemanticTypeReferenceKind.Concept);
    [Fact] void should_require_the_required_filter() => _shape.Arguments[0].IsOptional.ShouldBeFalse();
    [Fact] void should_let_the_caller_omit_the_optional_filter() => _shape.Arguments[1].IsOptional.ShouldBeTrue();
    [Fact] void should_carry_optionality_on_the_type() => _shape.Arguments[1].Type!.IsOptional.ShouldBeTrue();
}
