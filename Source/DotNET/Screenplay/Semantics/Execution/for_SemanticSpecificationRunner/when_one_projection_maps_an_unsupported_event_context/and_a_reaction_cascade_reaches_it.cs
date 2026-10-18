// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_one_projection_maps_an_unsupported_event_context;

public class and_a_reaction_cascade_reaches_it : given.a_model
{
    void Because() => _result = Run("Cascade");

    [Fact] void should_not_pass() => _result.Passed.ShouldBeFalse();
    [Fact] void should_propagate_the_projection_refusal() => ((SemanticUnsupported)_result.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
}
