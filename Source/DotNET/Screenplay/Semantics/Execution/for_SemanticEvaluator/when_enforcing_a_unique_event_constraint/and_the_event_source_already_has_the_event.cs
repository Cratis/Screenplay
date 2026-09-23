// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_enforcing_a_unique_event_constraint;

public class and_the_event_source_already_has_the_event : given.a_constrained_plan
{
    SemanticWorld _before;
    SemanticRejected _rejected;

    void Establish() => _before = World(PaymentRecorded(First));

    void Because() => _rejected = (SemanticRejected)RecordPayment(_before, First);

    [Fact] void should_report_a_constraint_rejection() => _rejected.Category.ShouldEqual(SemanticRejectionCategory.Constraint);
    [Fact] void should_report_the_constraint_name_as_the_code() => _rejected.Code.ShouldEqual("OnePaymentPerAttempt");
    [Fact] void should_report_the_default_message() => _rejected.Details.ShouldEqual(ViolatedEventMessage);
    [Fact] void should_keep_the_original_world() => ReferenceEquals(_rejected.World, _before).ShouldBeTrue();
}
