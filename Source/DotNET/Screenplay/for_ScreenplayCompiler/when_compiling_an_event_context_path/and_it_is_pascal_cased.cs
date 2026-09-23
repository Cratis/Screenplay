// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_an_event_context_path;

public class and_it_is_pascal_cased : given.a_projection_reading_the_event_context
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Projection("$eventContext.CausedBy.UserName"));

    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_path_as_written() => ((EventContextExpressionSyntax)_result.Value!.Blocks.OfType<FromSyntax>().Single().Mappings.OfType<SetMappingSyntax>().Single().Source).Path.ShouldEqual("CausedBy.UserName");
}
