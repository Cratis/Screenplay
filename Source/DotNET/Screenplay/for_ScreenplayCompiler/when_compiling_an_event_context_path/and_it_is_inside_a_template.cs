// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_an_event_context_path;

public class and_it_is_inside_a_template : given.a_projection_reading_the_event_context
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Projection("`week ${$eventContext.occurred.Week} of ${$eventContext.causationId}`"));

    [Fact] void should_report_only_the_unknown_member() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownEventContextMember);
}
