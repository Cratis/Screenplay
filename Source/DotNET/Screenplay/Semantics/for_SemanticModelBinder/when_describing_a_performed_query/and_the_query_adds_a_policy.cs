// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query;

public class and_the_query_adds_a_policy : given.an_authorized_performed_query
{
    SemanticPerformedQueryShape _shape;

    void Because() => _shape = ShapeOf("PublicFigures");

    [Fact] void should_combine_the_inherited_and_the_own_policy() => _shape.Authorization.ShouldEqual(
        new SemanticLogicalAuthorization(new SemanticPolicyReference("Buyers"), SemanticLogicalOperator.And, new SemanticPolicyReference("Anyone")));
}
