// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_duplicate_projection_events;

public class with_a_join_and_from : given.a_compiler
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection("projection Links\n  from LinkRevised\n  join link on linkId\n    with LinkRevised");

    [Fact] void should_keep_separate_event_and_join_sets() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
}
