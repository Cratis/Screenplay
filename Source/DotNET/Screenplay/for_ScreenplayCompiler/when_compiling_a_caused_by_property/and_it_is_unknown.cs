// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_caused_by_property;

public class and_it_is_unknown : given.a_projection_reading_who_caused_the_event
{
    CompilationResult<ProjectionSyntax> _result;

    void Because() => _result = _compiler.CompileProjection(Projection("email"));

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_unknown_property() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownCausedByProperty);
    [Fact] void should_name_what_the_identity_has() => _result.Diagnostics.Single().Message.ShouldEqual("Unknown $causedBy property 'email' - expected subject, name, userName, onBehalfOf");
}
