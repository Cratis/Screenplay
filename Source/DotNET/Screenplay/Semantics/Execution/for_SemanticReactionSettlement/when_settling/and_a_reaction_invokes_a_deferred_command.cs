// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_a_reaction_invokes_a_deferred_command : given.a_settlement
{
    void Establish()
    {
        Prepare(
            """
            reaction Continue
              runs as system role "Automation"
              when Started
                produces Continued
                invokes Complete
                  item = "one"
            """);

        // Command rules currently have no admission failures; index the command as deferred to pin the safety contract.
        _plan = new(
            _plan.Model,
            _plan.Commands,
            _plan.Events,
            _plan.Projections,
            _plan.ReadModels,
            _plan.Queries,
            _plan.Specifications,
            _plan.Constraints,
            _plan.Reactions,
            _plan.Captures,
            [_plan.Commands.Values.Single().Id]);
    }

    void Because() => _result = _settlement.Settle(_plan, _world, [_input], null);

    [Fact] void should_refuse_the_deferred_command() => _result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_report_the_command_capability() => ((SemanticUnsupported)_result).Capability.ShouldEqual(SemanticExecutionCapability.Command);
    [Fact] void should_identify_the_deferred_command() => ((SemanticUnsupported)_result).Details.ShouldContain("Complete");
    [Fact] void should_return_the_input_world() => _result.World.ShouldBeSame(_world);
    [Fact] void should_not_commit_cascading_facts() => _result.World.Facts.AsEnumerable().ShouldEqual([_input]);
    [Fact] void should_have_invoked_the_evaluator_after_the_first_cascading_effect() => _evaluator.Received(1).Execute(_plan, Arg.Is<SemanticWorld>(world => world.Facts.Length == 2), Arg.Any<SemanticExecutionRequest>());
}
