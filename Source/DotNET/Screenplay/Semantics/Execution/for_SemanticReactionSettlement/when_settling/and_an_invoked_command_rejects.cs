// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_an_invoked_command_rejects : given.a_settlement
{
    SemanticRejected _rejected;
    SemanticWorld _invocationWorld;

    void Establish()
    {
        Prepare(
            """
            reaction Continue
              when Started
                produces Continued
                invokes Complete
                  item = "one"
            """);
        _evaluator.When(evaluator => evaluator.Execute(Arg.Any<SemanticExecutionPlan>(), Arg.Any<SemanticWorld>(), Arg.Any<SemanticExecutionRequest>()))
            .Do(call => _invocationWorld = call.Arg<SemanticWorld>());
    }

    void Because() => _rejected = (SemanticRejected)_settlement.Settle(_plan, _world, [_input], null);

    [Fact] void should_return_the_input_world_not_the_partial_cascade() => ReferenceEquals(_rejected.World, _world).ShouldBeTrue();
    [Fact] void should_have_appended_a_fact_before_the_rejection() => _invocationWorld.Facts.Select(fact => fact.EventContract).ShouldEqual([EventId("Started"), EventId("Continued")]);
    [Fact] void should_preserve_the_rejection_category() => _rejected.Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_preserve_the_rejection_details() => _rejected.Details.ShouldEqual("Caller is not authorized.");
    [Fact] void should_leave_the_input_world_unchanged() => _world.Facts.AsEnumerable().ShouldEqual([_input]);
}
