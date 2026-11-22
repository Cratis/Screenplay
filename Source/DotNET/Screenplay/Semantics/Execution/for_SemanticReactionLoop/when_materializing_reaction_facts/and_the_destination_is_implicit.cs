// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionLoop.when_materializing_reaction_facts;

public class and_the_destination_is_implicit : for_SemanticReactionSettlement.given.a_settlement
{
    SemanticFact _fact;

    void Establish() => Prepare("reaction Continue\n  when Started\n    produces Continued");

    void Because()
    {
        var reaction = _plan.Reactions.Single();
        var loop = new SemanticReactionLoop(_evaluator, _plan, _world);
        loop.Fire(reaction, reaction.Triggers.Single(), new Dictionary<SemanticId, SemanticValue>(), SemanticExpressionRootKind.Event, _input, _occurred).ShouldBeNull();
        _fact = loop.Facts.Single();
    }

    [Fact] void should_keep_the_triggering_destination() => _fact.Destination.ShouldEqual(_input.Destination);
    [Fact] void should_keep_the_exact_triggering_context() => _fact.Context.ShouldBeSame(_input.Context);
}
