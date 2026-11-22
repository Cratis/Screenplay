// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionLoop.when_materializing_reaction_facts;

public class and_an_ordinary_expression_supplies_a_property : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    [Fact]
    void should_materialize_the_payload_in_authored_mapping_order()
    {
        Compile(
            """
            module M
              feature F
                slice Automation Work
                  event Started
                    first String
                    second String
                  event Continued
                    tag contract
                    first String
                    second String
                  reaction Continue
                    when Started
                      first
                      second
                      produces Continued
                        tag production
                        second = second
                        first = first
            """);
        var reaction = _plan.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        var production = trigger.Produces.Single();
        var started = _plan.Events.Values.Single(value => value.Name == "Started");
        var continued = _plan.Events.Values.Single(value => value.Name == "Continued");
        var context = new SemanticEventContext(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("source")));
        var cause = new SemanticFact(
            started.Id,
            context.EventSource.Value,
            [.. started.Properties.Select(property => new SemanticPropertyValue(property.Id, SemanticValue.Text(property.Name)))]) { Context = context };
        var occurred = new DateTimeOffset(2026, 10, 2, 9, 0, 0, TimeSpan.Zero);
        var loop = new SemanticReactionLoop(new SemanticEvaluator(), _plan, SemanticWorld.Create([cause], []));
        loop.Fire(reaction, trigger, cause.Values.ToDictionary(value => value.TargetProperty, value => value.Value), SemanticExpressionRootKind.Event, cause, occurred).ShouldBeNull();
        var fact = loop.Facts.Single();
        fact.Values.Select(value => value.TargetProperty).ShouldEqual(production.Mappings.Select(mapping => mapping.TargetProperty));
        fact.Values.Select(value => value.Value).ShouldEqual([SemanticValue.Text("second"), SemanticValue.Text("first")]);
        fact.EventContract.ShouldEqual(continued.Id);
    }
}
