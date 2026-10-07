// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_TimelineOrder;

public class when_a_reaction_is_triggered_from_its_right : Specification
{
    CompilationResult<Syntax.ApplicationSyntax> _result;
    void Because() => _result = new ScreenplayCompiler().Compile("module M\n  feature F\n    slice Automation React\n      reaction R\n        when E\n    slice StateChange Write\n      event E");
    [Fact] void should_report_the_trigger_reference() => _result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ShouldContainOnly("PLAY0516@5");
}
