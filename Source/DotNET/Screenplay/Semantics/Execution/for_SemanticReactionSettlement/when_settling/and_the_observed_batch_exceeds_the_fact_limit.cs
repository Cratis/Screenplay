// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_the_observed_batch_exceeds_the_fact_limit : given.a_settlement
{
    SemanticUnsupported _unsupported;

    void Establish()
    {
        Prepare(
            """
            reaction Finish
              runs as system role "Automation"
              when Started
                invokes Complete
                  item = "one"
            """);
        _world = SemanticWorld.Create([.. Enumerable.Repeat(_input, SemanticReactionLoop.MaximumFacts + 1)], []);
    }

    void Because() => _unsupported = (SemanticUnsupported)_settlement.Settle(_plan, _world, _world.Facts, null);

    [Fact] void should_keep_the_input_world() => ReferenceEquals(_unsupported.World, _world).ShouldBeTrue();
    [Fact] void should_report_a_cascade_that_does_not_settle() => _unsupported.Capability.ShouldEqual(SemanticExecutionCapability.Reaction);
    [Fact] void should_not_run_invocations_after_observation_fails() => _evaluator.DidNotReceive().Execute(Arg.Any<SemanticExecutionPlan>(), Arg.Any<SemanticWorld>(), Arg.Any<SemanticExecutionRequest>());
}
