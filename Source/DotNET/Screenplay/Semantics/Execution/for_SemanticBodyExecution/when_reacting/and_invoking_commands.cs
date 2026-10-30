// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_reacting;

public class and_invoking_commands : given.a_reaction_effect
{
    void Establish()
    {
        var command = Plan.Commands.Values.Single(command => command.Name == "Record");
        Answer = SemanticReactionBodyResult.Invoked([
            new(command.Id, SemanticValue.Composite([new(command.Properties.Single().Id, SemanticValue.Text("First"))])),
            new(command.Id, SemanticValue.Composite([new(command.Properties.Single().Id, SemanticValue.Text("Second"))]))]);
    }

    void Because() => Fire();

    [Fact] void should_execute_the_batch() => Failure.ShouldBeNull();
    [Fact] void should_record_each_invocation() => Loop.Invocations.Length.ShouldEqual(2);
    [Fact] void should_keep_invocation_order() => Loop.Facts.Select(fact => fact.Values.Single().Value).ShouldEqual([SemanticValue.Text("First"), SemanticValue.Text("Second")]);
    [Fact] void should_pass_the_occurrence() => Loop.Facts.All(fact => fact.Occurred == Occurred).ShouldBeTrue();
    [Fact] void should_pass_the_origin() => Loop.Facts.All(fact => fact.ReactionOrigin == Reaction.Id).ShouldBeTrue();
}
