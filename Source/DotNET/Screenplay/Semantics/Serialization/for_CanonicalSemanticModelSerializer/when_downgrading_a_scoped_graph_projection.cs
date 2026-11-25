// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_downgrading_a_scoped_graph_projection : for_SemanticModelBinder.given.a_graph_projection_source
{
    ExecutableSemanticModel _model;
    Exception _error;

    void Establish() => _model = Compile(Source);

    void Because() => _error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V38, SemanticVersion.V38, _model.Application));

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_v59_too() => Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V59, SemanticVersion.V59, _model.Application)).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_v58_too() => Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V58, SemanticVersion.V58, _model.Application)).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_canonical_serialization_too() => Catch.Exception(() => SemanticModelCanonicalJson.SerializeWithoutRevision(LanguageVersion.V38, SemanticVersion.V38, _model.Application)).ShouldBeOfExactType<InvalidSemanticContract>();
}
