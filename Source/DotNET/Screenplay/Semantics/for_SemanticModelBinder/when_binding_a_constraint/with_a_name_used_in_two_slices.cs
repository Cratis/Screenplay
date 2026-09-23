// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_constraint;

// The name keys the constraint's index in the event store, so two slices cannot both claim it.
public class with_a_name_used_in_two_slices : given.a_semantic_binder
{
    const string Source =
        """
        module Payments
          feature Ledger
            slice StateChange RecordPayment
              event PaymentRecorded
                paymentId Uuid
              constraint OneEntryPerPayment
                unique event PaymentRecorded
            slice StateChange ReversePayment
              event PaymentReversed
                paymentId Uuid
              constraint OneEntryPerPayment
                unique event PaymentReversed
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_second_declaration() => Duplicate.Location.Line.ShouldEqual(12);
    [Fact] void should_say_the_name_is_the_identity() => Duplicate.Message.ShouldContain("identity in the event store");

    Diagnostic Duplicate => _result.Diagnostics.Single(_ => _.Code == DiagnosticCodes.DuplicateConstraintName);
}
