// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_several_refusal_branches_match : given.a_settlement
{
    void Establish()
    {
        Prepare(
            """
            reaction Continue
              when Started
                invokes Complete
                  item = "one"
                  on refused by validation
                    produces Continued
                  on refused
                    produces Completed
                invokes Complete
                  item = "two"
            """);
        _evaluator.Execute(Arg.Any<SemanticExecutionPlan>(), Arg.Any<SemanticWorld>(), Arg.Any<SemanticExecutionRequest>())
            .Returns(call => new SemanticRejected(call.Arg<SemanticWorld>(), SemanticRejectionCategory.Validation, null, "refused") { ValidationFailures = [new("refused", SemanticValidationSeverity.Error)] });
    }

    void Because() => _result = _settlement.Settle(_plan, _world, [_input], null);

    [Fact] void should_accept_the_first_matching_branch() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_append_only_the_first_branch_effect() => ((SemanticAccepted)_result).Facts.Select(fact => fact.EventContract).ShouldEqual([EventId("Continued")]);
    [Fact] void should_default_to_the_triggering_event_source() => ((SemanticAccepted)_result).Facts.Single().Destination.ShouldEqual(_input.Destination);
    [Fact] void should_record_reaction_causation() => ((SemanticAccepted)_result).Facts.Single().ReactionOrigin.ShouldEqual(_plan.Reactions.Single().Id);
    [Fact] void should_stop_remaining_invocations() => _evaluator.Received(1).Execute(Arg.Any<SemanticExecutionPlan>(), Arg.Any<SemanticWorld>(), Arg.Any<SemanticExecutionRequest>());
}
