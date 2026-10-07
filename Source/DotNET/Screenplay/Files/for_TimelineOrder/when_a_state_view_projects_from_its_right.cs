// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files.for_TimelineOrder;

public class when_a_state_view_projects_from_its_right : Specification
{
    CompilationResult<Syntax.ApplicationSyntax> _result;
    void Because() => _result = new ScreenplayCompiler().Compile("module M\n  feature F\n    slice StateView View\n      projection P\n        from E\n        from E\n    slice StateChange Write\n      event E");
    [Fact] void should_report_once_at_the_first_reference() => _result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ShouldContainOnly("PLAY0516@5");
    [Fact] void should_report_information() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Information);
    [Fact] void should_still_succeed() => _result.Success.ShouldBeTrue();
}
