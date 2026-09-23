// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_constraint;

public class with_an_unknown_event : given.a_semantic_binder
{
    const string Source =
        """
        module Payments
          feature Ledger
            slice StateChange RecordPayment
              event PaymentRecorded
                paymentId Uuid
              constraint OnePaymentPerAttempt
                unique event PaymentReversed
              constraint UniqueReversal
                unique paymentId on PaymentReversed
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_both_constraints() => Unknown.Length.ShouldEqual(2);
    [Fact] void should_report_at_the_unique_event_constraint() => Unknown[0].Location.Line.ShouldEqual(7);
    [Fact] void should_report_at_the_unique_property_constraint() => Unknown[1].Location.Line.ShouldEqual(9);
    [Fact] void should_name_the_event() => Unknown[0].Message.ShouldEqual("Constraint 'OnePaymentPerAttempt' names event 'PaymentReversed', which the application does not declare.");

    Diagnostic[] Unknown => [.. _result.Diagnostics.Where(_ => _.Code == DiagnosticCodes.UnknownConstraintEvent)];
}
