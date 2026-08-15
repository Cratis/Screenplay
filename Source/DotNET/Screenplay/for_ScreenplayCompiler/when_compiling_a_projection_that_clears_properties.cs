// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_projection_that_clears_properties : given.a_compiler
{
    const string Source =
        """
        projection Notes => NoteReadModel
          from NoteCleared
            clear note
            clear owner.note
        """;

    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_a_mapping_for_each_line() => From.Mappings.Count().ShouldEqual(2);
    [Fact] void should_parse_the_property_to_clear() => Cleared.First().Property.ShouldEqual("note");
    [Fact] void should_parse_the_dotted_property_path_to_clear() => Cleared.Last().Property.ShouldEqual("owner.note");
    [Fact] void should_parse_both_lines_as_clear_mappings() => Cleared.Count().ShouldEqual(2);
    [Fact] void should_not_parse_a_clear_as_an_assignment() => From.Mappings.OfType<SetMappingSyntax>().ShouldBeEmpty();

    FromSyntax From => _result.Value!.Blocks.OfType<FromSyntax>().Single();

    IEnumerable<ClearMappingSyntax> Cleared => From.Mappings.OfType<ClearMappingSyntax>();
}
