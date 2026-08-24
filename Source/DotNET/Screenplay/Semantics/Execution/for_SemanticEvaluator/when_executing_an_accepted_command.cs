// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_executing_an_accepted_command : a_valid_semantic_model
{
    SemanticAccepted _accepted;
    SemanticWorld _before;

    void Because()
    {
        var plan = SemanticExecutionPlan.Compile(_model).Plan!;
        var specification = plan.Specifications.Values.Single(_ => _.ThenQueries.Length > 0);
        var request = SemanticExecutionRequest.Create(
            specification.When.Command,
            specification.When.Values,
            [.. specification.ThenQueries.Select(_ => new SemanticQueryRequest(_.Query, _.Key))]);
        _before = SemanticWorld.Empty;
        _accepted = (SemanticAccepted)new SemanticEvaluator().Execute(plan, _before, request);
    }

    [Fact] void should_accept_the_command() => _accepted.Kind.ShouldEqual(SemanticExecutionOutcomeKind.Accepted);
    [Fact] void should_leave_the_original_world_unchanged() => (_before.Facts.Length + _before.ReadModels.Length).ShouldEqual(0);
    [Fact] void should_produce_the_expected_fact() => _accepted.Facts.Single().EventContract.ShouldEqual(_eventId);
    [Fact] void should_commit_the_fact_once() => _accepted.World.Facts.Length.ShouldEqual(1);
    [Fact] void should_project_the_read_model() => _accepted.World.ReadModels.Single().ReadModel.ShouldEqual(_readModelId);
    [Fact] void should_return_the_project_from_the_query() => _accepted.Queries.Single().Results.Single().ReadModel.ShouldEqual(_readModelId);
    [Fact] void should_return_the_requested_query_key() => SemanticValueRules.AreEqual(_accepted.Queries.Single().Key, _accepted.Queries.Single().Results.Single().Key).ShouldBeTrue();
}
