// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_responses;

public class and_reactions_follow_the_command : given.a_v6_scenario
{
    const string Prefix = "concept Id : Uuid\nmodule Billing\n  feature F\n    slice StateChange S\n      command C\n        id String identifier\n        produces event Started\n          id String = id\n        returns id\n";

    void CompileScenario(string source)
    {
        var nextSlice = source.IndexOf("\n    slice ", source.IndexOf("\n    slice ", StringComparison.Ordinal) + 1, StringComparison.Ordinal);
        var specification = source.IndexOf("\n      specification ", StringComparison.Ordinal);
        Compile(source[..nextSlice] + source[specification..] + source[nextSlice..specification]);
    }

    [Fact]
    void should_keep_the_initiating_response_after_a_cascade()
    {
        CompileScenario(Prefix + "    slice Automation Follow\n      reaction R\n        when Started\n          produces Followed\n      event Followed\n      specification Accepted\n        when C\n          id = \"hello\"\n        then Started\n          id = \"hello\"\n        then Followed\n        then returns \"hello\"");
        var run = Run("Accepted");
        run.Passed.ShouldBeTrue();
        ((SemanticScalarExecutionResponse)((SemanticAccepted)run.Execution).Response!).Value.ShouldEqual(SemanticValue.Text("hello"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_execute_response_only_invocations_and_preserve_unmapped_optional_inputs(bool generated)
    {
        var command = generated
            ? "        id Id generated identifier\n        returns id\n"
            : "        note String?\n        returns note\n";
        CompileScenario(Prefix + "      command Invoked\n" + command + "    slice Automation Follow\n      reaction R\n        when Started\n          invokes Invoked\n      specification Accepted\n        when C\n          id = \"hello\"\n        then Started\n          id = \"hello\"\n        then returns \"hello\"");
        var run = Run("Accepted");
        run.Execution.World.Facts.Length.ShouldEqual(1);
        if (generated)
        {
            run.Passed.ShouldBeFalse();
            ((SemanticUnsupported)run.Execution).Capability.ShouldEqual(SemanticExecutionCapability.IdentityAllocation);
        }
        else
        {
            run.Passed.ShouldBeTrue();
            ((SemanticScalarExecutionResponse)((SemanticAccepted)run.Execution).Response!).Value.ShouldEqual(SemanticValue.Text("hello"));
        }
    }

    [Fact]
    void should_not_reach_generation_on_an_excluded_reaction_branch()
    {
        CompileScenario(Prefix + "      command Invoked\n        id Id generated identifier\n        returns id\n    slice Automation Follow\n      reaction R\n        when Started\n          id\n          invokes Invoked\n        where id == \"other\"\n      specification Accepted\n        when C\n          id = \"hello\"\n        then Started\n          id = \"hello\"\n        then returns \"hello\"");
        Run("Accepted").Passed.ShouldBeTrue();
    }

    [Fact]
    void should_retain_command_and_cascade_facts_after_a_later_failure()
    {
        CompileScenario(Prefix + "      command Invoked\n        id Id generated identifier\n        returns id\n    slice Automation Follow\n      reaction R\n        when Started\n          produces Followed\n          invokes Invoked\n      event Followed\n      specification Accepted\n        when C\n          id = \"hello\"\n        then Started\n          id = \"hello\"\n        then Followed\n        then returns \"hello\"");
        var run = Run("Accepted");
        ((SemanticUnsupported)run.Execution).Capability.ShouldEqual(SemanticExecutionCapability.IdentityAllocation);
        run.Execution.World.Facts.Length.ShouldEqual(2);
    }

    [Fact]
    void should_retain_accepted_facts_without_a_response_when_a_scenario_query_fails()
    {
        CompileScenario("policy SignedIn\n  require authenticated\n" + Prefix + "    slice StateView View\n      readmodel Details\n        id String\n      query ById => Details optional\n        by id String\n        authorize SignedIn\n      specification QueryFails\n        when C\n          id = \"hello\"\n        then Started\n          id = \"hello\"\n        then query ById\n          arguments\n            id = \"hello\"\n        then returns \"hello\"");
        var run = Run("QueryFails");
        ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
        run.Execution.World.Facts.Length.ShouldEqual(1);
        run.Passed.ShouldBeFalse();
    }
}
