// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_enforcing_a_unique_property_constraint;

public class and_another_event_source_holds_the_value : given.a_constrained_plan
{
    SemanticWorld _before;
    SemanticRejected _rejected;

    void Establish() => _before = World(ProjectRegistered(First, "ALPHA"));

    void Because() => _rejected = (SemanticRejected)RegisterProject(_before, Second, "ALPHA");

    [Fact] void should_reject_the_command() => _rejected.Kind.ShouldEqual(SemanticExecutionOutcomeKind.Rejected);
    [Fact] void should_report_a_constraint_rejection() => _rejected.Category.ShouldEqual(SemanticRejectionCategory.Constraint);
    [Fact] void should_report_the_constraint_name_as_the_code() => _rejected.Code.ShouldEqual("ProjectCodeIsUnique");
    [Fact] void should_report_the_default_message() => _rejected.Details.ShouldEqual(ViolatedValueMessage);
    [Fact] void should_not_echo_the_colliding_value() => _rejected.Details.ShouldNotContain("ALPHA");
    [Fact] void should_keep_the_original_world() => ReferenceEquals(_rejected.World, _before).ShouldBeTrue();
}
