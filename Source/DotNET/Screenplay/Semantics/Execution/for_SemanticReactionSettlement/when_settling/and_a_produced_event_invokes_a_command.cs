// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_a_produced_event_invokes_a_command : given.a_settlement
{
    SemanticAccepted _accepted;
    SemanticExecutionRequest _invocation;
    SemanticWorld _invocationWorld;

    void Establish()
    {
        Prepare(
            """
            reaction Continue
              when Started
                produces Continued
            reaction Finish
              runs as system role "Automation"
              when Continued
                invokes Complete
                  item = "one"
            """);
        _evaluator.When(evaluator => evaluator.Execute(Arg.Any<SemanticExecutionPlan>(), Arg.Any<SemanticWorld>(), Arg.Any<SemanticExecutionRequest>()))
            .Do(call =>
            {
                _invocation = call.Arg<SemanticExecutionRequest>();
                _invocationWorld = call.Arg<SemanticWorld>();
            });
    }

    void Because() => _accepted = (SemanticAccepted)_settlement.Settle(_plan, _world, [_input], _occurred);

    [Fact] void should_return_cascading_facts_in_append_order() => _accepted.Facts.Select(fact => fact.EventContract).ShouldEqual([EventId("Continued"), EventId("Completed")]);
    [Fact] void should_include_the_whole_cascade_in_the_world() => _accepted.World.Facts.AsEnumerable().ShouldEqual([_input, .. _accepted.Facts]);
    [Fact] void should_invoke_the_command_through_the_supplied_evaluator() => _evaluator.Received(1).Execute(_plan, Arg.Any<SemanticWorld>(), Arg.Any<SemanticExecutionRequest>());
    [Fact] void should_supply_the_world_after_the_direct_production() => _invocationWorld.Facts.Select(fact => fact.EventContract).ShouldEqual([EventId("Started"), EventId("Continued")]);
    [Fact] void should_supply_the_reactions_system_identity() => _invocation.RunsAs.ShouldEqual(_plan.Reactions.Single(reaction => reaction.Name == "Finish").RunsAs);
    [Fact] void should_supply_exactly_the_declared_roles() => _invocation.RunsAs!.Roles.AsEnumerable().ShouldEqual(["Automation"]);
    [Fact] void should_not_supply_a_caller_audit_identity() => _invocation.Caller.ShouldBeNull();
    [Fact] void should_record_the_invoking_reaction() => _invocation.ReactionOrigin.ShouldEqual(_plan.Reactions.Single(reaction => reaction.Name == "Finish").Id);
    [Fact] void should_propagate_the_occurrence_to_the_invoked_command() => _invocation.Occurrence!.Occurred.ShouldEqual(_occurred);
}
