// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// Chronicle lowers each joined event to a join on the 'on' property and discards both the identifier after 'join' and the
// joined event's auto-map (ProjectionDefinitionSyntaxVisitor.ProcessJoin, ProjectionDefinitionSyntaxVisitor.cs:147-156).
public class a_join_block : given.a_projection_block_binder
{
    const string Body =
        """
        from OrderPlaced key orderId
          label = label
        join customer on customerId
          with CustomerRegistered
            no automap
            customerName = customerName
        """;

    SemanticProjectionJoin _join;

    void Because()
    {
        _result = BindProjection("projection Orders => OrderView", Body);
        _join = Scope.Joins.Single();
    }

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_join_on_the_declared_property() => _join.On.ShouldEqual(ReadModelProperty("OrderView", "customerId"));
    [Fact] void should_join_the_event() => _join.EventContract.ShouldEqual(EventId("CustomerRegistered"));
    [Fact] void should_map_the_joined_event() => _join.Mappings.Single().Target.Single().ShouldEqual(ReadModelProperty("OrderView", "customerName"));
    [Fact] void should_warn_that_chronicle_drops_the_joined_event_auto_map() =>
        _result.Diagnostics.Single(_ => _.Code == DiagnosticCodes.PartiallyLoweredProjectionSyntax).Severity.ShouldEqual(DiagnosticSeverity.Warning);
}
