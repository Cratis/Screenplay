// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_branching_reactions_reach_the_limit : given.a_v6_scenario
{
    [Fact]
    void should_stop_a_branching_cycle_and_preserve_only_the_bounded_accepted_history()
    {
        Compile("""
            module Billing
              feature Automation
                slice Automation Echo
                  reaction Branch
                    when Pulse
                      produces Left
                      produces Right
                    when Left
                      produces Pulse
                    when Right
                      produces Pulse
                  event Pulse
                  event Left
                  event Right
                  specification BranchingForever
                    when append Pulse
                    then Left
            """);
        var run = Run("BranchingForever");
        var unsupported = (SemanticUnsupported)run.Execution;
        unsupported.Capability.ShouldEqual(SemanticExecutionCapability.Reaction);
        unsupported.World.Facts.Length.ShouldEqual(SemanticReactionLoop.MaximumFacts);
        unsupported.Details.ShouldContain("do not settle");
        run.Passed.ShouldBeFalse();
    }
}
