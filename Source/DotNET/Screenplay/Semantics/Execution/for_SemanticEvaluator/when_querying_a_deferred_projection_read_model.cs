// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_querying_a_deferred_projection_read_model : a_scoped_execution_model
{
    SemanticExecutionResult _result;

    void Establish() => DeferProjection();
    void Because() => _result = new SemanticEvaluator().Execute(
        _plan,
        SemanticWorld.Empty,
        SemanticExecutionRequest.ForQueries([new(_queryId, _fact.Destination)]));

    [Fact] void should_refuse_the_query() => _result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_report_the_query_capability() => ((SemanticUnsupported)_result).Capability.ShouldEqual(SemanticExecutionCapability.Query);
    [Fact] void should_leave_the_world_unchanged() => _result.World.ShouldEqual(SemanticWorld.Empty);
}
