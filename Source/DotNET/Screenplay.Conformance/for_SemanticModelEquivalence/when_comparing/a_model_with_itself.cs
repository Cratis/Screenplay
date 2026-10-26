// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class a_model_with_itself : given.compiled_models
{
    void Because() => _comparison = SemanticModelEquivalence.Compare(Compile(Source), Compile(Source));

    [Fact] void should_be_equivalent() => _comparison.IsEquivalent.ShouldBeTrue();
}
