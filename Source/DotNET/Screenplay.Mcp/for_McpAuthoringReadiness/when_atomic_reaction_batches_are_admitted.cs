// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringReadiness;

public class when_atomic_reaction_batches_are_admitted : Specification
{
    [Fact]
    void should_not_label_the_explicit_application_mode_as_syntax_only()
    {
        var application = new ScreenplayCompiler().Parse("reaction batches atomic\nmodule M\n  feature F\n    slice Automation S\n      event Started\n      reaction FollowUp\n        when Started\n").Value!;
        var readiness = new McpAuthoringReadiness(application);
        readiness.ModelSyntaxOnly.ShouldBeFalse();
        readiness.ModelExecutionReadiness.ShouldBeNull();
        readiness.SyntaxOnly(application.Modules.Single().Features.Single().Slices.Single().Reactions.Single()).ShouldBeFalse();
    }
}
