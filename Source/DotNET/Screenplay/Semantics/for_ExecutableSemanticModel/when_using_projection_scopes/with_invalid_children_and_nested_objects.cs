// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_using_projection_scopes;

// Chronicle ProjectionValidator.ValidateChildren (ProjectionValidator.cs:217-238): the children property must exist and be an
// array with an item schema. Chronicle ProjectionFactory.cs:482-484: an unset identity uses the read model key property's name,
// which the element type must then have. A nested object must be one composite property.
public class with_invalid_children_and_nested_objects : given.a_scoped_projection_model
{
    Exception _missingChildren;
    Exception _scalarChildren;
    Exception _unsetIdentityWithoutKeyProperty;
    Exception _collectionNested;

    void Because()
    {
        _missingChildren = ValidateOrders(OrdersScope with { Children = [Lines with { Property = EventId("OrderPlaced") }] });
        _scalarChildren = ValidateOrders(OrdersScope with { Children = [Lines with { Property = ReadModelProperty("Shipping") }], Nested = [] });
        _unsetIdentityWithoutKeyProperty = ValidateOrders(OrdersScope with { Children = [Lines with { IdentifiedBy = default }] });
        _collectionNested = ValidateOrders(OrdersScope with
        {
            Children = [],
            Nested = [OrdersScope.Nested.Single() with { Property = ReadModelProperty("Lines") }]
        });
    }

    [Fact] void should_reject_a_missing_children_property() => _missingChildren.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_children_that_are_not_a_collection() => _scalarChildren.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_unset_identity_without_the_read_model_key_property() => _unsetIdentityWithoutKeyProperty.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_nested_collection() => _collectionNested.ShouldBeOfExactType<InvalidSemanticContract>();
}
