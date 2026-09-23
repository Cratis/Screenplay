// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_constraint;

public class with_a_unique_event : given.a_semantic_binder
{
    const string Source =
        """
        module Payments
          feature Ledger
            slice StateChange RecordPayment
              command RecordPayment
                paymentId Uuid identifier
                produces PaymentRecorded
                  for paymentId
                  paymentId = paymentId
              event PaymentRecorded
                paymentId Uuid
              constraint OnePaymentPerAttempt
                unique event PaymentRecorded
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_carry_the_name_as_identity() => Constraint.Name.ShouldEqual("OnePaymentPerAttempt");
    [Fact] void should_constrain_event_occurrences() => Constraint.Kind.ShouldEqual(SemanticConstraintKind.UniqueEventOccurrence);
    [Fact] void should_be_enforced_across_the_event_sequence() => Constraint.Scope.ShouldEqual(SemanticConstraintScope.EventSequence);
    [Fact] void should_target_the_event() => Constraint.Targets.Single().EventContract.ShouldEqual(Slice.Events.Single().Id);
    [Fact] void should_name_no_properties() => Constraint.Targets.Single().Properties.ShouldBeEmpty();

    SemanticSlice Slice => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single();
    SemanticConstraint Constraint => Slice.Constraints.Single();
}
