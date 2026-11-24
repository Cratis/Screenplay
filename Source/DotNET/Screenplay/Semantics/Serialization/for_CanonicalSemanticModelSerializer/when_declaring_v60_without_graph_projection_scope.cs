// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_declaring_v60_without_graph_projection_scope : for_SemanticModelBinder.given.a_graph_projection_source
{
    SemanticApplication _application;
    Exception _error;

    void Establish()
    {
        var model = Compile(Source);
        _application = model.Application with
        {
            GraphProjections = [model.Application.GraphProjections.Single() with { DeclaringSlice = null }]
        };
    }

    void Because() => _error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V60, SemanticVersion.V60, _application));

    [Fact] void should_refuse_the_empty_version_upgrade() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
}
