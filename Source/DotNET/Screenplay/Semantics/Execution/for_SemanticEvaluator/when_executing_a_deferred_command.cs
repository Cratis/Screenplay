// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

// Current validated command rules never emit plan issues. Construct the indexed plan with a
// deferred command to pin refusal if future command capabilities become deferred admission issues.
public class when_executing_a_deferred_command : a_scoped_execution_model
{
    SemanticExecutionResult _result;

    void Establish()
    {
        var admitted = SemanticExecutionPlan.Compile(_model).Plan!;
        _plan = new(
            admitted.Model,
            admitted.Commands,
            admitted.Events,
            admitted.Projections,
            admitted.ReadModels,
            admitted.Queries,
            admitted.Specifications,
            admitted.Constraints,
            admitted.Reactions,
            admitted.Captures,
            [_commandId]);
    }

    void Because() => _result = new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, _request);

    [Fact] void should_refuse_the_command() => _result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_report_the_command_capability() => ((SemanticUnsupported)_result).Capability.ShouldEqual(SemanticExecutionCapability.Command);
    [Fact] void should_leave_the_world_unchanged() => _result.World.ShouldEqual(SemanticWorld.Empty);
}
