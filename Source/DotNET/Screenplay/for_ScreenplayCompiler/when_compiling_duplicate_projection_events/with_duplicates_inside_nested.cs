// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_duplicate_projection_events;

public class with_duplicates_inside_nested : given.a_compiler
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection("projection Links\n  nested details\n    from LinkRevised\n    clear with LinkRevised");

    [Fact] void should_report_the_duplicate() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DuplicateEventInProjectionScope);
    [Fact] void should_name_the_nested_property() => _result.Diagnostics.Single().Message.ShouldContain("at nested 'details'");
    [Fact] void should_locate_the_clear() => _result.Diagnostics.Single().Location.Line.ShouldEqual(4);
}
