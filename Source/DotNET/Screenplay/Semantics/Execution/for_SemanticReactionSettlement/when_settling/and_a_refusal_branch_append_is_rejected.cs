// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_a_refusal_branch_append_is_rejected : given.a_settlement
{
    SemanticReactionLoop _loop;
    SemanticRejected _failure;

    void Establish()
    {
        Prepare(
            """
            constraint OneContinuation
              unique event Continued
              message "Continuation already recorded"
            reaction Continue
              when Started
                produces Continued
                invokes Complete
                  item = "one"
                  on refused by authorization
                    produces Continued
                  on refused
                    acknowledge
                invokes Complete
                  item = "two"
            """);
        _loop = new(_evaluator, _plan, _world);
    }

    void Because()
    {
        _loop.Observe([_input], _occurred);
        _failure = (SemanticRejected)_loop.Settle()!;
    }

    [Fact] void should_end_with_the_branch_append_rejection() => _failure.Details.ShouldEqual("Continuation already recorded");
    [Fact] void should_not_handle_the_append_rejection_with_another_branch() => _failure.Category.ShouldEqual(SemanticRejectionCategory.Constraint);
    [Fact] void should_preserve_facts_accepted_before_the_branch_failure() => _loop.World.Facts.Select(fact => fact.EventContract).ShouldEqual([EventId("Started"), EventId("Continued")]);
    [Fact] void should_skip_remaining_invocations() => _evaluator.Received(1).Execute(Arg.Any<SemanticExecutionPlan>(), Arg.Any<SemanticWorld>(), Arg.Any<SemanticExecutionRequest>());
}
