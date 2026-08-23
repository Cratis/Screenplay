// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_a_default_array : a_valid_semantic_model
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ExecutableSemanticModel.Create(
        LanguageVersion.V1,
        SemanticVersion.V1,
        _application with { Concepts = default }));

    [Fact] void should_reject_the_model() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
