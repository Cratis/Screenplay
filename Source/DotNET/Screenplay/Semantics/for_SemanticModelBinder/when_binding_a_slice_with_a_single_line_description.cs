// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_slice_with_a_single_line_description : given.a_semantic_binder
{
    const string Description = "Registers a new project";
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              description "Registers a new project"
        """;

    CompilationResult<SemanticCompilation> _result;
    CompilationResult<SemanticCompilation> _repeat;

    void Because()
    {
        _result = Bind(Source);
        _repeat = Bind(Source);
    }

    SemanticId SliceId => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Id;

    [Fact] void should_bind_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_map_exactly_one_declaration_entry_for_the_slice() => _result.Value!.SourceMap.Entries.Count(entry => entry.SemanticId == SliceId && entry.Role == SemanticSourceMapRole.Declaration).ShouldEqual(1);
    [Fact] void should_map_exactly_one_description_entry_for_the_slice() => _result.Value!.SourceMap.Entries.Count(entry => entry.SemanticId == SliceId && entry.Role == SemanticSourceMapRole.Description).ShouldEqual(1);
    [Fact] void should_give_the_declaration_entry_a_zero_length_span() => _result.Value!.SourceMap.Entries.Single(entry => entry.SemanticId == SliceId && entry.Role == SemanticSourceMapRole.Declaration).Span.Length.ShouldEqual(0);
    [Fact] void should_give_the_description_entry_a_non_zero_span() => _result.Value!.SourceMap.Entries.Single(entry => entry.SemanticId == SliceId && entry.Role == SemanticSourceMapRole.Description).Span.Length.ShouldEqual(Description.Length);
    [Fact] void should_decode_the_description_span_to_the_exact_text() =>
        DescriptionText(_result).ShouldEqual(Description);
    [Fact] void should_be_deterministic_across_binds() => _result.Value!.SourceMap.Entries
        .Select(entry => (entry.SemanticId, entry.Span, entry.Role, entry.Origin))
        .SequenceEqual(_repeat.Value!.SourceMap.Entries.Select(entry => (entry.SemanticId, entry.Span, entry.Role, entry.Origin)))
        .ShouldBeTrue();

    string DescriptionText(CompilationResult<SemanticCompilation> result)
    {
        var entry = result.Value!.SourceMap.Entries.Single(candidate => candidate.SemanticId == SliceId && candidate.Role == SemanticSourceMapRole.Description);
        var document = result.Value!.Documents.Documents.Single(candidate => candidate.Id == entry.Span.Document);
        return document.Text.Substring(entry.Span.Start, entry.Span.Length);
    }
}
