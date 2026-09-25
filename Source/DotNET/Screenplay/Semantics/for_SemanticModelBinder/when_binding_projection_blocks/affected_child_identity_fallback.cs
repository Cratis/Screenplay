// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_projection_blocks;

// ProjectionFactory.CreateProjectionStructure (ProjectionFactory.cs:485-500) falls back to the key property for an unset child identity.
public class affected_child_identity_fallback : given.a_projection_block_binder
{
    SemanticAffectedProjectionInstance _join;
    SemanticAffectedProjectionInstance _removal;
    SemanticAffectedProjectionInstance _withoutSchema;

    void Because()
    {
        _result = BindProjection("projection Orders => OrderView", "children lines identified by lineNumber\n  join product on productId\n    with ProductUpdated\n      count quantity\n  remove via join on LineRemoved key lineNumber");
        if (!_result.Success) throw new InvalidOperationException(string.Join("; ", _result.Diagnostics.Select(_ => _.Message)));

        // An ESM consumer may receive an unset child identity even though this syntax always supplies one.
        var projection = Projection with { Scope = Scope with { Children = [Scope.Children.Single() with { IdentifiedBy = default }] } };
        _join = projection.GetAffectedInstances(Application).Single(_ => _.Block == SemanticAffectedProjectionBlock.Join);
        _removal = projection.GetAffectedInstances(Application).Single(_ => _.Block == SemanticAffectedProjectionBlock.JoinRemoval);
        _withoutSchema = projection.GetAffectedInstances().Single(_ => _.Block == SemanticAffectedProjectionBlock.Join);
    }

    [Fact] void should_fall_back_to_the_child_property_named_like_the_read_model_identifier() =>
        _join.Property.ShouldEqual(TypeProperty("OrderLine", "orderId"));
    [Fact] void should_fall_back_for_remove_via_join() => _removal.Property.ShouldEqual(TypeProperty("OrderLine", "orderId"));
    [Fact] void should_not_guess_an_unset_identity_without_a_schema() => _withoutSchema.Match.ShouldEqual(SemanticAffectedProjectionMatch.Unverified);
}
