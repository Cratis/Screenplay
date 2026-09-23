// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_enforcing_a_unique_property_constraint;

// A null constrained value is skipped: it neither claims nor collides.
public class and_the_value_is_null : given.a_constrained_plan
{
    SemanticExecutionResult _result;

    void Because() => _result = RegisterProject(World(ProjectRegistered(First, null), ProjectRegistered(Third, "ALPHA")), Second, null);

    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
}
