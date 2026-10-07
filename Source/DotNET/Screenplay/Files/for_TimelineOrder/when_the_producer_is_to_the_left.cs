// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_TimelineOrder;

public class when_the_producer_is_to_the_left : Specification
{
    CompilationResult<Syntax.ApplicationSyntax> _result;
    void Because() => _result = new ScreenplayCompiler().Compile("module M\n  feature F\n    slice StateChange Write\n      event E\n    slice StateView View\n      projection P\n        from E");
    [Fact] void should_not_report_a_forward_flow() => _result.Diagnostics.ShouldBeEmpty();
}
