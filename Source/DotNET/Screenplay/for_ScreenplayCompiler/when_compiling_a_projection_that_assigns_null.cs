// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

/// <summary>
/// The spelling that predates <c>clear</c>. A released Chronicle already compiles it into a scalar clear, so
/// it has to keep parsing exactly as it did - an assignment of the null literal, not a clear mapping.
/// </summary>
public class when_compiling_a_projection_that_assigns_null : given.a_compiler
{
    const string Source =
        """
        projection Notes => NoteReadModel
          from NoteCleared
            note = null
        """;

    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_the_line_as_an_assignment() => Assignment.Property.ShouldEqual("note");
    [Fact] void should_parse_the_assigned_value_as_a_literal() => Assignment.Source.ShouldBeOfExactType<LiteralExpressionSyntax>();
    [Fact] void should_parse_the_assigned_value_as_the_null_literal() => ((LiteralExpressionSyntax)Assignment.Source).Value.ShouldBeNull();
    [Fact] void should_not_parse_the_line_as_a_clear_mapping() => From.Mappings.OfType<ClearMappingSyntax>().ShouldBeEmpty();

    FromSyntax From => _result.Value!.Blocks.OfType<FromSyntax>().Single();

    SetMappingSyntax Assignment => From.Mappings.OfType<SetMappingSyntax>().Single();
}
