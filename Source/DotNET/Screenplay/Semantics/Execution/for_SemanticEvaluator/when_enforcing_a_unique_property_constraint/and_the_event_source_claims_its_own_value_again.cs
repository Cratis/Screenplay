// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_enforcing_a_unique_property_constraint;

// A value is claimed per event source, so the holder may state it again.
public class and_the_event_source_claims_its_own_value_again : given.a_constrained_plan
{
    SemanticExecutionResult _result;

    void Because() => _result = RegisterProject(World(ProjectRegistered(First, "ALPHA")), First, "ALPHA");

    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_append_the_fact() => ((SemanticAccepted)_result).Facts.Length.ShouldEqual(1);
}
