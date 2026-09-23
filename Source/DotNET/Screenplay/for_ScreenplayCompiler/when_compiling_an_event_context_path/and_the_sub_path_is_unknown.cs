// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_an_event_context_path;

public class and_the_sub_path_is_unknown : given.a_projection_reading_the_event_context
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Projection("$eventContext.eventType.name"));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_unknown_path() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownEventContextPath);
    [Fact] void should_warn() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_name_what_the_member_has() => _result.Diagnostics.Single().Message.ShouldEqual("Unknown event context path '$eventContext.eventType.name' - 'eventType' (EventType) has members id, generation, tombstone");
}
