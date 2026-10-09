// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.when_validating_public_events;

public class a_pre_v9_model_using_public_events : given.a_model_to_corrupt
{
    Exception _error;

    void Because()
    {
        var model = OutboundModel;
        _error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V7, SemanticVersion.V7, model.Application));
    }

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_name_the_version_that_admits_them() => _error.Message.ShouldContain("require ESM v9");
}
