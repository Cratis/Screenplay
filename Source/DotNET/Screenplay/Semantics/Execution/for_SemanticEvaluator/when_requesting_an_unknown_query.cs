// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_requesting_an_unknown_query : a_valid_semantic_model
{
    SemanticWorld _before;
    SemanticUnsupported _result;

    void Because()
    {
        var plan = SemanticExecutionPlan.Compile(_model).Plan!;
        var specification = plan.Specifications.Values.Single(_ => _.ThenQueries.Length > 0);
        var request = SemanticExecutionRequest.Create(
            specification.When.Command,
            specification.When.Values,
            [new(Id(SemanticKind.Query, "MissingQuery"), specification.ThenQueries.Single().Key)]);
        _before = SemanticWorld.Empty;
        _result = (SemanticUnsupported)new SemanticEvaluator().Execute(plan, _before, request);
    }

    [Fact] void should_report_the_query_capability() => _result.Capability.ShouldEqual(SemanticExecutionCapability.Query);
    [Fact] void should_keep_the_original_world() => ReferenceEquals(_result.World, _before).ShouldBeTrue();
    [Fact] void should_commit_no_fact() => _result.World.Facts.ShouldBeEmpty();
    [Fact] void should_change_no_read_model() => _result.World.ReadModels.ShouldBeEmpty();
}
