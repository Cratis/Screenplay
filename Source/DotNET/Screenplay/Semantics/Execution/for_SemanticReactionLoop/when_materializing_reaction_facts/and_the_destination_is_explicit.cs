// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionLoop.when_materializing_reaction_facts;

public class and_the_destination_is_explicit : for_SemanticReactionSettlement.given.a_settlement
{
    SemanticFact _fact;

    void Establish() => Prepare("reaction Continue\n  when Started\n    produces Continued");

    void Because()
    {
        var reaction = _plan.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        trigger = trigger with
        {
            Produces = [trigger.Produces.Single() with
            {
                Destination = new SemanticValueExpression(SemanticValue.Text("target")),
                DestinationType = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text)
            }]
        };
        var loop = new SemanticReactionLoop(_evaluator, _plan, _world);
        loop.Fire(reaction, trigger, new Dictionary<SemanticId, SemanticValue>(), SemanticExpressionRootKind.Event, _input, _occurred).ShouldBeNull();
        _fact = loop.Facts.Single();
    }

    [Fact] void should_use_the_explicit_destination() => _fact.Destination.ShouldEqual(SemanticValue.Text("target"));
    [Fact] void should_create_the_explicit_event_source_context() => _fact.Context.ShouldEqual(new SemanticEventContext(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("target"))));
    [Fact] void should_not_reuse_the_triggering_context() => ReferenceEquals(_fact.Context, _input.Context).ShouldBeFalse();
}
