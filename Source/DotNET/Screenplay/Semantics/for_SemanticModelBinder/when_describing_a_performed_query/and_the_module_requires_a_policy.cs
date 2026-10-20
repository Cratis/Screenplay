// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query;

public class and_the_module_requires_a_policy : given.an_authorized_performed_query
{
    SemanticPerformedQueryShape _shape;

    void Because() => _shape = ShapeOf("FigureList");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_carry_the_policy_the_module_requires() => _shape.Authorization.ShouldEqual(new SemanticPolicyReference("Buyers"));
}
