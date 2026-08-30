// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_slice_without_a_description : given.a_semantic_binder
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    SemanticId SliceId => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Id;

    [Fact] void should_bind_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_map_exactly_one_declaration_entry_for_the_slice() => _result.Value!.SourceMap.Entries.Count(entry => entry.SemanticId == SliceId && entry.Role == SemanticSourceMapRole.Declaration).ShouldEqual(1);
    [Fact] void should_map_no_description_entry_for_the_slice() => _result.Value!.SourceMap.Entries.Count(entry => entry.SemanticId == SliceId && entry.Role == SemanticSourceMapRole.Description).ShouldEqual(0);
}
