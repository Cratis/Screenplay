// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_duplicate_projection_events;

public class with_the_same_key : given.a_compiler
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection("projection Links\n  from LinkRevised key linkId\n  from LinkRevised key linkId");

    [Fact] void should_report_the_duplicate() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DuplicateEventInProjectionScope);
    [Fact] void should_report_the_second_reference() => _result.Diagnostics.Single().Location.Line.ShouldEqual(3);
}
