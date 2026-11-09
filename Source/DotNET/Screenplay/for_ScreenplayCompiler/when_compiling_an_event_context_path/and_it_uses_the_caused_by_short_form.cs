// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_an_event_context_path;

/// <summary>
/// The short form is admitted: Chronicle compiles, round-trips and resolves it at runtime (Cratis/Chronicle#4119), and it reads
/// the same value as <c>$eventContext.causedBy.&lt;property&gt;</c>.
/// </summary>
public class and_it_uses_the_caused_by_short_form : given.a_projection_reading_the_event_context
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Projection("$causedBy.subject"));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
}
