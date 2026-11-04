// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_property_order_has_no_valid_v36_base : Specification
{
    Exception _error;

    void Because()
    {
        var application = canonical_serialization_golden_vectors.CreateSemanticModelV36_1().Application with { StateMachines = [] };
        _error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V36_1, SemanticVersion.V36_1, application));
    }

    [Fact] void should_refuse_an_invalid_old_projection() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_preserve_the_historical_witness_requirement() => _error.Message.ShouldContain("An ESM v36 model must declare a state machine or a workflow.");
}
