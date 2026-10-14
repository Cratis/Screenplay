// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringReadiness;

public class when_refusal_handling_and_redelivery_are_unadmitted : Specification
{
    McpAuthoringReadiness _readiness;
    SliceSyntax _slice;

    void Establish()
    {
        var application = new ScreenplayCompiler().Parse("module Billing\n  feature Payments\n    slice Automation Claiming\n      event Approved\n      command Claim\n      reaction Claimer\n        when Approved\n          invokes Claim\n            on refused\n              acknowledge\n      specification Redelivering\n        given Approved\n        when redelivered Approved to Claimer\n").Value!;
        _slice = application.Modules.Single().Features.Single().Slices.Single();
        _readiness = new(application);
    }

    [Fact] void should_leave_the_model_admitted() => _readiness.ModelSyntaxOnly.ShouldBeFalse();
    [Fact] void should_not_report_an_unadmitted_feature() => _readiness.ModelExecutionReadiness.ShouldNotContain("#433");
    [Fact] void should_leave_the_reaction_admitted() => _readiness.SyntaxOnly(_slice.Reactions.Single()).ShouldBeFalse();
    [Fact] void should_leave_the_redelivery_specification_admitted() => _readiness.SyntaxOnly(_slice.Specifications.Single()).ShouldBeFalse();
    [Fact] void should_leave_the_unaffected_command_executable() => _readiness.SyntaxOnly(_slice.Commands.Single()).ShouldBeFalse();
}
