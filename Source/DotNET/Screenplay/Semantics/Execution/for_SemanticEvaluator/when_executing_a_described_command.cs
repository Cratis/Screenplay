// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_executing_a_described_command : a_v6_scenario
{
    const string Source = "policy Member\n  require authenticated\nmodule M\n  feature F\n    slice StateChange S\n      event Chosen\n        value String\n        at DateTime\n      command Choose\n        value String\n        authorize Member\n        validate\n          value not empty\n        handler\n          implementation\n            hint \"Choose payload values\"\n        produces Chosen\n          at = $context.occurred\n      specification Denied\n        given caller\n          role \"Guest\"\n        when Choose\n          value = \"valid\"\n        then denied\n      specification Invalid\n        given caller\n          authenticated\n        when Choose\n          value = \"\"\n        then error\n      specification Reached\n        given caller\n          authenticated\n        when Choose\n          value = \"valid\"\n        then Chosen\n          value = \"chosen\"\n          at = \"2026-01-01T00:00:00Z\"\n";

    void Establish() => Compile(Source);

    [Fact]
    void should_keep_denial_before_the_body()
    {
        var run = Run("Denied");
        run.Passed.ShouldBeTrue();
        ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    }

    [Fact]
    void should_keep_validation_before_the_body()
    {
        var run = Run("Invalid");
        run.Passed.ShouldBeTrue();
        ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Validation);
    }

    [Fact]
    void should_stop_at_the_body_without_evaluating_occurrence_mappings()
    {
        var run = Run("Reached");
        run.Passed.ShouldBeFalse();
        var result = (SemanticUnsupported)run.Execution;
        result.Capability.ShouldEqual(SemanticExecutionCapability.ImplementationBody);
        result.Details.ShouldContain("Choose");
        result.Details.ShouldContain(_plan.Commands.Values.Single().Handler!.RequirementId);
        result.World.Facts.ShouldBeEmpty();
    }

    [Fact]
    void should_report_executed_and_unsupported_separately()
    {
        var runs = _plan.Specifications.Values.Select(specification => new SemanticSpecificationRunner().Run(_plan, specification.Id)).ToArray();
        runs.Count(run => run.Execution is not SemanticUnsupported).ShouldEqual(2);
        runs.Count(run => run.Execution is SemanticUnsupported).ShouldEqual(1);
        runs.Where(run => run.Execution is SemanticUnsupported).All(run => run.Passed).ShouldBeFalse();
    }

    [Fact]
    void should_propagate_unsupported_through_a_reaction_not_its_refusal_branch()
    {
        Compile(Source + "    slice Automation A\n      event Started\n      event Refused\n      reaction FollowUp\n        runs as system\n        when Started\n          invokes Choose\n            value = \"valid\"\n            on refused\n              produces Refused\n      specification Reacted\n        when append Started\n        then no events\n");
        var run = Run("Reacted");
        run.Passed.ShouldBeFalse();
        ((SemanticUnsupported)run.Execution).Capability.ShouldEqual(SemanticExecutionCapability.ImplementationBody);
        ((SemanticUnsupported)run.Execution).Details.ShouldContain(_plan.Commands.Values.Single().Handler!.RequirementId);
    }

    [Fact]
    void should_treat_historical_events_as_ordinary_facts()
    {
        Compile(Source + "      command NoOp\n      specification History\n        given Chosen\n          value = \"old\"\n          at = \"2026-01-01T00:00:00Z\"\n        when NoOp\n        then no events\n");
        Run("History").Passed.ShouldBeTrue();
    }
}
