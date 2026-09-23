// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_enforcing_a_unique_event_constraint;

// A releasing event closes the cycle, so the event source may have the event again.
public class and_a_releasing_event_followed_it : given.a_constrained_plan
{
    SemanticExecutionResult _result;

    void Because()
    {
        Change("OnePaymentPerAttempt", constraint => constraint with { ReleasedBy = [Event("PaymentReversed").Id] });
        _result = RecordPayment(World(PaymentRecorded(First), PaymentReversed(First)), First);
    }

    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
}
