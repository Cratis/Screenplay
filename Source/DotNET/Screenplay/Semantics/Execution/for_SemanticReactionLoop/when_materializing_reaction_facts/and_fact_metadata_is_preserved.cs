// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticReactionLoop.when_materializing_reaction_facts;

public class and_fact_metadata_is_preserved : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    SemanticFact _fact;
    readonly DateTimeOffset _occurred = new(2026, 10, 2, 9, 0, 0, TimeSpan.Zero);

    void Establish() => Compile(
        """
        module M
          feature F
            slice Automation Work
              event Started
              event Continued
                tag contract
              reaction Continue
                when Started
                  produces Continued
                    tag production
        """);

    void Because()
    {
        var reaction = _plan.Reactions.Single();
        var cause = new SemanticFact(_plan.Events.Values.Single(value => value.Name == "Started").Id, SemanticValue.Text("source"), []);
        var loop = new SemanticReactionLoop(new SemanticEvaluator(), _plan, SemanticWorld.Create([cause], []));
        loop.Fire(reaction, reaction.Triggers.Single(), new Dictionary<SemanticId, SemanticValue>(), SemanticExpressionRootKind.Event, cause, _occurred).ShouldBeNull();
        _fact = loop.Facts.Single();
    }

    [Fact] void should_keep_contract_tags_before_production_tags() => _fact.Tags.AsEnumerable().ShouldEqual(["contract", "production"]);
    [Fact] void should_keep_the_occurrence_time() => _fact.Occurred.ShouldEqual(_occurred);
    [Fact] void should_keep_the_reaction_origin() => _fact.ReactionOrigin.ShouldEqual(_plan.Reactions.Single().Id);
}
