// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_an_event_target_projection_is_unadmitted;

public class and_a_fact_reaches_the_projection : given.a_model
{
    void Because() => _result = Run("Observed");

    [Fact] void should_not_pass() => _result.Passed.ShouldBeFalse();
    [Fact] void should_refuse_publication() => ((SemanticUnsupported)_result.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
}
