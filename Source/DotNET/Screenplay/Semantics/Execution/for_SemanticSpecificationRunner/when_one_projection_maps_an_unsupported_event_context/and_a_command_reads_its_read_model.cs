// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_one_projection_maps_an_unsupported_event_context;

public class and_a_command_reads_its_read_model : given.a_model
{
    void Because() => _result = Run("DecisionRead");

    [Fact] void should_not_pass() => _result.Passed.ShouldBeFalse();
    [Fact] void should_refuse_the_projection() => ((SemanticUnsupported)_result.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
}
