// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_duplicate_projection_events;

public class without_duplicates : given.a_compiler
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection("projection Links\n  from LinkCreated, LinkRevised\n  remove with LinkDeleted\n  join link on linkId\n    with LinkDescribed");

    [Fact] void should_not_report_any_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
}
