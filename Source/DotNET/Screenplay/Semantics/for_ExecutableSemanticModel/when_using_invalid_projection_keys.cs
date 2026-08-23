// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_invalid_projection_keys : a_valid_semantic_model
{
    Exception _wrongCardinality;
    Exception _wrongType;

    void Because()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Projections.Length > 0);
        var projection = slice.Projections.Single();
        var transition = projection.Transitions.Single();
        _wrongCardinality = Validate(transition with
        {
            AffectedInstance = transition.AffectedInstance with { Cardinality = AffectedInstanceCardinality.Many }
        });
        _wrongType = Validate(transition with
        {
            AffectedInstance = transition.AffectedInstance with
            {
                Key = SemanticExpression.Property(SemanticExpressionRootKind.Event, _eventNamePropertyId)
            }
        });

        Exception Validate(SemanticProjectionTransition replacement) => Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with { Projections = [projection with { Transitions = [replacement] }] })));
    }

    [Fact] void should_reject_incompatible_cardinality() => _wrongCardinality.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_incompatible_key_type() => _wrongType.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
