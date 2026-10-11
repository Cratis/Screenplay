// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_a_command_reaches_a_deferred_projection : a_scoped_execution_model
{
    SemanticExecutionResult _result;

    void Establish() => DeferProjection();
    void Because() => _result = new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, _request);

    [Fact] void should_refuse_projection() => _result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_report_the_projection_capability() => ((SemanticUnsupported)_result).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
    [Fact] void should_identify_the_deferred_projection() => ((SemanticUnsupported)_result).Details.ShouldContain("ProjectSummaryProjection");
    [Fact] void should_not_commit_any_facts() => _result.World.Facts.ShouldBeEmpty();
    [Fact] void should_not_commit_any_read_model_state() => _result.World.ReadModels.ShouldBeEmpty();
}
