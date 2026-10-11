// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_a_command_does_not_reach_a_deferred_query : a_scoped_execution_model
{
    SemanticExecutionResult _result;

    void Establish()
    {
        DeferQuery();
        _plan = SemanticExecutionPlan.Compile(_model, [_registration.Id]).Plan!;
    }

    void Because() => _result = new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, _request);

    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_commit_the_produced_fact() => _result.World.Facts.Single().EventContract.ShouldEqual(_eventId);
    [Fact] void should_update_the_admitted_projection() => _result.World.ReadModels.Single().ReadModel.ShouldEqual(_readModelId);
}
