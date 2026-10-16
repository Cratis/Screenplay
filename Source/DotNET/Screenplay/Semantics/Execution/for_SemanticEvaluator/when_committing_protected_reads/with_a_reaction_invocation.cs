// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_committing_protected_reads;

public class with_a_reaction_invocation : given.a_protected_command
{
    void Establish()
    {
        Compile(Source + "\n" + """
                slice Automation Follow
                  event Started
                    id String
                    lookup String
                  reaction FollowUp
                    when Started
                      id
                      lookup
                      invokes Update
                        id = id
                        lookup = lookup
            """);
        _world = World(Fact("Recorded", "read", "original"));
    }

    void Because()
    {
        var started = _plan.Events.Values.Single(value => value.Name == "Started");
        var input = new SemanticFact(
            started.Id,
            SemanticValue.Text("write"),
            [new(started.Properties.Single(property => property.Name == "id").Id, SemanticValue.Text("write")),
             new(started.Properties.Single(property => property.Name == "lookup").Id, SemanticValue.Text("read"))])
        {
            Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("write")))
        };
        var loop = new SemanticReactionLoop(_evaluator, _plan, _world);
        loop.Append(input, null).ShouldBeNull();
        loop.Settle().ShouldBeNull();
        _result = new SemanticAccepted(loop.World, loop.Facts, []);
    }

    [Fact] void should_invoke_the_protected_command() => _result.World.Facts[^1].EventContract.ShouldEqual(_plan.Events.Values.Single(value => value.Name == "Saved").Id);
    [Fact] void should_use_its_decided_read_value() => _result.World.Facts[^1].Values.Single().Value.ShouldEqual(SemanticValue.Text("original"));
    [Fact] void should_retain_reaction_causation() => _result.World.Facts[^1].ReactionOrigin.ShouldEqual(_plan.Reactions.Single().Id);
}
