// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_a_graph_projection_names_a_non_slice : for_SemanticModelBinder.given.a_graph_projection_source
{
    SemanticApplication _application;
    Exception _error;

    void Establish()
    {
        var model = Compile(Source);
        _application = model.Application with
        {
            GraphProjections = [model.Application.GraphProjections.Single() with { DeclaringSlice = model.Application.Modules.Single().Id }]
        };
    }

    void Because() => _error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V58, SemanticVersion.V58, _application));

    [Fact] void should_refuse_a_non_slice_reference() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
}
