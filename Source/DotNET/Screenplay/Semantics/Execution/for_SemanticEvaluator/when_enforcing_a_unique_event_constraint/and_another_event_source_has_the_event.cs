// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_enforcing_a_unique_event_constraint;

// The event may occur once per event source, not once in the whole event sequence.
public class and_another_event_source_has_the_event : given.a_constrained_plan
{
    SemanticExecutionResult _result;

    void Because() => _result = RecordPayment(World(PaymentRecorded(First)), Second);

    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
}
