// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_caused_by_property;

public class and_it_continues_below_a_value : given.a_projection_reading_who_caused_the_event
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Projection("onBehalfOf.subject.first"));

    [Fact] void should_report_the_unknown_property() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownCausedByProperty);
    [Fact] void should_say_the_value_has_no_members() => _result.Diagnostics.Single().Message.ShouldEqual("Unknown $causedBy property 'onBehalfOf.subject.first' - 'subject' has no members");
}
