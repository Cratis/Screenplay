// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// Chronicle admits every mapping kind, lowers 'clear x' and 'x = null' to the same expression, and implements 'count' as
// 'increment' (ProjectionDefinitionSyntaxVisitor.cs:209-245, PropertyMappers.cs:106-153). Target and source paths navigate
// composite objects (#210: nested sets and path expressions; ProjectionValidator.cs:349-381).
public class mapping_kinds : given.a_projection_block_binder
{
    const string Body =
        """
        from OrderPlaced key orderId
          no automap
          label = null
          clear customerName
          add total by quantity.amount
          subtract events by 2
          count amount
          quantity.basis = label
          basis = quantity.basis
        """;

    SemanticProjectionMapping[] _mappings;

    void Because()
    {
        _result = BindProjection("projection Orders => OrderView", Body);
        _mappings = [.. Scope.From.Single().Mappings];
    }

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_a_null_set_as_a_clear() => _mappings[0].Operation.ShouldEqual(SemanticProjectionOperation.Clear);
    [Fact] void should_bind_clear_as_the_same_clear() => (_mappings[1].Operation, _mappings[1].Source).ShouldEqual((SemanticProjectionOperation.Clear, null));
    [Fact] void should_add_from_a_nested_event_path() =>
        ((SemanticProjectionEventProperty)_mappings[2].Source!).Path.ShouldContainOnly([EventProperty("OrderPlaced", "quantity"), TypeProperty("Quantity", "amount")]);
    [Fact] void should_subtract_a_literal() => _mappings[3].Operation.ShouldEqual(SemanticProjectionOperation.Subtract);
    [Fact] void should_bind_count_as_increment() => _mappings[4].Operation.ShouldEqual(SemanticProjectionOperation.Increment);
    [Fact] void should_set_a_nested_target_path() => _mappings[5].Target.ShouldContainOnly([ReadModelProperty("OrderView", "quantity"), TypeProperty("Quantity", "basis")]);
    [Fact] void should_set_from_a_nested_source_path() => ((SemanticProjectionEventProperty)_mappings[6].Source!).Path.Length.ShouldEqual(2);
}
