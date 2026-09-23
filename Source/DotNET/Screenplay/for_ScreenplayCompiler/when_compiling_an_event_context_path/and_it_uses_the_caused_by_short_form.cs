// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_an_event_context_path;

/// <summary>
/// The short form stays admitted - Chronicle compiles and round-trips it - even though Chronicle cannot yet execute it
/// (Cratis/Chronicle#4119); the documentation points to <c>$eventContext.causedBy.&lt;property&gt;</c> instead.
/// </summary>
public class and_it_uses_the_caused_by_short_form : given.a_projection_reading_the_event_context
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Projection("$causedBy.subject"));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
}
