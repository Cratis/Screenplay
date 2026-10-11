// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.Extensions;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionSettlement.when_settling;

public class and_an_invoked_command_is_unsupported : given.a_settlement
{
    SemanticUnsupported _invocationFailure;

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
        _evaluator.Configure().Execute(Arg.Any<SemanticExecutionPlan>(), Arg.Any<SemanticWorld>(), Arg.Any<SemanticExecutionRequest>())
            .Returns(call => _invocationFailure = new SemanticUnsupported(call.Arg<SemanticWorld>(), SemanticExecutionCapability.Command, "The invoked command requires a host implementation."));
    }

    void Because() => _result = _settlement.Settle(_plan, _world, [_input], null);

    [Fact] void should_preserve_the_result_type() => _result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_preserve_the_missing_capability() => ((SemanticUnsupported)_result).Capability.ShouldEqual(_invocationFailure.Capability);
    [Fact] void should_preserve_the_unsupported_details() => ((SemanticUnsupported)_result).Details.ShouldEqual(_invocationFailure.Details);
    [Fact] void should_return_the_input_world_not_the_partial_cascade() => _result.World.ShouldBeSame(_world);
    [Fact] void should_have_appended_a_fact_before_the_failure() => _invocationFailure.World.Facts.Select(fact => fact.EventContract).ShouldEqual([EventId("Started"), EventId("Continued")]);
    [Fact] void should_leave_the_input_world_unchanged() => _world.Facts.AsEnumerable().ShouldEqual([_input]);
}
