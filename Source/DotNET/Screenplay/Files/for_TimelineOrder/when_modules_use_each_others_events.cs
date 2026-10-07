// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files.for_TimelineOrder;

public class when_modules_use_each_others_events : Specification
{
    CompilationResult<Syntax.ApplicationSyntax> _result;
    void Because() => _result = new ScreenplayCompiler().Compile("module A\n  feature F\n    slice StateView ViewA\n      event EA\n      projection PA\n        from EB\nmodule B\n  feature F\n    slice StateView ViewB\n      event EB\n      projection PB\n        from EA");
    [Fact] void should_report_one_cycle_group() => _result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}@{diagnostic.Location.Line}").ShouldContainOnly("PLAY0517@6");
    [Fact] void should_list_members_in_timeline_order() => _result.Diagnostics.Single().Message.ShouldContain("'A', 'B'");
}
