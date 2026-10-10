// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_models_belong_to_different_applications : given.two_models
{
    Exception? _error;

    void Establish()
    {
        _after = Create(Source, application: "Other");
    }

    void Because() => _error = Catch.Exception(() => ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after)));

    [Fact] void should_refuse_identity_matching() => _error.ShouldBeOfExactType<IncompatibleModelIdentities>();
}
