// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query;

public class and_the_query_has_a_key : given.an_authorized_performed_query
{
    SemanticPerformedQueryShape _shape;

    void Because() => _shape = ShapeOf("OpenFigures");

    [Fact] void should_still_describe_the_key() => _shape.Arguments.Single().Name.ShouldEqual("figureId");
    [Fact] void should_carry_the_policy_the_module_requires() => _shape.Authorization.ShouldEqual(new SemanticPolicyReference("Buyers"));
}
