// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reacting;

public class and_production_batches_are_atomic : given.a_reaction_effect
{
    void Establish()
    {
        Configure("reaction batches atomic\n" + Source + "\n      constraint Once\n        unique event Recorded\n        message \"Already recorded\"");
        Answer = SemanticReactionBodyResult.Produced([new(Target, Payload), new(Target, Payload)]);
    }
    void Because() => Fire();
    [Fact] void should_return_the_constraint_refusal() => Failure.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_settle_nothing() => Loop.Facts.ShouldBeEmpty();
    [Fact] void should_leave_no_pending_work() => Loop.World.PendingChildren.ShouldBeEmpty();
}
