// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_using_projection_scopes;

// Chronicle ProjectionValidator.ValidateCompositeKey (ProjectionValidator.cs:383-465): the composite key type must exist and every
// part must name one of its properties. Chronicle stores only a text literal key as a value (ProjectionDefinitionSyntaxVisitor.cs:263-268).
// A parent key only routes inside a child collection (ProjectionFactory.cs:975-1007).
public class with_invalid_keys : given.a_scoped_projection_model
{
    Exception _unknownCompositeType;
    Exception _unknownCompositePart;
    Exception _numericLiteralKey;
    Exception _parentKeyOutsideChildren;
    Exception _missingParentKeyInsideChildren;
    Exception _incompatibleKeyType;

    void Because()
    {
        var lookupFrom = _lineLookup.Scope!.From.Single();
        var composite = (SemanticProjectionCompositeKey)lookupFrom.Key;
        _unknownCompositeType = Validate(_lineLookup with { Scope = _lineLookup.Scope with { From = [lookupFrom with { Key = composite with { Type = EventId("OrderPlaced") } }] } });
        _unknownCompositePart = Validate(_lineLookup with
        {
            Scope = _lineLookup.Scope with { From = [lookupFrom with { Key = composite with { Parts = [.. composite.Parts, new(TypeProperty("OrderLine", "Quantity"), SemanticProjectionValue.EventSourceIdentity)] } }] }
        });
        var placed = OrdersScope.From[0];
        _numericLiteralKey = ValidateOrders(OrdersScope with { From = [placed with { Key = new SemanticProjectionValueKey(SemanticProjectionValue.Literal(SemanticValue.Number(1))) }, OrdersScope.From[1]] });
        _parentKeyOutsideChildren = ValidateOrders(OrdersScope with { From = [placed with { ParentKey = SemanticProjectionKey.EventSourceIdentity }, OrdersScope.From[1]] });
        var lineFrom = Lines.Scope.From.Single();
        _missingParentKeyInsideChildren = ValidateOrders(OrdersScope with { Children = [Lines with { Scope = Lines.Scope with { From = [lineFrom with { ParentKey = null }] } }] });
        _incompatibleKeyType = ValidateOrders(OrdersScope with
        {
            From = [placed with { Key = new SemanticProjectionValueKey(SemanticProjectionValue.EventProperty([EventProperty("OrderPlaced", "Label")])) }, OrdersScope.From[1]]
        });
    }

    [Fact] void should_reject_an_unknown_composite_key_type() => _unknownCompositeType.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_composite_part_the_type_does_not_have() => _unknownCompositePart.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_literal_key_that_is_not_text() => _numericLiteralKey.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_parent_key_outside_a_child_collection() => _parentKeyOutsideChildren.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_child_block_without_a_parent_key() => _missingParentKeyInsideChildren.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_key_of_another_type_than_the_identity() => _incompatibleKeyType.ShouldBeOfExactType<InvalidSemanticContract>();
}
