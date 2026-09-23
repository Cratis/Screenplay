// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_enforcing_a_unique_property_constraint;

// Values are compared with their casing unless the constraint says otherwise.
public class and_the_value_differs_only_in_casing : given.a_constrained_plan
{
    SemanticExecutionResult _caseSensitive;
    SemanticExecutionResult _ignoringCasing;

    void Because()
    {
        var world = World(ProjectRegistered(First, "ALPHA"));
        _caseSensitive = RegisterProject(world, Second, "alpha");
        Change("ProjectCodeIsUnique", constraint => constraint with { IgnoreCasing = true });
        _ignoringCasing = RegisterProject(world, Second, "alpha");
    }

    [Fact] void should_accept_it_by_default() => _caseSensitive.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_reject_it_when_ignoring_casing() => ((SemanticRejected)_ignoringCasing).Code.ShouldEqual("ProjectCodeIsUnique");
}
