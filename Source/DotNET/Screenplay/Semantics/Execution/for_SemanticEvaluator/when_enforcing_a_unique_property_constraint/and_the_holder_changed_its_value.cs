// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_enforcing_a_unique_property_constraint;

// An event source holds one claim per constraint, so changing its value frees the one it held before.
public class and_the_holder_changed_its_value : given.a_constrained_plan
{
    SemanticExecutionResult _result;

    void Because() => _result = RegisterProject(World(ProjectRegistered(First, "ALPHA"), ProjectRegistered(First, "BETA")), Second, "ALPHA");

    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
}
