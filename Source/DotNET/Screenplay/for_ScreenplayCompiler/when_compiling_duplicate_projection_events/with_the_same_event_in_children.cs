// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_duplicate_projection_events;

public class with_the_same_event_in_children : given.a_compiler
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection("projection Links\n  from LinkRevised\n  children first identified by id\n    from LinkRevised\n  children second identified by id\n    from LinkRevised");

    [Fact] void should_start_fresh_sets_per_children_block() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
}
