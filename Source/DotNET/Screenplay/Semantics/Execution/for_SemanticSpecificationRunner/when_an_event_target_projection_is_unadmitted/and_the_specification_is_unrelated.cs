// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_an_event_target_projection_is_unadmitted;

public class and_the_specification_is_unrelated : given.a_model
{
    void Because() => _result = Run("Unrelated");

    [Fact] void should_pass() => _result.Passed.ShouldBeTrue();
    [Fact] void should_execute() => _result.Execution.ShouldBeOfExactType<SemanticAccepted>();
}
