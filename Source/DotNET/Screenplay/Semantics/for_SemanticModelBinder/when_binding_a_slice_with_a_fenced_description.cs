// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_slice_with_a_fenced_description : given.a_semantic_binder
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              description
                ```
                Registers a new project.
                ```
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    SemanticId SliceId => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Id;

    [Fact] void should_bind_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_the_fenced_description_text() => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Name.ShouldEqual("RegisterProject");
    [Fact] void should_map_exactly_one_declaration_entry_for_the_slice() => _result.Value!.SourceMap.Entries.Count(entry => entry.SemanticId == SliceId && entry.Role == SemanticSourceMapRole.Declaration).ShouldEqual(1);
    [Fact] void should_map_no_description_entry_for_a_fenced_description() => _result.Value!.SourceMap.Entries.Count(entry => entry.SemanticId == SliceId && entry.Role == SemanticSourceMapRole.Description).ShouldEqual(0);
}
