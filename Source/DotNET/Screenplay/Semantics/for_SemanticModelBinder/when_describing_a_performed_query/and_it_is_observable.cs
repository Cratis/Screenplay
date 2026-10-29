// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query;

public class and_it_is_observable : given.a_performed_query
{
    SemanticPerformedQueryShape _shape;

    void Because() => _shape = ShapeOf("LiveFigures");

    [Fact] void should_be_live() => _shape.Delivery.ShouldEqual(SemanticQueryDelivery.Live);
    [Fact] void should_return_many() => _shape.Cardinality.ShouldEqual(SemanticQueryCardinality.Many);
    [Fact] void should_carry_the_live_result_version() => RequirementOf("LiveFigures").ResultVersion.ShouldEqual(2u);
    [Fact] void should_keep_the_first_context_version() => RequirementOf("LiveFigures").ContextVersion.ShouldEqual(1u);
    [Fact] void should_have_no_arguments() => _shape.Arguments.ShouldBeEmpty();
}
