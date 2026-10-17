// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// Chronicle honors 'no automap' on one joined event even when its level maps automatically
// (ProjectionFactory.GetMergedJoinProperties, Chronicle 19.10.0 and later).
public class a_join_block_without_auto_map : given.a_projection_block_binder
{
    const string Body =
        """
        from OrderPlaced key orderId
        join customer on customerId
          with CustomerRegistered
            no automap
        """;

    SemanticProjectionJoin _join;

    void Because()
    {
        _result = BindProjection("projection Orders => OrderView", Body);
        _join = Scope.Joins.Single();
    }

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_map_nothing_from_the_joined_event() => _join.Mappings.ShouldBeEmpty();
    [Fact] void should_not_warn_about_the_joined_event_auto_map() => _result.Diagnostics.ShouldNotContain(_ => _.Code == DiagnosticCodes.PartiallyLoweredProjectionSyntax);
}
