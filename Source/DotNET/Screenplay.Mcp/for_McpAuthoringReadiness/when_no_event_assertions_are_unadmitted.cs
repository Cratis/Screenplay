// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringReadiness;

public class when_no_event_assertions_are_unadmitted : Specification
{
    McpAuthoringReadiness _readiness;

    void Establish() => _readiness = new(new ScreenplayCompiler().Parse("module Billing\n  feature Claims\n    slice StateChange Nothing\n      command DoNothing\n      specification NothingHappens\n        when DoNothing\n        then no events").Value!);

    [Fact] void should_mark_the_model_as_syntax_only() => _readiness.ModelSyntaxOnly.ShouldBeTrue();
    [Fact] void should_name_the_unadmitted_feature() => _readiness.ModelExecutionReadiness.ShouldContain("explicit no-event assertions (#433)");
}
