// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query;

public class and_nothing_requires_a_policy : given.an_authorized_performed_query
{
    SemanticPerformedQueryShape _shape;

    void Because() => _shape = ShapeOf("CurrentBanner");

    [Fact] void should_carry_no_authorization() => _shape.Authorization.ShouldBeNull();
}
