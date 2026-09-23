// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_using_projection_scopes;

// Chronicle lowers 'x = null' to the clear expression (ProjectionDefinitionSyntaxVisitor.cs:242-245), types arithmetic by the
// read-model target (PropertyMappers.cs:47-67, 183-219), and resolves an event-context path by reflection over EventContext
// (Chronicle/Source/Kernel/Concepts/Events/EventContext.cs:28-44), so an unknown path can only be caught here.
public class with_invalid_mappings : given.a_scoped_projection_model
{
    Exception _nullSet;
    Exception _unknownContextPath;
    Exception _nonCanonicalContextPath;
    Exception _collectionContextPath;
    Exception _arithmeticOnText;
    Exception _clearOfARequiredTarget;
    Exception _everyReadingAnEventProperty;
    Exception _duplicateTarget;

    void Because()
    {
        var placed = OrdersScope.From[0];
        SemanticProjectionScope WithPlacedMapping(SemanticProjectionMapping mapping) =>
            OrdersScope with { From = [placed with { Mappings = [.. placed.Mappings, mapping] }, OrdersScope.From[1]] };

        _nullSet = ValidateOrders(WithPlacedMapping(Map(SemanticProjectionOperation.Set, SemanticProjectionValue.Literal(SemanticValue.Null), ReadModelProperty("LastSeen"))));
        _unknownContextPath = ValidateOrders(WithPlacedMapping(Map(SemanticProjectionOperation.Set, SemanticProjectionValue.EventContext("causationId"), ReadModelProperty("LastSeen"))));
        _nonCanonicalContextPath = ValidateOrders(WithPlacedMapping(Map(SemanticProjectionOperation.Set, SemanticProjectionValue.EventContext("Occurred"), ReadModelProperty("LastSeen"))));
        _collectionContextPath = ValidateOrders(WithPlacedMapping(Map(SemanticProjectionOperation.Set, SemanticProjectionValue.EventContext("tags"), ReadModelProperty("LastSeen"))));
        _arithmeticOnText = ValidateOrders(WithPlacedMapping(Map(SemanticProjectionOperation.Increment, null, ReadModelProperty("CustomerName"))));
        _clearOfARequiredTarget = ValidateOrders(WithPlacedMapping(Map(SemanticProjectionOperation.Clear, null, ReadModelProperty("OrderId"))));
        _everyReadingAnEventProperty = ValidateOrders(OrdersScope with
        {
            Every = OrdersScope.Every! with { Mappings = [Map(SemanticProjectionOperation.Set, SemanticProjectionValue.EventProperty([EventProperty("OrderPlaced", "Label")]), ReadModelProperty("CustomerName"))] }
        });
        _duplicateTarget = ValidateOrders(WithPlacedMapping(placed.Mappings[0]));
    }

    [Fact] void should_reject_a_null_set_because_it_is_a_clear() => _nullSet.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_path_chronicles_event_context_does_not_have() => _unknownContextPath.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_path_not_spelled_canonically() => _nonCanonicalContextPath.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_path_naming_a_collection() => _collectionContextPath.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_arithmetic_on_a_text_target() => _arithmeticOnText.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_clearing_a_required_target() => _clearOfARequiredTarget.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_every_mapping_that_reads_an_event_property() => _everyReadingAnEventProperty.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_duplicated_target() => _duplicateTarget.ShouldBeOfExactType<InvalidSemanticContract>();
}
