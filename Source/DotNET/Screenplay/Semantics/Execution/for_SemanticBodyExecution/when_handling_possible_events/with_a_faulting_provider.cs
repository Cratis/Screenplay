// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_possible_events;

public class with_a_faulting_provider : given.a_possible_events_handler
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_keep_faults_outside_the_violation_channel(bool cancellation)
    {
        Provider.Failure = cancellation ? new OperationCanceledException() : new InvalidOperationException("provider failed");
        var exception = Assert.Throws<SemanticCommandBodyFaultException>(Execute);
        exception.Kind.ShouldEqual(cancellation ? SemanticCommandBodyFaultKind.Cancelled : SemanticCommandBodyFaultKind.Faulted);
    }
}
