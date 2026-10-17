// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_duplicate_projection_events;

public class with_a_repeated_event_in_one_list : given.a_compiler
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection("projection Links\n  from LinkRevised, LinkRevised, LinkRevised");

    [Fact] void should_report_second_and_later_references() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual([DiagnosticCodes.DuplicateEventInProjectionScope, DiagnosticCodes.DuplicateEventInProjectionScope]);
    [Fact] void should_locate_the_list() => _result.Diagnostics.All(diagnostic => diagnostic.Location.Line == 2 && diagnostic.Location.Column == 3).ShouldBeTrue();
}
