// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionLoop.when_materializing_reaction_facts;

public class and_a_property_and_the_destination_are_invalid : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    SemanticRejected _failure;
    SemanticReactionLoop _loop;
    SemanticWorld _world;

    void Establish() => Compile(
        """
        module M
          feature F
            slice Automation Work
              event Started
              event Continued
                count Int
              reaction Continue
                when Started
                  produces Continued
                    count = 1
        """);

    void Because()
    {
        var reaction = _plan.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        trigger = trigger with
        {
            Produces = [trigger.Produces.Single() with
            {
                Mappings = [new(_plan.Events.Values.Single(value => value.Name == "Continued").Properties.Single().Id, new SemanticValueExpression(SemanticValue.Text("invalid")))],
                Destination = new SemanticValueExpression(SemanticValue.Text("invalid")),
                DestinationType = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid)
            }]
        };
        _world = SemanticWorld.Empty;
        _loop = new(new SemanticEvaluator(), _plan, _world);
        _failure = (SemanticRejected)_loop.Fire(reaction, trigger, new Dictionary<SemanticId, SemanticValue>(), SemanticExpressionRootKind.Event, null, null)!;
    }

    [Fact] void should_validate_the_property_before_the_destination() => _failure.Details.ShouldEqual("A event property 'count' is incompatible with its target type.");
    [Fact] void should_reject_the_contract() => _failure.Category.ShouldEqual(SemanticRejectionCategory.Contract);
    [Fact] void should_retain_the_failure_world() => _failure.World.ShouldBeSame(_world);
    [Fact] void should_not_append_a_fact() => _loop.Facts.ShouldBeEmpty();
    [Fact] void should_retain_the_input_world() => _loop.World.ShouldBeSame(_world);
}
