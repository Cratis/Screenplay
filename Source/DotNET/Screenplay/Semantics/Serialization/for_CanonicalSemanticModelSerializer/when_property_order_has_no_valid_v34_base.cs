// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_property_order_has_no_valid_v34_base : Specification
{
    Exception _error;

    void Because()
    {
        var application = canonical_serialization_golden_vectors.CreateSemanticModelV34_1().Application;
        var module = application.Modules[0];
        var feature = module.Features[0];
        var slices = feature.Slices.Select(slice => slice with
        {
            Specifications = [.. slice.Specifications.Select(specification => specification with { ThenInvocations = [] })]
        });
        var updated = feature with { Slices = [.. slices] };
        application = application with { Modules = [module with { Features = [updated] }] };
        _error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V34_1, SemanticVersion.V34_1, application));
    }

    [Fact] void should_refuse_an_invalid_old_projection() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_preserve_the_historical_witness_requirement() => _error.Message.ShouldContain("An ESM v34 model must contain a specification that states 'then invokes'.");
}
